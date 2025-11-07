(**
---
title: Lambda Production Defaults
category: AWS Lambda
categoryindex: 3
index: 2
description: Production-safe defaults for AWS Lambda functions based on Yan Cui's serverless best practices
---

# Lambda production defaults

FsCDK bakes in the guidance from AWS Heroes **Yan Cui**, **Heitor Lessa**, and **Alex Casalboni**, along with the **AWS Lambda Operator Guide**, so every function starts production-ready without additional wiring.

## Why these defaults matter

Serverless workloads succeed when they:
- **Retain failed events** – DLQs capture payloads for replay and debugging.
- **Control spend and contention** – Reserved concurrency prevents noisy neighbours.
- **Emit rich telemetry** – JSON logs, X-Ray tracing, and Powertools illuminate behaviour.
- **Fail predictably** – Retry and event-age limits avoid infinite loops and stale updates.

FsCDK turns these best practices into defaults so you don’t have to wire them by hand.

---

## Defaults applied to every function

Every Lambda function in FsCDK automatically gets these production-safe defaults:

| Feature | Default value | Rationale |
|---------|---------------|-----------|
| **Reserved concurrency** | `10` | Limits blast radius and mirrors Yan Cui’s cost-control advice. |
| **X-Ray tracing** | `ACTIVE` | Enables end-to-end tracing per Heitor Lessa’s observability workshops. |
| **Logging format** | `JSON` | Unlocks CloudWatch Logs Insights and downstream analytics. |
| **Retry attempts** | `2` | Prevents infinite retry loops while allowing transient recovery. |
| **Max event age** | `6 hours` | Avoids stale data processing as recommended in the Lambda operator guide. |
| **Dead-letter queue** | Auto-created SQS (14-day retention) | Guarantees recoverability and audit trails. |
| **Lambda Powertools** | Enabled | Adds structured logging, metrics, and tracing helpers automatically. |

---

## Basic usage (defaults already applied)

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
    app

    lambda "OrderProcessor" {
        handler "index.handler"
        runtime Runtime.PYTHON_3_11
        code "./src"

        environment [ "TABLE_NAME", "orders-table"; "POWERTOOLS_SERVICE_NAME", "order-service" ] // Powertools auto-configured
    }
// Reserved concurrency = 10
// X-Ray tracing enabled
// JSON logging enabled
// DLQ auto-created: "OrderProcessor-dlq"
// Powertools layer added automatically
// Max event age = 6 hours
// Retry attempts = 2
}

(**

---

## Overriding defaults

When you understand traffic patterns and operational maturity, override the presets. Follow the decision points outlined in **Production-Ready Serverless** and validate changes through load tests before relaxing safeguards:

*)

stack "CustomStack" {
    app

    // High-throughput function
    lambda "HighVolumeProcessor" {
        handler "index.handler"
        runtime Runtime.NODEJS_20_X
        code "./dist"

        // Override defaults
        reservedConcurrentExecutions 500 // Allow higher concurrency
        tracing Tracing.PASS_THROUGH // Disable X-Ray
        loggingFormat LoggingFormat.TEXT // Use plain text logs
        autoCreateDLQ false // Disable DLQ
        autoAddPowertools false // Skip Powertools layer
        maxEventAge (Duration.Hours(1.0)) // 1 hour instead of 6
        retryAttempts 0 // No retries
    }
}

(**

---

## Dead Letter Queue (DLQ) Auto-Creation

### How it works

FsCDK provisions a standard SQS queue and wires it to the Lambda’s asynchronous failure destination—matching the blueprint from the **AWS Lambda Operator Guide**:

- **Naming convention**: `{FunctionName}-dlq`
- **Retention**: 14 days (enough to investigate issues and reprocess, per Yan Cui’s guidance)
- **Queue type**: Standard SQS
- **Construct ID**: `{ConstructId}-DLQ`

*)

lambda "PaymentProcessor" {
    handler "process_payment"
    runtime Runtime.PYTHON_3_11
    code "./payment-service"
}
// Automatically creates SQS queue: "PaymentProcessor-dlq"
// Failed events stored for 14 days

(**

### Manual DLQ configuration

Large or regulated workloads may require custom encryption, retention, or monitoring. The pattern below mirrors the approach described in the **AWS Compute Blog** post “Designing resilient asynchronous workflows with DLQs.”

*)

open Amazon.CDK.AWS.SQS

stack "CustomDLQStack" {
    app

    // Create custom DLQ with specific settings
    let customDlq =
        queue "critical-failures" {
            messageRetention (30.0 * 24.0 * 3600.0) // 30 days in seconds
            encryption QueueEncryption.KMS
        }

    lambda "CriticalFunction" {
        handler "index.handler"
        runtime Runtime.PYTHON_3_11
        code "./src"
        deadLetterQueue customDlq // Use custom DLQ
        autoCreateDLQ false // Disable auto-creation
    }
}

(**

---

## AWS Lambda Powertools integration

Created by AWS Principal Engineer **Heitor Lessa**, Lambda Powertools delivers the structured logging, metrics, and tracing helpers showcased in the **Powertools Live Workshops** (average rating 4.9★). FsCDK automatically layers these utilities so your handlers follow the same blueprint Heitor presents in re:Invent sessions.

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
}
// Powertools layer added automatically!

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

### Disabling Powertools

If you don't want Powertools (e.g., for .NET or custom logging):

*)

lambda "CustomLoggingFunction" {
    handler "index.handler"
    runtime Runtime.DOTNET_8
    code "./dotnet-service"
    autoAddPowertools false // Skip Powertools layer
}

(**

---

## Reserved Concurrency

### Why reserved concurrency?

By default, Lambda can consume the entire account concurrency pool (1,000 per Region). Without guard rails you risk surprise costs and throttling other services—a scenario highlighted in **Yan Cui’s “All you need to know about Lambda concurrency”**. FsCDK therefore caps concurrency at 10 by default.

### When to override

Increase or remove the cap only after:
- Load testing confirms throughput requirements
- You’ve configured cost and error alarms
- Other critical functions have explicit concurrency protections in place

*)

// Development/staging - keep defaults
lambda "DevFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
// Reserved concurrency = 10 (default)
}

// Production - increase based on load testing
lambda "ProdFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    reservedConcurrentExecutions 500 // Tested capacity
}

// Unlimited (use with caution!)
lambda "UnlimitedFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    reservedConcurrentExecutions -1 // Remove limit (not recommended!)
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

Async invocations inherit a 6-hour max age and two retry attempts, echoing the configuration recommended in the **AWS Lambda Operator Guide** to balance resiliency with timely processing.

### When to override

Use `configureAsyncInvoke` when business SLAs require tighter delivery windows or fewer retries—after validating downstream systems can cope.

*)

lambda "CustomAsyncFunction" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"

    // Custom async configuration
    configureAsyncInvoke {
        maxEventAge (Duration.Minutes(30.0)) // Process within 30 min
        retryAttempts 1 // Only retry once
    }
}
// Note: When using configureAsyncInvoke, MaxEventAge/RetryAttempts
// are NOT set on the function props to avoid conflicts

(**

### Why limit event age?

Stale events lead to incorrect business logic, wasted compute, and confusing audit trails—an issue repeatedly called out in the **AWS Compute Blog**. FsCDK therefore drops payloads older than six hours by default.

---

## Complete Production Example

Here's a complete example showing all production features:

*)

let productionApp = App()

stack "ProductionOrderService" {
    productionApp

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
        memory 512
        timeout 30.0

        // Environment variables for Powertools
        environment
            [ "TABLE_NAME", ordersTable.TableName
              "POWERTOOLS_SERVICE_NAME", "order-processor"
              "POWERTOOLS_METRICS_NAMESPACE", "OrderService"
              "LOG_LEVEL", "INFO" ]

        // Production-safe defaults applied automatically:
        // Reserved concurrency = 10
        // X-Ray tracing = ACTIVE
        // JSON logging
        // DLQ = "OrderProcessor-dlq" (14-day retention)
        // Powertools layer added
        // Max event age = 6 hours
        // Retry attempts = 2

        // Grant DynamoDB permissions
        policyStatement {
            actions [ "dynamodb:PutItem"; "dynamodb:UpdateItem" ]
            resources [ ordersTable.Table.Value.TableArn ]
        }
    }

    // High-volume notification sender (custom concurrency)
    lambda "NotificationSender" {
        handler "send_notification.handler"
        runtime Runtime.NODEJS_20_X
        code "./services/notification-sender"
        memory 256
        timeout 10.0

        // Override concurrency for high volume
        reservedConcurrentExecutions 200 // Validated through load testing

        environment
            [ "POWERTOOLS_SERVICE_NAME", "notification-sender"
              "POWERTOOLS_METRICS_NAMESPACE", "OrderService" ]

    // Other defaults still applied:
    // X-Ray tracing = ACTIVE
    // JSON logging
    // DLQ auto-created
    // Powertools layer
    }

    // Legacy function (custom configuration)
    lambda "LegacyProcessor" {
        handler "legacy.handler"
        runtime Runtime.JAVA_17
        code "./legacy-service"

        // Disable new features for legacy compatibility
        autoCreateDLQ false // Custom error handling
        autoAddPowertools false // Custom logging library
        loggingFormat LoggingFormat.TEXT // Legacy log parser

    // Still protected:
    // Reserved concurrency = 10
    // X-Ray tracing = ACTIVE (can disable if needed)
    }
}

productionApp.Synth() |> ignore

(**

---

## Best practices (from AWS Heroes)

### DO
- Keep the defaults for new workloads—Yan Cui calls them “sane guard rails” for any greenfield project.
- Embrace Lambda Powertools to standardise logging, metrics, and tracing without reinventing tooling.
- Monitor DLQs with CloudWatch alarms and establish a replay runbook.
- Load test before raising reserved concurrency or removing limits.
- Retain JSON logging to unlock Logs Insights, OpenSearch, and external observability platforms.

### DON’T
- Remove concurrency limits without validated traffic modelling; it’s the fastest path to bill shock.
- Disable DLQs or you’ll lose the audit trail needed during incidents.
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
- [AWS Lambda Concurrency Deep Dive](https://theburningmonk.com/2019/09/all-you-need-to-know-about-lambda-concurrency/) - Understanding reserved concurrency, burst limits, and throttling
- [Lambda Cold Starts: You're Thinking About It Wrong](https://theburningmonk.com/2018/01/im-afraid-youre-thinking-about-aws-lambda-cold-starts-all-wrong/) - Data-driven analysis of cold start performance
- [Lambda Best Practices Series](https://theburningmonk.com/tag/best-practice/) - Comprehensive collection of serverless best practices

**Cost Optimization:**
- [How to Reduce Lambda Costs](https://theburningmonk.com/2020/07/how-to-reduce-your-aws-lambda-costs/) - Practical strategies including memory optimization and reserved concurrency
- [Lambda Power Tuning](https://github.com/alexcasalboni/aws-lambda-power-tuning) - Data-driven tool to optimize Lambda memory/cost (created by AWS SA)
- [Serverless Cost Calculator](https://cost-calculator.bref.sh/) - Estimate Lambda costs vs traditional infrastructure

**Observability & Debugging:**
- [Serverless Observability Best Practices](https://theburningmonk.com/2019/03/serverless-observability-what-can-you-use-out-of-the-box/) - Built-in vs third-party observability tools
- [Distributed Tracing with X-Ray](https://theburningmonk.com/2018/04/you-need-to-use-x-ray-with-lambda/) - Why X-Ray is essential for serverless
- [Structured Logging in Lambda](https://theburningmonk.com/2018/01/you-need-to-use-structured-logging-with-aws-lambda/) - Moving beyond console.log

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
2. Read [Yan Cui's Cold Start Article](https://theburningmonk.com/2018/01/im-afraid-youre-thinking-about-aws-lambda-cold-starts-all-wrong/)
3. Implement [Lambda Powertools](https://docs.powertools.aws.dev/lambda/) in your functions
4. Learn [X-Ray Tracing](https://docs.aws.amazon.com/lambda/latest/dg/services-xray.html)

**Intermediate → Advanced:**
1. Deep dive into [Lambda Concurrency](https://theburningmonk.com/2019/09/all-you-need-to-know-about-lambda-concurrency/)
2. Take [Production-Ready Serverless Course](https://productionreadyserverless.com/)
3. Implement [Cost Optimization Strategies](https://theburningmonk.com/2020/07/how-to-reduce-your-aws-lambda-costs/)
4. Master [Serverless Observability](https://theburningmonk.com/2019/03/serverless-observability-what-can-you-use-out-of-the-box/)

### Why FsCDK Uses These Defaults

FsCDK's production defaults are based on lessons learned by AWS Heroes and the broader serverless community:

1. **Reserved Concurrency (10)** - Prevents the "$30,000 Lambda bill" scenario ([source](https://theburningmonk.com/2020/07/how-to-reduce-your-aws-lambda-costs/))
2. **Auto-create DLQ** - Never lose events, always have audit trail ([AWS DLQ docs](https://docs.aws.amazon.com/lambda/latest/dg/invocation-async.html#invocation-dlq))
3. **X-Ray Tracing** - Essential for debugging distributed systems ([Yan Cui](https://theburningmonk.com/2018/04/you-need-to-use-x-ray-with-lambda/))
4. **Structured JSON Logging** - Enables CloudWatch Logs Insights queries ([Yan Cui](https://theburningmonk.com/2018/01/you-need-to-use-structured-logging-with-aws-lambda/))
5. **Lambda Powertools** - Battle-tested utilities from AWS ([official docs](https://docs.powertools.aws.dev/lambda/))

### FsCDK Implementation Details

These defaults are implemented in `src/Function.fs` and `src/LambdaPowertools.fs`. See [AGENTS.md](../AGENTS.md) for architectural details.

---

## Summary

FsCDK's production defaults ensure your Lambda functions are:

| Feature | Benefit |
|---------|---------|
| **Reserved Concurrency** | Prevents runaway costs |
| **X-Ray Tracing** | Full observability across services |
| **JSON Logging** | Structured logs for better debugging |
| **Auto-created DLQ** | Never lose failed events |
| **Lambda Powertools** | Production-grade logging, metrics, tracing |
| **Event Age Limits** | Prevents stale event processing |
| **Retry Limits** | Prevents infinite retry loops |

**These defaults can all be overridden**, but they provide a safe starting point for production workloads.

*)
()
