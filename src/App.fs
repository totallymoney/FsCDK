namespace FsCDK

open Amazon.CDK

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

    // Helper function to build stacks with an existing App instance
    let buildApp (cdkApp: App) (stackSpecs: StackSpec seq) =
        for spec in stackSpecs do
            let stack =
                match spec.Props with
                | Some p -> Stack(cdkApp, spec.Name, p)
                | None -> Stack(cdkApp, spec.Name)

            for op in spec.Operations do
                op stack

        cdkApp.Synth()
