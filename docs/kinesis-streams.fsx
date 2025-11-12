(**
---
title: Kinesis Streams Example
category: docs
index: 9
---

# ![Kinesis](img/icons/Arch_Amazon-Kinesis_48.png) Kinesis Streams Example

This example demonstrates how to create Amazon Kinesis Data Streams using FsCDK for real-time data streaming.

## What is Kinesis?

Amazon Kinesis Data Streams enables you to build custom applications that process or analyze streaming data for specialized needs. It can continuously capture and store terabytes of data per hour from hundreds of thousands of sources.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- AWS credentials configured (for deployment)

## Basic Kinesis Stream with Lambda Consumer
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Lambda.EventSources
open FsCDK

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "KinesisStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "Kinesis Data Streams example with Lambda consumer"
    tags [ "Project", "FsCDK-Examples"; "Service", "Kinesis"; "ManagedBy", "FsCDK" ]

    // Create a basic Kinesis stream with encryption
    let stream =
        kinesisStream "MyDataStream" {
            streamName "my-data-stream"
            shardCount 1
            retentionPeriod (Duration.Hours 24.)
            encryption StreamEncryption.KMS
        }

    // Lambda function to process stream records
    let processor =
        lambda "stream-processor" {
            handler "index.handler"
            runtime Runtime.NODEJS_18_X
            code "./lambda-code"
            memory 512
            timeout 60.0

            environment [ "STREAM_NAME", stream.StreamName ]

            description "Processes records from Kinesis stream"
        }

    ()
}

(**
## Stream Configuration Options

### On-Demand Mode

For unpredictable workloads, use on-demand capacity mode. The stream automatically scales to handle varying throughput.

**When to use:**
- Unpredictable traffic patterns
- Sporadic workloads
- New applications with unknown load

**Pricing:** Pay per GB of data written and read
*)

stack "OnDemandKinesisStack" {
    description "Kinesis stream with on-demand capacity"

    let onDemandStream =
        kinesisStream "OnDemandStream" {
            streamName "on-demand-stream"
            streamMode StreamMode.ON_DEMAND
            retentionPeriod (Duration.Hours 168.) // 7 days
            encryption StreamEncryption.KMS
        }

    ()
}

(**
### Provisioned Mode (High Throughput)

For predictable, high-volume data ingestion with multiple shards.

**When to use:**
- Consistent high throughput
- Cost optimization with reserved capacity
- Need for predictable performance

**Pricing:** Pay per shard-hour
*)

stack "HighThroughputKinesisStack" {
    description "High-throughput Kinesis stream with multiple shards"

    let highThroughputStream =
        kinesisStream "HighThroughputStream" {
            streamName "high-throughput-stream"
            shardCount 10
            retentionPeriod (Duration.Hours 168.) // 7 days
            encryption StreamEncryption.KMS
        }

    ()
}

(**
## Use Cases

### 1. Real-Time Analytics Pipeline

Process streaming data for real-time dashboards and metrics.
*)

stack "AnalyticsPipelineStack" {
    description "Real-time analytics with Kinesis and Lambda"

    // Kinesis stream captures clickstream data
    let clickstream =
        kinesisStream "ClickstreamData" {
            streamName "clickstream-events"
            shardCount 5
            retentionPeriod (Duration.Hours 48.)
            encryption StreamEncryption.KMS
        }

    // Lambda processes events in real-time
    let analyticsProcessor =
        lambda "analytics-processor" {
            handler "analytics.handler"
            runtime Runtime.PYTHON_3_11
            code "./analytics-code"
            memory 1024
            timeout 60.0

            environment
                [ "STREAM_NAME", clickstream.StreamName
                  "METRIC_NAMESPACE", "Analytics/Clickstream" ]

            description "Processes clickstream events for real-time analytics"
        }

    // This value-cross-linking would need some nicer API.
    clickstream.GrantReads.Add(analyticsProcessor.Function.Value.Role) |> ignore

    analyticsProcessor.EventSources.Add(
        KinesisEventSource(
            clickstream.Stream.Value,
            KinesisEventSourceProps(
                StartingPosition = StartingPosition.LATEST,
                BatchSize = 500.,
                MaxBatchingWindow = Duration.Seconds(10.),
                ParallelizationFactor = 5.
            )
        )
    )
    |> ignore

    ()
}

(**
### 2. Event Streaming Architecture

Capture application events for multiple independent consumers.
*)

stack "EventStreamingStack" {
    description "Event streaming with multiple consumers"

    // Central event stream
    let eventStream =
        kinesisStream "ApplicationEvents" {
            streamName "application-events"
            shardCount 3
            retentionPeriod (Duration.Hours 72.)
            encryption StreamEncryption.KMS
        }

    // Consumer 1: Event archiver
    let archiver =
        lambda "event-archiver" {
            handler "archiver.handler"
            runtime Runtime.PYTHON_3_11
            code "./archiver-code"
            memory 512
            timeout 120.0
            description "Archives events to S3"
        }

    eventStream.GrantReads.Add(archiver.Function.Value.Role) |> ignore

    archiver.EventSources.Add(
        KinesisEventSource(
            eventStream.Stream.Value,
            KinesisEventSourceProps(StartingPosition = StartingPosition.TRIM_HORIZON, BatchSize = 100.)
        )
    )
    |> ignore

    // Consumer 2: Metrics aggregator
    let metricsAggregator =
        lambda "metrics-aggregator" {
            handler "metrics.handler"
            runtime Runtime.NODEJS_18_X
            code "./metrics-code"
            memory 512
            timeout 60.0
            description "Aggregates metrics from events"
        }

    eventStream.GrantReads.Add(metricsAggregator.Function.Value.Role) |> ignore

    metricsAggregator.EventSources.Add(
        KinesisEventSource(
            eventStream.Stream.Value,
            KinesisEventSourceProps(StartingPosition = StartingPosition.LATEST, BatchSize = 200.)
        )
    )
    |> ignore

    ()
}

(**
### 3. Log Aggregation

Centralize logs from multiple sources for analysis and storage.
*)

stack "LogAggregationStack" {
    description "Centralized log aggregation with Kinesis"

    // Log aggregation stream
    let logStream =
        kinesisStream "LogAggregation" {
            streamName "application-logs"
            shardCount 4
            retentionPeriod (Duration.Hours 24.)
            encryption StreamEncryption.KMS
        }

    // Log processor
    let logProcessor =
        lambda "log-processor" {
            handler "logs.handler"
            runtime Runtime.PYTHON_3_11
            code "./log-processor-code"
            memory 1024
            timeout 120.0

            environment [ "LOG_GROUP", "/aws/kinesis/logs" ]

            description "Processes and filters log data"
        }

    logStream.GrantReads.Add(logProcessor.Function.Value.Role) |> ignore

    logProcessor.EventSources.Add(
        KinesisEventSource(
            logStream.Stream.Value,
            KinesisEventSourceProps(
                StartingPosition = StartingPosition.LATEST,
                BatchSize = 500.,
                MaxBatchingWindow = Duration.Seconds(5.)
            )
        )
    )
    |> ignore

    ()
}

(**
## Security Best Practices

### Encryption

**At-Rest Encryption:**
- KMS encryption is enabled by default
- Use customer-managed keys for sensitive data
- Automatic key rotation available

**In-Transit Encryption:**
- TLS encryption for all data transmission
- HTTPS endpoints only

You can add encryptionKey as parameter to builder.

*)


(**
### IAM Permissions

Grant least-privilege access to streams.
*)

// Producer permissions
let producerRole = IAM.createRole "lambda.amazonaws.com" "stream-producer-role"

let producerStmt =
    IAM.allow [ "kinesis:PutRecord"; "kinesis:PutRecords" ] [ "arn:aws:kinesis:*:*:stream/my-stream" ]
//producerRole.AddToPolicy producerStmt |> ignore

// Consumer permissions
let consumerRole = IAM.createRole "lambda.amazonaws.com" "stream-consumer-role"

let consumerStmt =
    IAM.allow
        [ "kinesis:GetRecords"
          "kinesis:GetShardIterator"
          "kinesis:DescribeStream"
          "kinesis:ListShards" ]
        [ "arn:aws:kinesis:*:*:stream/my-stream" ]
//consumerRole.AddToPolicy consumerStmt |> ignore

(**
## Monitoring and Observability

### CloudWatch Metrics

Kinesis automatically publishes metrics for monitoring:

- **IncomingBytes**: Data volume ingested
- **IncomingRecords**: Record count ingested
- **GetRecords.IteratorAgeMilliseconds**: Consumer lag
- **WriteProvisionedThroughputExceeded**: Throttling events
- **ReadProvisionedThroughputExceeded**: Consumer throttling

### CloudWatch Alarms

Monitor stream health with alarms:
*)

stack "MonitoredKinesisStack" {
    description "Kinesis stream with CloudWatch monitoring"

    let monitoredStream =
        kinesisStream "MonitoredStream" {
            streamName "monitored-stream"
            shardCount 2
        }

    // Alarm for consumer lag
    cloudwatchAlarm "stream-lag-alarm" {
        description "Alert when consumers fall behind"
        metricNamespace "AWS/Kinesis"
        metricName "GetRecords.IteratorAgeMilliseconds"
        dimensions [ "StreamName", monitoredStream.StreamName ]
        statistic "Maximum"
        threshold 60000.0 // 1 minute in milliseconds
        evaluationPeriods 2
        period (Duration.Minutes(5.0))
    }

    // Alarm for write throttling
    cloudwatchAlarm "write-throttle-alarm" {
        description "Alert on write throttling"
        metricNamespace "AWS/Kinesis"
        metricName "WriteProvisionedThroughputExceeded"
        dimensions [ "StreamName", monitoredStream.StreamName ]
        statistic "Sum"
        threshold 10.0
        evaluationPeriods 1
        period (Duration.Minutes(5.0))
    }

    ()
}

(**
## Performance Optimization

### Shard Calculation

Calculate required shards based on throughput requirements:

```
Incoming write bandwidth: 1 MB/sec per shard
Outgoing read bandwidth: 2 MB/sec per shard

Required shards = max(
    incoming_write_bandwidth_in_MB / 1,
    outgoing_read_bandwidth_in_MB / 2
)
```

### Batch Processing Configuration

Optimize Lambda processing for cost and latency:
*)

stack "OptimizedProcessingStack" {
    description "Optimized Kinesis processing with Lambda"

    let stream =
        kinesisStream "OptimizedStream" {
            streamName "optimized-stream"
            shardCount 5
        }

    let optimizedConsumer =
        lambda "optimized-consumer" {
            handler "optimized.handler"
            runtime Runtime.PYTHON_3_11
            code "./optimized-code"
            memory 1024
            timeout 300.0
            reservedConcurrentExecutions 10
            description "Optimized batch processor"
        }

    stream.GrantReads.Add(optimizedConsumer.Function.Value.Role) |> ignore

    // Optimized event source mapping
    optimizedConsumer.EventSources.Add(
        KinesisEventSource(
            stream.Stream.Value,
            KinesisEventSourceProps(
                StartingPosition = StartingPosition.LATEST,
                BatchSize = 1000., // Larger batches reduce Lambda invocations
                MaxBatchingWindow = Duration.Seconds(10.), // Wait up to 10s to collect records
                ParallelizationFactor = 5., // Process 5 batches per shard concurrently
                RetryAttempts = 3., // Retry failed batches
                MaxRecordAge = (Duration.Hours 24.), // Discard old records
                BisectBatchOnError = true, // Split batch on error for faster recovery
                ReportBatchItemFailures = true // Report individual failures
            )
        )
    )
    |> ignore

    ()
}

(**
## Complete Production Example
*)

stack "ProductionKinesisStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "Production-ready Kinesis streaming pipeline"
    tags [ "Environment", "Production"; "Project", "DataPipeline"; "ManagedBy", "FsCDK" ]

    // Production stream with extended retention
    let prodStream =
        kinesisStream "ProductionStream" {
            streamName "production-data-stream"
            shardCount 10
            retentionPeriod (Duration.Hours 168.)
            encryption StreamEncryption.KMS
        }

    // Producer Lambda
    let producer =
        lambda "data-producer" {
            handler "producer.handler"
            runtime Runtime.PYTHON_3_11
            code "./producer-code"
            memory 512
            timeout 60.0

            environment [ "STREAM_NAME", prodStream.StreamName; "BATCH_SIZE", "500" ]

            description "Produces events to Kinesis stream"
        }

    prodStream.GrantWrites.Add(producer.Function.Value.Role) |> ignore

    // Consumer Lambda with optimal settings
    let consumer =
        lambda "data-consumer" {
            handler "consumer.handler"
            runtime Runtime.PYTHON_3_11
            code "./consumer-code"
            memory 2048
            timeout 300.0
            reservedConcurrentExecutions 50

            environment [ "DESTINATION_BUCKET", "processed-data-bucket"; "BATCH_SIZE", "1000" ]

            description "Consumes and processes events from Kinesis"
        }

    prodStream.GrantReads.Add(consumer.Function.Value.Role) |> ignore

    consumer.EventSources.Add(
        KinesisEventSource(
            prodStream.Stream.Value,
            KinesisEventSourceProps(
                StartingPosition = StartingPosition.TRIM_HORIZON,
                BatchSize = 1000.,
                MaxBatchingWindow = Duration.Seconds(10.),
                ParallelizationFactor = 10.,
                RetryAttempts = 3.,
                BisectBatchOnError = true,
                ReportBatchItemFailures = true
            )
        )
    )
    |> ignore

    // CloudWatch alarm for monitoring
    cloudwatchAlarm "production-stream-lag" {
        description "Critical: Production stream consumer lag"
        metricNamespace "AWS/Kinesis"
        metricName "GetRecords.IteratorAgeMilliseconds"
        dimensions [ "StreamName", prodStream.StreamName ]
        statistic "Maximum"
        threshold 300000.0 // 5 minutes
        evaluationPeriods 2
        period (Duration.Minutes(5.0))
    }

    ()
}

(**
## Cost Optimization

### Choosing the Right Mode

**Provisioned Mode:**
- Best for: Consistent workloads
- Cost: $0.015 per shard-hour + $0.014 per million PUT payload units
- Example: 2 shards x 24 hours x 30 days = $21.60/month

**On-Demand Mode:**
- Best for: Variable workloads
- Cost: $0.04 per GB written + $0.0125 per GB read
- Example: 100 GB write + 200 GB read = $6.50/month

### Data Retention

- **24 hours**: Default, suitable for most use cases ($0/month)
- **7 days**: Extended retention ($0.023 per shard-hour extra)
- **365 days**: Maximum retention ($0.033 per shard-hour extra)

## Deployment

```bash
# Synthesize CloudFormation template
cdk synth

# Deploy to AWS
cdk deploy ProductionKinesisStack

# Destroy resources when done
cdk destroy ProductionKinesisStack
```

## Next Steps

- Integrate with [Lambda Functions](lambda-quickstart.html) for stream processing
- Review [IAM Best Practices](iam-best-practices.html) for access control
- Explore [DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/) for storing processed data

## Resources

- [AWS Kinesis Documentation](https://docs.aws.amazon.com/kinesis/)
- [Kinesis Data Streams Developer Guide](https://docs.aws.amazon.com/streams/latest/dev/introduction.html)
- [Lambda Kinesis Integration](https://docs.aws.amazon.com/lambda/latest/dg/with-kinesis.html)
- [Kinesis Best Practices](https://docs.aws.amazon.com/streams/latest/dev/best-practices.html)
- [Kinesis Pricing](https://aws.amazon.com/kinesis/data-streams/pricing/)
*)

(*** hide ***)
()
