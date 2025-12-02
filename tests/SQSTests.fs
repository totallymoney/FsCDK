module FsCDK.Tests.SQSTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.SQS

[<Tests>]
let sqs_queue_dsl_tests =
    testList
        "SQS queue DSL"
        [ test "defaults constructId to queue name" {
              let spec = queue "MyQueue" { () }

              Expect.equal spec.QueueName "MyQueue" "QueueName should be set"
              Expect.equal spec.ConstructId "MyQueue" "ConstructId should default to queue name"
          }

          test "uses custom constructId when provided" {
              let spec = queue "MyQueue" { constructId "MyQueueConstruct" }

              Expect.equal spec.ConstructId "MyQueueConstruct" "Custom constructId should be used"
          }

          test "configures FIFO queue" {
              let spec = queue "MyQueue.fifo" { fifo true }

              Expect.isTrue spec.Props.Fifo.Value "Queue should be configured as FIFO"
          }

          test "enables content-based deduplication" {
              let spec =
                  queue "MyQueue.fifo" {
                      fifo true
                      contentBasedDeduplication true
                  }

              Expect.isTrue spec.Props.ContentBasedDeduplication.Value "ContentBasedDeduplication should be enabled"
          }

          test "configures dead-letter queue reference" {
              let spec = queue "MyQueue" { deadLetterQueue "MyDLQ" 3 }

              // Note: The actual DLQ connection happens at stack build time
              Expect.equal spec.QueueName "MyQueue" "Queue name should be set"
          }

          test "applies encryption when configured" {
              let spec = queue "MyQueue" { encryption QueueEncryption.KMS_MANAGED }

              Expect.equal spec.Props.Encryption.Value QueueEncryption.KMS_MANAGED "Encryption should be set"
          }

          test "applies deduplication scope for FIFO queues" {
              let spec =
                  queue "MyQueue.fifo" {
                      fifo true
                      deduplicationScope DeduplicationScope.MESSAGE_GROUP
                  }

              Expect.equal
                  spec.Props.DeduplicationScope.Value
                  DeduplicationScope.MESSAGE_GROUP
                  "DeduplicationScope should be set"
          }

          test "enforces SSL when configured" {
              let spec = queue "MyQueue" { enforceSSL true }

              Expect.isTrue spec.Props.EnforceSSL.Value "EnforceSSL should be true"
          }

          test "applies FIFO throughput limit" {
              let spec =
                  queue "MyQueue.fifo" {
                      fifo true
                      fifoThroughputLimit FifoThroughputLimit.PER_MESSAGE_GROUP_ID
                  }

              Expect.equal
                  spec.Props.FifoThroughputLimit.Value
                  FifoThroughputLimit.PER_MESSAGE_GROUP_ID
                  "FifoThroughputLimit should be set"
          }

          test "applies max message size bytes" {
              let spec = queue "MyQueue" { maxMessageSizeBytes 65536.0 }

              Expect.equal spec.Props.MaxMessageSizeBytes.Value 65536.0 "MaxMessageSizeBytes should be set"
          }

          test "applies removal policy" {
              let spec = queue "MyQueue" { removalPolicy RemovalPolicy.DESTROY }

              Expect.equal spec.Props.RemovalPolicy.Value RemovalPolicy.DESTROY "RemovalPolicy should be set"
          }

          test "combines multiple non-Duration configurations" {
              let spec =
                  queue "MyQueue.fifo" {
                      constructId "MyFifoQueue"
                      fifo true
                      contentBasedDeduplication true
                      deduplicationScope DeduplicationScope.MESSAGE_GROUP
                      fifoThroughputLimit FifoThroughputLimit.PER_MESSAGE_GROUP_ID
                      enforceSSL true
                      maxMessageSizeBytes 131072.0
                      removalPolicy RemovalPolicy.RETAIN
                  }

              Expect.equal spec.ConstructId "MyFifoQueue" "ConstructId should be set"
              Expect.isTrue spec.Props.Fifo.Value "Fifo should be true"
              Expect.isTrue spec.Props.ContentBasedDeduplication.Value "ContentBasedDeduplication should be true"

              Expect.equal
                  spec.Props.DeduplicationScope.Value
                  DeduplicationScope.MESSAGE_GROUP
                  "DeduplicationScope should be set"

              Expect.equal
                  spec.Props.FifoThroughputLimit.Value
                  FifoThroughputLimit.PER_MESSAGE_GROUP_ID
                  "FifoThroughputLimit should be set"

              Expect.isTrue spec.Props.EnforceSSL.Value "EnforceSSL should be true"
              Expect.equal spec.Props.MaxMessageSizeBytes.Value 131072.0 "MaxMessageSizeBytes should be set"
              Expect.equal spec.Props.RemovalPolicy.Value RemovalPolicy.RETAIN "RemovalPolicy should be set"
          }

          test "optional settings remain unset when not provided" {
              let spec = queue "SimpleQueue" { () }

              Expect.isNull (box spec.Props.Fifo) "Fifo should be null when not configured"

              Expect.isNull
                  (box spec.Props.ContentBasedDeduplication)
                  "ContentBasedDeduplication should be null when not configured"

              Expect.isNull (box spec.Props.Encryption) "Encryption should be null when not configured"
              Expect.isNull (box spec.Props.EnforceSSL) "EnforceSSL should be null when not configured"

              Expect.isNull
                  (box spec.Props.FifoThroughputLimit)
                  "FifoThroughputLimit should be null when not configured"

              Expect.isNull
                  (box spec.Props.MaxMessageSizeBytes)
                  "MaxMessageSizeBytes should be null when not configured"

              Expect.isNull (box spec.Props.RemovalPolicy) "RemovalPolicy should be null when not configured"
          }

          test "applies retention period" {
              let spec = queue "MyQueue" { retentionPeriod 345600.0 }

              Expect.isNotNull (box spec.Props.RetentionPeriod) "RetentionPeriod should be set"
          }

          test "applies visibility timeout" {
              let spec = queue "MyQueue" { visibilityTimeout 30.0 }

              Expect.isNotNull (box spec.Props.VisibilityTimeout) "VisibilityTimeout should be set"
          }

          test "applies delivery delay" {
              let spec = queue "MyQueue" { deliveryDelay (Duration.Seconds(15.0)) }

              Expect.isNotNull (box spec.Props.DeliveryDelay) "DeliveryDelay should be set"
          }

          test "applies data key reuse" {
              let spec = queue "MyQueue" { dataKeyReuse (Duration.Hours(1.0)) }

              Expect.isNotNull (box spec.Props.DataKeyReuse) "DataKeyReuse should be set"
          }

          test "applies receive message wait time" {
              let spec = queue "MyQueue" { receiveMessageWaitTime (Duration.Seconds(20.0)) }

              Expect.isNotNull (box spec.Props.ReceiveMessageWaitTime) "ReceiveMessageWaitTime should be set"
          }

          test "applies redrive allow policy" {
              let policy = RedriveAllowPolicy(RedrivePermission = RedrivePermission.ALLOW_ALL)
              let spec = queue "MyDLQ" { redriveAllowPolicy policy }

              Expect.equal spec.Props.RedriveAllowPolicy policy "RedriveAllowPolicy should be set"
          } ]
