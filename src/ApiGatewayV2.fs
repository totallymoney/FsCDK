namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Apigatewayv2

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
/// Access the underlying CDK HttpApi via the `Api` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type HttpApiConfig =
    { ApiName: string
      ConstructId: string option
      Description: string option
      CorsPreflightOptions: CorsPreflightOptions option
      CreateDefaultStage: bool voption }

type HttpApiResource =
    {
        ApiName: string
        ConstructId: string
        /// The underlying CDK HttpApi construct
        Api: HttpApi
    }

type HttpApiBuilder(name: string) =
    member _.Yield _ : HttpApiConfig =
        { ApiName = name
          ConstructId = None
          Description = None
          CorsPreflightOptions = None
          CreateDefaultStage = ValueSome true }

    member _.Zero() : HttpApiConfig =
        { ApiName = name
          ConstructId = None
          Description = None
          CorsPreflightOptions = None
          CreateDefaultStage = ValueSome true }

    member _.Combine(state1: HttpApiConfig, state2: HttpApiConfig) : HttpApiConfig =
        { ApiName = state2.ApiName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          CorsPreflightOptions = state2.CorsPreflightOptions |> Option.orElse state1.CorsPreflightOptions
          CreateDefaultStage = state2.CreateDefaultStage |> ValueOption.orElse state1.CreateDefaultStage }

    member inline _.Delay([<InlineIfLambda>] f: unit -> HttpApiConfig) : HttpApiConfig = f ()

    member inline x.For(config: HttpApiConfig, [<InlineIfLambda>] f: unit -> HttpApiConfig) : HttpApiConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: HttpApiConfig) : HttpApiResource =
        let apiName = config.ApiName
        let constructId = config.ConstructId |> Option.defaultValue apiName

        let props = HttpApiProps()
        props.ApiName <- apiName

        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.CorsPreflightOptions |> Option.iter (fun v -> props.CorsPreflight <- v)

        config.CreateDefaultStage
        |> ValueOption.iter (fun v -> props.CreateDefaultStage <- System.Nullable<bool>(v))

        { ApiName = apiName
          ConstructId = constructId
          Api = null }

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

/// Helper functions for creating HTTP API CORS configurations
module HttpApiHelpers =

    /// Creates CORS preflight options with common defaults
    let cors (allowOrigins: string list) (allowMethods: CorsHttpMethod list) (allowHeaders: string list) =
        CorsPreflightOptions(
            AllowOrigins = (allowOrigins |> Array.ofList),
            AllowMethods = (allowMethods |> Array.ofList),
            AllowHeaders = (allowHeaders |> Array.ofList),
            AllowCredentials = true,
            MaxAge = Duration.Hours(1.0)
        )

    /// Creates permissive CORS for development (allows all origins, methods, headers)
    let corsPermissive () =
        cors [ "*" ] [ CorsHttpMethod.ANY ] [ "*" ]

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
    /// Example: httpApi "my-api" { description "My API"; cors (HttpApiHelpers.corsPermissive ()) }
    /// </summary>
    let httpApi name = HttpApiBuilder name
