namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SSM

// ============================================================================
// Operation Types - Unified Discriminated Union
// ============================================================================

type Operation =
    | Table of TableSpec
    | Function of FunctionSpec
    | DockerImageFunction of DockerImageFunctionSpec
    | Grant of GrantSpec
    | Topic of TopicSpec
    | Queue of QueueSpec
    | Subscription of SubscriptionSpec
    | Datadog of bool

// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

// Stack configuration
type StackConfig =
    { Name: string
      Environment: string option
      Version: string option
      Props: StackProps option
      Operations: Operation list }

type StackSpec =
    { Name: string
      Environment: string option
      Version: string option
      Props: StackProps option
      Operations: Operation list }

type StackBuilder(name) =

    member _.Yield _ : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [] }

    // Allow yielding operations directly - this is the key for Pattern 2
    member _.Yield(op: Operation) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ op ] }

    member _.Zero() : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [] }

    // Combine multiple operations - essential for Pattern 2
    member _.Combine(state1: StackConfig, state2: StackConfig) : StackConfig =
        { Name = state1.Name
          Environment =
            if state1.Environment.IsSome then
                state1.Environment
            else
                state2.Environment
          Version =
            if state1.Version.IsSome then
                state1.Version
            else
                state2.Version
          Props = if state1.Props.IsSome then state1.Props else state2.Props
          Operations = state1.Operations @ state2.Operations }

    // Delay for proper computation expression evaluation
    member _.Delay(f: unit -> StackConfig) : StackConfig = f ()

    member _.Run(config: StackConfig) : StackSpec =
        // Stack name is required
        let name = config.Name

        { Name = name
          Environment = config.Environment
          Version = config.Version
          Props = config.Props
          Operations = config.Operations }

    // Configuration operations
    [<CustomOperation("env")>]
    member _.Environment(config: StackConfig, value: string) : StackConfig =
        { config with Environment = Some value }

    [<CustomOperation("version")>]
    member _.Version(config: StackConfig, value: string) : StackConfig = { config with Version = Some value }

    [<CustomOperation("props")>]
    member _.Props(config: StackConfig, value: StackProps) : StackConfig = { config with Props = Some value }

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (config: StackConfig) (operation: Operation) : unit =
        match operation with
        | Table tableSpec ->
            Amazon.CDK.AWS.DynamoDB.Table(stack, tableSpec.ConstructId, tableSpec.Props)
            |> ignore

        | Function lambdaSpec ->
            Amazon.CDK.AWS.Lambda.Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)
            |> ignore

        | DockerImageFunction imageLambdaSpec ->
            // Create code lazily to avoid JSII side effects during spec construction
            imageLambdaSpec.Props.Code <- DockerImageCode.FromImageAsset(imageLambdaSpec.Code)
            // Apply deferred timeout to avoid jsii in tests
            if imageLambdaSpec.TimeoutSeconds.HasValue then
                imageLambdaSpec.Props.Timeout <- Duration.Seconds(imageLambdaSpec.TimeoutSeconds.Value)

            Amazon.CDK.AWS.Lambda.DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)
            |> ignore

        | Grant grantSpec -> Grants.processGrant stack grantSpec

        | Topic topicSpec ->
            Amazon.CDK.AWS.SNS.Topic(stack, topicSpec.ConstructId, topicSpec.Props)
            |> ignore

        | Queue queueSpec -> SQS.processQueue stack queueSpec

        | Subscription subscriptionSpec -> SNS.processSubscription stack subscriptionSpec

        | Datadog enabled ->
            if enabled then
                // Get env and version from the stack context or config
                let env =
                    match config.Environment with
                    | Some e -> e
                    | None ->
                        match stack.Node.TryGetContext("environment") with
                        | null -> "dev"
                        | e -> e.ToString()

                let version =
                    match config.Version with
                    | Some v -> v
                    | None ->
                        match stack.Node.TryGetContext("stack-version") with
                        | null ->
                            match stack.Node.TryGetContext("version") with
                            | null -> "local-dev"
                            | v -> v.ToString()
                        | v -> v.ToString()

                // Find all Function constructs in the stack and configure Datadog
                for child in stack.Node.Children do
                    match child with
                    | :? Function as lambda ->
                        // Configure Datadog for the Lambda function
                        let settings = DatadogConfig.getDefaultSettings env version
                        DatadogConfig.configureDatadogForLambda stack lambda settings
                    | _ -> ()

                // Configure Datadog log forwarding
                printfn $"Datadog integration enabled for stack: {stack.StackName}"

                // Get the Datadog forwarder ARN from SSM
                let ddForwarderArn =
                    StringParameter.ValueForStringParameter(stack, "/datadog/forwarder-arn")

                // Use the module-based approach for simplicity and clarity
                DatadogLogSubscription.configureStackLogSubscriptions stack ddForwarderArn
