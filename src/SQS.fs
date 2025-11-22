namespace FsCDK

// ============================================================================
// SQS Queue Configuration DSL
// ============================================================================

open Amazon.CDK
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.KMS

// SQS Queue configuration DSL
type QueueConfig =
    { QueueName: string
      ConstructId: string option // Optional custom construct ID
      VisibilityTimeout: float option // seconds
      MessageRetention: float option // seconds
      FifoQueue: bool option
      ContentBasedDeduplication: bool option
      MaxReceiveCount: int option // for DLQ
      DeadLetterQueueName: string option // Reference to DLQ
      DelaySeconds: int option
      Encryption: QueueEncryption option
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
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None
          Encryption = None
          EncryptionMasterKey = None }

    member _.Zero() : QueueConfig =
        { QueueName = name
          ConstructId = None
          VisibilityTimeout = None
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None
          Encryption = None
          EncryptionMasterKey = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> QueueConfig) : QueueConfig = f ()

    member _.Combine(state1: QueueConfig, state2: QueueConfig) : QueueConfig =
        { QueueName = state1.QueueName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          VisibilityTimeout = state2.VisibilityTimeout |> Option.orElse state1.VisibilityTimeout
          MessageRetention = state2.MessageRetention |> Option.orElse state1.MessageRetention
          FifoQueue = state2.FifoQueue |> Option.orElse state1.FifoQueue
          ContentBasedDeduplication =
            state2.ContentBasedDeduplication
            |> Option.orElse state1.ContentBasedDeduplication
          MaxReceiveCount = state2.MaxReceiveCount |> Option.orElse state1.MaxReceiveCount
          DeadLetterQueueName = state2.DeadLetterQueueName |> Option.orElse state1.DeadLetterQueueName
          DelaySeconds = state2.DelaySeconds |> Option.orElse state1.DelaySeconds
          Encryption = state2.Encryption |> Option.orElse state1.Encryption
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

        config.VisibilityTimeout
        |> Option.iter (fun v -> props.VisibilityTimeout <- Duration.Seconds(v))

        config.MessageRetention
        |> Option.iter (fun r -> props.RetentionPeriod <- Duration.Seconds(r))

        config.FifoQueue |> Option.iter (fun f -> props.Fifo <- f)

        config.ContentBasedDeduplication
        |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

        config.DelaySeconds
        |> Option.iter (fun d -> props.DeliveryDelay <- Duration.Seconds(float d))

        config.Encryption |> Option.iter (fun e -> props.Encryption <- e)

        config.EncryptionMasterKey
        |> Option.iter (fun k -> props.EncryptionMasterKey <- k)

        // Avoid using Amazon.CDK.Duration at spec-build time to keep tests jsii-free
        { QueueName = queueName
          ConstructId = constructId
          Props = props
          Queue = None }

    /// <summary>Sets the construct ID for the queue.</summary>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     constructId "MyQueueConstruct"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: QueueConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the visibility timeout for messages in the queue.</summary>
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
    /// <param name="seconds">The retention period in seconds.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     messageRetention 345600.0 // 4 days
    /// }
    /// </code>
    [<CustomOperation("messageRetention")>]
    member _.MessageRetention(config: QueueConfig, seconds: float) =
        { config with
            MessageRetention = Some seconds }

    /// <summary>Configures the queue as a FIFO queue.</summary>
    /// <param name="isFifo">Whether the queue is FIFO.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue.fifo" {
    ///     fifo true
    /// }
    /// </code>
    [<CustomOperation("fifo")>]
    member _.Fifo(config: QueueConfig, isFifo: bool) = { config with FifoQueue = Some isFifo }

    /// <summary>Enables content-based deduplication for FIFO queues.</summary>
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
    /// <param name="dlqName">The name of the dead-letter queue.</param>
    /// <param name="maxReceiveCount">Maximum receives before sending to DLQ.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     deadLetterQueue "MyDLQ" 3
    /// }
    /// </code>
    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: QueueConfig, dlqName: string, maxReceiveCount: int) =
        { config with
            DeadLetterQueueName = Some dlqName
            MaxReceiveCount = Some maxReceiveCount }

    /// <summary>Sets the delay for all messages in the queue.</summary>
    /// <param name="seconds">The delay in seconds.</param>
    /// <code lang="fsharp">
    /// queue "MyQueue" {
    ///     delaySeconds 15
    /// }
    /// </code>
    [<CustomOperation("delaySeconds")>]
    member _.DelaySeconds(config: QueueConfig, seconds: int) =
        { config with
            DelaySeconds = Some seconds }

    /// <summary>Sets the encryption type for the queue.</summary>
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
