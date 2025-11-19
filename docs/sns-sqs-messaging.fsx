(**
---
title: SNS and SQS Messaging
category: Resources
categoryindex: 24
---

# ![SNS](img/icons/Arch_Amazon-Simple-Notification-Service_48.png) ![SQS](img/icons/Arch_Amazon-Simple-Queue-Service_48.png) Amazon SNS and SQS

Amazon SNS (Simple Notification Service) and SQS (Simple Queue Service) are fully managed messaging services
that enable you to decouple and scale microservices, distributed systems, and serverless applications.

## SNS/SQS Messaging Patterns

![SNS/SQS Messaging Patterns](img/diagrams/sns-sqs-messaging-patterns.svg)

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.Lambda

(**
## SNS: Basic Topic

Create a simple SNS topic for pub/sub messaging.
*)

stack "BasicSNS" { topic "OrderNotifications" { displayName "Order Processing Notifications" } }

(**
## SNS: FIFO Topic

Create a FIFO topic for ordered, exactly-once message delivery.
*)

stack "FIFOTopic" {
    topic "Transactions.fifo" {
        displayName "Transaction Events"
        fifo true
        contentBasedDeduplication true
    }
}

(**
## SQS: Basic Queue

Create a simple SQS queue for asynchronous message processing.
*)

stack "BasicSQS" {
    queue "OrderProcessing" {
        visibilityTimeout 30.0
        messageRetention 345600.0 // 4 days
    }
}

(**
## SQS: FIFO Queue

Create a FIFO queue for ordered message processing.
*)

stack "FIFOQueue" {
    queue "Transactions.fifo" {
        fifo true
        contentBasedDeduplication true
        visibilityTimeout 60.0
    }
}

(**
## SQS: Queue with Dead Letter Queue

Implement error handling with a dead-letter queue.
*)

stack "QueueWithDLQ" {
    // Create dead-letter queue first
    let dlq =
        queue "ProcessingDLQ" {
            messageRetention 1209600.0 // 14 days
        }

    // Create main queue with DLQ
    queue "OrderProcessing" {
        visibilityTimeout 30.0
        deadLetterQueue "ProcessingDLQ" 3 // 3 max receives
    }
}

(**
## SNS to SQS: Fan-Out Pattern

Distribute messages from one SNS topic to multiple SQS queues.
*)

stack "FanOutPattern" {
    // Create SNS topic
    let orderTopic = topic "OrderEvents" { displayName "Order Processing Events" }

    // Create multiple queues for different processing
    let inventoryQueue = queue "InventoryProcessing" { visibilityTimeout 30.0 }

    let shippingQueue = queue "ShippingProcessing" { visibilityTimeout 60.0 }

    let analyticsQueue = queue "AnalyticsProcessing" { visibilityTimeout 120.0 }

    // Note: Subscriptions must be configured using CDK directly
    // Example: orderTopic.AddSubscription(SqsSubscription inventoryQueue)
    ()
}

(**
## SNS to Lambda

Trigger Lambda functions from SNS topics.
*)

stack "SNSToLambda" {
    // Create Lambda function
    let processorFunc =
        lambda "EventProcessor" {
            runtime Runtime.DOTNET_8
            handler "App::ProcessEvent"
            code "./lambda"
        }

    // Create SNS topic
    let eventTopic = topic "SystemEvents" { displayName "System Event Notifications" }

    // Note: Subscription must be configured using CDK directly
    // Example: eventTopic.AddSubscription(LambdaSubscription processorFunc)
    ()
}

(**
## SQS to Lambda

Process SQS messages with Lambda functions.
*)

stack "SQSToLambda" {
    // Create queue
    let workQueue =
        queue "WorkQueue" {
            visibilityTimeout 300.0 // 5 minutes (match Lambda timeout)
        }

    // Create Lambda function
    let workerFunc =
        lambda "Worker" {
            runtime Runtime.DOTNET_8
            handler "App::ProcessMessage"
            code "./lambda"
            timeout 300.0 // 5 minutes
        }

    // Note: Event source mapping must be configured using CDK directly
    // Example: workQueue.GrantConsumeMessages workerFunc
    ()
}

(**
## Email Notifications

Send email notifications using SNS.
*)

(**
Note: Email subscriptions must be configured using CDK directly.
Email addresses require confirmation.
*)

(**
## SMS Notifications

Send SMS notifications using SNS.
*)

(**
Note: SMS subscriptions must be configured using CDK directly.
Requires SMS settings configuration in AWS account.
*)

(**
## Best Practices

### SNS Best Practices

#### Performance
- Use batching for high-volume publishing
- Implement retry logic with exponential backoff
- Use message attributes for filtering
- Consider FIFO topics only when ordering is critical

#### Security
- Use IAM policies for topic access control
- Enable encryption at rest and in transit
- Use VPC endpoints for private access
- Audit access with CloudTrail

#### Cost Optimization
- Use message filtering to reduce unnecessary deliveries
- Monitor unused topics and remove them
- Use standard topics unless FIFO is required
- Consolidate topics where possible

#### Reliability
- Implement DLQ for failed deliveries
- Set appropriate retry policies
- Monitor delivery failures in CloudWatch
- Use multiple subscriptions for redundancy

### SQS Best Practices

#### Performance
- Set visibility timeout >= Lambda timeout
- Use long polling (reduce empty receives)
- Batch messages when possible
- Use message attributes for metadata

#### Security
- Use IAM policies for queue access control
- Enable encryption at rest (SQS-managed or KMS)
- Use VPC endpoints for private access
- Implement least-privilege access

#### Cost Optimization
- Use standard queues unless FIFO is required
- Set appropriate message retention (don't over-retain)
- Monitor queue age and empty receives
- Use long polling to reduce API calls

#### Reliability
- Always implement dead-letter queues
- Set maxReceiveCount appropriately (3-5 typical)
- Monitor DLQ depth and alert on messages
- Implement idempotent message processing
- Set retention high enough for recovery

### Message Processing Patterns

#### Fan-Out (SNS to Multiple SQS)
- One message triggers multiple processing pipelines
- Each queue processes independently
- Use for parallel processing of same event

#### Queue Chain (SQS to Lambda to SQS)
- Multi-stage processing pipeline
- Each stage processes and forwards
- Use for complex workflows

#### Priority Queue
- Multiple queues with different Lambda concurrency
- High-priority queue gets more workers
- Use for SLA-based processing

#### Circuit Breaker
- Monitor DLQ depth
- Stop processing on repeated failures
- Use for protecting downstream systems

## SNS vs SQS

| Feature | SNS | SQS |
|---------|-----|-----|
| **Pattern** | Pub/Sub (push) | Queue (pull) |
| **Delivery** | Push to subscribers | Pull by consumers |
| **Message Persistence** | No | Yes (up to 14 days) |
| **Subscribers** | Multiple | One consumer per message |
| **Ordering** | FIFO topics only | FIFO queues only |
| **Best For** | Fan-out, notifications | Decoupling, buffering |

## FIFO vs Standard

| Feature | Standard | FIFO |
|---------|----------|------|
| **Throughput** | Unlimited | 300 msg/s (batch: 3000) |
| **Ordering** | Best effort | Guaranteed |
| **Delivery** | At least once | Exactly once |
| **Cost** | Lower | Higher |
| **Use Case** | Most scenarios | Banking, trading |

## Message Attributes

Use message attributes for filtering and routing:

```fsharp
// In Lambda publishing to SNS:
let attributes = Dictionary<string, MessageAttributeValue>()
attributes.Add("eventType", MessageAttributeValue(StringValue = "order"))
attributes.Add("priority", MessageAttributeValue(StringValue = "high"))

topic.Publish(PublishRequest(
    Message = json,
    MessageAttributes = attributes
))
```

## Resources

- [Amazon SNS Documentation](https://docs.aws.amazon.com/sns/)
- [Amazon SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [SNS Message Filtering](https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html)
- [SQS Best Practices](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-best-practices.html)
- [Fan-Out Pattern](https://docs.aws.amazon.com/sns/latest/dg/sns-common-scenarios.html)
*)
