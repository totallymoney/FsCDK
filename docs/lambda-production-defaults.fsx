(**
---
title: Lambda Production Defaults
category: Resources
categoryindex: 17
description: Production-safe defaults for AWS Lambda functions based on Yan Cui's serverless best practices
---

# ![Lambda](img/icons/Arch_AWS-Lambda_48.png) Lambda production defaults

![Lambda Production Defaults](img/FsCDK-lambda-production-defaults.png)

FsCDK bakes in the guidance from AWS Heroes **Yan Cui**, **Heitor Lessa**, and **Alex Casalboni**, along with the **AWS Lambda Operator Guide**, so every function starts production-ready without additional wiring.

### Lambda Production Architecture

![Lambda Production Architecture with FsCDK Defaults](img/diagrams/lambda-production-defaults.svg)

## Why these defaults matter

Serverless workloads succeed when they:
- **Retain failed events** – DLQs capture payloads for replay and debugging.
- **Control spend and contention** – Reserved concurrency prevents noisy neighbours.
- **Emit rich telemetry** – JSON logs, X-Ray tracing, and Powertools illuminate behaviour.
- **Fail predictably** – Retry and event-age limits avoid infinite loops and stale updates.

FsCDK turns the telemetry practices into defaults and makes the rest one-line opt-ins, so nothing changes your
function's behaviour or cost without you asking for it.

---

## Defaults applied to every function

| Feature | Default value | Rationale |
|---------|---------------|-----------|
| **X-Ray tracing** | `ACTIVE` | Enables end-to-end tracing per Heitor Lessa's observability workshops. |
| **Logging format** | `JSON` | Unlocks CloudWatch Logs Insights and downstream analytics. |

Everything else uses the AWS Lambda defaults: no reserved concurrency (the function shares the account's unreserved
pool), 512 MB ephemeral storage, and for asynchronous invocations 2 retry attempts and a 6-hour maximum event age.

## Opt-in production features

| Feature | Operation | What it does |
|---------|-----------|--------------|
| **Dead-letter queue** | `autoCreateDLQ true` | CDK creates an SQS dead-letter queue (14-day retention) for failed async events, unless you set `deadLetterQueue` or `deadLetterTopic` yourself. |
| **Lambda Powertools** | `autoAddPowertools true` | Attaches the Powertools layer for the stack's region (Python, Node.js, Java; .NET uses NuGet packages). |
| **Reserved concurrency** | `reservedConcurrentExecutions n` | Caps concurrency to limit blast radius and cost. It also reserves `n` from the account quota. |
| **Async retries / event age** | `retryAttempts n`, `maxEventAge d` | Overrides the Lambda defaults of 2 retries and 6 hours. |
| **Log retention** | `logGroup` | Pre-create the log group with a retention period instead of keeping logs forever. |

> **Upgrading from FsCDK 0.2.x:** earlier versions documented the DLQ and Powertools as automatic but never
> created them, and silently set reserved concurrency to 10 on every function. That hidden cap is gone: set
> `reservedConcurrentExecutions` explicitly if you relied on it.

---

## Basic usage with the opt-ins enabled

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

#r "nuget: Amazon.CDK.Lib, 2.128.0"

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open FsCDK

let app = App()

stack "ProductionStack" {
    scope app

    lambda "OrderProcessor" {
        handler "index.handler"
        runtime Runtime.PYTHON_3_11
        code "./src"

        environment [ "TABLE_NAME", "orders-table"; "POWERTOOLS_SERVICE_NAME", "order-service" ]

        autoCreateDLQ true // SQS dead-letter queue for failed async events
        autoAddPowertools true // Powertools layer for the stack's region
        reservedConcurrentExecutions 10 // Explicit concurrency cap
    }
// Always on: X-Ray tracing ACTIVE, JSON logging
// Lambda defaults: max event age 6 hours, 2 retry attempts
}

(**

---

## Overriding defaults

When you understand traffic patterns and operational maturity, override the presets. Follow the decision points outlined in **Production-Ready Serverless** and validate changes through load tests before relaxing safeguards:

*)

stack "CustomStack" {
    scope app

    // High-throughput function
    lambda "HighVolumeProcessor" {
        handler "index.handler"
        runtime Runtime.NODEJS_20_X
        code "./dist"

        // Override defaults
        reservedConcurrentExecutions 500 // Explicit, load-tested cap
        tracing Tracing.PASS_THROUGH // Only trace when upstream sends a trace header
        loggingFormat LoggingFormat.TEXT // Use plain text logs
        maxEventAge (Duration.Hours(1.0)) // 1 hour instead of 6
        retryAttempts 0 // No retries
    }
}

(**

---

## Dead Letter Queue (DLQ) auto-creation (opt-in)

### How it works

With `autoCreateDLQ true`, CDK provisions a standard SQS queue as the function's dead-letter queue, matching the
blueprint from the **AWS Lambda Operator Guide**:

- **Retention**: 14 days (enough to investigate issues and reprocess, per Yan Cui’s guidance)
- **Queue type**: Standard SQS, named by CloudFormation
- **Skipped** when you configure `deadLetterQueue`, `deadLetterTopic` or `deadLetterQueueEnabled` yourself

DLQs only capture failed **asynchronous** invocations (S3, SNS, EventBridge...). Stream and queue event sources
(SQS, Kinesis, DynamoDB Streams) use the event source mapping's own failure destination instead.

*)

lambda "PaymentProcessor" {
    handler "process_payment"
    runtime Runtime.PYTHON_3_11
    code "./payment-service"
    autoCreateDLQ true
}
// CDK creates the SQS dead-letter queue; failed events are kept for 14 days

(**

### Manual DLQ configuration

Large or regulated workloads may require custom encryption, retention, or monitoring. The pattern below mirrors the approach described in the **AWS Compute Blog** post “Designing resilient asynchronous workflows with DLQs.”

*)

open Amazon.CDK.AWS.SQS

stack "CustomDLQStack" {
    scope app

    // Create custom DLQ with specific settings
    let! customDlq =
        queue "critical-failures" {
            retentionPeriod (30.0 * 24.0 * 3600.0) // 30 days in seconds
            encryption QueueEncryption.KMS
        }

    lambda "CriticalFunction" {
        handler "index.handler"
        runtime Runtime.PYTHON_3_11
        code "./src"
        deadLetterQueue customDlq // Use custom DLQ
    }
}

(**

---

## AWS Lambda Powertools integration

Created by AWS Principal Engineer **Heitor Lessa**, Lambda Powertools delivers the structured logging, metrics, and tracing helpers showcased in the **Powertools Live Workshops** (average rating 4.9★). Add `autoAddPowertools true` and FsCDK attaches the Powertools layer for the stack's region, shared by all functions of the same runtime in the stack.

### Key capabilities

- Structured logging with correlation IDs
- Custom metrics without manual CloudWatch API calls
- X-Ray-compatible tracing helpers
- Input validation, idempotency utilities, and middleware patterns

### Supported runtimes

| Runtime | Delivery | Notes |
|---------|----------|-------|
| Python 3.8–3.12 | AWSLambdaPowertoolsPython layer | Latest version pinned automatically |
| Node.js 14–20 | AWSLambdaPowertoolsTypeScript layer | Includes utilities for ESM and CommonJS |
| Java 8, 11, 17 | AWSLambdaPowertoolsJava layer | Aligns with Powertools Java starter kit |
| .NET 6+ | NuGet packages | Add `AWS.Lambda.Powertools.*` packages manually |

> For .NET, install the NuGet packages (`Logging`, `Metrics`, `Tracing`) as demonstrated in the official Powertools .NET docs.

### Python Example

*)

lambda "PythonFunction" {
    handler "app.handler"
    runtime Runtime.PYTHON_3_11
    code "./python-service"

    environment
        [ "POWERTOOLS_SERVICE_NAME", "user-service"
          "POWERTOOLS_METRICS_NAMESPACE", "MyApp"
          "LOG_LEVEL", "INFO" ]

    autoAddPowertools true
}

(**

**In your Python code:**

```python
from aws_lambda_powertools import Logger, Tracer, Metrics
from aws_lambda_powertools.utilities.typing import LambdaContext

logger = Logger()
tracer = Tracer()
metrics = Metrics()

@logger.inject_lambda_context
@tracer.capture_lambda_handler
@metrics.log_metrics(capture_cold_start_metric=True)
def handler(event: dict, context: LambdaContext) -> dict:
    logger.info("Processing user request", extra={"user_id": event.get("user_id")})
    metrics.add_metric(name="UserRequestProcessed", unit="Count", value=1)

    # Your business logic here

    return {"statusCode": 200, "body": "Success"}
```

### Node.js/TypeScript Example

*)

lambda "NodeFunction" {
    handler "index.handler"
    runtime Runtime.NODEJS_20_X
    code "./nodejs-service"

    environment
        [ "POWERTOOLS_SERVICE_NAME", "order-service"
          "POWERTOOLS_METRICS_NAMESPACE", "MyApp"
          "LOG_LEVEL", "INFO" ]

    autoAddPowertools true
}

(**

**In your TypeScript code:**

```typescript
import { Logger } from '@aws-lambda-powertools/logger';
import { Tracer } from '@aws-lambda-powertools/tracer';
import { Metrics } from '@aws-lambda-powertools/metrics';

const logger = new Logger();
const tracer = new Tracer();
const metrics = new Metrics();

export const handler = async (event: any) => {
    logger.info('Processing order', { orderId: event.orderId });
    metrics.addMetric('OrderProcessed', 'Count', 1);

    // Your business logic here

    return { statusCode: 200, body: 'Success' };
};
```

### Functions without Powertools

Powertools is off unless you enable it, so .NET functions (which use the NuGet packages) and functions with
their own logging library need nothing extra:

*)

lambda "CustomLoggingFunction" {
    handler "index.handler"
    runtime Runtime.DOTNET_8
    code "./dotnet-service"
}

(**

---

## Reserved Concurrency

### Why reserved concurrency?

By default, Lambda can consume the entire account concurrency pool (1,000 per Region). Without guard rails you risk surprise costs and throttling other services—a scenario highlighted in **Yan Cui’s “All you need to know about Lambda concurrency”**.

FsCDK does not set reserved concurrency for you: a reservation is both a cap for the function and a slice of the
account quota taken away from every other function, so the right value depends on your traffic and account.

### When to set it

- To protect a downstream system (database, third-party API) from more parallel calls than it can handle
- To guarantee capacity for a critical function in a busy account
- To cap the cost of a function that could be triggered in bulk

Pick the value from load tests, and add throttle and error alarms when you do.

*)

// No reservation: shares the account's unreserved concurrency
lambda "DevFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
}

// Production - cap based on load testing
lambda "ProdFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    reservedConcurrentExecutions 500 // Tested capacity
}

(**

---

## X-Ray Tracing

### Default: ACTIVE

Active tracing mirrors the workflow taught in **AWS Powertools workshops**—capturing every invocation for dependency mapping, latency analysis, and incident response. Keep it enabled unless regulatory requirements dictate otherwise.

### Tracing modes

*)

// Active tracing (default) - traces all requests
lambda "ActiveTracing" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    tracing Tracing.ACTIVE // Default
}

// Pass-through - only traces if upstream sent trace header
lambda "PassThroughTracing" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    tracing Tracing.PASS_THROUGH
}

// Disabled - no tracing
lambda "NoTracing" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    tracing Tracing.DISABLED
}

(**

### X-Ray with Powertools

When using Lambda Powertools, X-Ray tracing is automatically integrated:

```python
from aws_lambda_powertools import Tracer

tracer = Tracer()

@tracer.capture_lambda_handler
def handler(event, context):
    # Automatically creates X-Ray segments
    process_order()

@tracer.capture_method
def process_order():
    # Creates subsegments for detailed tracing
    validate_order()
    save_to_database()
```

---

## Structured Logging (JSON)

### Default: JSON format

Structured JSON logs are the backbone of Yan Cui’s observability playbook and the **AWS Lambda Operator Guide**. They unlock CloudWatch Logs Insights, make Datadog/Splunk ingestion straightforward, and enforce consistent fields for downstream analytics.

*)

lambda "JsonLoggingFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    loggingFormat LoggingFormat.JSON // Default
}

// Override to text format if needed
lambda "TextLoggingFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    loggingFormat LoggingFormat.TEXT
}

(**

### CloudWatch Logs Insights Example

With JSON logging, you can query logs efficiently:

```sql
fields @timestamp, level, message, userId
| filter level = "ERROR"
| filter userId = "user-123"
| sort @timestamp desc
| limit 20
```

---

## Async Invocation Configuration

### Default event handling

Unless you set `retryAttempts` or `maxEventAge`, async invocations use Lambda's own defaults: a 6-hour maximum event age and two retry attempts.

### When to override

Use `configureAsyncInvoke` when business SLAs require tighter delivery windows or fewer retries—after validating downstream systems can cope.

*)

lambda "CustomAsyncFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"

    // Custom async configuration

    asyncInvokeOption (
        eventInvokeConfigOptions {
            maxEventAge (Duration.Minutes(30.0)) // Process within 30 min
            retryAttempts 1 // Only retry once
        }
    )
}
// Note: When using configureAsyncInvoke, MaxEventAge/RetryAttempts
// are NOT set on the function props to avoid conflicts

(**

### Why limit event age?

Stale events lead to incorrect business logic, wasted compute, and confusing audit trails—an issue repeatedly called out in the **AWS Compute Blog**. Lambda drops payloads older than six hours by default; shorten that with `maxEventAge` when an event loses its value sooner, and pair it with a DLQ so dropped events are kept.

---

## Complete Production Example

Here's a complete example showing all production features:

*)

let productionApp = App()

stack "ProductionOrderService" {
    scope productionApp

    // DynamoDB table for orders
    let ordersTable =
        table "production-orders" {
            partitionKey "orderId" AWS.DynamoDB.AttributeType.STRING
            billingMode AWS.DynamoDB.BillingMode.PAY_PER_REQUEST
            pointInTimeRecovery true
        // Note: Encryption is managed by AWS by default
        }

    // Order processor with all production defaults
    lambda "OrderProcessor" {
        handler "process_order.handler"
        runtime Runtime.PYTHON_3_11
        code "./services/order-processor"
        memorySize 512
        timeout 30.0

        // Environment variables for Powertools
        environment
            [ "TABLE_NAME", ordersTable.TableName
              "POWERTOOLS_SERVICE_NAME", "order-processor"
              "POWERTOOLS_METRICS_NAMESPACE", "OrderService"
              "LOG_LEVEL", "INFO" ]

        // Opt-in production features
        autoCreateDLQ true // SQS DLQ, 14-day retention
        autoAddPowertools true
        reservedConcurrentExecutions 50 // Protects the table from bursts
        // Always on: X-Ray tracing ACTIVE, JSON logging

        // Grant DynamoDB permissions
        addRolePolicyStatement (
            policyStatement {
                actions [ "dynamodb:PutItem"; "dynamodb:UpdateItem" ]
                resources [ ordersTable.Table.Value.TableArn ]
            }
        )
    }

    // High-volume notification sender (custom concurrency)
    lambda "NotificationSender" {
        handler "send_notification.handler"
        runtime Runtime.NODEJS_20_X
        code "./services/notification-sender"
        memorySize 256
        timeout 10.0

        // Override concurrency for high volume
        reservedConcurrentExecutions 200 // Validated through load testing

        environment
            [ "POWERTOOLS_SERVICE_NAME", "notification-sender"
              "POWERTOOLS_METRICS_NAMESPACE", "OrderService" ]

        autoCreateDLQ true
        autoAddPowertools true
    }

    // Legacy function: no opt-ins, plain text logs
    lambda "LegacyProcessor" {
        handler "legacy.handler"
        runtime Runtime.JAVA_17
        code "./legacy-service"
        loggingFormat LoggingFormat.TEXT // Legacy log parser
    // Still on: X-Ray tracing ACTIVE (can disable if needed)
    }
}

productionApp.Synth() |> ignore

(**

---

## Best practices (from AWS Heroes)

### DO

- Turn on `autoCreateDLQ` (or set your own DLQ) for every asynchronously invoked function.
- Embrace Lambda Powertools to standardise logging, metrics, and tracing without reinventing tooling.
- Monitor DLQs with CloudWatch alarms and establish a replay runbook.
- Set reserved concurrency from load tests where a downstream system or the account quota needs protecting.
- Retain JSON logging to unlock Logs Insights, OpenSearch, and external observability platforms.

### DON’T

- Rely on unbounded concurrency for functions that hit fragile downstream systems; it’s the fastest path to bill shock and outages.
- Leave async functions without a DLQ or on-failure destination, or you’ll lose the audit trail needed during incidents.
- Turn off X-Ray in production unless compliance requires it—the overhead is minimal.
- Set overly tight `MaxEventAge`; you’ll drop legitimate events unnecessarily.
- Revert to plaintext logging; structured logs are the foundation for analytics.

---

## Related Documentation

- [Lambda Quickstart](lambda-quickstart.html) - Basic Lambda usage
- [Getting Started](getting-started-extended.html) - FsCDK fundamentals
- [IAM Best Practices](iam-best-practices.html) - Securing Lambda permissions

---

## 📚 Learning Resources from AWS Heroes

### Yan Cui (The Burning Monk) - AWS Serverless Hero

**Essential Reading:**

- [Production-Ready Serverless Course](https://productionreadyserverless.com/) - Yan Cui's comprehensive 10-module course covering everything from Lambda basics to production observability
- [The Burning Monk Blog](https://theburningmonk.com/) - Yan Cui's comprehensive serverless best practices, including articles on concurrency, cold starts, and production patterns
- [AWS Lambda Operator Guide](https://docs.aws.amazon.com/lambda/latest/operatorguide/intro.html) - Official production best practices

**Cost Optimization:**

- [Lambda Cost Optimization](https://aws.amazon.com/blogs/compute/operating-lambda-performance-optimization-part-1/) - AWS official strategies for memory optimization and reserved concurrency
- [Lambda Power Tuning](https://github.com/alexcasalboni/aws-lambda-power-tuning) - Data-driven tool to optimize Lambda memory/cost (created by AWS SA)
- [Serverless Cost Calculator](https://cost-calculator.bref.sh/) - Estimate Lambda costs vs traditional infrastructure

**Observability & Debugging:**

- [AWS Lambda Powertools](https://docs.powertools.aws.dev/lambda/python/latest/) - Built-in observability with structured logging, tracing, and metrics
- [X-Ray Tracing Guide](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html) - Official AWS X-Ray integration documentation
- [CloudWatch Logs Insights](https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/AnalyzingLogData.html) - Query and analyze Lambda logs

**Video Content:**

- [AWS re:Invent 2023 - Production-Ready Serverless](https://www.youtube.com/watch?v=4_ZEBN8EuG8) - Latest Lambda best practices from AWS
- [Yan Cui - Serverless Observability](https://www.youtube.com/watch?v=YX4BNX_B6hg) - How to achieve observability in serverless apps
- [Lambda Performance Optimization](https://www.youtube.com/watch?v=bLVROrCj5ug) - Practical tips from AWS experts

### AWS Lambda Powertools

**Official Documentation:**

- [Lambda Powertools Python](https://docs.powertools.aws.dev/lambda/python/) - Structured logging, metrics, and tracing for Python
- [Lambda Powertools TypeScript](https://docs.powertools.aws.dev/lambda/typescript/) - Enterprise-grade utilities for Node.js/TypeScript
- [Lambda Powertools Java](https://docs.powertools.aws.dev/lambda/java/) - Production-ready utilities for Java Lambda functions
- [Lambda Powertools .NET](https://docs.powertools.aws.dev/lambda/dotnet/) - Observability utilities for .NET Lambda functions

**Key Features Explained:**

- [Structured Logging](https://docs.powertools.aws.dev/lambda/python/latest/core/logger/) - Automatically log correlation IDs and context
- [Custom Metrics](https://docs.powertools.aws.dev/lambda/python/latest/core/metrics/) - Emit CloudWatch metrics without API calls
- [Distributed Tracing](https://docs.powertools.aws.dev/lambda/python/latest/core/tracer/) - X-Ray integration with minimal code
- [Validation](https://docs.powertools.aws.dev/lambda/python/latest/utilities/validation/) - JSON Schema validation for Lambda events

### AWS Official Resources

**Lambda Best Practices:**

- [Lambda Operator Guide](https://docs.aws.amazon.com/lambda/latest/operatorguide/intro.html) - Comprehensive guide for operating Lambda at scale
- [Lambda Security Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/lambda-security.html) - IAM, VPC, and encryption guidance
- [Lambda Performance Optimization](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html) - Memory, timeout, and concurrency tuning

**Async Invocation & Error Handling:**

- [Asynchronous Invocation](https://docs.aws.amazon.com/lambda/latest/dg/invocation-async.html) - How Lambda processes async events
- [Dead Letter Queues (DLQ)](https://docs.aws.amazon.com/lambda/latest/dg/invocation-async.html#invocation-dlq) - Capturing failed events
- [Event Source Mapping](https://docs.aws.amazon.com/lambda/latest/dg/invocation-eventsourcemapping.html) - Stream processing with Lambda

**X-Ray Tracing:**

- [Using X-Ray with Lambda](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html) - Enable distributed tracing
- [X-Ray SDK for Lambda](https://docs.aws.amazon.com/xray/latest/devguide/xray-sdk-nodejs.html) - Instrument your Lambda code
- [X-Ray Service Map](https://docs.aws.amazon.com/xray/latest/devguide/xray-console.html#xray-console-servicemap) - Visualize your architecture

### Community Tools

**Lambda Development:**

- [Serverless Framework](https://www.serverless.com/) - Popular IaC framework for serverless
- [SAM (Serverless Application Model)](https://aws.amazon.com/serverless/sam/) - AWS-native serverless framework
- [LocalStack](https://localstack.cloud/) - Local AWS cloud emulator for testing
- [Lumigo](https://lumigo.io/) - Serverless observability platform (commercial)

**Monitoring & Alerting:**

- [CloudWatch Logs Insights](https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/AnalyzingLogData.html) - Query structured logs
- [CloudWatch Lambda Insights](https://docs.aws.amazon.com/lambda/latest/dg/monitoring-insights.html) - Enhanced Lambda monitoring
- [AWS Distro for OpenTelemetry](https://aws-otel.github.io/) - Open-source observability

### Recommended Reading Order

**Beginner → Intermediate:**

1. Start with [AWS Lambda Operator Guide](https://docs.aws.amazon.com/lambda/latest/operatorguide/intro.html)
2. Read [AWS Lambda Cold Start Best Practices](https://docs.aws.amazon.com/lambda/latest/operatorguide/execution-environments.html)
3. Implement [Lambda Powertools](https://docs.powertools.aws.dev/lambda/) in your functions
4. Learn [X-Ray Tracing](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html)

**Intermediate → Advanced:**

1. Deep dive into [Lambda Concurrency](https://docs.aws.amazon.com/lambda/latest/dg/lambda-concurrency.html)
2. Take [Production-Ready Serverless Course](https://productionreadyserverless.com/)
3. Implement [Cost Optimization Strategies](https://aws.amazon.com/blogs/compute/operating-lambda-performance-optimization-part-1/)
4. Master [Serverless Observability](https://docs.aws.amazon.com/lambda/latest/operatorguide/observability.html)

### Why FsCDK Uses These Defaults

FsCDK's defaults and opt-ins are based on lessons learned by AWS Heroes and the broader serverless community:

1. **X-Ray Tracing** (default) - Essential for debugging distributed systems ([AWS X-Ray](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html))
2. **Structured JSON Logging** (default) - Enables CloudWatch Logs Insights queries ([Lambda Powertools](https://docs.powertools.aws.dev/lambda/python/latest/))
3. **Auto-create DLQ** (opt-in) - Never lose events, always have audit trail ([AWS DLQ docs](https://docs.aws.amazon.com/lambda/latest/dg/invocation-async.html#invocation-dlq))
4. **Lambda Powertools** (opt-in) - Battle-tested utilities from AWS ([official docs](https://docs.powertools.aws.dev/lambda/))
5. **Reserved Concurrency** (explicit) - Prevents runaway costs and protects downstream systems ([AWS docs](https://docs.aws.amazon.com/lambda/latest/dg/lambda-concurrency.html))

### FsCDK Implementation Details

These defaults are implemented in `src/Function.fs` and `src/LambdaPowertools.fs`. See [AGENTS.md](../AGENTS.md) for architectural details.

---

## Summary

| Feature | How | Benefit |
|---------|-----|---------|
| **X-Ray Tracing** | Default | Full observability across services |
| **JSON Logging** | Default | Structured logs for better debugging |
| **Auto-created DLQ** | `autoCreateDLQ true` | Never lose failed events |
| **Lambda Powertools** | `autoAddPowertools true` | Production-grade logging, metrics, tracing |
| **Reserved Concurrency** | `reservedConcurrentExecutions n` | Prevents runaway costs |
| **Event Age / Retry Limits** | `maxEventAge`, `retryAttempts` | Prevents stale events and retry storms |

The defaults can be overridden, and the opt-ins are one line each.

*)
()
