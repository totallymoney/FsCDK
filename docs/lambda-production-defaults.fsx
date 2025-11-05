(**
---
title: Lambda Production Defaults
category: AWS Lambda
categoryindex: 3
index: 2
description: Production-safe defaults for AWS Lambda functions based on Yan Cui's serverless best practices
---

# Lambda Production Defaults

FsCDK implements production-safe defaults for all Lambda functions based on **Yan Cui's serverless best practices**. 
These defaults ensure your Lambda functions are secure, observable, and resilient from day one.

## Why Production Defaults Matter

In production environments, Lambda functions should:
- **Never lose events** - Auto-create Dead Letter Queues (DLQ)
- **Prevent runaway costs** - Reserved concurrency limits
- **Be fully observable** - X-Ray tracing, structured logging, and Powertools
- **Handle failures gracefully** - Retry limits and event age restrictions

FsCDK makes these best practices the **default behavior** rather than opt-in.

---

## Production Defaults Applied to All Functions

Every Lambda function in FsCDK automatically gets these production-safe defaults:

| Feature | Default Value | Purpose |
|---------|--------------|---------|
| **Reserved Concurrency** | `10` | Prevents unbounded scaling and runaway costs |
| **X-Ray Tracing** | `ACTIVE` | Enables distributed tracing across services |
| **Logging Format** | `JSON` | Structured logs for better querying |
| **Retry Attempts** | `2` | Limits async retries to prevent infinite loops |
| **Max Event Age** | `6 hours` | Prevents processing of stale events |
| **Auto-create DLQ** | `true` | Never loses failed events (14-day retention) |
| **Auto-add Powertools** | `true` | Adds AWS Lambda Powertools layer automatically |

---

## Basic Usage (Defaults Applied Automatically)

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

## Overriding Defaults

You can override any default to suit your specific needs:

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

### How It Works

When enabled (default), FsCDK automatically creates an SQS queue for failed events:

- **Naming Convention**: `{FunctionName}-dlq`
- **Retention Period**: 14 days (enough time to debug)
- **Queue Type**: Standard SQS queue
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

### Manual DLQ Configuration

If you need a custom DLQ setup:

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

## AWS Lambda Powertools Integration

### What is Lambda Powertools?

AWS Lambda Powertools provides battle-tested utilities for:
- **Structured Logging** with correlation IDs
- **Custom Metrics** without CloudWatch API calls
- **Distributed Tracing** integration
- **Input validation** and error handling

### Supported Runtimes

FsCDK automatically adds the Powertools layer for these runtimes:

| Runtime | Layer | Version |
|---------|-------|---------|
| Python 3.8-3.12 | AWSLambdaPowertoolsPython | Latest |
| Node.js 14-20 | AWSLambdaPowertoolsTypeScript | Latest |
| Java 8, 11, 17 | AWSLambdaPowertoolsJava | Latest |
| .NET 6+ | NuGet packages (manual) | - |

> **Note**: .NET uses NuGet packages instead of layers. Add these to your project:
> - `AWS.Lambda.Powertools.Logging`
> - `AWS.Lambda.Powertools.Metrics`
> - `AWS.Lambda.Powertools.Tracing`

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

### Why Reserved Concurrency?

By default, Lambda functions can scale to consume **all available account concurrency** (1,000 by default). 
This can:
- Cause unexpected bills
- Starve other functions
- Trigger throttling errors

FsCDK sets a default of **10 concurrent executions** to protect your account.

### When to Override

Override reserved concurrency when you:
- Have validated your function's throughput needs
- Are implementing a high-volume production workload
- Have set up proper cost alerts

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

FsCDK enables X-Ray tracing by default to provide:
- **End-to-end request tracking** across services
- **Performance bottleneck identification**
- **Error rate visualization**
- **Service dependency mapping**

### Tracing Modes

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

### Default: JSON Format

FsCDK configures Lambda to output logs in JSON format by default, enabling:
- **CloudWatch Logs Insights** queries
- **Log aggregation** in tools like Datadog, Splunk
- **Structured searching** and filtering
- **Better debugging** with consistent field names

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

### Default Event Handling

FsCDK sets these defaults for async invocations:
- **Max Event Age**: 6 hours
- **Retry Attempts**: 2

### When to Override

Use `configureAsyncInvoke` for custom async behavior:

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

### Why Limit Event Age?

Processing stale events can:
- Cause incorrect business logic (outdated inventory, prices)
- Waste Lambda execution time
- Create confusing error states

By default, FsCDK discards events older than 6 hours.

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

## Best Practices

### DO

- **Keep defaults for new functions** - They're production-tested
- **Use Powertools** - Zero cold-start impact, huge observability gains
- **Monitor DLQ** - Set up CloudWatch alarms for DLQ messages
- **Test concurrency limits** - Load test before increasing reserved concurrency
- **Use JSON logging** - Makes debugging much easier

### DON'T

- **Remove concurrency limits without testing** - Can cause bill shock
- **Disable DLQ** - You'll lose valuable error information
- **Disable X-Ray in production** - Tracing overhead is minimal (~1% cost)
- **Set very short MaxEventAge** - Can cause unnecessary retries
- **Use TEXT logging** - JSON provides much better queryability

---

## Related Documentation

- [Lambda Quickstart](lambda-quickstart.fsx) - Basic Lambda usage
- [Getting Started](getting-started-extended.fsx) - FsCDK fundamentals
- [IAM Best Practices](iam-best-practices.fsx) - Securing Lambda permissions

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
