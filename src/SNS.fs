namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SNS.Subscriptions
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SQS
// ============================================================================
// SNS Topic and Subscription Configuration DSL
// ============================================================================

// SNS Topic configuration DSL
type TopicConfig =
    { TopicName: string
      ConstructId: string option // Optional custom construct ID
      DisplayName: string option
      FifoTopic: bool option
      ContentBasedDeduplication: bool option
      EnforceSSL: bool option
      FifoThroughputScope: FifoThroughputScope option
      LoggingConfigs: ILoggingConfig list
      MasterKey: Amazon.CDK.AWS.KMS.IKey option
      MessageRetentionPeriodInDays: float option
      SignatureVersion: string option
      TracingConfig: TracingConfig option }

type TopicSpec =
    { TopicName: string
      ConstructId: string // Construct ID for CDK
      Props: TopicProps
      mutable Topic: ITopic option }

type TopicBuilder(name: string) =
    member _.Yield(_: unit) : TopicConfig =
        { TopicName = name
          ConstructId = None
          DisplayName = None
          FifoTopic = None
          ContentBasedDeduplication = None
          EnforceSSL = None
          FifoThroughputScope = None
          LoggingConfigs = []
          MasterKey = None
          MessageRetentionPeriodInDays = None
          SignatureVersion = None
          TracingConfig = None }

    member _.Zero() : TopicConfig =
        { TopicName = name
          ConstructId = None
          DisplayName = None
          FifoTopic = None
          ContentBasedDeduplication = None
          EnforceSSL = None
          FifoThroughputScope = None
          LoggingConfigs = []
          MasterKey = None
          MessageRetentionPeriodInDays = None
          SignatureVersion = None
          TracingConfig = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> TopicConfig) : TopicConfig = f ()

    member _.Combine(state1: TopicConfig, state2: TopicConfig) : TopicConfig =
        { TopicName = state1.TopicName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          DisplayName = state2.DisplayName |> Option.orElse state1.DisplayName
          FifoTopic = state2.FifoTopic |> Option.orElse state1.FifoTopic
          ContentBasedDeduplication =
            state2.ContentBasedDeduplication
            |> Option.orElse state1.ContentBasedDeduplication
          EnforceSSL = state2.EnforceSSL |> Option.orElse state1.EnforceSSL
          FifoThroughputScope = state2.FifoThroughputScope |> Option.orElse state1.FifoThroughputScope
          LoggingConfigs =
            if state2.LoggingConfigs.IsEmpty then
                state1.LoggingConfigs
            else
                state2.LoggingConfigs
          MasterKey = state2.MasterKey |> Option.orElse state1.MasterKey
          MessageRetentionPeriodInDays =
            state2.MessageRetentionPeriodInDays
            |> Option.orElse state1.MessageRetentionPeriodInDays
          SignatureVersion = state2.SignatureVersion |> Option.orElse state1.SignatureVersion
          TracingConfig = state2.TracingConfig |> Option.orElse state1.TracingConfig }

    member inline x.For(config: TopicConfig, [<InlineIfLambda>] f: unit -> TopicConfig) : TopicConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: TopicConfig) : TopicSpec =
        let topicName = config.TopicName
        let constructId = config.ConstructId |> Option.defaultValue topicName

        let props = TopicProps()

        props.TopicName <- topicName

        config.DisplayName |> Option.iter (fun d -> props.DisplayName <- d)
        config.FifoTopic |> Option.iter (fun f -> props.Fifo <- f)

        config.ContentBasedDeduplication
        |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

        config.EnforceSSL |> Option.iter (fun e -> props.EnforceSSL <- e)

        config.FifoThroughputScope
        |> Option.iter (fun f -> props.FifoThroughputScope <- f)

        if not config.LoggingConfigs.IsEmpty then
            props.LoggingConfigs <- List.toArray config.LoggingConfigs

        config.MasterKey |> Option.iter (fun k -> props.MasterKey <- k)

        config.MessageRetentionPeriodInDays
        |> Option.iter (fun d -> props.MessageRetentionPeriodInDays <- d)

        config.SignatureVersion |> Option.iter (fun s -> props.SignatureVersion <- s)
        config.TracingConfig |> Option.iter (fun t -> props.TracingConfig <- t)

        { TopicName = topicName
          ConstructId = constructId
          Props = props
          Topic = None }

    /// <summary>Sets the construct ID for the topic.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     constructId "MyTopicConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: TopicConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the display name for the topic.</summary>
    /// <param name="displayName">The display name shown in email notifications.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     displayName "Order Notifications"
    /// }
    /// </code>
    [<CustomOperation("displayName")>]
    member _.DisplayName(config: TopicConfig, displayName: string) =
        { config with
            DisplayName = Some displayName }

    /// <summary>Configures the topic as a FIFO topic.</summary>
    /// <param name="isFifo">Whether the topic is FIFO.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic.fifo" {
    ///     fifo true
    /// }
    /// </code>
    [<CustomOperation("fifo")>]
    member _.Fifo(config: TopicConfig, isFifo: bool) = { config with FifoTopic = Some isFifo }

    /// <summary>Enables content-based deduplication for FIFO topics.</summary>
    /// <param name="enabled">Whether content-based deduplication is enabled.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic.fifo" {
    ///     fifo true
    ///     contentBasedDeduplication true
    /// }
    /// </code>
    [<CustomOperation("contentBasedDeduplication")>]
    member _.ContentBasedDeduplication(config: TopicConfig, enabled: bool) =
        { config with
            ContentBasedDeduplication = Some enabled }

    /// <summary>Enforces SSL/TLS for all topic communications.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="enforce">Whether to enforce SSL.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     enforceSSL true
    /// }
    /// </code>
    [<CustomOperation("enforceSSL")>]
    member _.EnforceSSL(config: TopicConfig, enforce: bool) =
        { config with
            EnforceSSL = Some enforce }

    /// <summary>Sets the throughput scope for FIFO topics.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="scope">The FIFO throughput scope (PerTopic or PerMessageGroupId).</param>
    /// <code lang="fsharp">
    /// topic "MyTopic.fifo" {
    ///     fifo true
    ///     fifoThroughputScope FifoThroughputScope.PerMessageGroupId
    /// }
    /// </code>
    [<CustomOperation("fifoThroughputScope")>]
    member _.FifoThroughputScope(config: TopicConfig, scope: FifoThroughputScope) =
        { config with
            FifoThroughputScope = Some scope }

    /// <summary>Configures logging for the topic.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="loggingConfigs">List of logging configurations.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     loggingConfigs [loggingConfig1; loggingConfig2]
    /// }
    /// </code>
    [<CustomOperation("loggingConfigs")>]
    member _.LoggingConfigs(config: TopicConfig, loggingConfigs: ILoggingConfig list) =
        { config with
            LoggingConfigs = loggingConfigs }

    /// <summary>Sets the KMS master key for topic encryption.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="key">The KMS key to use for encryption.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     masterKey myKmsKey
    /// }
    /// </code>
    [<CustomOperation("masterKey")>]
    member _.MasterKey(config: TopicConfig, key: Amazon.CDK.AWS.KMS.IKey) = { config with MasterKey = Some key }

    /// <summary>Sets the message retention period in days.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="days">Number of days to retain messages.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     messageRetentionPeriodInDays 7.0
    /// }
    /// </code>
    [<CustomOperation("messageRetentionPeriodInDays")>]
    member _.MessageRetentionPeriodInDays(config: TopicConfig, days: float) =
        { config with
            MessageRetentionPeriodInDays = Some days }

    /// <summary>Sets the signature version for message signing.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="version">The signature version (e.g., "1" or "2").</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     signatureVersion "2"
    /// }
    /// </code>
    [<CustomOperation("signatureVersion")>]
    member _.SignatureVersion(config: TopicConfig, version: string) =
        { config with
            SignatureVersion = Some version }

    /// <summary>Enables tracing configuration for the topic.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="tracingConfig">The tracing configuration (Active or PassThrough).</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     tracingConfig TracingConfig.ACTIVE
    /// }
    /// </code>
    [<CustomOperation("tracingConfig")>]
    member _.TracingConfig(config: TopicConfig, tracingConfig: TracingConfig) =
        { config with
            TracingConfig = Some tracingConfig }

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
    member _.Yield(_: unit) : SubscriptionConfig =
        { TopicConstructId = None
          Endpoint = None
          FilterPolicy = None
          DeadLetterQueue = None }

    member _.Zero() : SubscriptionConfig =
        { TopicConstructId = None
          Endpoint = None
          FilterPolicy = None
          DeadLetterQueue = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SubscriptionConfig) : SubscriptionConfig = f ()

    member _.Combine(state1: SubscriptionConfig, state2: SubscriptionConfig) : SubscriptionConfig =
        { TopicConstructId = state2.TopicConstructId |> Option.orElse state1.TopicConstructId
          Endpoint = state2.Endpoint |> Option.orElse state1.Endpoint
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue }

    member inline x.For
        (
            config: SubscriptionConfig,
            [<InlineIfLambda>] f: unit -> SubscriptionConfig
        ) : SubscriptionConfig =
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

    /// <summary>Sets the SNS topic for the subscription.</summary>
    /// <param name="topicConstructId">The construct ID of the topic.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     lambda "MyFunction"
    /// }
    /// </code>
    [<CustomOperation("topic")>]
    member _.Topic(config: SubscriptionConfig, topicConstructId: string) =
        { config with
            TopicConstructId = Some topicConstructId }

    /// <summary>Subscribes a Lambda function to the topic.</summary>
    /// <param name="lambdaConstructId">The construct ID of the Lambda function.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     lambda "MyFunction"
    /// }
    /// </code>
    [<CustomOperation("lambda")>]
    member _.Lambda(config: SubscriptionConfig, lambdaConstructId: string) =
        { config with
            Endpoint = Some(LambdaEndpoint lambdaConstructId) }

    /// <summary>Subscribes an SQS queue to the topic.</summary>
    /// <param name="queueConstructId">The construct ID of the queue.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     queue "MyQueue"
    /// }
    /// </code>
    [<CustomOperation("queue")>]
    member _.Queue(config: SubscriptionConfig, queueConstructId: string) =
        { config with
            Endpoint = Some(QueueEndpoint queueConstructId) }

    /// <summary>Subscribes an email address to the topic.</summary>
    /// <param name="emailAddress">The email address.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     email "admin@example.com"
    /// }
    /// </code>
    [<CustomOperation("email")>]
    member _.Email(config: SubscriptionConfig, emailAddress: string) =
        { config with
            Endpoint = Some(EmailEndpoint emailAddress) }

    /// <summary>Subscribes a phone number for SMS to the topic.</summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     sms "+1234567890"
    /// }
    /// </code>
    [<CustomOperation("sms")>]
    member _.Sms(config: SubscriptionConfig, phoneNumber: string) =
        { config with
            Endpoint = Some(SmsEndpoint phoneNumber) }

    /// <summary>Subscribes an HTTP endpoint to the topic.</summary>
    /// <param name="url">The HTTP URL.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     http "http://example.com/webhook"
    /// }
    /// </code>
    [<CustomOperation("http")>]
    member _.Http(config: SubscriptionConfig, url: string) =
        { config with
            Endpoint = Some(HttpEndpoint url) }

    /// <summary>Subscribes an HTTPS endpoint to the topic.</summary>
    /// <param name="url">The HTTPS URL.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     https "https://example.com/webhook"
    /// }
    /// </code>
    [<CustomOperation("https")>]
    member _.Https(config: SubscriptionConfig, url: string) =
        { config with
            Endpoint = Some(HttpsEndpoint url) }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="policy">List of key-value pairs for the filter policy.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     lambda "MyFunction"
    ///     filterPolicy [ "eventType", "order"; "priority", "high" ]
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: SubscriptionConfig, policy: (string * obj) list) =
        { config with
            FilterPolicy = Some(policy |> Map.ofList) }

    /// <summary>Sets a dead-letter queue for the subscription.</summary>
    /// <param name="queueConstructId">The construct ID of the dead-letter queue.</param>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     lambda "MyFunction"
    ///     subscriptionDeadLetterQueue "MyDLQ"
    /// }
    /// </code>
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

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module SNSBuilders =
    /// <summary>Creates an SNS topic configuration.</summary>
    /// <param name="name">The topic name.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     displayName "My Notification Topic"
    ///     fifo true
    /// }
    /// </code>
    let topic name = TopicBuilder(name)

    /// <summary>Creates an SNS subscription configuration.</summary>
    /// <code lang="fsharp">
    /// subscription {
    ///     topic "MyTopic"
    ///     lambda "MyFunction"
    ///     filterPolicy [ "eventType", "order" ]
    /// }
    /// </code>
    let subscription = SubscriptionBuilder()
