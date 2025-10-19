namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.EC2

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
    | VpcOp of VpcSpec
    | SecurityGroupOp of SecurityGroupSpec

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (operation: Operation) : unit =
        match operation with
        | TableOp tableSpec -> Table(stack, tableSpec.ConstructId, tableSpec.Props) |> ignore

        | FunctionOp lambdaSpec ->
            let fn = Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)

            for action in lambdaSpec.Actions do
                action fn

        | DockerImageFunctionOp imageLambdaSpec ->
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

        | VpcOp vpcSpec -> Vpc(stack, vpcSpec.ConstructId, vpcSpec.Props) |> ignore

        | SecurityGroupOp sgSpec -> SecurityGroup(stack, sgSpec.ConstructId, sgSpec.Props) |> ignore


// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

type StackConfig =
    { Name: string
      App: App option
      Props: StackProps option
      Operations: Operation list }

type StackBuilder(name: string) =

    member _.Yield _ : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [] }

    member _.Yield(tableSpec: TableSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ TableOp tableSpec ] }

    member _.Yield(app: App) : StackConfig =
        { Name = name
          App = Some app
          Props = None
          Operations = [] }

    member _.Yield(funcSpec: FunctionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ FunctionOp funcSpec ] }

    member _.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ DockerImageFunctionOp dockerSpec ] }

    member _.Yield(grantSpec: GrantSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ GrantOp grantSpec ] }

    member _.Yield(topicSpec: TopicSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ TopicOp topicSpec ] }

    member _.Yield(queueSpec: QueueSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ QueueOp queueSpec ] }

    member _.Yield(bucketSpec: BucketSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ BucketOp bucketSpec ] }

    member _.Yield(subSpec: SubscriptionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ SubscriptionOp subSpec ] }

    member _.Yield(vpcSpec: VpcSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ VpcOp vpcSpec ] }

    member _.Yield(sgSpec: SecurityGroupSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ SecurityGroupOp sgSpec ] }

    member _.Yield(props: StackProps) : StackConfig =
        { Name = name
          App = None
          Props = Some props
          Operations = [] }

    member _.Zero() : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [] }

    member _.Combine(state1: StackConfig, state2: StackConfig) : StackConfig =
        { Name = state1.Name
          App = state1.App
          Props = if state1.Props.IsSome then state1.Props else state2.Props
          Operations = state1.Operations @ state2.Operations }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackConfig) : StackConfig = f ()

    member inline x.For(config: StackConfig, [<InlineIfLambda>] f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.Run(config: StackConfig) =
        let app = config.App |> Option.defaultWith (fun () -> App())

        let stack =
            match config.Props with
            | Some props -> Stack(app, name, props)
            | None -> Stack(app, name)

        for op in config.Operations do
            StackOperations.processOperation stack op

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module StackBuilders =
    /// <summary>Creates an AWS CDK Stack construct.</summary>
    /// <param name="name">The name of the stack.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     lambda myFunction
    ///     bucket myBucket
    /// }
    /// </code>
    let stack name = StackBuilder(name)
