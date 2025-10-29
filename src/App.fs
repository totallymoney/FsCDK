namespace FsCDK

open Amazon.CDK

type AppConfig =
    { AnalyticsReporting: bool option
      AutoSynth: bool option
      Context: Map<string, obj>
      DefaultStackSynthesizer: IReusableStackSynthesizer option
      OutputDirectory: string option
      PolicyValidationBeta1: IPolicyValidationPluginBeta1 seq option
      PostCliContext: Map<string, obj> option
      PropertyInjectors: IPropertyInjector seq option
      StackTraces: bool option
      TreeMetadata: bool option }

type AppBuilder() =
    member _.Zero() =
        { Context = Map.empty
          StackTraces = None
          DefaultStackSynthesizer = None
          AnalyticsReporting = Some false
          AutoSynth = Some false
          OutputDirectory = Some "cdk.out"
          PolicyValidationBeta1 = Some Seq.empty
          PostCliContext = Some Map.empty
          PropertyInjectors = Some Seq.empty
          TreeMetadata = Some false }

    member this.Yield _ : AppConfig =
        { Context = Map.empty
          StackTraces = None
          DefaultStackSynthesizer = None
          AnalyticsReporting = Some false
          AutoSynth = Some false
          OutputDirectory = Some "cdk.out"
          PolicyValidationBeta1 = Some Seq.empty
          PostCliContext = Some Map.empty
          PropertyInjectors = Some Seq.empty
          TreeMetadata = Some false }

    member _.Combine(config1: AppConfig, config2: AppConfig) =
        { Context = Map.fold (fun acc k v -> Map.add k v acc) config2.Context config1.Context
          StackTraces = config1.StackTraces |> Option.orElse config2.StackTraces
          DefaultStackSynthesizer = config1.DefaultStackSynthesizer |> Option.orElse config2.DefaultStackSynthesizer
          AnalyticsReporting = config1.AnalyticsReporting |> Option.orElse config2.AnalyticsReporting
          AutoSynth = config1.AutoSynth |> Option.orElse config2.AutoSynth
          OutputDirectory = config1.OutputDirectory |> Option.orElse config2.OutputDirectory
          PolicyValidationBeta1 = config1.PolicyValidationBeta1 |> Option.orElse config2.PolicyValidationBeta1
          PostCliContext = config1.PostCliContext |> Option.orElse config2.PostCliContext
          PropertyInjectors = config1.PropertyInjectors |> Option.orElse config2.PropertyInjectors
          TreeMetadata = config1.TreeMetadata |> Option.orElse config2.TreeMetadata }

    member _.Delay(f: unit -> AppConfig) = f ()

    member inline x.For(config: AppConfig, f: unit -> AppConfig) : AppConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.For(sequence: seq<'T>, body: 'T -> AppConfig) =
        let mutable state = this.Zero()

        for item in sequence do
            state <- this.Combine(state, body item)

        state

    /// <summary>Adds context to the App with a key-value pair.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="keys">The context key-value pairs to add.</param>
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
    /// <param name="config">The current app configuration.</param>
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
    /// <param name="config">The current app configuration.</param>
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


    /// <summary>Sets the output directory for the synthesized CloudFormation templates.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="outputDir">The output directory path.</param>
    /// <code lang="fsharp">
    /// app {
    ///     outputDirectory "my-output-dir"
    /// }
    /// </code>
    [<CustomOperation("outputDirectory")>]
    member _.OutputDirectory(config: AppConfig, outputDir: string) : AppConfig =
        { config with
            OutputDirectory = Some outputDir }


    /// <summary>Enables or disables analytics reporting for the App.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="enabled">Whether analytics reporting should be enabled.</param>
    /// <code lang="fsharp">
    /// app {
    ///     analyticsReporting true
    /// }
    /// </code>
    [<CustomOperation("analyticsReporting")>]
    member _.AnalyticsReporting(config: AppConfig, enabled: bool) : AppConfig =
        { config with
            AnalyticsReporting = Some enabled }

    /// <summary>Enables or disables automatic synthesis for the App.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="enabled">Whether automatic synthesis should be enabled.</param>
    /// <code lang="fsharp">
    /// app {
    ///     autoSynth true
    /// }
    /// </code>
    [<CustomOperation("autoSynth")>]
    member _.AutoSynth(config: AppConfig, enabled: bool) : AppConfig =
        { config with AutoSynth = Some enabled }

    /// <summary>Adds policy validation plugins to the App.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="plugins">The policy validation plugins to add.</param>
    /// <code lang="fsharp">
    /// app {
    ///     policyValidationBeta1 [ MyPolicyPlugin() ]
    /// }
    /// </code>
    [<CustomOperation("policyValidationBeta1")>]
    member _.PolicyValidationBeta1(config: AppConfig, plugins: IPolicyValidationPluginBeta1 seq) : AppConfig =
        { config with
            PolicyValidationBeta1 = Some plugins }

    /// <summary>Adds post CLI context to the App with a key-value pair.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="context">The post CLI context key-value pairs to add.</param>
    /// <code lang="fsharp">
    /// app {
    ///     postCliContext [
    ///         ("additional-info", "value")
    ///         ("debug-mode", false) ]
    /// }
    /// </code>
    [<CustomOperation("postCliContext")>]
    member _.PostCliContext(config: AppConfig, context: (string * obj) seq) : AppConfig =
        { config with
            PostCliContext = Some(context |> Seq.fold (fun ctx (k, v) -> Map.add k v ctx) Map.empty) }

    /// <summary>Adds property injectors to the App.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="injectors">The property injectors to add.</param>
    /// <code lang="fsharp">
    /// app {
    ///     propertyInjectors [ MyPropertyInjector() ]
    /// }
    /// </code>
    [<CustomOperation("propertyInjectors")>]
    member _.PropertyInjectors(config: AppConfig, injectors: IPropertyInjector seq) : AppConfig =
        { config with
            PropertyInjectors = Some injectors }

    /// <summary>Enables or disables tree metadata for the App.</summary>
    /// <param name="config">The current app configuration.</param>
    /// <param name="enabled">Whether tree metadata should be included.</param>
    /// <code lang="fsharp">
    /// app {
    ///     treeMetadata true
    /// }
    /// </code>
    [<CustomOperation("treeMetadata")>]
    member _.TreeMetadata(config: AppConfig, enabled: bool) : AppConfig =
        { config with
            TreeMetadata = Some enabled }

    member _.Run(config: AppConfig) =
        let props = AppProps(Context = config.Context)
        props.AnalyticsReporting <- config.AnalyticsReporting |> Option.defaultValue false
        props.AutoSynth <- config.AutoSynth |> Option.defaultValue false
        props.Outdir <- config.OutputDirectory |> Option.defaultValue "cdk.out"
        props.TreeMetadata <- config.TreeMetadata |> Option.defaultValue false

        config.PolicyValidationBeta1
        |> Option.iter (fun v -> props.PolicyValidationBeta1 <- v |> Seq.toArray)

        config.PostCliContext |> Option.iter (fun v -> props.PostCliContext <- v)

        config.PropertyInjectors
        |> Option.iter (fun v -> props.PropertyInjectors <- v |> Seq.toArray)

        config.StackTraces |> Option.iter (fun v -> props.StackTraces <- v)

        config.DefaultStackSynthesizer
        |> Option.iter (fun v -> props.DefaultStackSynthesizer <- v)

        App(props)

    /// Run delayed config
    member this.Run(f: unit -> AppConfig) = this.Run(f ())

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module AppBuilders =
    /// <summary>Creates an AWS CDK App construct.</summary>
    /// <code lang="fsharp">
    /// app {
    ///     context [ ("environment", "production"); ("feature-flag", true) ]
    ///     stackTraces true
    /// }
    /// </code>
    let app = AppBuilder()
