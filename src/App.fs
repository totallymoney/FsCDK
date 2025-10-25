namespace FsCDK

open Amazon.CDK

type AppConfig =
    { Context: Map<string, obj>
      StackTraces: bool option
      DefaultStackSynthesizer: IReusableStackSynthesizer option }

type AppBuilder() =
    member _.Zero() =
        { Context = Map.empty
          StackTraces = None
          DefaultStackSynthesizer = None }

    member this.Yield _ : AppConfig =
        { Context = Map.empty
          StackTraces = None
          DefaultStackSynthesizer = None }

    member _.Combine(config1: AppConfig, config2: AppConfig) =
        { Context = Map.fold (fun acc k v -> Map.add k v acc) config2.Context config1.Context
          StackTraces = config1.StackTraces |> Option.orElse config2.StackTraces
          DefaultStackSynthesizer = config1.DefaultStackSynthesizer |> Option.orElse config2.DefaultStackSynthesizer }

    member _.Delay(f: unit -> AppConfig) = f ()

    member inline x.For(config: AppConfig, f: unit -> AppConfig) : AppConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    /// <summary>Adds context to the App with a key-value pair.</summary>
    /// <param name="keys">The context key.</param>
    /// <code lang="fsharp">
    /// app {
    ///     context [
    ///         ("environment", "production")
    ///         ("feature-flag", true) ]
    /// }
    /// </code>
    [<CustomOperation("context")>]
    member _.Context(config: AppConfig, keys: (string * obj) seq) : AppConfig =
        { config with
            Context = keys |> Seq.fold (fun ctx (k, v) -> Map.add k v ctx) config.Context }

    /// <summary>Enables or disables stack traces in synthesized CloudFormation templates.</summary>
    /// <param name="enabled">Whether stack traces should be included.</param>
    /// <code lang="fsharp">
    /// app {
    ///     stackTraces true
    /// }
    /// </code>
    [<CustomOperation("stackTraces")>]
    member _.StackTraces(config: AppConfig, enabled: bool) : AppConfig =
        { config with
            StackTraces = Some enabled }

    /// <summary>Sets the stack synthesizer for the App.</summary>
    /// <param name="synthesizer">The stack synthesizer to use.</param>
    /// <code lang="fsharp">
    /// app {
    ///     synthesizer (DefaultStackSynthesizer())
    /// }
    /// </code>
    [<CustomOperation("synthesizer")>]
    member _.Synthesizer(config: AppConfig, synthesizer: IReusableStackSynthesizer) : AppConfig =
        { config with
            DefaultStackSynthesizer = Some synthesizer }

    member _.Run(config: AppConfig) =
        let props = AppProps(Context = config.Context)

        config.StackTraces |> Option.iter (fun v -> props.StackTraces <- v)

        config.DefaultStackSynthesizer
        |> Option.iter (fun v -> props.DefaultStackSynthesizer <- v)

        App(props)

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module AppBuilders =
    /// <summary>Creates an AWS CDK App construct.</summary>
    /// <code lang="fsharp">
    /// app {
    ///     context "key" "value"
    ///     stackTraces true
    /// }
    /// </code>
    let app = AppBuilder()
