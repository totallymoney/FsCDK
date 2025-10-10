namespace FsCDK

open Amazon.CDK
open System.Collections.Generic

type AppConfig =
    { Context: (string * obj) list
      StackTraces: bool option
      DefaultStackSynthesizer: IReusableStackSynthesizer option }

type AppBuilder() =
    member _.Zero() =
        { Context = []
          StackTraces = None
          DefaultStackSynthesizer = None }

    member this.Yield(_: unit) : AppConfig = this.Zero()

    member _.Combine(config1: AppConfig, config2: AppConfig) =
        { Context = config1.Context @ config2.Context
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
                    |> List.iter (fun (k, v) -> contextDict[k] <- v // Use indexer to allow overwriting
                    )

                    props.Context <- contextDict

                config.StackTraces |> Option.iter (fun st -> props.StackTraces <- st)

                config.DefaultStackSynthesizer
                |> Option.iter (fun s -> props.DefaultStackSynthesizer <- s)

                props

        if isNull appProps then App() else App(appProps)

[<AutoOpen>]
module Builders =
    let environment = EnvironmentBuilder()
    let stackProps = StackPropsBuilder()
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
    let stack name = StackBuilder(name)
    let app = AppBuilder()
