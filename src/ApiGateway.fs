namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.APIGateway
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.IAM

// ============================================================================
// Type References and Helpers
// ============================================================================

/// Represents a reference to a REST API that can be resolved later
type RestApiRef =
    | RestApiInterface of IRestApi
    | RestApiSpecRef of RestApiSpec

and RestApiSpec =
    { ApiName: string
      ConstructId: string
      Props: RestApiProps
      mutable RestApi: IRestApi option }

    /// Gets the underlying IRestApi resource. Must be called after the stack is built.
    member this.Resource =
        match this.RestApi with
        | Some api -> api
        | None ->
            failwith
                $"RestApi '{this.ApiName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

module RestApiHelpers =
    /// Resolves a REST API reference to an IRestApi
    let resolveRestApiRef (ref: RestApiRef) =
        match ref with
        | RestApiInterface api -> api
        | RestApiSpecRef spec ->
            match spec.RestApi with
            | Some api -> api
            | None ->
                failwith
                    $"RestApi '{spec.ApiName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

// ============================================================================
// REST API Configuration DSL
// ============================================================================

/// <summary>
/// High-level API Gateway REST API builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Endpoint type = REGIONAL (recommended for most use cases)
/// - Deploy = true (automatically creates deployment)
/// - CloudWatch role = automatically configured
///
/// **Rationale:**
/// REST APIs provide full control over API Gateway features including:
/// - Request/response transformation
/// - API keys and usage plans
/// - Request validators
/// - VPC Link integration
/// - Custom authorizers
///
/// **Escape Hatch:**
/// Access the underlying CDK RestApi via the `RestApi` property on RestApiSpec
/// for advanced scenarios not covered by this builder.
/// </summary>
type RestApiConfig =
    { ApiName: string
      ConstructId: string option
      Description: string option
      EndpointTypes: EndpointType list
      Deploy: bool option
      DeployOptions: StageOptions option
      CloudWatchRole: bool option
      Policy: PolicyDocument option
      DefaultCorsPreflightOptions: CorsOptions option
      DefaultIntegration: Integration option
      DefaultMethodOptions: MethodOptions option
      BinaryMediaTypes: string list
      MinimumCompressionSize: Size option
      ApiKeySourceType: ApiKeySourceType option
      DisableExecuteApiEndpoint: bool option }

type RestApiBuilder(name: string) =

    member _.Yield _ : RestApiConfig =
        { ApiName = name
          ConstructId = None
          Description = None
          EndpointTypes = [ EndpointType.REGIONAL ]
          Deploy = Some true
          DeployOptions = None
          CloudWatchRole = Some true
          Policy = None
          DefaultCorsPreflightOptions = None
          DefaultIntegration = None
          DefaultMethodOptions = None
          BinaryMediaTypes = []
          MinimumCompressionSize = None
          ApiKeySourceType = None
          DisableExecuteApiEndpoint = None }

    member _.Zero() : RestApiConfig =
        { ApiName = name
          ConstructId = None
          Description = None
          EndpointTypes = [ EndpointType.REGIONAL ]
          Deploy = Some true
          DeployOptions = None
          CloudWatchRole = Some true
          Policy = None
          DefaultCorsPreflightOptions = None
          DefaultIntegration = None
          DefaultMethodOptions = None
          BinaryMediaTypes = []
          MinimumCompressionSize = None
          ApiKeySourceType = None
          DisableExecuteApiEndpoint = None }

    member _.Combine(state1: RestApiConfig, state2: RestApiConfig) : RestApiConfig =
        { ApiName = state2.ApiName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          EndpointTypes =
            if state2.EndpointTypes.IsEmpty then
                state1.EndpointTypes
            else
                state2.EndpointTypes
          Deploy = state2.Deploy |> Option.orElse state1.Deploy
          DeployOptions = state2.DeployOptions |> Option.orElse state1.DeployOptions
          CloudWatchRole = state2.CloudWatchRole |> Option.orElse state1.CloudWatchRole
          Policy = state2.Policy |> Option.orElse state1.Policy
          DefaultCorsPreflightOptions =
            state2.DefaultCorsPreflightOptions
            |> Option.orElse state1.DefaultCorsPreflightOptions
          DefaultIntegration = state2.DefaultIntegration |> Option.orElse state1.DefaultIntegration
          DefaultMethodOptions = state2.DefaultMethodOptions |> Option.orElse state1.DefaultMethodOptions
          BinaryMediaTypes =
            if state2.BinaryMediaTypes.IsEmpty then
                state1.BinaryMediaTypes
            else
                state2.BinaryMediaTypes @ state1.BinaryMediaTypes
          MinimumCompressionSize = state2.MinimumCompressionSize |> Option.orElse state1.MinimumCompressionSize
          ApiKeySourceType = state2.ApiKeySourceType |> Option.orElse state1.ApiKeySourceType
          DisableExecuteApiEndpoint =
            state2.DisableExecuteApiEndpoint
            |> Option.orElse state1.DisableExecuteApiEndpoint }

    member inline _.Delay([<InlineIfLambda>] f: unit -> RestApiConfig) : RestApiConfig = f ()

    member inline x.For(config: RestApiConfig, [<InlineIfLambda>] f: unit -> RestApiConfig) : RestApiConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: RestApiConfig) : RestApiSpec =
        let apiName = config.ApiName
        let constructId = config.ConstructId |> Option.defaultValue apiName

        let props = RestApiProps()
        props.RestApiName <- apiName

        config.Description |> Option.iter (fun v -> props.Description <- v)

        if not config.EndpointTypes.IsEmpty then
            props.EndpointTypes <- Array.ofList config.EndpointTypes

        config.Deploy |> Option.iter (fun v -> props.Deploy <- v)
        config.DeployOptions |> Option.iter (fun v -> props.DeployOptions <- v)
        config.CloudWatchRole |> Option.iter (fun v -> props.CloudWatchRole <- v)
        config.Policy |> Option.iter (fun v -> props.Policy <- v)

        config.DefaultCorsPreflightOptions
        |> Option.iter (fun v -> props.DefaultCorsPreflightOptions <- v)

        config.DefaultIntegration
        |> Option.iter (fun v -> props.DefaultIntegration <- v)

        config.DefaultMethodOptions
        |> Option.iter (fun v -> props.DefaultMethodOptions <- v)

        if not config.BinaryMediaTypes.IsEmpty then
            props.BinaryMediaTypes <- Array.ofList config.BinaryMediaTypes

        config.MinimumCompressionSize
        |> Option.iter (fun v -> props.MinCompressionSize <- v)

        config.ApiKeySourceType |> Option.iter (fun v -> props.ApiKeySourceType <- v)

        config.DisableExecuteApiEndpoint
        |> Option.iter (fun v -> props.DisableExecuteApiEndpoint <- v)

        { ApiName = apiName
          ConstructId = constructId
          Props = props
          RestApi = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: RestApiConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the description for the REST API.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: RestApiConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets a single endpoint type.</summary>
    [<CustomOperation("endpointType")>]
    member _.EndpointType(config: RestApiConfig, endpointType: EndpointType) =
        { config with
            EndpointTypes = [ endpointType ] }

    /// <summary>Sets multiple endpoint types.</summary>
    [<CustomOperation("endpointTypes")>]
    member _.EndpointTypes(config: RestApiConfig, endpointTypes: EndpointType list) =
        { config with
            EndpointTypes = endpointTypes }

    /// <summary>Controls whether to automatically deploy the API.</summary>
    [<CustomOperation("deploy")>]
    member _.Deploy(config: RestApiConfig, deploy: bool) = { config with Deploy = Some deploy }

    /// <summary>Sets the deployment stage options.</summary>
    [<CustomOperation("deployOptions")>]
    member _.DeployOptions(config: RestApiConfig, options: StageOptions) =
        { config with
            DeployOptions = Some options }

    /// <summary>Controls whether to automatically create CloudWatch role for logging.</summary>
    [<CustomOperation("cloudWatchRole")>]
    member _.CloudWatchRole(config: RestApiConfig, enabled: bool) =
        { config with
            CloudWatchRole = Some enabled }

    /// <summary>Sets the resource policy for the API.</summary>
    [<CustomOperation("policy")>]
    member _.Policy(config: RestApiConfig, policy: PolicyDocument) = { config with Policy = Some policy }

    /// <summary>Sets default CORS preflight options for all methods.</summary>
    [<CustomOperation("defaultCorsPreflightOptions")>]
    member _.DefaultCorsPreflightOptions(config: RestApiConfig, corsOptions: CorsOptions) =
        { config with
            DefaultCorsPreflightOptions = Some corsOptions }

    /// <summary>Sets the default integration for all methods.</summary>
    [<CustomOperation("defaultIntegration")>]
    member _.DefaultIntegration(config: RestApiConfig, integration: Integration) =
        { config with
            DefaultIntegration = Some integration }

    /// <summary>Sets default method options for all methods.</summary>
    [<CustomOperation("defaultMethodOptions")>]
    member _.DefaultMethodOptions(config: RestApiConfig, options: MethodOptions) =
        { config with
            DefaultMethodOptions = Some options }

    /// <summary>Adds a binary media type.</summary>
    [<CustomOperation("binaryMediaType")>]
    member _.BinaryMediaType(config: RestApiConfig, mediaType: string) =
        { config with
            BinaryMediaTypes = mediaType :: config.BinaryMediaTypes }

    /// <summary>Adds multiple binary media types.</summary>
    [<CustomOperation("binaryMediaTypes")>]
    member _.BinaryMediaTypes(config: RestApiConfig, mediaTypes: string list) =
        { config with
            BinaryMediaTypes = mediaTypes @ config.BinaryMediaTypes }

    /// <summary>Sets the minimum response compression size in bytes.</summary>
    [<CustomOperation("minimumCompressionSize")>]
    member _.MinimumCompressionSize(config: RestApiConfig, size: Size) =
        { config with
            MinimumCompressionSize = Some size }

    /// <summary>Sets the source of the API key for requests.</summary>
    [<CustomOperation("apiKeySourceType")>]
    member _.ApiKeySourceType(config: RestApiConfig, sourceType: ApiKeySourceType) =
        { config with
            ApiKeySourceType = Some sourceType }

    /// <summary>Disables the default execute-api endpoint.</summary>
    [<CustomOperation("disableExecuteApiEndpoint")>]
    member _.DisableExecuteApiEndpoint(config: RestApiConfig, disable: bool) =
        { config with
            DisableExecuteApiEndpoint = Some disable }

// ============================================================================
// Lambda Authorizer Configuration DSL
// ============================================================================

type TokenAuthorizerConfig =
    { AuthorizerName: string
      ConstructId: string option
      Handler: IFunction option
      IdentitySource: string option
      ValidationRegex: string option
      AuthorizerResultTtl: Duration option
      AssumeRole: IRole option }

type TokenAuthorizerSpec =
    { AuthorizerName: string
      ConstructId: string
      Props: TokenAuthorizerProps
      mutable Authorizer: IAuthorizer option }

    /// Gets the underlying IAuthorizer resource. Must be called after the stack is built.
    member this.Resource =
        match this.Authorizer with
        | Some auth -> auth
        | None ->
            failwith
                $"TokenAuthorizer '{this.AuthorizerName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type TokenAuthorizerBuilder(name: string) =

    member _.Yield _ : TokenAuthorizerConfig =
        { AuthorizerName = name
          ConstructId = None
          Handler = None
          IdentitySource = Some "method.request.header.Authorization"
          ValidationRegex = None
          AuthorizerResultTtl = Some(Duration.Minutes(5.0))
          AssumeRole = None }

    member _.Zero() : TokenAuthorizerConfig =
        { AuthorizerName = name
          ConstructId = None
          Handler = None
          IdentitySource = Some "method.request.header.Authorization"
          ValidationRegex = None
          AuthorizerResultTtl = Some(Duration.Minutes(5.0))
          AssumeRole = None }

    member _.Combine(state1: TokenAuthorizerConfig, state2: TokenAuthorizerConfig) : TokenAuthorizerConfig =
        { AuthorizerName = state2.AuthorizerName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Handler = state2.Handler |> Option.orElse state1.Handler
          IdentitySource = state2.IdentitySource |> Option.orElse state1.IdentitySource
          ValidationRegex = state2.ValidationRegex |> Option.orElse state1.ValidationRegex
          AuthorizerResultTtl = state2.AuthorizerResultTtl |> Option.orElse state1.AuthorizerResultTtl
          AssumeRole = state2.AssumeRole |> Option.orElse state1.AssumeRole }

    member inline _.Delay([<InlineIfLambda>] f: unit -> TokenAuthorizerConfig) : TokenAuthorizerConfig = f ()

    member inline x.For
        (
            config: TokenAuthorizerConfig,
            [<InlineIfLambda>] f: unit -> TokenAuthorizerConfig
        ) : TokenAuthorizerConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: TokenAuthorizerConfig) : TokenAuthorizerSpec =
        let authorizerName = config.AuthorizerName
        let constructId = config.ConstructId |> Option.defaultValue authorizerName

        let props = TokenAuthorizerProps()
        props.AuthorizerName <- authorizerName

        match config.Handler with
        | Some handler -> props.Handler <- handler
        | None -> invalidArg "handler" "Lambda handler is required for TokenAuthorizer"

        config.IdentitySource |> Option.iter (fun v -> props.IdentitySource <- v)
        config.ValidationRegex |> Option.iter (fun v -> props.ValidationRegex <- v)
        config.AuthorizerResultTtl |> Option.iter (fun v -> props.ResultsCacheTtl <- v)

        config.AssumeRole |> Option.iter (fun v -> props.AssumeRole <- v)

        { AuthorizerName = authorizerName
          ConstructId = constructId
          Props = props
          Authorizer = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TokenAuthorizerConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the Lambda function to use for authorization.</summary>
    [<CustomOperation("handler")>]
    member _.Handler(config: TokenAuthorizerConfig, handler: IFunction) = { config with Handler = Some handler }

    /// <summary>Sets the identity source for the authorization token.</summary>
    [<CustomOperation("identitySource")>]
    member _.IdentitySource(config: TokenAuthorizerConfig, source: string) =
        { config with
            IdentitySource = Some source }

    /// <summary>Sets a validation regex for the authorization token.</summary>
    [<CustomOperation("validationRegex")>]
    member _.ValidationRegex(config: TokenAuthorizerConfig, regex: string) =
        { config with
            ValidationRegex = Some regex }

    /// <summary>Sets how long API Gateway caches authorizer results.</summary>
    [<CustomOperation("resultsCacheTtl")>]
    member _.ResultsCacheTtl(config: TokenAuthorizerConfig, ttl: Duration) =
        { config with
            AuthorizerResultTtl = Some ttl }

    /// <summary>Sets the IAM role for API Gateway to assume when calling the authorizer.</summary>
    [<CustomOperation("assumeRole")>]
    member _.AssumeRole(config: TokenAuthorizerConfig, role: IRole) = { config with AssumeRole = Some role }

// ============================================================================
// VPC Link Configuration DSL
// ============================================================================

type VpcLinkConfig =
    { VpcLinkName: string
      ConstructId: string option
      Description: string option
      Targets: INetworkLoadBalancer list
      VpcLinkName_: string option }

type VpcLinkSpec =
    { VpcLinkName: string
      ConstructId: string
      Props: VpcLinkProps
      mutable VpcLink: IVpcLink option }

    /// Gets the underlying IVpcLink resource. Must be called after the stack is built.
    member this.Resource =
        match this.VpcLink with
        | Some link -> link
        | None ->
            failwith
                $"VpcLink '{this.VpcLinkName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type VpcLinkBuilder(name: string) =

    member _.Yield _ : VpcLinkConfig =
        { VpcLinkName = name
          ConstructId = None
          Description = None
          Targets = []
          VpcLinkName_ = None }

    member _.Zero() : VpcLinkConfig =
        { VpcLinkName = name
          ConstructId = None
          Description = None
          Targets = []
          VpcLinkName_ = None }

    member _.Combine(state1: VpcLinkConfig, state2: VpcLinkConfig) : VpcLinkConfig =
        { VpcLinkName = state2.VpcLinkName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          Targets =
            if state2.Targets.IsEmpty then
                state1.Targets
            else
                state2.Targets @ state1.Targets
          VpcLinkName_ = state2.VpcLinkName_ |> Option.orElse state1.VpcLinkName_ }

    member inline _.Delay([<InlineIfLambda>] f: unit -> VpcLinkConfig) : VpcLinkConfig = f ()

    member inline x.For(config: VpcLinkConfig, [<InlineIfLambda>] f: unit -> VpcLinkConfig) : VpcLinkConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: VpcLinkConfig) : VpcLinkSpec =
        let vpcLinkName = config.VpcLinkName
        let constructId = config.ConstructId |> Option.defaultValue vpcLinkName

        let props = VpcLinkProps()

        config.VpcLinkName_ |> Option.iter (fun v -> props.VpcLinkName <- v)
        config.Description |> Option.iter (fun v -> props.Description <- v)

        if not config.Targets.IsEmpty then
            props.Targets <- Array.ofList config.Targets

        { VpcLinkName = vpcLinkName
          ConstructId = constructId
          Props = props
          VpcLink = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: VpcLinkConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the description for the VPC Link.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: VpcLinkConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Sets the VPC Link name (physical resource name).</summary>
    [<CustomOperation("vpcLinkName")>]
    member _.VpcLinkName_(config: VpcLinkConfig, name: string) =
        { config with VpcLinkName_ = Some name }

    /// <summary>Adds a Network Load Balancer target.</summary>
    [<CustomOperation("target")>]
    member _.Target(config: VpcLinkConfig, target: INetworkLoadBalancer) =
        { config with
            Targets = target :: config.Targets }

    /// <summary>Adds multiple Network Load Balancer targets.</summary>
    [<CustomOperation("targets")>]
    member _.Targets(config: VpcLinkConfig, targets: INetworkLoadBalancer list) =
        { config with
            Targets = targets @ config.Targets }

// ============================================================================
// Builder Functions
// ============================================================================

[<AutoOpen>]
module ApiGatewayBuilders =
    /// <summary>
    /// Creates a new REST API builder with best practices.
    /// Example: restApi "my-api" { description "My REST API" }
    /// </summary>
    let restApi name = RestApiBuilder name

    /// <summary>
    /// Creates a new Token Authorizer builder for Lambda authorization.
    /// Example: tokenAuthorizer "my-auth" { handler authFunction }
    /// </summary>
    let tokenAuthorizer name = TokenAuthorizerBuilder name

    /// <summary>
    /// Creates a new VPC Link builder for private integrations.
    /// Example: vpcLink "my-link" { target nlb }
    /// </summary>
    let vpcLink name = VpcLinkBuilder name
