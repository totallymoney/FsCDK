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
open Amazon.CDK.AWS.SNS.Subscriptions
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
## SNS: Topic with All Options

Configure a topic with all available options.
*)

stack "FullTopic" {
    topic "SecureNotifications" {
        constructId "MySecureTopic"
        displayName "Secure Notification Topic"
        enforceSSL true
        signatureVersion "2"
        tracingConfig TracingConfig.ACTIVE
        messageRetentionPeriodInDays 7.0
    }
}

(**
## SQS: Basic Queue

Create a simple SQS queue for asynchronous message processing.
*)

stack "BasicSQS" {
    queue "OrderProcessing" {
        visibilityTimeout 30.0
        retentionPeriod 345600.0 // 4 days
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

Implement error handling with a dead-letter queue using `let!` to bind the DLQ.
*)

stack "QueueWithDLQ" {
    // Create a dead-letter queue first and bind it with let!
    let! dlqQueue =
        queue "ProcessingDLQ" {
            retentionPeriod 1209600.0 // 14 days
        }

    // Create the DLQ configuration
    let dlqConfig =
        deadLetterQueue {
            queue dlqQueue
            maxReceiveCount 3
        }

    // Create main queue with DLQ
    queue "OrderProcessing" {
        visibilityTimeout 30.0
        deadLetterQueue dlqConfig
    }
}

(**
## SNS Subscription Builders

FsCDK provides type-safe subscription builders for all SNS subscription types.
Each builder creates an `ITopicSubscription` that can be added to topics.

### Available Subscription Builders

| Builder | Purpose | Required Field |
|---------|---------|----------------|
| `lambdaSubscription` | Subscribe Lambda functions | `handler` |
| `sqsSubscription` | Subscribe SQS queues | `queue` |
| `emailSubscription` | Subscribe email addresses | `email` |
| `smsSubscription` | Subscribe phone numbers | `phoneNumber` |
| `urlSubscription` | Subscribe HTTP/HTTPS endpoints | `url` |

### Common Options

All subscription builders support these optional configurations:

- `deadLetterQueue` - Queue for failed message delivery
- `filterPolicy` - Filter messages by attributes
- `filterPolicyWithMessageBody` - Filter messages by body content

*)

(**
## Lambda Subscription

Subscribe a Lambda function to an SNS topic. The function is invoked for each message published.
Use `let!` within a `stack` CE to convert FsCDK builders to CDK constructs.
*)

stack "LambdaSubscriptionStack" {
    // Create Lambda function using FsCDK builder
    let! processorFunc =
        lambda "Processor" {
            handler "App::ProcessEvent"
            runtime Runtime.DOTNET_8
            code (Code.FromAsset("./lambda"))
        }

    // Create topic with Lambda subscription using implicit yield
    topic "EventTopic" {
        displayName "Event Notifications"
        lambdaSubscription { handler processorFunc }
    }
}

(**
### Lambda Subscription with Filter Policy

Filter which messages trigger the Lambda function based on message attributes.
*)

stack "FilteredLambdaStack" {
    let! processorFunc =
        lambda "FilteredProcessor" {
            handler "App::ProcessEvent"
            runtime Runtime.DOTNET_8
            code (Code.FromAsset("./lambda"))
        }

    topic "FilteredEventTopic" {
        displayName "Filtered Events"

        lambdaSubscription {
            handler processorFunc

            filterPolicy (
                dict
                    [ "eventType",
                      SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order"; "payment" |])) ]
            )
        }
    }
}

(**
## SQS Subscription

Subscribe an SQS queue to receive messages from an SNS topic.
This is the foundation of the fan-out pattern.
*)

stack "SQSSubscriptionStack" {
    // Create SQS queue using FsCDK builder
    let! orderQueue = queue "OrderQueue" { visibilityTimeout 30.0 }

    // Create topic with SQS subscription using implicit yield
    topic "OrderEvents" {
        displayName "Order Processing Events"

        sqsSubscription {
            queue orderQueue
            rawMessageDelivery true // Send raw message without SNS metadata
        }
    }
}

(**
### SQS Subscription with Dead Letter Queue

Configure a dead letter queue to capture messages that fail to deliver.
*)

stack "SQSWithDLQStack" {
    let! orderQueue = queue "OrderQueue" { visibilityTimeout 30.0 }
    let! dlqQueue = queue "DLQ" { retentionPeriod 1209600.0 } // 14 days

    topic "ReliableEvents" {
        displayName "Reliable Event Delivery"

        sqsSubscription {
            queue orderQueue
            deadLetterQueue dlqQueue
            rawMessageDelivery true
        }
    }
}

(**
## Email Subscription

Subscribe an email address to receive notifications. The email address will receive
a confirmation email and must confirm before receiving messages.
*)

let topicWithEmail =
    topic "AlertTopic" {
        displayName "System Alerts"
        emailSubscription { email "ops-team@example.com" }
    }

(**
### Email Subscription with JSON Format

Send the full SNS notification as JSON instead of just the message body.
*)

let topicWithJsonEmail =
    topic "DetailedAlerts" {
        displayName "Detailed System Alerts"

        emailSubscription {
            email "dev-team@example.com"
            json true // Send full JSON notification
        }
    }

(**
## SMS Subscription

Subscribe a phone number to receive SMS notifications.
Phone numbers should be in E.164 format (e.g., +1234567890).
*)

let topicWithSms =
    topic "UrgentAlerts" {
        displayName "Urgent Notifications"
        smsSubscription { phoneNumber "+1234567890" }
    }

(**
## URL (HTTP/HTTPS) Subscription

Subscribe an HTTP or HTTPS endpoint to receive notifications via webhook.
The endpoint must confirm the subscription.
*)

let topicWithUrl =
    topic "WebhookTopic" {
        displayName "Webhook Notifications"

        urlSubscription {
            url "https://api.example.com/webhooks/sns"
            rawMessageDelivery true
        }
    }

(**
### URL Subscription with Protocol Override

Explicitly set the protocol (useful when URL doesn't clearly indicate HTTP vs HTTPS).
*)

let topicWithHttps =
    topic "SecureWebhooks" {
        displayName "Secure Webhook Notifications"

        urlSubscription {
            url "https://secure.example.com/notifications"
            protocol SubscriptionProtocol.HTTPS
            rawMessageDelivery true
        }
    }

(**
## Multiple Subscriptions

Add multiple subscriptions to a single topic using implicit yields (each subscription builder
is automatically added to the topic).
*)

stack "MultiSubscriptionStack" {
    let! notificationQueue = queue "NotificationQueue" { () }

    topic "MultiChannelAlerts" {
        displayName "Multi-Channel Alert System"

        // Notify ops team via email
        emailSubscription { email "ops@example.com" }

        // Send to the processing queue
        sqsSubscription {
            queue notificationQueue
            rawMessageDelivery true
        }

        // Webhook for external integration
        urlSubscription { url "https://slack.example.com/webhook" }

        // SMS for critical alerts
        smsSubscription { phoneNumber "+1987654321" }
    }
}

(**
## Fan-Out Pattern with Subscription Builders

The fan-out pattern distributes messages from one SNS topic to multiple SQS queues
for parallel processing. Each queue can have different processing characteristics.
*)

stack "FanOutStack" {
    // Create multiple queues for different processing pipelines using FsCDK builders
    let! inventoryQueue = queue "InventoryQueue" { visibilityTimeout 30.0 }
    let! shippingQueue = queue "ShippingQueue" { visibilityTimeout 60.0 }
    let! analyticsQueue = queue "AnalyticsQueue" { visibilityTimeout 120.0 }

    // Create a topic with fan-out to all queues using implicit yields
    topic "OrderEvents" {
        displayName "Order Processing Events"

        subscriptions
            [ sqsSubscription {
                  queue inventoryQueue
                  rawMessageDelivery true
              }

              sqsSubscription {
                  queue shippingQueue
                  rawMessageDelivery true
              }

              sqsSubscription {
                  queue analyticsQueue
                  rawMessageDelivery true
              } ]
    }
}

(**
## Filter Policy Examples

Use filter policies to route messages to specific subscribers based on message attributes.

### String Matching
*)

stack "FilterPolicyExamplesStack" {
    let! filterExampleFn =
        lambda "FilterExampleFn" {
            handler "App::ProcessEvent"
            runtime Runtime.DOTNET_8
            code (Code.FromAsset("./lambda"))
        }

    // String Matching Filter
    let stringFilterExample =
        lambdaSubscription {
            handler filterExampleFn

            filterPolicy (
                dict
                    [ "eventType",
                      SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order"; "refund" |])) ]
            )
        }

    // Numeric Matching Filter
    let numericFilterExample =
        lambdaSubscription {
            handler filterExampleFn

            filterPolicy (
                dict
                    [ "amount",
                      SubscriptionFilter.NumericFilter(
                          NumericConditions(Between = BetweenCondition(Start = 100.0, Stop = 1000.0))
                      ) ]
            )
        }

    // Prefix Matching Filter
    let prefixFilterExample =
        lambdaSubscription {
            handler filterExampleFn

            filterPolicy (
                dict
                    [ "source",
                      SubscriptionFilter.StringFilter(StringConditions(MatchPrefixes = [| "prod-"; "staging-" |])) ]
            )
        }

    // Use filters in a topic
    topic "FilteredTopic" {
        displayName "Topic with Filtered Subscriptions"
        stringFilterExample
        numericFilterExample
        prefixFilterExample
    }
}

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

## Subscription Builder Reference

### LambdaSubscriptionBuilder

| Operation | Type | Description |
|-----------|------|-------------|
| `handler` | `IFunction` | **Required.** Lambda function to invoke |
| `deadLetterQueue` | `IQueue` | Queue for failed deliveries |
| `filterPolicy` | `IDictionary<string, SubscriptionFilter>` | Filter by message attributes |
| `filterPolicyWithMessageBody` | `IDictionary<string, FilterOrPolicy>` | Filter by message body |

### SqsSubscriptionBuilder

| Operation | Type | Description |
|-----------|------|-------------|
| `queue` | `IQueue` | **Required.** SQS queue to subscribe |
| `rawMessageDelivery` | `bool` | Send raw message without SNS envelope |
| `deadLetterQueue` | `IQueue` | Queue for failed deliveries |
| `filterPolicy` | `IDictionary<string, SubscriptionFilter>` | Filter by message attributes |
| `filterPolicyWithMessageBody` | `IDictionary<string, FilterOrPolicy>` | Filter by message body |

### EmailSubscriptionBuilder

| Operation | Type | Description |
|-----------|------|-------------|
| `email` | `string` | **Required.** Email address to subscribe |
| `json` | `bool` | Send full JSON notification |
| `deadLetterQueue` | `IQueue` | Queue for failed deliveries |
| `filterPolicy` | `IDictionary<string, SubscriptionFilter>` | Filter by message attributes |
| `filterPolicyWithMessageBody` | `IDictionary<string, FilterOrPolicy>` | Filter by message body |

### SmsSubscriptionBuilder

| Operation | Type | Description |
|-----------|------|-------------|
| `phoneNumber` | `string` | **Required.** Phone number (E.164 format) |
| `deadLetterQueue` | `IQueue` | Queue for failed deliveries |
| `filterPolicy` | `IDictionary<string, SubscriptionFilter>` | Filter by message attributes |
| `filterPolicyWithMessageBody` | `IDictionary<string, FilterOrPolicy>` | Filter by message body |

### UrlSubscriptionBuilder

| Operation | Type | Description |
|-----------|------|-------------|
| `url` | `string` | **Required.** HTTP/HTTPS endpoint URL |
| `protocol` | `SubscriptionProtocol` | HTTP or HTTPS protocol |
| `rawMessageDelivery` | `bool` | Send raw message without SNS envelope |
| `deadLetterQueue` | `IQueue` | Queue for failed deliveries |
| `filterPolicy` | `IDictionary<string, SubscriptionFilter>` | Filter by message attributes |
| `filterPolicyWithMessageBody` | `IDictionary<string, FilterOrPolicy>` | Filter by message body |

## Resources

- [Amazon SNS Documentation](https://docs.aws.amazon.com/sns/)
- [Amazon SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [SNS Message Filtering](https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html)
- [SQS Best Practices](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-best-practices.html)
- [Fan-Out Pattern](https://docs.aws.amazon.com/sns/latest/dg/sns-common-scenarios.html)
*)
