namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SSM
// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

// Stack configuration
type StackConfig =
    { Name: string option
      Environment: string option
      Version: string option
      Props: StackProps option
      Operations: (Stack -> unit) seq }

type StackSpec =
    { Name: string
      Environment: string option
      Version: string option
      Props: StackProps option
      Operations: (Stack -> unit) seq }

type StackBuilder() =

    member _.Yield _ : StackConfig =
        { Name = None
          Environment = None
          Version = None
          Props = None
          Operations = [] }

    member _.Zero() : StackConfig =
        { Name = None
          Environment = None
          Version = None
          Props = None
          Operations = [] }

    member _.Run(config: StackConfig) : StackSpec =
        // Stack name is required
        let name =
            match config.Name with
            | Some n -> n
            | None -> failwith "Stack name is required"

        { Name = name
          Environment = config.Environment
          Version = config.Version
          Props = config.Props
          Operations = config.Operations }

    // Configuration operations
    [<CustomOperation("name")>]
    member _.Name(config: StackConfig, value: string) : StackConfig = { config with Name = Some value }

    [<CustomOperation("env")>]
    member _.Environment(config: StackConfig, value: string) : StackConfig =
        { config with Environment = Some value }

    [<CustomOperation("version")>]
    member _.Version(config: StackConfig, value: string) : StackConfig = { config with Version = Some value }

    [<CustomOperation("props")>]
    member _.Props(config: StackConfig, value: StackProps) : StackConfig = { config with Props = Some value }

    // Resource operations with nested DSL - using shorter names for cleaner syntax
    [<CustomOperation("addTable")>]
    member _.AddTable(config: StackConfig, tableSpec: TableSpec) : StackConfig =
        let op =
            fun (stack: Stack) ->
                // Use the specified construct ID
                Table(stack, tableSpec.ConstructId, tableSpec.Props) |> ignore

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("addLambda")>]
    member _.AddLambda(config: StackConfig, lambdaSpec: LambdaSpec) : StackConfig =
        let op =
            fun (stack: Stack) ->
                // Use the specified construct ID
                Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props) |> ignore

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("addDockerImageLambda")>]
    member _.AddDockerImageLambda(config: StackConfig, imageLambdaSpec: DockerImageFunctionSpec) : StackConfig =
        let op =
            fun (stack: Stack) ->
                // Create code lazily to avoid JSII side-effects during spec construction
                imageLambdaSpec.Props.Code <- DockerImageCode.FromImageAsset(imageLambdaSpec.Code)
                // Apply deferred timeout to avoid jsii in tests
                if imageLambdaSpec.TimeoutSeconds.HasValue then
                    imageLambdaSpec.Props.Timeout <- Duration.Seconds(imageLambdaSpec.TimeoutSeconds.Value)
                // Use the specified construct ID
                DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)
                |> ignore

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("addGrant")>]
    member _.AddGrant(config: StackConfig, grantSpec: GrantSpec) : StackConfig =
        let op = fun (stack: Stack) -> Grants.processGrant stack grantSpec

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("addTopic")>]
    member _.AddTopic(config: StackConfig, topicSpec: TopicSpec) : StackConfig =
        let op =
            fun (stack: Stack) ->
                // Use the specified construct ID
                Topic(stack, topicSpec.ConstructId, topicSpec.Props) |> ignore

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("addQueue")>]
    member _.AddQueue(config: StackConfig, queueSpec: QueueSpec) : StackConfig =
        let op = fun (stack: Stack) -> SQS.processQueue stack queueSpec

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("subscribe")>]
    member _.Subscribe(config: StackConfig, subscriptionSpec: SubscriptionSpec) : StackConfig =
        let op = fun (stack: Stack) -> SNS.processSubscription stack subscriptionSpec

        { config with
            Operations = Seq.toList config.Operations @ [ op ] }

    [<CustomOperation("datadog")>]
    member _.Datadog(config: StackConfig, enabled: bool) : StackConfig =
        if enabled then
            let op =
                fun (stack: Stack) ->
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

            // Alternative: Use the Aspect-based approach (also JSII-compatible now)
            // Aspects.Of(stack).Add(DatadogLogSubscriptionAspect(stack, ddForwarderArn))

            { config with
                Operations = Seq.toList config.Operations @ [ op ] }
        else
            config // Return unchanged config if Datadog is not enabled
