namespace FsCDK

open Amazon.CDK
open FsCDK.StackOperations


type AppBuilder() =
    member _.Yield _ = []

    member _.Yield(stackSpec: StackSpec) = [ stackSpec ]

    member _.Zero() = []

    member _.Combine(specs1: StackSpec list, specs2: StackSpec list) = specs1 @ specs2

    member _.Delay(f: unit -> StackSpec list) = f ()

    member _.Run(specs: StackSpec list) =
        let app = App()

        let globalVersion =
            match app.Node.TryGetContext("version") with
            | null -> None
            | v -> Some(v.ToString())

        for spec in specs do
            let stackVersion =
                match spec.Version with
                | Some v -> Some v
                | None -> globalVersion

            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            stackVersion |> Option.iter (fun v -> stack.Node.SetContext("stack-version", v))

            let config: StackConfig =
                { Name = spec.Name
                  Environment = spec.Environment
                  Version = spec.Version
                  Props = spec.Props
                  Operations = [] }

            for op in spec.Operations do
                processOperation stack config op

        app

    member _.RunWithApp(specs: StackSpec list, app: App) =
        let globalVersion =
            match app.Node.TryGetContext("version") with
            | null -> None
            | v -> Some(v.ToString())

        for spec in specs do
            let stackVersion =
                match spec.Version with
                | Some v -> Some v
                | None -> globalVersion

            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            stackVersion |> Option.iter (fun v -> stack.Node.SetContext("stack-version", v))

            let config: StackConfig =
                { Name = spec.Name
                  Environment = spec.Environment
                  Version = spec.Version
                  Props = spec.Props
                  Operations = [] }

            for op in spec.Operations do
                processOperation stack config op

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
    let functionUrlOptions = FunctionUrlOptionsBuilder()
    let functionUrlCorsOptions = FunctionUrlCorsOptionsBuilder()
    let eventSourceMappingOptions = EventSourceMappingOptionsBuilder()
    let permission = PermissionBuilder()
    let eventInvokeConfigOptions = EventInvokeConfigOptionsBuilder()
    let policyStatementProps = PolicyStatementPropsBuilder()
    let policyStatement = PolicyStatementBuilder()
    let corsRule = CorsRuleBuilder()
    let app = AppBuilder()
