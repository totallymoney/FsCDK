namespace FsCDK

open Amazon.CDK


type AppBuilder() =
    member _.Yield _ = []

    member _.Zero() = []

    [<CustomOperation("stacks")>]
    member _.Stacks(_, specs: StackSpec seq) = specs

    member _.Run(specs: StackSpec seq) =
        let app = App()


        // Get version from CDK context (can be overridden per stack)
        let globalVersion =
            match app.Node.TryGetContext("version") with
            | null -> None
            | v -> Some(v.ToString())

        for spec in specs do
            // Use a stack-specific version if provided, otherwise use global version from context
            let stackVersion =
                match spec.Version with
                | Some v -> Some v
                | None -> globalVersion

            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            // Make a version available to operations if needed
            stackVersion |> Option.iter (fun v -> stack.Node.SetContext("stack-version", v))

            for op in spec.Operations do
                op stack

        app

    member _.RunWithApp(specs: StackSpec seq, app: App) =
        // Get version from CDK context (can be overridden per stack)
        let globalVersion =
            match app.Node.TryGetContext("version") with
            | null -> None
            | v -> Some(v.ToString())

        for spec in specs do
            // Use a stack-specific version if provided, otherwise use global version from context
            let stackVersion =
                match spec.Version with
                | Some v -> Some v
                | None -> globalVersion

            let stack =
                match spec.Props with
                | Some p -> Stack(app, spec.Name, p)
                | None -> Stack(app, spec.Name)

            // Make a version available to operations if needed
            stackVersion |> Option.iter (fun v -> stack.Node.SetContext("stack-version", v))

            for op in spec.Operations do
                op stack

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
    let stack = StackBuilder()
    let table = TableBuilder()
    let lambda = LambdaBuilder()
    let dockerImageFunction = DockerImageFunctionBuilder()
    let topic = TopicBuilder()
    let queue = QueueBuilder()
    let subscription = SubscriptionBuilder()
    let grant = GrantBuilder()
    let app = AppBuilder()
