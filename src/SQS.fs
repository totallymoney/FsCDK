namespace FsCDK

// ============================================================================
// SQS Queue Configuration DSL
// ============================================================================

open Amazon.CDK
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.KMS

type DeadLetterConfig =
    { Queue: IQueue option
      MaxReceiveCount: int option }

type DeadLetterBuilder() =
    member _.Yield(_: unit) : DeadLetterConfig =
        { Queue = None; MaxReceiveCount = None }

    member _.Zero() : DeadLetterConfig =
        { Queue = None; MaxReceiveCount = None }

    member _.Run(config: DeadLetterConfig) : IDeadLetterQueue =
        let queue =
            config.Queue
            |> Option.defaultWith (fun () -> failwith "Queue must be specified using 'queue'")

        let maxReceiveCount = config.MaxReceiveCount |> Option.defaultValue 3

        DeadLetterQueue(Queue = queue, MaxReceiveCount = maxReceiveCount)

    member inline _.Delay([<InlineIfLambda>] f: unit -> DeadLetterConfig) : DeadLetterConfig = f ()

    member _.Combine(state1: DeadLetterConfig, state2: DeadLetterConfig) : DeadLetterConfig =
        { Queue = state1.Queue |> Option.orElse state2.Queue
          MaxReceiveCount = state1.MaxReceiveCount |> Option.orElse state2.MaxReceiveCount }

    member inline x.For(config: DeadLetterConfig, [<InlineIfLambda>] f: unit -> DeadLetterConfig) : DeadLetterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    /// <summary>Sets the queue that will receive dead letters.</summary>
    /// <param name="config">The dead-letter queue configuration.</param>
    /// <param name="queue">The queue to use as the dead-letter queue.</param>
    /// <code lang="fsharp">
    /// deadLetterQueue {
    ///     queue myDeadLetterQueue
    ///     maxReceiveCount 5
    /// }
    /// </code>
    [<CustomOperation("queue")>]
    member _.Queue(config: DeadLetterConfig, queue: IQueue) = { config with Queue = Some queue }

    /// <summary>Sets the maximum number of times a message can be delivered to the source queue before being moved to the dead-letter queue.</summary>
    /// <param name="config">The dead-letter queue configuration.</param>
    /// <param name="count">The maximum receive count.</param>
    /// <code lang="fsharp">
    /// deadLetterQueue {
    ///  maxReceiveCount 10
    /// }
    /// </code>
    [<CustomOperation("maxReceiveCount")>]
    member _.MaxReceiveCount(config: DeadLetterConfig, count: int) =
        { config with
            MaxReceiveCount = Some count }

// SQS Queue configuration DSL
type QueueConfig =
    { QueueName: string
      ConstructId: string option
      VisibilityTimeout: float option
      RetentionPeriod: float option
      ContentBasedDeduplication: bool option
      MaxReceiveCount: int option
      DeadLetterQueue: IDeadLetterQueue option
      DeduplicationScope: DeduplicationScope option
      DeliveryDelay: Duration option
      DataKeyReuse: Duration option
      Encryption: QueueEncryption option
      EnforceSSL: bool option
      Fifo: bool option
      FifoThroughputLimit: FifoThroughputLimit option
      MaxMessageSizeBytes: float option
      ReceiveMessageWaitTime: Duration option
      RedriveAllowPolicy: IRedriveAllowPolicy option
      RemovalPolicy: RemovalPolicy option
      EncryptionMasterKey: IKey option }

type QueueSpec =
    { QueueName: string
      ConstructId: string
      Props: QueueProps
      mutable Queue: IQueue option }

type QueueBuilder(name: string) =
    member _.Yield(_: unit) : QueueConfig =
        { QueueName = name
          ConstructId = None
          VisibilityTimeout = None
          RetentionPeriod = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueue = None
          DeduplicationScope = None
          DeliveryDelay = None
          DataKeyReuse = None
          Encryption = None
          EnforceSSL = None
          Fifo = None
          FifoThroughputLimit = None
          MaxMessageSizeBytes = None
          ReceiveMessageWaitTime = None
          RedriveAllowPolicy = None
          RemovalPolicy = None
          EncryptionMasterKey = None }

    member _.Zero() : QueueConfig =
        { QueueName = name
          ConstructId = None
          VisibilityTimeout = None
          RetentionPeriod = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueue = None
          DeduplicationScope = None
          DeliveryDelay = None
          DataKeyReuse = None
          Encryption = None
          EnforceSSL = None
          Fifo = None
          FifoThroughputLimit = None
          MaxMessageSizeBytes = None
          ReceiveMessageWaitTime = None
          RedriveAllowPolicy = None
          RemovalPolicy = None
          EncryptionMasterKey = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> QueueConfig) : QueueConfig = f ()

    member _.Combine(state1: QueueConfig, state2: QueueConfig) : QueueConfig =
        { QueueName = state1.QueueName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          VisibilityTimeout = state2.VisibilityTimeout |> Option.orElse state1.VisibilityTimeout
          RetentionPeriod = state2.RetentionPeriod |> Option.orElse state1.RetentionPeriod
          ContentBasedDeduplication =
            state2.ContentBasedDeduplication
            |> Option.orElse state1.ContentBasedDeduplication
          MaxReceiveCount = state2.MaxReceiveCount |> Option.orElse state1.MaxReceiveCount
          DeadLetterQueue = state2.DeadLetterQueue |> Option.orElse state1.DeadLetterQueue
          DeduplicationScope = state2.DeduplicationScope |> Option.orElse state1.DeduplicationScope
          DeliveryDelay = state2.DeliveryDelay |> Option.orElse state1.DeliveryDelay
          DataKeyReuse = state2.DataKeyReuse |> Option.orElse state1.DataKeyReuse
          Encryption = state2.Encryption |> Option.orElse state1.Encryption
          EnforceSSL = state2.EnforceSSL |> Option.orElse state1.EnforceSSL
          Fifo = state2.Fifo |> Option.orElse state1.Fifo
          FifoThroughputLimit = state2.FifoThroughputLimit |> Option.orElse state1.FifoThroughputLimit
          MaxMessageSizeBytes = state2.MaxMessageSizeBytes |> Option.orElse state1.MaxMessageSizeBytes
          ReceiveMessageWaitTime = state2.ReceiveMessageWaitTime |> Option.orElse state1.ReceiveMessageWaitTime
          RedriveAllowPolicy = state2.RedriveAllowPolicy |> Option.orElse state1.RedriveAllowPolicy
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          EncryptionMasterKey = state2.EncryptionMasterKey |> Option.orElse state1.EncryptionMasterKey }

    member inline x.For(config: QueueConfig, [<InlineIfLambda>] f: unit -> QueueConfig) : QueueConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: QueueConfig) : QueueSpec =
        // Queue name is required
        let queueName = config.QueueName

        // Construct ID defaults to queue name if not specified
        let constructId = config.ConstructId |> Option.defaultValue queueName

        let props = QueueProps()
        props.QueueName <- config.QueueName

        config.DeadLetterQueue |> Option.iter (fun d -> props.DeadLetterQueue <- d)

        config.VisibilityTimeout
        |> Option.iter (fun v -> props.VisibilityTimeout <- Duration.Seconds(v))

        config.RetentionPeriod
        |> Option.iter (fun r -> props.RetentionPeriod <- Duration.Seconds(r))

        config.ContentBasedDeduplication
        |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

        config.DeduplicationScope
        |> Option.iter (fun d -> props.DeduplicationScope <- d)

        config.DeliveryDelay |> Option.iter (fun d -> props.DeliveryDelay <- d)

        config.DataKeyReuse |> Option.iter (fun d -> props.DataKeyReuse <- d)

        config.Encryption |> Option.iter (fun e -> props.Encryption <- e)

        config.EnforceSSL |> Option.iter (fun e -> props.EnforceSSL <- e)

        config.Fifo |> Option.iter (fun f -> props.Fifo <- f)

        config.FifoThroughputLimit
        |> Option.iter (fun f -> props.FifoThroughputLimit <- f)

        config.MaxMessageSizeBytes
        |> Option.iter (fun m -> props.MaxMessageSizeBytes <- m)

        config.ReceiveMessageWaitTime
        |> Option.iter (fun r -> props.ReceiveMessageWaitTime <- r)

        config.RedriveAllowPolicy
        |> Option.iter (fun r -> props.RedriveAllowPolicy <- r)

        config.RemovalPolicy |> Option.iter (fun r -> props.RemovalPolicy <- r)

        config.EncryptionMasterKey
        |> Option.iter (fun k -> props.EncryptionMasterKey <- k)

        { QueueName = queueName
          ConstructId = constructId
          Props = props
          Queue = None }

    /// <summary>Sets the construct ID for the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     constructId "MyQueueConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: QueueConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the visibility timeout for messages in the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="seconds">The visibility timeout in seconds.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     visibilityTimeout 30.0
    /// }
    /// </code>
    [<CustomOperation("visibilityTimeout")>]
    member _.VisibilityTimeout(config: QueueConfig, seconds: float) =
        { config with
            VisibilityTimeout = Some seconds }

    /// <summary>Sets the message retention period for the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="seconds">The retention period in seconds.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     retentionPeriod 345600.0 // 4 days
    /// }
    /// </code>
    [<CustomOperation("retentionPeriod")>]
    member _.RetentionPeriod(config: QueueConfig, seconds: float) =
        { config with
            RetentionPeriod = Some seconds }

    /// <summary>Configures the queue as a FIFO queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="isFifo">Whether the queue is FIFO.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue.fifo" {
    ///     fifo true
    /// }
    /// </code>
    [<CustomOperation("fifo")>]
    member _.Fifo(config: QueueConfig, isFifo: bool) = { config with Fifo = Some isFifo }

    /// <summary>Enables content-based deduplication for FIFO queues.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="enabled">Whether content-based deduplication is enabled.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue.fifo" {
    ///     fifo true
    ///     contentBasedDeduplication true
    /// }
    /// </code>
    [<CustomOperation("contentBasedDeduplication")>]
    member _.ContentBasedDeduplication(config: QueueConfig, enabled: bool) =
        { config with
            ContentBasedDeduplication = Some enabled }

    /// <summary>Configures a dead-letter queue for the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="deadLetterQueue">The dead-letter queue configuration.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     deadLetterQueue myDeadLetterQueue
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: QueueConfig, deadLetterQueue: IDeadLetterQueue) =
        { config with
            DeadLetterQueue = Some deadLetterQueue }

    /// <summary>Sets the delay for all messages in the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="delay">The delay duration.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     deliveryDelay (Duration.Seconds(15.0))
    /// }
    /// </code>
    [<CustomOperation("deliveryDelay")>]
    member _.DeliveryDelay(config: QueueConfig, delay: Duration) =
        { config with
            DeliveryDelay = Some delay }

    /// <summary>Sets the encryption type for the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="encryption">The encryption type (KMS, KMS_MANAGED, SQS_MANAGED, or UNENCRYPTED).</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     encryption QueueEncryption.KMS_MANAGED
    /// }
    /// </code>
    [<CustomOperation("encryption")>]
    member _.Encryption(config: QueueConfig, encryption: QueueEncryption) =
        { config with
            Encryption = Some encryption }

    /// <summary>Sets the KMS master key for encryption.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="key">The KMS key.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     encryption QueueEncryption.KMS
    ///     encryptionMasterKey myKmsKey
    /// }
    /// </code>
    [<CustomOperation("encryptionMasterKey")>]
    member _.EncryptionMasterKey(config: QueueConfig, key: IKey) =
        { config with
            EncryptionMasterKey = Some key }

    /// <summary>Sets the deduplication scope for FIFO queues.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="scope">The deduplication scope (Queue or MessageGroup).</param>
    /// <code lang="fsharp">
    /// queue "MyQueue.fifo" {
    ///     fifo true
    ///     deduplicationScope DeduplicationScope.MESSAGE_GROUP
    /// }
    /// </code>
    [<CustomOperation("deduplicationScope")>]
    member _.DeduplicationScope(config: QueueConfig, scope: DeduplicationScope) =
        { config with
            DeduplicationScope = Some scope }

    /// <summary>Enforces SSL/TLS for all queue communications.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="enforce">Whether to enforce SSL.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     enforceSSL true
    /// }
    /// </code>
    [<CustomOperation("enforceSSL")>]
    member _.EnforceSSL(config: QueueConfig, enforce: bool) =
        { config with
            EnforceSSL = Some enforce }

    /// <summary>Sets the throughput limit for FIFO queues.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="limit">The throughput limit (PerQueue or PerMessageGroupId).</param>
    /// <code lang="fsharp">
    /// queue "MyQueue.fifo" {
    ///     fifo true
    ///     fifoThroughputLimit FifoThroughputLimit.PER_MESSAGE_GROUP_ID
    /// }
    /// </code>
    [<CustomOperation("fifoThroughputLimit")>]
    member _.FifoThroughputLimit(config: QueueConfig, limit: FifoThroughputLimit) =
        { config with
            FifoThroughputLimit = Some limit }

    /// <summary>Sets the maximum message size in bytes (1,024 to 262,144 bytes).</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="bytes">The maximum message size in bytes.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     maxMessageSizeBytes 65536.0
    /// }
    /// </code>
    [<CustomOperation("maxMessageSizeBytes")>]
    member _.MaxMessageSizeBytes(config: QueueConfig, bytes: float) =
        { config with
            MaxMessageSizeBytes = Some bytes }

    /// <summary>Sets the receive message wait time for long polling.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="duration">The wait time duration.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     receiveMessageWaitTime (Duration.Seconds(20.0))
    /// }
    /// </code>
    [<CustomOperation("receiveMessageWaitTime")>]
    member _.ReceiveMessageWaitTime(config: QueueConfig, duration: Duration) =
        { config with
            ReceiveMessageWaitTime = Some duration }

    /// <summary>Sets the redrive allow policy for this queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="policy">The redrive allow policy.</param>
    /// <code lang="fsharp">
    /// queue "MyDLQ" {
    ///     redriveAllowPolicy myRedriveAllowPolicy
    /// }
    /// </code>
    [<CustomOperation("redriveAllowPolicy")>]
    member _.RedriveAllowPolicy(config: QueueConfig, policy: IRedriveAllowPolicy) =
        { config with
            RedriveAllowPolicy = Some policy }

    /// <summary>Sets the removal policy for the queue.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="policy">The removal policy (DESTROY, RETAIN, SNAPSHOT, or RETAIN_ON_UPDATE_OR_DELETE).</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     removalPolicy RemovalPolicy.DESTROY
    /// }
    /// </code>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: QueueConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    /// <summary>Sets the data key reuse period for SSE.</summary>
    /// <param name="config">The queue configuration.</param>
    /// <param name="duration">The data key reuse duration.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     dataKeyReuse (Duration.Hours(1.0))
    /// }
    /// </code>
    [<CustomOperation("dataKeyReuse")>]
    member _.DataKeyReuse(config: QueueConfig, duration: Duration) =
        { config with
            DataKeyReuse = Some duration }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module SQSBuilders =
    /// <summary>Creates an SQS queue configuration.</summary>
    /// <param name="name">The queue name.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     visibilityTimeout 30.0
    ///     fifo true
    /// }
    /// </code>
    let queue name = QueueBuilder(name)

    /// <summary>Creates a dead-letter queue configuration.</summary>
    /// <code lang="fsharp">
    /// deadLetterQueue {
    ///     queue myDeadLetterQueue
    ///     maxReceiveCount 5
    /// }
    /// </code>
    let deadLetterQueue = DeadLetterBuilder()
