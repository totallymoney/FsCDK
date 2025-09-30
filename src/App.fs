namespace FsCDK

open Amazon.CDK
open FsCDK.StackOperations


type AppBuilder() =
    member _.Yield _ = []

    // Allow yielding StackSpec directly for implicit syntax
    member _.Yield(stackSpec: StackSpec) = [ stackSpec ]

    member _.Zero() = []

    // Combine stacks when multiple is yielded
    member _.Combine(specs1: StackSpec list, specs2: StackSpec list) = specs1 @ specs2

    // Delay for proper computation expression evaluation
    member _.Delay(f: unit -> StackSpec list) = f ()

    [<CustomOperation("stacks")>]
    member _.Stacks(_, specs: StackSpec seq) = Seq.toList specs

    member _.Run(specs: StackSpec list) =
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

            // Process operations using the processOperation function
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

            // Process operations using the processOperation function
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
    let subscription = SubscriptionBuilder()
    let grant = GrantBuilder()
    let app = AppBuilder()
