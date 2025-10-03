namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SQS
open System.Collections.Generic

// ============================================================================
// SQS Queue Configuration DSL
// ============================================================================

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
      DelaySeconds: int option }

type QueueSpec =
    { QueueName: string
      ConstructId: string // Construct ID for CDK
      VisibilityTimeout: float option // seconds
      MessageRetention: float option // seconds
      FifoQueue: bool option
      ContentBasedDeduplication: bool option
      DelaySeconds: int option
      DeadLetterQueueName: string option
      MaxReceiveCount: int option }

type QueueBuilder(name: string) =
    member _.Yield _ : QueueConfig =
        { QueueName = name
          ConstructId = None
          VisibilityTimeout = None
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None }

    member _.Zero() : QueueConfig =
        { QueueName = name
          ConstructId = None
          VisibilityTimeout = None
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None }

    member _.Delay(f: unit -> QueueConfig) : QueueConfig = f ()

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
          DelaySeconds = state2.DelaySeconds |> Option.orElse state1.DelaySeconds }

    member x.For(config: QueueConfig, f: unit -> QueueConfig) : QueueConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: QueueConfig) : QueueSpec =
        // Queue name is required
        let queueName = config.QueueName

        // Construct ID defaults to queue name if not specified
        let constructId = config.ConstructId |> Option.defaultValue queueName

        // Avoid using Amazon.CDK.Duration at spec-build time to keep tests jsii-free
        { QueueName = queueName
          ConstructId = constructId
          VisibilityTimeout = config.VisibilityTimeout
          MessageRetention = config.MessageRetention
          FifoQueue = config.FifoQueue
          ContentBasedDeduplication = config.ContentBasedDeduplication
          DelaySeconds = config.DelaySeconds
          DeadLetterQueueName = config.DeadLetterQueueName
          MaxReceiveCount = config.MaxReceiveCount }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: QueueConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("visibilityTimeout")>]
    member _.VisibilityTimeout(config: QueueConfig, seconds: float) =
        { config with
            VisibilityTimeout = Some seconds }

    [<CustomOperation("messageRetention")>]
    member _.MessageRetention(config: QueueConfig, seconds: float) =
        { config with
            MessageRetention = Some seconds }

    [<CustomOperation("fifo")>]
    member _.Fifo(config: QueueConfig, isFifo: bool) = { config with FifoQueue = Some isFifo }

    [<CustomOperation("contentBasedDeduplication")>]
    member _.ContentBasedDeduplication(config: QueueConfig, enabled: bool) =
        { config with
            ContentBasedDeduplication = Some enabled }

    [<CustomOperation("deadLetterQueue")>]
    member _.DeadLetterQueue(config: QueueConfig, dlqName: string, maxReceiveCount: int) =
        { config with
            DeadLetterQueueName = Some dlqName
            MaxReceiveCount = Some maxReceiveCount }

    [<CustomOperation("delaySeconds")>]
    member _.DelaySeconds(config: QueueConfig, seconds: int) =
        { config with
            DelaySeconds = Some seconds }
