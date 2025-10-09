namespace FsCDK

open Amazon.CDK
open FsCDK.StackOperations
open System.Collections.Generic

type AppConfig =
    { Stacks: StackSpec list
      Context: (string * obj) list
      StackTraces: bool option
      DefaultStackSynthesizer: IReusableStackSynthesizer option }

type AppBuilder() =
    member _.Yield _ =
        { Stacks = []
          Context = []
          StackTraces = None
          DefaultStackSynthesizer = None }

    member _.Yield(stackSpec: StackSpec) =
        { Stacks = [ stackSpec ]
          Context = []
          StackTraces = None
          DefaultStackSynthesizer = None }

    member _.Zero() =
        { Stacks = []
          Context = []
          StackTraces = None
          DefaultStackSynthesizer = None }

    member _.Combine(config1: AppConfig, config2: AppConfig) =
        { Stacks = config1.Stacks @ config2.Stacks
          Context = config1.Context @ config2.Context
          StackTraces = config1.StackTraces |> Option.orElse config2.StackTraces
          DefaultStackSynthesizer = config1.DefaultStackSynthesizer |> Option.orElse config2.DefaultStackSynthesizer }

    member _.Delay(f: unit -> AppConfig) = f ()

    member inline x.For(config: AppConfig, f: unit -> AppConfig) : AppConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    [<CustomOperation("context")>]
    member _.Context(config: AppConfig, key: string, value: obj) : AppConfig =
        { config with
            Context = (key, value) :: config.Context }

    [<CustomOperation("stackTraces")>]
    member _.StackTraces(config: AppConfig, enabled: bool) : AppConfig =
        { config with
            StackTraces = Some enabled }

    [<CustomOperation("synthesizer")>]
    member _.Synthesizer(config: AppConfig, synthesizer: IReusableStackSynthesizer) : AppConfig =
        { config with
            DefaultStackSynthesizer = Some synthesizer }

    member _.Run(config: AppConfig) =
        let appProps =
            if
                config.Context.IsEmpty
                && config.StackTraces.IsNone
                && config.DefaultStackSynthesizer.IsNone
            then
                null
            else
                let props = AppProps()

                if not config.Context.IsEmpty then
                    let contextDict = Dictionary<string, obj>()

                    config.Context
                    |> List.rev // Reverse to process in declaration order
                    |> List.iter (fun (k, v) -> contextDict.[k] <- v // Use indexer to allow overwriting
                    )

                    props.Context <- contextDict

                config.StackTraces |> Option.iter (fun st -> props.StackTraces <- st)

                config.DefaultStackSynthesizer
                |> Option.iter (fun s -> props.DefaultStackSynthesizer <- s)

                props

        let app = if isNull appProps then App() else App(appProps)

        for spec in config.Stacks do
            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            for op in spec.Operations do
                processOperation stack op

        app

    member _.RunWithApp(config: AppConfig, app: App) =
        for spec in config.Stacks do
            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            for op in spec.Operations do
                processOperation stack op

        app

// ============================================================================
// F# CDK DSL - Main Module
// This module re-exports all the builders and helper functions from individual files
// ============================================================================

// Module with builder instances
[<AutoOpen>]
module Builders =
    let environment = EnvironmentBuilder()
    let stackProps = StackPropsBuilder()
    let stack name = StackBuilder(name)
    let table name = TableBuilder(name)
    let lambda name = FunctionBuilder(name)
    let dockerImageFunction name = DockerImageFunctionBuilder(name)
    let topic name = TopicBuilder(name)
    let queue name = QueueBuilder(name)
    let bucket name = BucketBuilder(name)
    let subscription = SubscriptionBuilder()
    let grant = GrantBuilder()
    let importSource = ImportSourceBuilder()
    let functionUrl = FunctionUrlOptionsBuilder()
    let cors = FunctionUrlCorsOptionsBuilder()
    let eventSourceMapping id = EventSourceMappingOptionsBuilder(id)
    let permission id = PermissionBuilder(id)
    let eventInvokeConfigOptions = EventInvokeConfigOptionsBuilder()
    let configureAsyncInvoke = EventInvokeConfigOptionsBuilder()
    let policyStatementProps = PolicyStatementPropsBuilder()
    let policyStatement = PolicyStatementBuilder()
    let corsRule = CorsRuleBuilder()
    let lifecycleRule = LifecycleRuleBuilder()
    let transition = TransitionBuilder()
    let noncurrentVersionTransition = NoncurrentVersionTransitionBuilder()
    let metrics = BucketMetricsBuilder()
    let vpcSubnets = SubnetSelectionBuilder()
    let versionOptions = VersionOptionsBuilder()
    let lambdaFileSystem = LambdaFileSystemBuilder()
    let efsFileSystem id = EfsFileSystemBuilder(id)
    let accessPointProps fs = AccessPointPropsBuilder(fs)
    let accessPoint id = AccessPointBuilder(id)
    let app = AppBuilder()
