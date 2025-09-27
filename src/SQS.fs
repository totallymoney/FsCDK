namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SQS
open System.Collections.Generic

// ============================================================================
// SQS Queue Configuration DSL
// ============================================================================

// SQS Queue configuration DSL
type QueueConfig =
    { QueueName: string option
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
      Props: QueueProps
      DeadLetterQueueName: string option
      MaxReceiveCount: int option }

type QueueBuilder() =
    member _.Yield _ : QueueConfig =
        { QueueName = None
          ConstructId = None
          VisibilityTimeout = None
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None }

    member _.Zero() : QueueConfig =
        { QueueName = None
          ConstructId = None
          VisibilityTimeout = None
          MessageRetention = None
          FifoQueue = None
          ContentBasedDeduplication = None
          MaxReceiveCount = None
          DeadLetterQueueName = None
          DelaySeconds = None }

    member _.Run(config: QueueConfig) : QueueSpec =
        // Queue name is required
        let queueName =
            match config.QueueName with
            | Some name -> name
            | None -> failwith "Queue name is required"

        // Construct ID defaults to queue name if not specified
        let constructId = config.ConstructId |> Option.defaultValue queueName

        let props = QueueProps()

        // Set queue name
        props.QueueName <- queueName

        // Set optional properties
        config.VisibilityTimeout
        |> Option.iter (fun v -> props.VisibilityTimeout <- Duration.Seconds(v))

        config.MessageRetention
        |> Option.iter (fun r -> props.RetentionPeriod <- Duration.Seconds(r))

        config.FifoQueue |> Option.iter (fun f -> props.Fifo <- f)

        config.ContentBasedDeduplication
        |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

        config.DelaySeconds
        |> Option.iter (fun d -> props.DeliveryDelay <- Duration.Seconds(float d))

        // Note: Dead letter queue configuration is handled separately in Stack builder

        { QueueName = queueName
          ConstructId = constructId
          Props = props
          DeadLetterQueueName = config.DeadLetterQueueName
          MaxReceiveCount = config.MaxReceiveCount }

    [<CustomOperation("name")>]
    member _.Name(config: QueueConfig, queueName: string) =
        { config with
            QueueName = Some queueName }

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

module SQS =
    // Queue processing function for Stack builder
    let processQueue (stack: Stack) (queueSpec: QueueSpec) =
        // Handle dead letter queue configuration if specified
        let props =
            match queueSpec.DeadLetterQueueName, queueSpec.MaxReceiveCount with
            | Some dlqName, Some maxReceive ->
                // Find the DLQ in the stack first if it exists
                try
                    let dlq = stack.Node.FindChild(dlqName) :?> Queue
                    let dlqSpec = DeadLetterQueue(Queue = dlq, MaxReceiveCount = maxReceive)
                    // Create new props with DLQ configured
                    let propsWithDlq = QueueProps()
                    // Copy all properties from original spec
                    propsWithDlq.QueueName <- queueSpec.Props.QueueName

                    if queueSpec.Props.VisibilityTimeout <> null then
                        propsWithDlq.VisibilityTimeout <- queueSpec.Props.VisibilityTimeout

                    if queueSpec.Props.RetentionPeriod <> null then
                        propsWithDlq.RetentionPeriod <- queueSpec.Props.RetentionPeriod

                    propsWithDlq.Fifo <- queueSpec.Props.Fifo
                    propsWithDlq.ContentBasedDeduplication <- queueSpec.Props.ContentBasedDeduplication

                    if queueSpec.Props.DeliveryDelay <> null then
                        propsWithDlq.DeliveryDelay <- queueSpec.Props.DeliveryDelay
                    // Set the DLQ
                    propsWithDlq.DeadLetterQueue <- dlqSpec
                    propsWithDlq
                with ex ->
                    printfn $"Warning: Could not configure DLQ for queue %s{queueSpec.QueueName}: %s{ex.Message}"
                    queueSpec.Props
            | _ -> queueSpec.Props

        // Use the specified construct ID and configured props
        Queue(stack, queueSpec.ConstructId, props) |> ignore
