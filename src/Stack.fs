namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.S3

// ============================================================================
// Operation Types - Unified Discriminated Union
// ============================================================================

type Operation =
    | TableOp of TableSpec
    | FunctionOp of FunctionSpec
    | DockerImageFunctionOp of DockerImageFunctionSpec
    | GrantOp of GrantSpec
    | TopicOp of TopicSpec
    | QueueOp of QueueSpec
    | BucketOp of BucketSpec
    | SubscriptionOp of SubscriptionSpec

// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

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

    member _.Yield(tableSpec: TableSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ TableOp tableSpec ] }

    member _.Yield(funcSpec: FunctionSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ FunctionOp funcSpec ] }

    member _.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ DockerImageFunctionOp dockerSpec ] }

    member _.Yield(grantSpec: GrantSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ GrantOp grantSpec ] }

    member _.Yield(topicSpec: TopicSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ TopicOp topicSpec ] }

    member _.Yield(queueSpec: QueueSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ QueueOp queueSpec ] }

    member _.Yield(bucketSpec: BucketSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ BucketOp bucketSpec ] }

    member _.Yield(subSpec: SubscriptionSpec) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [ SubscriptionOp subSpec ] }

    member _.Yield(props: StackProps) : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = Some props
          Operations = [] }

    member _.Zero() : StackConfig =
        { Name = name
          Environment = None
          Version = None
          Props = None
          Operations = [] }

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

    member _.Delay(f: unit -> StackConfig) : StackConfig = f ()

    member x.For(config: StackConfig, f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: StackConfig) : StackSpec =
        let name = config.Name

        { Name = name
          Environment = config.Environment
          Version = config.Version
          Props = config.Props
          Operations = config.Operations }

    [<CustomOperation("env")>]
    member _.Environment(config: StackConfig, value: string) : StackConfig =
        { config with Environment = Some value }

    [<CustomOperation("version")>]
    member _.Version(config: StackConfig, value: string) : StackConfig = { config with Version = Some value }

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (config: StackConfig) (operation: Operation) : unit =
        match operation with
        | TableOp tableSpec -> Table(stack, tableSpec.ConstructId, tableSpec.Props) |> ignore

        | FunctionOp lambdaSpec ->
            let fn = Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)

            for action in lambdaSpec.Actions do
                action fn

        | DockerImageFunctionOp imageLambdaSpec ->
            // Create code lazily to avoid JSII side effects during spec construction
            imageLambdaSpec.Props.Code <- DockerImageCode.FromImageAsset(imageLambdaSpec.Code)
            // Apply deferred timeout to avoid jsii in tests
            if imageLambdaSpec.TimeoutSeconds.HasValue then
                imageLambdaSpec.Props.Timeout <- Duration.Seconds(imageLambdaSpec.TimeoutSeconds.Value)

            DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)
            |> ignore

        | GrantOp grantSpec -> Grants.processGrant stack grantSpec

        | TopicOp topicSpec -> Topic(stack, topicSpec.ConstructId, topicSpec.Props) |> ignore

        | QueueOp queueSpec ->
            // Build QueueProps from spec (convert primitives to Duration etc.)
            let props = QueueProps()
            props.QueueName <- queueSpec.QueueName

            queueSpec.VisibilityTimeout
            |> Option.iter (fun v -> props.VisibilityTimeout <- Duration.Seconds(v))

            queueSpec.MessageRetention
            |> Option.iter (fun r -> props.RetentionPeriod <- Duration.Seconds(r))

            queueSpec.FifoQueue |> Option.iter (fun f -> props.Fifo <- f)

            queueSpec.ContentBasedDeduplication
            |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

            queueSpec.DelaySeconds
            |> Option.iter (fun d -> props.DeliveryDelay <- Duration.Seconds(float d))

            match queueSpec.DeadLetterQueueName, queueSpec.MaxReceiveCount with
            | Some dlqName, Some maxReceive ->
                try
                    let dlq = stack.Node.FindChild(dlqName) :?> Queue
                    let dlqSpec = DeadLetterQueue(Queue = dlq, MaxReceiveCount = maxReceive)
                    props.DeadLetterQueue <- dlqSpec
                with ex ->
                    printfn $"Warning: Could not configure DLQ for queue %s{queueSpec.QueueName}: %s{ex.Message}"
            | _ -> ()

            Queue(stack, queueSpec.ConstructId, props) |> ignore

        | BucketOp bucketSpec -> Bucket(stack, bucketSpec.ConstructId, bucketSpec.Props) |> ignore

        | SubscriptionOp subscriptionSpec -> SNS.processSubscription stack subscriptionSpec
