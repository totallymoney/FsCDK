namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SNS.Subscriptions
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SQS
open Constructs

// ============================================================================
// SNS Topic and Subscription Configuration DSL
// ============================================================================

// SNS Topic configuration DSL
type TopicConfig =
    { TopicName: string
      ConstructId: string option // Optional custom construct ID
      DisplayName: string option
      FifoTopic: bool option
      ContentBasedDeduplication: bool option }

type TopicSpec =
    { TopicName: string
      ConstructId: string // Construct ID for CDK
      Props: TopicProps }

type TopicBuilder(name: string) =
    member _.Yield _ : TopicConfig =
        { TopicName = name
          ConstructId = None
          DisplayName = None
          FifoTopic = None
          ContentBasedDeduplication = None }

    member _.Zero() : TopicConfig =
        { TopicName = name
          ConstructId = None
          DisplayName = None
          FifoTopic = None
          ContentBasedDeduplication = None }

    member _.Delay(f: unit -> TopicConfig) : TopicConfig = f ()

    member _.Combine(state1: TopicConfig, state2: TopicConfig) : TopicConfig =
        { TopicName = state1.TopicName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          DisplayName = state2.DisplayName |> Option.orElse state1.DisplayName
          FifoTopic = state2.FifoTopic |> Option.orElse state1.FifoTopic
          ContentBasedDeduplication =
            state2.ContentBasedDeduplication
            |> Option.orElse state1.ContentBasedDeduplication }

    member x.For(config: TopicConfig, f: unit -> TopicConfig) : TopicConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: TopicConfig) : TopicSpec =
        // Topic name is required
        let topicName = config.TopicName
        // Construct ID defaults to topic name if not specified
        let constructId = config.ConstructId |> Option.defaultValue topicName

        let props = TopicProps()

        // Set topic name
        props.TopicName <- topicName

        // Set optional properties
        config.DisplayName |> Option.iter (fun d -> props.DisplayName <- d)
        config.FifoTopic |> Option.iter (fun f -> props.Fifo <- f)

        config.ContentBasedDeduplication
        |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

        { TopicName = topicName
          ConstructId = constructId
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TopicConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("displayName")>]
    member _.DisplayName(config: TopicConfig, displayName: string) =
        { config with
            DisplayName = Some displayName }

    [<CustomOperation("fifo")>]
    member _.Fifo(config: TopicConfig, isFifo: bool) = { config with FifoTopic = Some isFifo }

    [<CustomOperation("contentBasedDeduplication")>]
    member _.ContentBasedDeduplication(config: TopicConfig, enabled: bool) =
        { config with
            ContentBasedDeduplication = Some enabled }

// SNS Subscription types
type SubscriptionEndpointType =
    | LambdaEndpoint of string // Lambda construct ID
    | QueueEndpoint of string // Queue construct ID
    | EmailEndpoint of string // Email address
    | SmsEndpoint of string // Phone number
    | HttpEndpoint of string // HTTP URL
    | HttpsEndpoint of string // HTTPS URL

// SNS Subscription configuration
type SubscriptionConfig =
    { TopicConstructId: string option
      Endpoint: SubscriptionEndpointType option
      FilterPolicy: Map<string, obj> option
      DeadLetterQueue: string option } // Queue construct ID for subscription DLQ

type SubscriptionSpec =
    { TopicConstructId: string
      Endpoint: SubscriptionEndpointType
      FilterPolicy: Map<string, obj> option
      DeadLetterQueue: string option }

type SubscriptionBuilder() =
    member _.Yield _ : SubscriptionConfig =
        { TopicConstructId = None
          Endpoint = None
          FilterPolicy = None
          DeadLetterQueue = None }

    member _.Zero() : SubscriptionConfig =
        { TopicConstructId = None
          Endpoint = None
          FilterPolicy = None
          DeadLetterQueue = None }

    member _.Delay(f: unit -> SubscriptionConfig) : SubscriptionConfig = f ()

    member _.Combine(state1: SubscriptionConfig, state2: SubscriptionConfig) : SubscriptionConfig =
        { TopicConstructId = state2.TopicConstructId |> Option.orElse state1.TopicConstructId
          Endpoint = state2.Endpoint |> Option.orElse state1.Endpoint
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue }

    member x.For(config: SubscriptionConfig, f: unit -> SubscriptionConfig) : SubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SubscriptionConfig) : SubscriptionSpec =
        match config.TopicConstructId, config.Endpoint with
        | Some t, Some e ->
            { TopicConstructId = t
              Endpoint = e
              FilterPolicy = config.FilterPolicy
              DeadLetterQueue = config.DeadLetterQueue }
        | _ -> failwith "Subscription must specify topic and endpoint"

    [<CustomOperation("topic")>]
    member _.Topic(config: SubscriptionConfig, topicConstructId: string) =
        { config with
            TopicConstructId = Some topicConstructId }

    [<CustomOperation("lambda")>]
    member _.Lambda(config: SubscriptionConfig, lambdaConstructId: string) =
        { config with
            Endpoint = Some(LambdaEndpoint lambdaConstructId) }

    [<CustomOperation("queue")>]
    member _.Queue(config: SubscriptionConfig, queueConstructId: string) =
        { config with
            Endpoint = Some(QueueEndpoint queueConstructId) }

    [<CustomOperation("email")>]
    member _.Email(config: SubscriptionConfig, emailAddress: string) =
        { config with
            Endpoint = Some(EmailEndpoint emailAddress) }

    [<CustomOperation("sms")>]
    member _.Sms(config: SubscriptionConfig, phoneNumber: string) =
        { config with
            Endpoint = Some(SmsEndpoint phoneNumber) }

    [<CustomOperation("http")>]
    member _.Http(config: SubscriptionConfig, url: string) =
        { config with
            Endpoint = Some(HttpEndpoint url) }

    [<CustomOperation("https")>]
    member _.Https(config: SubscriptionConfig, url: string) =
        { config with
            Endpoint = Some(HttpsEndpoint url) }

    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: SubscriptionConfig, policy: (string * obj) list) =
        { config with
            FilterPolicy = Some(policy |> Map.ofList) }

    [<CustomOperation("subscriptionDeadLetterQueue")>]
    member _.SubscriptionDeadLetterQueue(config: SubscriptionConfig, queueConstructId: string) =
        { config with
            DeadLetterQueue = Some queueConstructId }

module SNS =
    // Subscription processing function for Stack builder
    let processSubscription (stack: Stack) (subscriptionSpec: SubscriptionSpec) =
        try
            // Find the topic
            let topic = stack.Node.FindChild(subscriptionSpec.TopicConstructId) :?> Topic

            // Create subscription based on endpoint type
            match subscriptionSpec.Endpoint with
            | LambdaEndpoint lambdaId ->
                let lambda = stack.Node.FindChild(lambdaId) :?> Function
                topic.AddSubscription(LambdaSubscription(lambda)) |> ignore
            | QueueEndpoint queueId ->
                let queue = stack.Node.FindChild(queueId) :?> Queue
                topic.AddSubscription(SqsSubscription(queue)) |> ignore
            | EmailEndpoint email -> topic.AddSubscription(EmailSubscription(email)) |> ignore
            | SmsEndpoint phone -> topic.AddSubscription(SmsSubscription(phone)) |> ignore
            | HttpEndpoint url -> topic.AddSubscription(UrlSubscription(url)) |> ignore
            | HttpsEndpoint url -> topic.AddSubscription(UrlSubscription(url)) |> ignore

            // Apply filter policy if specified
            // Note: Filter policy configuration would need to be handled via subscription props
            ()
        with ex ->
            printfn $"Warning: Failed to create subscription: %s{ex.Message}"
