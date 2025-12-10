namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SNS.Subscriptions
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SQS
open System.Collections.Generic

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
      Subscriptions: ITopicSubscription list
      TracingConfig: TracingConfig option }

type TopicSpec =
    { TopicName: string
      ConstructId: string
      Subscriptions: ITopicSubscription list
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
          Subscriptions = []
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
          Subscriptions = []
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
          Subscriptions = state2.Subscriptions @ state1.Subscriptions
          TracingConfig = state2.TracingConfig |> Option.orElse state1.TracingConfig }

    member inline x.For(config: TopicConfig, [<InlineIfLambda>] f: unit -> TopicConfig) : TopicConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    /// <summary>Yields a subscription to be added to the topic (implicit yield).</summary>
    /// <param name="subscription">The subscription to add.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     lambdaSubscription { handler myFunction }
    ///     sqsSubscription { queue myQueue }
    /// }
    /// </code>
    member _.Yield(subscription: ITopicSubscription) : TopicConfig =
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
          Subscriptions = [ subscription ]
          TracingConfig = None }

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
          Subscriptions = config.Subscriptions
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

    /// <summary>Adds a subscription to the topic.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="subscription">The subscription to add.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     subscription (lambdaSubscription {
    ///         handler myLambdaFunction
    ///     })
    /// }
    /// </code>
    [<CustomOperation("subscription")>]
    member _.Subscription(config: TopicConfig, subscription: ITopicSubscription) =
        { config with
            Subscriptions = subscription :: config.Subscriptions }

    /// <summary>Adds multiple subscriptions to the topic.</summary>
    /// <param name="config">The topic configuration.</param>
    /// <param name="subscriptions">The subscriptions to add.</param>
    /// <code lang="fsharp">
    /// topic "MyTopic" {
    ///     subscriptions [
    ///         lambdaSubscription { handler myFunction }
    ///         sqsSubscription { queue myQueue }
    ///     ]
    /// }
    /// </code>
    [<CustomOperation("subscriptions")>]
    member _.Subscriptions(config: TopicConfig, subscriptions: ITopicSubscription list) =
        { config with
            Subscriptions = subscriptions @ config.Subscriptions }

// ============================================================================
// Lambda Subscription Builder
// ============================================================================

type LambdaSubscriptionConfig =
    { Function: IFunction option
      DeadLetterQueue: IQueue option
      FilterPolicy: IDictionary<string, SubscriptionFilter> option
      FilterPolicyWithMessageBody: IDictionary<string, FilterOrPolicy> option }

type LambdaSubscriptionBuilder() =
    member _.Yield(_: unit) : LambdaSubscriptionConfig =
        { Function = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None }

    member _.Zero() : LambdaSubscriptionConfig =
        { Function = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> LambdaSubscriptionConfig) : LambdaSubscriptionConfig = f ()

    member _.Combine(state1: LambdaSubscriptionConfig, state2: LambdaSubscriptionConfig) : LambdaSubscriptionConfig =
        { Function = state2.Function |> Option.orElse state1.Function
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          FilterPolicyWithMessageBody =
            state2.FilterPolicyWithMessageBody
            |> Option.orElse state1.FilterPolicyWithMessageBody }

    member inline x.For
        (
            config: LambdaSubscriptionConfig,
            [<InlineIfLambda>] f: unit -> LambdaSubscriptionConfig
        ) : LambdaSubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: LambdaSubscriptionConfig) : ITopicSubscription =
        let fn =
            config.Function
            |> Option.defaultWith (fun () -> failwith "Lambda subscription must specify a function using 'handler'")

        let props = LambdaSubscriptionProps()
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)
        config.FilterPolicy |> Option.iter (fun p -> props.FilterPolicy <- p)

        config.FilterPolicyWithMessageBody
        |> Option.iter (fun p -> props.FilterPolicyWithMessageBody <- p)

        LambdaSubscription(fn, props)

    /// <summary>Sets the Lambda function for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="func">The Lambda function to subscribe.</param>
    /// <code lang="fsharp">
    /// lambdaSubscription {
    ///     handler myLambdaFunction
    /// }
    /// </code>
    [<CustomOperation("handler")>]
    member _.Handler(config: LambdaSubscriptionConfig, func: IFunction) = { config with Function = Some func }

    /// <summary>Sets a dead-letter queue for failed messages.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="queue">The SQS queue to use as DLQ.</param>
    /// <code lang="fsharp">
    /// lambdaSubscription {
    ///     fn myFunction
    ///     deadLetterQueue myDlqQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: LambdaSubscriptionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy dictionary.</param>
    /// <code lang="fsharp">
    /// lambdaSubscription {
    ///     fn myFunction
    ///     filterPolicy (dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order" |])) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: LambdaSubscriptionConfig, policy: IDictionary<string, SubscriptionFilter>) =
        { config with
            FilterPolicy = Some policy }

    /// <summary>Sets a filter policy with message body for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy with message body dictionary.</param>
    /// <code lang="fsharp">
    /// lambdaSubscription {
    ///     fn myFunction
    ///     filterPolicyWithMessageBody (dict [ "body", FilterOrPolicy.Filter(...) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicyWithMessageBody")>]
    member _.FilterPolicyWithMessageBody
        (
            config: LambdaSubscriptionConfig,
            policy: IDictionary<string, FilterOrPolicy>
        ) =
        { config with
            FilterPolicyWithMessageBody = Some policy }

// ============================================================================
// SQS Subscription Builder
// ============================================================================

type SqsSubscriptionConfig =
    { Queue: IQueue option
      DeadLetterQueue: IQueue option
      FilterPolicy: IDictionary<string, SubscriptionFilter> option
      FilterPolicyWithMessageBody: IDictionary<string, FilterOrPolicy> option
      RawMessageDelivery: bool option }

type SqsSubscriptionBuilder() =
    member _.Yield(_: unit) : SqsSubscriptionConfig =
        { Queue = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          RawMessageDelivery = None }

    member _.Zero() : SqsSubscriptionConfig =
        { Queue = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          RawMessageDelivery = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SqsSubscriptionConfig) : SqsSubscriptionConfig = f ()

    member _.Combine(state1: SqsSubscriptionConfig, state2: SqsSubscriptionConfig) : SqsSubscriptionConfig =
        { Queue = state2.Queue |> Option.orElse state1.Queue
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          FilterPolicyWithMessageBody =
            state2.FilterPolicyWithMessageBody
            |> Option.orElse state1.FilterPolicyWithMessageBody
          RawMessageDelivery = state2.RawMessageDelivery |> Option.orElse state1.RawMessageDelivery }

    member inline x.For
        (
            config: SqsSubscriptionConfig,
            [<InlineIfLambda>] f: unit -> SqsSubscriptionConfig
        ) : SqsSubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SqsSubscriptionConfig) : ITopicSubscription =
        let queue =
            config.Queue
            |> Option.defaultWith (fun () -> failwith "SQS subscription must specify a queue using 'queue'")

        let props = SqsSubscriptionProps()
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)
        config.FilterPolicy |> Option.iter (fun p -> props.FilterPolicy <- p)

        config.FilterPolicyWithMessageBody
        |> Option.iter (fun p -> props.FilterPolicyWithMessageBody <- p)

        config.RawMessageDelivery
        |> Option.iter (fun r -> props.RawMessageDelivery <- r)

        SqsSubscription(queue, props)

    /// <summary>Sets the SQS queue for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="q">The SQS queue to subscribe.</param>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue mySqsQueue
    /// }
    /// </code>
    [<CustomOperation("queue")>]
    member _.Queue(config: SqsSubscriptionConfig, q: IQueue) = { config with Queue = Some q }

    /// <summary>Sets a dead-letter queue for failed messages.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="q">The SQS queue to use as DLQ.</param>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue myQueue
    ///     deadLetterQueue myDlqQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: SqsSubscriptionConfig, q: IQueue) =
        { config with DeadLetterQueue = Some q }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy dictionary.</param>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue myQueue
    ///     filterPolicy (dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order" |])) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: SqsSubscriptionConfig, policy: IDictionary<string, SubscriptionFilter>) =
        { config with
            FilterPolicy = Some policy }

    /// <summary>Sets a filter policy with message body for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy with message body dictionary.</param>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue myQueue
    ///     filterPolicyWithMessageBody (dict [ "body", FilterOrPolicy.Filter(...) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicyWithMessageBody")>]
    member _.FilterPolicyWithMessageBody(config: SqsSubscriptionConfig, policy: IDictionary<string, FilterOrPolicy>) =
        { config with
            FilterPolicyWithMessageBody = Some policy }

    /// <summary>Enables raw message delivery (without SNS metadata).</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="enabled">Whether to enable raw message delivery.</param>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue myQueue
    ///     rawMessageDelivery true
    /// }
    /// </code>
    [<CustomOperation("rawMessageDelivery")>]
    member _.RawMessageDelivery(config: SqsSubscriptionConfig, enabled: bool) =
        { config with
            RawMessageDelivery = Some enabled }

// ============================================================================
// Email Subscription Builder
// ============================================================================

type EmailSubscriptionConfig =
    { EmailAddress: string option
      DeadLetterQueue: IQueue option
      FilterPolicy: IDictionary<string, SubscriptionFilter> option
      FilterPolicyWithMessageBody: IDictionary<string, FilterOrPolicy> option
      Json: bool option }

type EmailSubscriptionBuilder() =
    member _.Yield(_: unit) : EmailSubscriptionConfig =
        { EmailAddress = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          Json = None }

    member _.Zero() : EmailSubscriptionConfig =
        { EmailAddress = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          Json = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> EmailSubscriptionConfig) : EmailSubscriptionConfig = f ()

    member _.Combine(state1: EmailSubscriptionConfig, state2: EmailSubscriptionConfig) : EmailSubscriptionConfig =
        { EmailAddress = state2.EmailAddress |> Option.orElse state1.EmailAddress
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          FilterPolicyWithMessageBody =
            state2.FilterPolicyWithMessageBody
            |> Option.orElse state1.FilterPolicyWithMessageBody
          Json = state2.Json |> Option.orElse state1.Json }

    member inline x.For
        (
            config: EmailSubscriptionConfig,
            [<InlineIfLambda>] f: unit -> EmailSubscriptionConfig
        ) : EmailSubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: EmailSubscriptionConfig) : ITopicSubscription =
        let email =
            config.EmailAddress
            |> Option.defaultWith (fun () -> failwith "Email subscription must specify an email address using 'email'")

        let props = EmailSubscriptionProps()
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)
        config.FilterPolicy |> Option.iter (fun p -> props.FilterPolicy <- p)

        config.FilterPolicyWithMessageBody
        |> Option.iter (fun p -> props.FilterPolicyWithMessageBody <- p)

        config.Json |> Option.iter (fun j -> props.Json <- j)

        EmailSubscription(email, props)

    /// <summary>Sets the email address for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="address">The email address to subscribe.</param>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    /// }
    /// </code>
    [<CustomOperation("email")>]
    member _.Email(config: EmailSubscriptionConfig, address: string) =
        { config with
            EmailAddress = Some address }

    /// <summary>Sets a dead-letter queue for failed messages.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="queue">The SQS queue to use as DLQ.</param>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    ///     deadLetterQueue myDlqQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: EmailSubscriptionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy dictionary.</param>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    ///     filterPolicy (dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "alert" |])) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: EmailSubscriptionConfig, policy: IDictionary<string, SubscriptionFilter>) =
        { config with
            FilterPolicy = Some policy }

    /// <summary>Sets a filter policy with message body for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy with message body dictionary.</param>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    ///     filterPolicyWithMessageBody (dict [ "body", FilterOrPolicy.Filter(...) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicyWithMessageBody")>]
    member _.FilterPolicyWithMessageBody(config: EmailSubscriptionConfig, policy: IDictionary<string, FilterOrPolicy>) =
        { config with
            FilterPolicyWithMessageBody = Some policy }

    /// <summary>Sends the full notification JSON to the email address.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="enabled">Whether to send JSON format.</param>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    ///     json true
    /// }
    /// </code>
    [<CustomOperation("json")>]
    member _.Json(config: EmailSubscriptionConfig, enabled: bool) = { config with Json = Some enabled }

// ============================================================================
// SMS Subscription Builder
// ============================================================================

type SmsSubscriptionConfig =
    { PhoneNumber: string option
      DeadLetterQueue: IQueue option
      FilterPolicy: IDictionary<string, SubscriptionFilter> option
      FilterPolicyWithMessageBody: IDictionary<string, FilterOrPolicy> option }

type SmsSubscriptionBuilder() =
    member _.Yield(_: unit) : SmsSubscriptionConfig =
        { PhoneNumber = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None }

    member _.Zero() : SmsSubscriptionConfig =
        { PhoneNumber = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> SmsSubscriptionConfig) : SmsSubscriptionConfig = f ()

    member _.Combine(state1: SmsSubscriptionConfig, state2: SmsSubscriptionConfig) : SmsSubscriptionConfig =
        { PhoneNumber = state2.PhoneNumber |> Option.orElse state1.PhoneNumber
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          FilterPolicyWithMessageBody =
            state2.FilterPolicyWithMessageBody
            |> Option.orElse state1.FilterPolicyWithMessageBody }

    member inline x.For
        (
            config: SmsSubscriptionConfig,
            [<InlineIfLambda>] f: unit -> SmsSubscriptionConfig
        ) : SmsSubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: SmsSubscriptionConfig) : ITopicSubscription =
        let phone =
            config.PhoneNumber
            |> Option.defaultWith (fun () ->
                failwith "SMS subscription must specify a phone number using 'phoneNumber'")

        let props = SmsSubscriptionProps()
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)
        config.FilterPolicy |> Option.iter (fun p -> props.FilterPolicy <- p)

        config.FilterPolicyWithMessageBody
        |> Option.iter (fun p -> props.FilterPolicyWithMessageBody <- p)

        SmsSubscription(phone, props)

    /// <summary>Sets the phone number for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="number">The phone number to subscribe (E.164 format recommended).</param>
    /// <code lang="fsharp">
    /// smsSubscription {
    ///     phoneNumber "+1234567890"
    /// }
    /// </code>
    [<CustomOperation("phoneNumber")>]
    member _.PhoneNumber(config: SmsSubscriptionConfig, number: string) =
        { config with
            PhoneNumber = Some number }

    /// <summary>Sets a dead-letter queue for failed messages.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="queue">The SQS queue to use as DLQ.</param>
    /// <code lang="fsharp">
    /// smsSubscription {
    ///     phoneNumber "+1234567890"
    ///     deadLetterQueue myDlqQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: SmsSubscriptionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy dictionary.</param>
    /// <code lang="fsharp">
    /// smsSubscription {
    ///     phoneNumber "+1234567890"
    ///     filterPolicy (dict [ "priority", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "high" |])) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: SmsSubscriptionConfig, policy: IDictionary<string, SubscriptionFilter>) =
        { config with
            FilterPolicy = Some policy }

    /// <summary>Sets a filter policy with message body for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy with message body dictionary.</param>
    /// <code lang="fsharp">
    /// smsSubscription {
    ///     phoneNumber "+1234567890"
    ///     filterPolicyWithMessageBody (dict [ "body", FilterOrPolicy.Filter(...) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicyWithMessageBody")>]
    member _.FilterPolicyWithMessageBody(config: SmsSubscriptionConfig, policy: IDictionary<string, FilterOrPolicy>) =
        { config with
            FilterPolicyWithMessageBody = Some policy }

// ============================================================================
// URL Subscription Builder
// ============================================================================

type UrlSubscriptionConfig =
    { Url: string option
      DeadLetterQueue: IQueue option
      FilterPolicy: IDictionary<string, SubscriptionFilter> option
      FilterPolicyWithMessageBody: IDictionary<string, FilterOrPolicy> option
      RawMessageDelivery: bool option
      Protocol: SubscriptionProtocol option }

type UrlSubscriptionBuilder() =
    member _.Yield(_: unit) : UrlSubscriptionConfig =
        { Url = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          RawMessageDelivery = None
          Protocol = None }

    member _.Zero() : UrlSubscriptionConfig =
        { Url = None
          DeadLetterQueue = None
          FilterPolicy = None
          FilterPolicyWithMessageBody = None
          RawMessageDelivery = None
          Protocol = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> UrlSubscriptionConfig) : UrlSubscriptionConfig = f ()

    member _.Combine(state1: UrlSubscriptionConfig, state2: UrlSubscriptionConfig) : UrlSubscriptionConfig =
        { Url = state2.Url |> Option.orElse state1.Url
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          FilterPolicy = state2.FilterPolicy |> Option.orElse state1.FilterPolicy
          FilterPolicyWithMessageBody =
            state2.FilterPolicyWithMessageBody
            |> Option.orElse state1.FilterPolicyWithMessageBody
          RawMessageDelivery = state2.RawMessageDelivery |> Option.orElse state1.RawMessageDelivery
          Protocol = state2.Protocol |> Option.orElse state1.Protocol }

    member inline x.For
        (
            config: UrlSubscriptionConfig,
            [<InlineIfLambda>] f: unit -> UrlSubscriptionConfig
        ) : UrlSubscriptionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: UrlSubscriptionConfig) : ITopicSubscription =
        let url =
            config.Url
            |> Option.defaultWith (fun () -> failwith "URL subscription must specify a URL using 'url'")

        let props = UrlSubscriptionProps()
        config.DeadLetterQueue |> Option.iter (fun q -> props.DeadLetterQueue <- q)
        config.FilterPolicy |> Option.iter (fun p -> props.FilterPolicy <- p)

        config.FilterPolicyWithMessageBody
        |> Option.iter (fun p -> props.FilterPolicyWithMessageBody <- p)

        config.RawMessageDelivery
        |> Option.iter (fun r -> props.RawMessageDelivery <- r)

        config.Protocol |> Option.iter (fun p -> props.Protocol <- p)

        UrlSubscription(url, props)

    /// <summary>Sets the URL endpoint for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="endpoint">The HTTP/HTTPS URL to subscribe.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    /// }
    /// </code>
    [<CustomOperation("url")>]
    member _.Url(config: UrlSubscriptionConfig, endpoint: string) = { config with Url = Some endpoint }

    /// <summary>Sets a dead-letter queue for failed messages.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="queue">The SQS queue to use as DLQ.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     deadLetterQueue myDlqQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: UrlSubscriptionConfig, queue: IQueue) =
        { config with
            DeadLetterQueue = Some queue }

    /// <summary>Sets a filter policy for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy dictionary.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     filterPolicy (dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order" |])) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicy")>]
    member _.FilterPolicy(config: UrlSubscriptionConfig, policy: IDictionary<string, SubscriptionFilter>) =
        { config with
            FilterPolicy = Some policy }

    /// <summary>Sets a filter policy with message body for the subscription.</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="policy">The filter policy with message body dictionary.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     filterPolicyWithMessageBody (dict [ "body", FilterOrPolicy.Filter(...) ])
    /// }
    /// </code>
    [<CustomOperation("filterPolicyWithMessageBody")>]
    member _.FilterPolicyWithMessageBody(config: UrlSubscriptionConfig, policy: IDictionary<string, FilterOrPolicy>) =
        { config with
            FilterPolicyWithMessageBody = Some policy }

    /// <summary>Enables raw message delivery (without SNS metadata).</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="enabled">Whether to enable raw message delivery.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     rawMessageDelivery true
    /// }
    /// </code>
    [<CustomOperation("rawMessageDelivery")>]
    member _.RawMessageDelivery(config: UrlSubscriptionConfig, enabled: bool) =
        { config with
            RawMessageDelivery = Some enabled }

    /// <summary>Sets the protocol for the subscription (HTTP or HTTPS).</summary>
    /// <param name="config">The subscription configuration.</param>
    /// <param name="proto">The subscription protocol.</param>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     protocol SubscriptionProtocol.HTTPS
    /// }
    /// </code>
    [<CustomOperation("protocol")>]
    member _.Protocol(config: UrlSubscriptionConfig, proto: SubscriptionProtocol) =
        { config with Protocol = Some proto }

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

    /// <summary>Creates a Lambda subscription configuration.</summary>
    /// <code lang="fsharp">
    /// lambdaSubscription {
    ///     handler myLambdaFunction
    ///     filterPolicy (dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order" |])) ])
    /// }
    /// </code>
    let lambdaSubscription = LambdaSubscriptionBuilder()

    /// <summary>Creates an SQS subscription configuration.</summary>
    /// <code lang="fsharp">
    /// sqsSubscription {
    ///     queue mySqsQueue
    ///     rawMessageDelivery true
    /// }
    /// </code>
    let sqsSubscription = SqsSubscriptionBuilder()

    /// <summary>Creates an email subscription configuration.</summary>
    /// <code lang="fsharp">
    /// emailSubscription {
    ///     email "admin@example.com"
    ///     json true
    /// }
    /// </code>
    let emailSubscription = EmailSubscriptionBuilder()

    /// <summary>Creates an SMS subscription configuration.</summary>
    /// <code lang="fsharp">
    /// smsSubscription {
    ///     phoneNumber "+1234567890"
    /// }
    /// </code>
    let smsSubscription = SmsSubscriptionBuilder()

    /// <summary>Creates a URL subscription configuration.</summary>
    /// <code lang="fsharp">
    /// urlSubscription {
    ///     url "https://example.com/webhook"
    ///     rawMessageDelivery true
    /// }
    /// </code>
    let urlSubscription = UrlSubscriptionBuilder()
