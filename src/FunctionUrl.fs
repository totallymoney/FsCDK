namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Lambda Function URL Types
// ============================================================================

type FunctionUrlSpec = { Options: IFunctionUrlOptions }

type FunctionUrlCorsSpec = { Options: IFunctionUrlCorsOptions }

// ============================================================================
// Lambda Function URL CORS Options Builder DSL
// ============================================================================

type FunctionUrlCorsOptionsConfig =
    { AllowCredentials: bool option
      AllowedHeaders: string list option
      AllowedMethods: HttpMethod list option
      AllowedOrigins: string list option
      ExposeHeaders: string list option
      MaxAge: Duration option }

type FunctionUrlCorsOptionsBuilder() =
    member _.Yield _ : FunctionUrlCorsOptionsConfig =
        { AllowCredentials = None
          AllowedHeaders = None
          AllowedMethods = None
          AllowedOrigins = None
          ExposeHeaders = None
          MaxAge = None }

    member _.Zero() : FunctionUrlCorsOptionsConfig =
        { AllowCredentials = None
          AllowedHeaders = None
          AllowedMethods = None
          AllowedOrigins = None
          ExposeHeaders = None
          MaxAge = None }

    member inline _.Delay(f: unit -> FunctionUrlCorsOptionsConfig) = f ()

    member inline x.For(state: FunctionUrlCorsOptionsConfig, f: unit -> FunctionUrlCorsOptionsConfig) =
        x.Combine(state, f ())

    member _.Combine
        (
            state1: FunctionUrlCorsOptionsConfig,
            state2: FunctionUrlCorsOptionsConfig
        ) : FunctionUrlCorsOptionsConfig =
        { AllowCredentials =
            if state1.AllowCredentials.IsSome then
                state1.AllowCredentials
            else
                state2.AllowCredentials
          AllowedHeaders =
            if state1.AllowedHeaders.IsSome then
                state1.AllowedHeaders
            else
                state2.AllowedHeaders
          AllowedMethods =
            if state1.AllowedMethods.IsSome then
                state1.AllowedMethods
            else
                state2.AllowedMethods
          AllowedOrigins =
            if state1.AllowedOrigins.IsSome then
                state1.AllowedOrigins
            else
                state2.AllowedOrigins
          ExposeHeaders =
            if state1.ExposeHeaders.IsSome then
                state1.ExposeHeaders
            else
                state2.ExposeHeaders
          MaxAge =
            if state1.MaxAge.IsSome then
                state1.MaxAge
            else
                state2.MaxAge }

    member _.Run(config: FunctionUrlCorsOptionsConfig) : FunctionUrlCorsSpec =
        let o = FunctionUrlCorsOptions()
        config.AllowCredentials |> Option.iter (fun v -> o.AllowCredentials <- v)

        config.AllowedHeaders
        |> Option.iter (fun v -> o.AllowedHeaders <- (v |> List.toArray))

        config.AllowedMethods
        |> Option.iter (fun v -> o.AllowedMethods <- (v |> List.toArray))

        config.AllowedOrigins
        |> Option.iter (fun v -> o.AllowedOrigins <- (v |> List.toArray))

        config.ExposeHeaders
        |> Option.iter (fun v -> o.ExposedHeaders <- (v |> List.toArray))

        config.MaxAge |> Option.iter (fun d -> o.MaxAge <- d)
        { Options = o :> IFunctionUrlCorsOptions }

    [<CustomOperation("allowCredentials")>]
    member _.AllowCredentials(config: FunctionUrlCorsOptionsConfig, value: bool) =
        { config with
            AllowCredentials = Some value }

    [<CustomOperation("allowedHeaders")>]
    member _.AllowedHeaders(config: FunctionUrlCorsOptionsConfig, headers: string list) =
        { config with
            AllowedHeaders = Some headers }

    [<CustomOperation("allowedMethods")>]
    member _.AllowedMethods(config: FunctionUrlCorsOptionsConfig, methods: HttpMethod list) =
        { config with
            AllowedMethods = Some methods }

    [<CustomOperation("allowedOrigins")>]
    member _.AllowedOrigins(config: FunctionUrlCorsOptionsConfig, origins: string list) =
        { config with
            AllowedOrigins = Some origins }

    [<CustomOperation("exposeHeaders")>]
    member _.ExposeHeaders(config: FunctionUrlCorsOptionsConfig, headers: string list) =
        { config with
            ExposeHeaders = Some headers }

    [<CustomOperation("maxAge")>]
    member _.MaxAge(config: FunctionUrlCorsOptionsConfig, duration: Duration) = { config with MaxAge = Some duration }

// ============================================================================
// Lambda Function URL Options Builder DSL
// ============================================================================

type FunctionUrlOptionsConfig =
    { AuthType: FunctionUrlAuthType option
      Cors: IFunctionUrlCorsOptions option
      InvokeMode: InvokeMode option }

type FunctionUrlOptionsBuilder() =
    member _.Yield _ : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = None
          InvokeMode = None }

    member _.Zero() : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = None
          InvokeMode = None }

    member _.Run(config: FunctionUrlOptionsConfig) : FunctionUrlSpec =
        let opts = FunctionUrlOptions()
        config.AuthType |> Option.iter (fun a -> opts.AuthType <- a)
        config.Cors |> Option.iter (fun c -> opts.Cors <- c)
        config.InvokeMode |> Option.iter (fun m -> opts.InvokeMode <- m)
        { Options = opts :> IFunctionUrlOptions }

    member inline _.Delay([<InlineIfLambda>] f: unit -> FunctionUrlOptionsConfig) : FunctionUrlOptionsConfig = f ()

    member inline x.For
        (
            config: FunctionUrlOptionsConfig,
            [<InlineIfLambda>] f: unit -> FunctionUrlOptionsConfig
        ) : FunctionUrlOptionsConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: FunctionUrlOptionsConfig, state2: FunctionUrlOptionsConfig) : FunctionUrlOptionsConfig =
        { AuthType =
            if state1.AuthType.IsSome then
                state1.AuthType
            else
                state2.AuthType
          Cors = if state1.Cors.IsSome then state1.Cors else state2.Cors
          InvokeMode =
            if state1.InvokeMode.IsSome then
                state1.InvokeMode
            else
                state2.InvokeMode }

    [<CustomOperation("authType")>]
    member _.AuthType(config: FunctionUrlOptionsConfig, auth: FunctionUrlAuthType) =
        { config with AuthType = Some auth }

    [<CustomOperation("invokeMode")>]
    member _.InvokeMode(config: FunctionUrlOptionsConfig, mode: InvokeMode) = { config with InvokeMode = Some mode }

    member _.Yield(spec: FunctionUrlCorsSpec) : FunctionUrlOptionsConfig =
        { AuthType = None
          Cors = Some spec.Options
          InvokeMode = None }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module FunctionUrlBuilders =
    let functionUrl = FunctionUrlOptionsBuilder()
    let functionUrlCors = FunctionUrlCorsOptionsBuilder()
    let cors = FunctionUrlCorsOptionsBuilder()
