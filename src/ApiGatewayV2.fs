namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Apigatewayv2
open Amazon.CDK.AwsApigatewayv2Integrations
open Amazon.CDK.AwsApigatewayv2Authorizers
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.Logs

/// <summary>
/// The default authorizer applied to every route of an HTTP API unless the route overrides it.
/// </summary>
type HttpApiAuthorizer =
    /// JWT authorizer for any OIDC issuer (Cognito, Auth0, Entra ID...).
    /// API Gateway accepts a token when its `aud` (or, for Cognito access tokens, `client_id`) matches an audience.
    | JwtAuthorizer of name: string * issuer: string * audiences: string list
    /// Cognito user pool authorizer; the issuer is derived from the pool and the audiences are the app clients.
    | UserPoolAuthorizer of name: string * userPool: IUserPool * clients: IUserPoolClient list
    /// Any pre-built authorizer (for example an HttpLambdaAuthorizer).
    | CustomAuthorizer of IHttpRouteAuthorizer

/// <summary>What a route invokes.</summary>
type HttpRouteTarget =
    | LambdaTarget of Amazon.CDK.AWS.Lambda.IFunction
    | IntegrationTarget of HttpRouteIntegration

/// <summary>How a route is authorized.</summary>
type HttpRouteAuthorization =
    /// Uses the API's default authorizer (none if the API has no default authorizer).
    | DefaultAuthorization
    /// Explicitly public route, even when the API has a default authorizer.
    | NoAuthorization
    /// Route-specific authorizer.
    | RouteAuthorizer of IHttpRouteAuthorizer

type HttpRouteSpec =
    { Path: string
      Method: HttpMethod
      Target: HttpRouteTarget
      Authorization: HttpRouteAuthorization
      AuthorizationScopes: string list }

/// <summary>Custom domain for the API's default stage.</summary>
type HttpApiDomain =
    /// Creates a regional API Gateway domain name. The certificate must be in the API's region.
    | NewDomain of domainName: string * certificate: ICertificate * mappingKey: string option
    /// Maps an existing API Gateway domain name.
    | ExistingDomain of domain: IDomainName * mappingKey: string option

/// <summary>Default-stage throttling: steady-state requests per second and burst capacity.</summary>
type HttpApiThrottle = { RateLimit: float; BurstLimit: int }

/// <summary>
/// High-level API Gateway V2 HTTP API builder following AWS best practices.
///
/// **Default Security Settings:**
/// - CORS = disabled (opt-in for specific origins)
/// - Auto-deploy = true
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - CORS disabled by default prevents unauthorized cross-origin access
/// - HTTP API provides low-latency, cost-effective API Gateway
/// - Auto-deploy simplifies deployment workflow
///
/// **Escape Hatch:**
/// Bind the API with `let!` to get the underlying CDK HttpApi
/// for advanced scenarios not covered by this builder.
/// </summary>
type HttpApiConfig =
    { ApiName: string
      ConstructId: string option
      Description: string option
      CorsPreflightOptions: CorsPreflightOptions option
      CreateDefaultStage: bool voption
      DisableExecuteApiEndpoint: bool option
      Authorizer: HttpApiAuthorizer option
      DefaultAuthorizationScopes: string list
      Routes: HttpRouteSpec list
      CustomDomain: HttpApiDomain option
      Throttle: HttpApiThrottle option
      AccessLogGroup: ILogGroup option }

type HttpApiSpec =
    { ApiName: string
      ConstructId: string
      Props: HttpApiProps
      Authorizer: HttpApiAuthorizer option
      Routes: HttpRouteSpec list
      CustomDomain: HttpApiDomain option
      Throttle: HttpApiThrottle option
      AccessLogGroup: ILogGroup option
      mutable HttpApi: HttpApi option
      mutable DomainName: IDomainName option }

    /// Gets the underlying CDK HttpApi. Must be called after the stack is built.
    member this.Api =
        match this.HttpApi with
        | Some api -> api
        | None ->
            failwith
                $"HttpApi '{this.ApiName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

module HttpApiOperations =

    /// JSON access log format. Leaves out JWT claims and request bodies so tokens and personal data stay out of logs.
    let defaultAccessLogFormat =
        """{"requestId":"$context.requestId","ip":"$context.identity.sourceIp","requestTime":"$context.requestTime","httpMethod":"$context.httpMethod","routeKey":"$context.routeKey","status":"$context.status","protocol":"$context.protocol","responseLength":"$context.responseLength","integrationLatency":"$context.integrationLatency","integrationError":"$context.integrationErrorMessage","authorizerError":"$context.authorizer.error"}"""

    let private createAuthorizer (authorizer: HttpApiAuthorizer) : IHttpRouteAuthorizer =
        match authorizer with
        | JwtAuthorizer(name, issuer, audiences) ->
            HttpJwtAuthorizer(name, issuer, HttpJwtAuthorizerProps(JwtAudience = Array.ofList audiences))
        | UserPoolAuthorizer(name, pool, clients) ->
            let props = HttpUserPoolAuthorizerProps()

            if not clients.IsEmpty then
                props.UserPoolClients <- Array.ofList clients

            HttpUserPoolAuthorizer(name, pool, props)
        | CustomAuthorizer a -> a

    /// Creates the HttpApi, its domain mapping, routes, throttling and access logging in the stack.
    let create (stack: Stack) (spec: HttpApiSpec) : unit =
        let props = spec.Props

        spec.Authorizer
        |> Option.iter (fun a -> props.DefaultAuthorizer <- createAuthorizer a)

        let domain =
            spec.CustomDomain
            |> Option.map (fun d ->
                let domain, key =
                    match d with
                    | NewDomain(domainName, certificate, key) ->
                        DomainName(
                            stack,
                            $"{spec.ConstructId}Domain",
                            DomainNameProps(DomainName = domainName, Certificate = certificate)
                        )
                        :> IDomainName,
                        key
                    | ExistingDomain(domain, key) -> domain, key

                let mapping = DomainMappingOptions(DomainName = domain)
                key |> Option.iter (fun k -> mapping.MappingKey <- k)
                props.DefaultDomainMapping <- mapping
                domain)

        let api = HttpApi(stack, spec.ConstructId, props)

        // One integration per function: CDK adds a Lambda permission per route on a shared integration.
        let integrations =
            System.Collections.Generic.Dictionary<string, HttpRouteIntegration>()

        let integrationFor (target: HttpRouteTarget) =
            match target with
            | IntegrationTarget i -> i
            | LambdaTarget fn ->
                match integrations.TryGetValue fn.Node.Addr with
                | true, i -> i
                | false, _ ->
                    let i =
                        HttpLambdaIntegration($"{fn.Node.Id}Integration", fn) :> HttpRouteIntegration

                    integrations.[fn.Node.Addr] <- i
                    i

        for route in spec.Routes do
            let options =
                AddRoutesOptions(
                    Path = route.Path,
                    Methods = [| route.Method |],
                    Integration = integrationFor route.Target
                )

            match route.Authorization with
            | DefaultAuthorization -> ()
            | NoAuthorization -> options.Authorizer <- HttpNoneAuthorizer()
            | RouteAuthorizer a -> options.Authorizer <- a

            if not route.AuthorizationScopes.IsEmpty then
                options.AuthorizationScopes <- Array.ofList route.AuthorizationScopes

            api.AddRoutes(options) |> ignore

        if spec.Throttle.IsSome || spec.AccessLogGroup.IsSome then
            let stage =
                match api.DefaultStage with
                | null ->
                    failwith
                        $"HttpApi '{spec.ApiName}': throttle and accessLogs apply to the default stage; remove 'createDefaultStage false'."
                | s -> s.Node.DefaultChild :?> CfnStage

            spec.Throttle
            |> Option.iter (fun t ->
                stage.DefaultRouteSettings <-
                    CfnStage.RouteSettingsProperty(
                        ThrottlingRateLimit = t.RateLimit,
                        ThrottlingBurstLimit = float t.BurstLimit
                    ))

            spec.AccessLogGroup
            |> Option.iter (fun lg ->
                stage.AccessLogSettings <-
                    CfnStage.AccessLogSettingsProperty(
                        DestinationArn = lg.LogGroupArn,
                        Format = defaultAccessLogFormat
                    ))

        spec.HttpApi <- Some api
        spec.DomainName <- domain

type HttpApiBuilder(name: string) =
    member _.Yield(_: unit) : HttpApiConfig =
        { ApiName = name
          ConstructId = None
          Description = None
          CorsPreflightOptions = None
          CreateDefaultStage = ValueSome true
          DisableExecuteApiEndpoint = None
          Authorizer = None
          DefaultAuthorizationScopes = []
          Routes = []
          CustomDomain = None
          Throttle = None
          AccessLogGroup = None }

    member x.Zero() : HttpApiConfig = x.Yield()

    member _.Combine(state1: HttpApiConfig, state2: HttpApiConfig) : HttpApiConfig =
        { ApiName = state2.ApiName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          CorsPreflightOptions = state2.CorsPreflightOptions |> Option.orElse state1.CorsPreflightOptions
          CreateDefaultStage = state2.CreateDefaultStage |> ValueOption.orElse state1.CreateDefaultStage
          DisableExecuteApiEndpoint =
            state2.DisableExecuteApiEndpoint
            |> Option.orElse state1.DisableExecuteApiEndpoint
          Authorizer = state2.Authorizer |> Option.orElse state1.Authorizer
          DefaultAuthorizationScopes = state1.DefaultAuthorizationScopes @ state2.DefaultAuthorizationScopes
          Routes = state1.Routes @ state2.Routes
          CustomDomain = state2.CustomDomain |> Option.orElse state1.CustomDomain
          Throttle = state2.Throttle |> Option.orElse state1.Throttle
          AccessLogGroup = state2.AccessLogGroup |> Option.orElse state1.AccessLogGroup }

    member inline _.Delay([<InlineIfLambda>] f: unit -> HttpApiConfig) : HttpApiConfig = f ()

    member inline x.For(config: HttpApiConfig, [<InlineIfLambda>] f: unit -> HttpApiConfig) : HttpApiConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: HttpApiConfig) : HttpApiSpec =
        let apiName = config.ApiName
        let constructId = config.ConstructId |> Option.defaultValue apiName

        let props = HttpApiProps()
        props.ApiName <- apiName

        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.CorsPreflightOptions |> Option.iter (fun v -> props.CorsPreflight <- v)

        config.CreateDefaultStage
        |> ValueOption.iter (fun v -> props.CreateDefaultStage <- System.Nullable<bool>(v))

        config.DisableExecuteApiEndpoint
        |> Option.iter (fun v -> props.DisableExecuteApiEndpoint <- System.Nullable<bool>(v))

        if not config.DefaultAuthorizationScopes.IsEmpty then
            props.DefaultAuthorizationScopes <- Array.ofList config.DefaultAuthorizationScopes

        config.Throttle
        |> Option.iter (fun t ->
            if t.RateLimit <= 0.0 || t.BurstLimit < 0 then
                invalidArg "throttle" $"HttpApi '{apiName}': throttle rate must be positive and burst non-negative.")

        { ApiName = apiName
          ConstructId = constructId
          Props = props
          Authorizer = config.Authorizer
          Routes = config.Routes
          CustomDomain = config.CustomDomain
          Throttle = config.Throttle
          AccessLogGroup = config.AccessLogGroup
          HttpApi = None
          DomainName = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: HttpApiConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("description")>]
    member _.Description(config: HttpApiConfig, description: string) =
        { config with
            Description = Some description }

    [<CustomOperation("cors")>]
    member _.Cors(config: HttpApiConfig, corsOptions: CorsPreflightOptions) =
        { config with
            CorsPreflightOptions = Some corsOptions }

    [<CustomOperation("createDefaultStage")>]
    member _.CreateDefaultStage(config: HttpApiConfig, create: bool) =
        { config with
            CreateDefaultStage = ValueSome create }

    /// <summary>Disables the default execute-api endpoint so clients must use the custom domain.</summary>
    /// <param name="disabled">True to disable the execute-api endpoint.</param>
    /// <code lang="fsharp">
    /// httpApi "Api" {
    ///     customDomain "api.example.com" certificate
    ///     disableExecuteApiEndpoint true
    /// }
    /// </code>
    [<CustomOperation("disableExecuteApiEndpoint")>]
    member _.DisableExecuteApiEndpoint(config: HttpApiConfig, disabled: bool) =
        { config with
            DisableExecuteApiEndpoint = Some disabled }

    /// <summary>Sets a JWT authorizer as the default for all routes.</summary>
    /// <param name="name">Authorizer name.</param>
    /// <param name="issuer">Token issuer URL, e.g. https://cognito-idp.eu-west-2.amazonaws.com/eu-west-2_abc.</param>
    /// <param name="audiences">Accepted audiences (`aud`, or `client_id` for Cognito access tokens).</param>
    /// <code lang="fsharp">
    /// httpApi "Api" {
    ///     jwtAuthorizer "Jwt" "https://auth.example.com/" [ "my-api" ]
    /// }
    /// </code>
    [<CustomOperation("jwtAuthorizer")>]
    member _.JwtAuthorizer(config: HttpApiConfig, name: string, issuer: string, audiences: string list) =
        if audiences.IsEmpty then
            invalidArg "audiences" "A JWT authorizer needs at least one audience."

        { config with
            Authorizer = Some(JwtAuthorizer(name, issuer, audiences)) }

    /// <summary>Sets a Cognito user pool JWT authorizer as the default for all routes.</summary>
    /// <param name="name">Authorizer name.</param>
    /// <param name="pool">The user pool that issues the tokens.</param>
    /// <param name="clients">App clients whose tokens are accepted.</param>
    /// <code lang="fsharp">
    /// let! pool = userPool "Users" { () }
    /// let! client = userPoolClient "Web" { userPool pool }
    /// httpApi "Api" {
    ///     cognitoAuthorizer "Cognito" pool [ client ]
    /// }
    /// </code>
    [<CustomOperation("cognitoAuthorizer")>]
    member _.CognitoAuthorizer(config: HttpApiConfig, name: string, pool: IUserPool, clients: IUserPoolClient list) =
        // Without clients CDK would create a new app client with default (implicit-flow) settings
        if clients.IsEmpty then
            invalidArg "clients" "A Cognito authorizer needs the app clients whose tokens it accepts."

        { config with
            Authorizer = Some(UserPoolAuthorizer(name, pool, clients)) }

    /// <summary>Sets a pre-built authorizer (e.g. HttpLambdaAuthorizer) as the default for all routes.</summary>
    /// <param name="authorizer">The authorizer.</param>
    [<CustomOperation("defaultAuthorizer")>]
    member _.DefaultAuthorizer(config: HttpApiConfig, authorizer: IHttpRouteAuthorizer) =
        { config with
            Authorizer = Some(CustomAuthorizer authorizer) }

    /// <summary>Sets OAuth scopes required by default on every route that uses the default authorizer.</summary>
    /// <param name="scopes">Scopes, e.g. [ "api/read" ].</param>
    [<CustomOperation("defaultAuthorizationScopes")>]
    member _.DefaultAuthorizationScopes(config: HttpApiConfig, scopes: string list) =
        { config with
            DefaultAuthorizationScopes = config.DefaultAuthorizationScopes @ scopes }

    /// <summary>Adds a route to a Lambda function, authorized by the API's default authorizer.</summary>
    /// <param name="method">HTTP method (HttpMethod.ANY for all).</param>
    /// <param name="path">Route path, e.g. "/items/{id}" or "/{proxy+}".</param>
    /// <param name="handler">The Lambda function.</param>
    /// <code lang="fsharp">
    /// let! fn = lambda "Items" { ... }
    /// httpApi "Api" {
    ///     route HttpMethod.GET "/items" fn
    /// }
    /// </code>
    [<CustomOperation("route")>]
    member _.Route(config: HttpApiConfig, method: HttpMethod, path: string, handler: Amazon.CDK.AWS.Lambda.IFunction) =
        { config with
            Routes =
                config.Routes
                @ [ { Path = path
                      Method = method
                      Target = LambdaTarget handler
                      Authorization = DefaultAuthorization
                      AuthorizationScopes = [] } ] }

    /// <summary>Adds a route to any HTTP API integration, authorized by the API's default authorizer.</summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Route path.</param>
    /// <param name="integration">The integration (e.g. HttpUrlIntegration).</param>
    [<CustomOperation("route")>]
    member _.Route(config: HttpApiConfig, method: HttpMethod, path: string, integration: HttpRouteIntegration) =
        { config with
            Routes =
                config.Routes
                @ [ { Path = path
                      Method = method
                      Target = IntegrationTarget integration
                      Authorization = DefaultAuthorization
                      AuthorizationScopes = [] } ] }

    /// <summary>Adds an unauthenticated route to a Lambda function, even when a default authorizer is set.</summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Route path.</param>
    /// <param name="handler">The Lambda function.</param>
    /// <code lang="fsharp">
    /// httpApi "Api" {
    ///     cognitoAuthorizer "Cognito" pool [ client ]
    ///     publicRoute HttpMethod.GET "/health" fn
    /// }
    /// </code>
    [<CustomOperation("publicRoute")>]
    member _.PublicRoute
        (
            config: HttpApiConfig,
            method: HttpMethod,
            path: string,
            handler: Amazon.CDK.AWS.Lambda.IFunction
        ) =
        { config with
            Routes =
                config.Routes
                @ [ { Path = path
                      Method = method
                      Target = LambdaTarget handler
                      Authorization = NoAuthorization
                      AuthorizationScopes = [] } ] }

    /// <summary>Adds a fully specified route (custom authorizer or scopes per route).</summary>
    /// <param name="spec">The route.</param>
    /// <code lang="fsharp">
    /// httpApi "Api" {
    ///     routeSpec (HttpRoute.lambda HttpMethod.POST "/admin" fn |> HttpRoute.withScopes [ "api/admin" ])
    /// }
    /// </code>
    [<CustomOperation("routeSpec")>]
    member _.RouteSpec(config: HttpApiConfig, spec: HttpRouteSpec) =
        { config with
            Routes = config.Routes @ [ spec ] }

    /// <summary>Creates a regional custom domain and maps it to the default stage.</summary>
    /// <param name="domainName">The domain, e.g. "api.example.com".</param>
    /// <param name="certificate">ACM certificate in the same region as the API.</param>
    /// <code lang="fsharp">
    /// let! cert = certificate "ApiCert" { domainName "api.example.com" }
    /// httpApi "Api" {
    ///     customDomain "api.example.com" cert
    /// }
    /// </code>
    [<CustomOperation("customDomain")>]
    member _.CustomDomain(config: HttpApiConfig, domainName: string, certificate: ICertificate) =
        { config with
            CustomDomain = Some(NewDomain(domainName, certificate, None)) }

    /// <summary>Creates a regional custom domain and maps the default stage under a base path.</summary>
    /// <param name="domainName">The domain.</param>
    /// <param name="certificate">ACM certificate in the same region as the API.</param>
    /// <param name="mappingKey">Base path, e.g. "v1".</param>
    [<CustomOperation("customDomain")>]
    member _.CustomDomain(config: HttpApiConfig, domainName: string, certificate: ICertificate, mappingKey: string) =
        { config with
            CustomDomain = Some(NewDomain(domainName, certificate, Some mappingKey)) }

    /// <summary>Maps an existing API Gateway domain name to the default stage.</summary>
    /// <param name="domain">The domain name construct.</param>
    [<CustomOperation("domainMapping")>]
    member _.DomainMapping(config: HttpApiConfig, domain: IDomainName) =
        { config with
            CustomDomain = Some(ExistingDomain(domain, None)) }

    /// <summary>Sets default-stage throttling for all routes.</summary>
    /// <param name="rateLimit">Steady-state requests per second.</param>
    /// <param name="burstLimit">Burst capacity (concurrent requests).</param>
    /// <code lang="fsharp">
    /// httpApi "Api" {
    ///     throttle 100.0 200
    /// }
    /// </code>
    [<CustomOperation("throttle")>]
    member _.Throttle(config: HttpApiConfig, rateLimit: float, burstLimit: int) =
        { config with
            Throttle =
                Some
                    { RateLimit = rateLimit
                      BurstLimit = burstLimit } }

    /// <summary>Writes default-stage access logs as JSON to a log group (see HttpApiOperations.defaultAccessLogFormat).</summary>
    /// <param name="logGroup">Destination log group.</param>
    /// <code lang="fsharp">
    /// let! logs = logGroup "ApiAccessLogs" { retention RetentionDays.ONE_YEAR }
    /// httpApi "Api" {
    ///     accessLogs logs
    /// }
    /// </code>
    [<CustomOperation("accessLogs")>]
    member _.AccessLogs(config: HttpApiConfig, logGroup: ILogGroup) =
        { config with
            AccessLogGroup = Some logGroup }

/// Helpers for building HttpRouteSpec values for `routeSpec`.
module HttpRoute =

    /// A route to a Lambda function using the API's default authorizer.
    let lambda (method: HttpMethod) (path: string) (handler: Amazon.CDK.AWS.Lambda.IFunction) : HttpRouteSpec =
        { Path = path
          Method = method
          Target = LambdaTarget handler
          Authorization = DefaultAuthorization
          AuthorizationScopes = [] }

    /// A route to any integration using the API's default authorizer.
    let integration (method: HttpMethod) (path: string) (integration: HttpRouteIntegration) : HttpRouteSpec =
        { Path = path
          Method = method
          Target = IntegrationTarget integration
          Authorization = DefaultAuthorization
          AuthorizationScopes = [] }

    /// Makes the route public.
    let noAuthorization (route: HttpRouteSpec) =
        { route with
            Authorization = NoAuthorization }

    /// Uses a route-specific authorizer.
    let withAuthorizer (authorizer: IHttpRouteAuthorizer) (route: HttpRouteSpec) =
        { route with
            Authorization = RouteAuthorizer authorizer }

    /// Requires OAuth scopes on the route.
    let withScopes (scopes: string list) (route: HttpRouteSpec) =
        { route with
            AuthorizationScopes = route.AuthorizationScopes @ scopes }

/// Helper functions for creating HTTP API CORS configurations
module HttpApiHelpers =

    /// Creates CORS preflight options with common defaults.
    /// Credentials are not allowed by default: API Gateway rejects
    /// AllowCredentials=true combined with a wildcard origin.
    let cors (allowOrigins: string list) (allowMethods: CorsHttpMethod list) (allowHeaders: string list) =
        CorsPreflightOptions(
            AllowOrigins = (allowOrigins |> Array.ofList),
            AllowMethods = (allowMethods |> Array.ofList),
            AllowHeaders = (allowHeaders |> Array.ofList),
            MaxAge = Duration.Hours(1.0)
        )

    /// Creates CORS preflight options that allow credentials. Origins must be
    /// explicit (no "*"): API Gateway rejects credentials with a wildcard origin.
    let corsWithCredentials
        (allowOrigins: string list)
        (allowMethods: CorsHttpMethod list)
        (allowHeaders: string list)
        =
        let options = cors allowOrigins allowMethods allowHeaders
        options.AllowCredentials <- true
        options

    /// Creates permissive CORS for development (allows all origins, methods, headers)
    let corsPermissive () =
        CorsPreflightOptions(
            AllowOrigins = [| "*" |],
            AllowMethods = [| CorsHttpMethod.ANY |],
            AllowHeaders = [| "*" |],
            MaxAge = Duration.Hours(1.0)
        )

    /// Creates an HTTP route key for any HTTP method
    let routeKey (method: HttpMethod) (path: string) = HttpRouteKey.With(path, method)

    /// Creates an HTTP route key for GET requests
    let getRoute path = HttpRouteKey.With(path, HttpMethod.GET)

    /// Creates an HTTP route key for POST requests
    let postRoute path =
        HttpRouteKey.With(path, HttpMethod.POST)

    /// Creates an HTTP route key for PUT requests
    let putRoute path = HttpRouteKey.With(path, HttpMethod.PUT)

    /// Creates an HTTP route key for DELETE requests
    let deleteRoute path =
        HttpRouteKey.With(path, HttpMethod.DELETE)

    /// Creates an HTTP route key for PATCH requests
    let patchRoute path =
        HttpRouteKey.With(path, HttpMethod.PATCH)

    /// Creates an HTTP route key for any method
    let anyRoute path = HttpRouteKey.With(path, HttpMethod.ANY)

[<AutoOpen>]
module HttpApiBuilders =
    /// <summary>
    /// Creates a new HTTP API builder with secure defaults.
    /// Example: httpApi "my-api" { description "My API"; route HttpMethod.GET "/items" fn }
    /// </summary>
    let httpApi name = HttpApiBuilder name
