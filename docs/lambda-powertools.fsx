(**
---
title: Lambda Powertools Integration
category: Resources
categoryindex: 16
description: AWS Lambda Powertools integration for production-grade observability
---

# ![Lambda](img/icons/Arch_AWS-Lambda_48.png) Lambda Powertools Integration

FsCDK provides first-class integration with **AWS Lambda Powertools**, a suite of utilities for:
- Structured logging with correlation IDs
- Custom metrics without CloudWatch API overhead
- Distributed tracing integration
- Best practice environment variables

Powertools layers are **automatically added** to supported Lambda functions (Python, Node.js, Java).

---

## Quick Start

*)

#r "nuget: Amazon.CDK.Lib, 2.128.0"
#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"


open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open FsCDK

let app = App()

stack "PowertoolsDemo" {
    app

    lambda "MyFunction" {
        handler "app.handler"
        runtime Runtime.PYTHON_3_11
        code "./src"

        // Powertools layer added automatically!
        // Just add environment variables for configuration
        environment
            [ "POWERTOOLS_SERVICE_NAME", "order-service"
              "POWERTOOLS_METRICS_NAMESPACE", "MyApp"
              "LOG_LEVEL", "INFO" ]
    }
}

(**

That's it! The Powertools layer is automatically added, and your function is ready to use Powertools features.

---

## Supported Runtimes

### Automatic Layer Addition

FsCDK automatically adds Powertools layers for these runtimes:

| Runtime | Powertools Layer | Status |
|---------|-----------------|---------|
| **Python 3.8** | AWSLambdaPowertoolsPython | Auto-added |
| **Python 3.9** | AWSLambdaPowertoolsPython | Auto-added |
| **Python 3.10** | AWSLambdaPowertoolsPython | Auto-added |
| **Python 3.11** | AWSLambdaPowertoolsPython | Auto-added |
| **Python 3.12** | AWSLambdaPowertoolsPython | Auto-added |
| **Node.js 14** | AWSLambdaPowertoolsTypeScript | Auto-added |
| **Node.js 16** | AWSLambdaPowertoolsTypeScript | Auto-added |
| **Node.js 18** | AWSLambdaPowertoolsTypeScript | Auto-added |
| **Node.js 20** | AWSLambdaPowertoolsTypeScript | Auto-added |
| **Java 8** | AWSLambdaPowertoolsJava | Auto-added |
| **Java 11** | AWSLambdaPowertoolsJava | Auto-added |
| **Java 17** | AWSLambdaPowertoolsJava | Auto-added |
| **.NET 6+** | NuGet packages | Manual (see below) |

> **Note**: Currently, Powertools layers use **us-east-1** region by default. The layer ARNs are regionalized and work in all AWS regions, but the ARN string currently references us-east-1. This will be improved in a future release to use the actual stack region.

### .NET Special Case

.NET Lambda functions use **NuGet packages** instead of layers. Add these to your `.csproj` or `.fsproj`:

```xml
<ItemGroup>
  <PackageReference Include="AWS.Lambda.Powertools.Logging" Version="1.5.0" />
  <PackageReference Include="AWS.Lambda.Powertools.Metrics" Version="1.5.0" />
  <PackageReference Include="AWS.Lambda.Powertools.Tracing" Version="1.5.0" />
</ItemGroup>
```

FsCDK **skips** layer auto-addition for .NET runtimes.

---

## Python Example

### CDK Definition

*)

lambda "PythonOrderProcessor" {
    handler "app.lambda_handler"
    runtime Runtime.PYTHON_3_11
    code "./python-service"
    memory 512
    timeout 30.0

    environment
        [ "POWERTOOLS_SERVICE_NAME", "order-processor"
          "POWERTOOLS_METRICS_NAMESPACE", "ECommerceApp"
          "LOG_LEVEL", "INFO"
          // Optional advanced configuration
          "POWERTOOLS_LOGGER_SAMPLE_RATE", "0.1" // Sample 10% of logs
          "POWERTOOLS_LOGGER_LOG_EVENT", "true" // Log incoming event
          "POWERTOOLS_TRACE_DISABLED", "false" ] // Enable tracing
}

(**

### Python Lambda Code

**app.py:**

```python
from aws_lambda_powertools import Logger, Tracer, Metrics
from aws_lambda_powertools.utilities.typing import LambdaContext
from aws_lambda_powertools.utilities.data_classes import event_source, SQSEvent
from aws_lambda_powertools.metrics import MetricUnit

# Initialize Powertools
logger = Logger()
tracer = Tracer()
metrics = Metrics()

@logger.inject_lambda_context(log_event=True)
@tracer.capture_lambda_handler
@metrics.log_metrics(capture_cold_start_metric=True)
def lambda_handler(event: dict, context: LambdaContext) -> dict:
    """
    Process order events with full observability
    """
    
    # Structured logging with correlation IDs (automatic)
    logger.info("Processing order", extra={
        "order_id": event.get("orderId"),
        "customer_id": event.get("customerId"),
        "amount": event.get("amount")
    })
    
    try:
        # Your business logic
        order_id = process_order(event)
        
        # Add custom metrics (no CloudWatch API calls!)
        metrics.add_metric(name="OrderProcessed", unit=MetricUnit.Count, value=1)
        metrics.add_metric(name="OrderAmount", unit=MetricUnit.None, value=event.get("amount", 0))
        
        logger.info("Order processed successfully", extra={"order_id": order_id})
        
        return {"statusCode": 200, "orderId": order_id}
        
    except Exception as e:
        logger.exception("Failed to process order")
        metrics.add_metric(name="OrderFailed", unit=MetricUnit.Count, value=1)
        raise

@tracer.capture_method
def process_order(event: dict) -> str:
    """
    Process order with method-level tracing
    """
    # This method automatically creates an X-Ray subsegment
    order_id = event["orderId"]
    
    # Add annotations for X-Ray filtering
    tracer.put_annotation(key="OrderId", value=order_id)
    tracer.put_metadata(key="order_details", value=event)
    
    # Your order processing logic here
    validate_order(event)
    save_to_database(order_id)
    
    return order_id

@tracer.capture_method
def validate_order(event: dict):
    """Validate order details"""
    logger.debug("Validating order", extra={"order": event})
    # Validation logic...

@tracer.capture_method
def save_to_database(order_id: str):
    """Save order to database"""
    logger.info("Saving order to database", extra={"order_id": order_id})
    # Database logic...
```

### SQS Event Processing with Powertools

```python
from aws_lambda_powertools.utilities.data_classes import event_source, SQSEvent
from aws_lambda_powertools.utilities.batch import BatchProcessor, EventType

processor = BatchProcessor(event_type=EventType.SQS)

@logger.inject_lambda_context
@tracer.capture_lambda_handler
def lambda_handler(event, context):
    return processor.process(event=event, record_handler=process_record)

def process_record(record):
    """Process individual SQS message"""
    logger.info("Processing record", extra={"message_id": record.message_id})
    # Process message...
```

---

## Node.js/TypeScript Example

### CDK Definition

*)

lambda "NodeOrderProcessor" {
    handler "index.handler"
    runtime Runtime.NODEJS_20_X
    code "./nodejs-service"
    memory 512
    timeout 30.0

    environment
        [ "POWERTOOLS_SERVICE_NAME", "order-processor"
          "POWERTOOLS_METRICS_NAMESPACE", "ECommerceApp"
          "LOG_LEVEL", "INFO"
          // Optional configuration
          "POWERTOOLS_LOGGER_SAMPLE_RATE", "0.1"
          "POWERTOOLS_LOGGER_LOG_EVENT", "true" ]
}

(**

### TypeScript Lambda Code

**Install dependencies:**

```bash
npm install @aws-lambda-powertools/logger \
            @aws-lambda-powertools/tracer \
            @aws-lambda-powertools/metrics
```

**index.ts:**

```typescript
import { Logger } from '@aws-lambda-powertools/logger';
import { Tracer } from '@aws-lambda-powertools/tracer';
import { Metrics, MetricUnits } from '@aws-lambda-powertools/metrics';
import { Context } from 'aws-lambda';

// Initialize Powertools
const logger = new Logger();
const tracer = new Tracer();
const metrics = new Metrics();

export const handler = async (event: any, context: Context) => {
    // Add context information
    logger.addContext(context);
    
    // Structured logging with correlation IDs
    logger.info('Processing order', { 
        orderId: event.orderId,
        customerId: event.customerId,
        amount: event.amount 
    });
    
    try {
        // Your business logic
        const orderId = await processOrder(event);
        
        // Add custom metrics
        metrics.addMetric('OrderProcessed', MetricUnits.Count, 1);
        metrics.addMetric('OrderAmount', MetricUnits.None, event.amount);
        
        logger.info('Order processed successfully', { orderId });
        
        return { statusCode: 200, orderId };
        
    } catch (error) {
        logger.error('Failed to process order', error as Error);
        metrics.addMetric('OrderFailed', MetricUnits.Count, 1);
        throw error;
    }
};

async function processOrder(event: any): Promise<string> {
    // Create manual segment for detailed tracing
    const segment = tracer.getSegment();
    const subsegment = segment?.addNewSubsegment('processOrder');
    
    try {
        const orderId = event.orderId;
        
        // Add annotations for X-Ray
        subsegment?.addAnnotation('OrderId', orderId);
        subsegment?.addMetadata('order_details', event);
        
        // Your order processing logic
        await validateOrder(event);
        await saveToDatabase(orderId);
        
        subsegment?.close();
        return orderId;
        
    } catch (error) {
        subsegment?.close(error as Error);
        throw error;
    }
}

async function validateOrder(event: any): Promise<void> {
    logger.debug('Validating order', { order: event });
    // Validation logic...
}

async function saveToDatabase(orderId: string): Promise<void> {
    logger.info('Saving order to database', { orderId });
    // Database logic...
}
```

---

## Java Example

### CDK Definition

*)

lambda "JavaOrderProcessor" {
    handler "com.example.OrderHandler::handleRequest"
    runtime Runtime.JAVA_17
    code "./java-service/target/order-service.jar"
    memory 1024
    timeout 60.0

    environment
        [ "POWERTOOLS_SERVICE_NAME", "order-processor"
          "POWERTOOLS_METRICS_NAMESPACE", "ECommerceApp"
          "POWERTOOLS_LOG_LEVEL", "INFO" ]
}

(**

### Java Lambda Code

**Maven dependencies (pom.xml):**

```xml
<dependency>
    <groupId>software.amazon.lambda</groupId>
    <artifactId>powertools-logging</artifactId>
    <version>1.18.0</version>
</dependency>
<dependency>
    <groupId>software.amazon.lambda</groupId>
    <artifactId>powertools-metrics</artifactId>
    <version>1.18.0</version>
</dependency>
<dependency>
    <groupId>software.amazon.lambda</groupId>
    <artifactId>powertools-tracing</artifactId>
    <version>1.18.0</version>
</dependency>
```

**OrderHandler.java:**

```java
package com.example;

import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestHandler;
import software.amazon.lambda.powertools.logging.Logging;
import software.amazon.lambda.powertools.metrics.Metrics;
import software.amazon.lambda.powertools.tracing.Tracing;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class OrderHandler implements RequestHandler<OrderEvent, OrderResponse> {
    
    private static final Logger logger = LogManager.getLogger(OrderHandler.class);
    
    @Override
    @Logging(logEvent = true)
    @Metrics(namespace = "ECommerceApp", service = "order-processor")
    @Tracing
    public OrderResponse handleRequest(OrderEvent event, Context context) {
        
        logger.info("Processing order: " + event.getOrderId());
        
        try {
            String orderId = processOrder(event);
            
            // Metrics are automatically published
            MetricsUtils.putMetric("OrderProcessed", 1, Unit.COUNT);
            MetricsUtils.putMetric("OrderAmount", event.getAmount(), Unit.NONE);
            
            logger.info("Order processed successfully: " + orderId);
            
            return new OrderResponse(200, orderId);
            
        } catch (Exception e) {
            logger.error("Failed to process order", e);
            MetricsUtils.putMetric("OrderFailed", 1, Unit.COUNT);
            throw e;
        }
    }
    
    @Tracing
    private String processOrder(OrderEvent event) {
        // Automatically creates X-Ray subsegment
        validateOrder(event);
        saveToDatabase(event.getOrderId());
        return event.getOrderId();
    }
    
    private void validateOrder(OrderEvent event) {
        logger.debug("Validating order: " + event);
    }
    
    private void saveToDatabase(String orderId) {
        logger.info("Saving order to database: " + orderId);
    }
}
```

---

## Environment Variables Reference

### Logger Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `POWERTOOLS_SERVICE_NAME` | - | Service name (required) |
| `LOG_LEVEL` | `INFO` | Logging level: DEBUG, INFO, WARNING, ERROR |
| `POWERTOOLS_LOGGER_LOG_EVENT` | `false` | Log incoming event |
| `POWERTOOLS_LOGGER_SAMPLE_RATE` | `0` | Sample rate (0.0-1.0) for debug logs |

### Metrics Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `POWERTOOLS_METRICS_NAMESPACE` | - | CloudWatch namespace (required) |
| `POWERTOOLS_SERVICE_NAME` | - | Service name dimension |

### Tracer Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `POWERTOOLS_TRACE_DISABLED` | `false` | Disable tracing |
| `POWERTOOLS_TRACER_CAPTURE_RESPONSE` | `true` | Capture response in traces |
| `POWERTOOLS_TRACER_CAPTURE_ERROR` | `true` | Capture errors in traces |

---

## Advanced Configuration

### Multiple Runtimes in One Stack

*)

stack "MultiRuntimeStack" {
    app

    // Python function - Powertools added automatically
    lambda "PythonFunction" {
        handler "app.handler"
        runtime Runtime.PYTHON_3_11
        code "./python-service"

        environment
            [ "POWERTOOLS_SERVICE_NAME", "python-service"
              "POWERTOOLS_METRICS_NAMESPACE", "MyApp" ]
    }

    // Node.js function - Powertools added automatically
    lambda "NodeFunction" {
        handler "index.handler"
        runtime Runtime.NODEJS_20_X
        code "./nodejs-service"

        environment
            [ "POWERTOOLS_SERVICE_NAME", "nodejs-service"
              "POWERTOOLS_METRICS_NAMESPACE", "MyApp" ]
    }

    // .NET function - No layer added (uses NuGet)
    lambda "DotNetFunction" {
        handler "Assembly::Namespace.Class::Method"
        runtime Runtime.DOTNET_8
        code "./dotnet-service"
    // No Powertools layer - handled via NuGet packages
    }
}

(**

### Disabling Auto-Addition

If you need to use a custom Powertools layer version or disable it entirely:

*)

// Note: Custom Powertools layer version must be created outside lambda builder
// Example:
//   let customPowertoolsLayer =
//       LayerVersion.FromLayerVersionArn(
//           scope,  // The CDK Stack or Construct
//           "CustomPowertools",
//           "arn:aws:lambda:us-east-1:017000801446:layer:AWSLambdaPowertoolsPython:50"
//       )
//
// Then use in lambda:
lambda "CustomPowertoolsFunction" {
    handler "app.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    autoAddPowertools false // Disable auto-addition
// layers [ customPowertoolsLayer ]  // Add the custom layer
}

(**

### Using with Custom Layers

Powertools layers are added alongside your custom layers:

*)

// Note: Custom layers must be created using CDK LayerVersion directly
// Example:
//   let myCustomLayer = LayerVersion(stack, "CustomLayer",
//       LayerVersionProps(
//           LayerVersionName = "my-utils",
//           Code = Code.FromAsset("./layers/utils"),
//           CompatibleRuntimes = [| Runtime.PYTHON_3_11 |]
//       ))

lambda "MultiLayerFunction" {
    handler "app.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
// layers [ myCustomLayer ]  // Add custom layers here
// Powertools layer added automatically in addition to your layers
}

(**

---

## CloudWatch Integration

### Querying Structured Logs

With Powertools JSON logging, use CloudWatch Logs Insights:

```sql
-- Find all errors for a specific order
fields @timestamp, level, message, order_id, error
| filter level = "ERROR" and order_id = "order-123"
| sort @timestamp desc

-- Calculate average processing time
fields @timestamp, function_request_id, function_cold_start, duration
| stats avg(duration), max(duration), count(*) by function_cold_start

-- Find slowest operations
fields @timestamp, xray_trace_id, operation, duration
| sort duration desc
| limit 20
```

### Custom Metrics Dashboard

Powertools metrics automatically appear in CloudWatch:

- **Namespace**: Your configured `POWERTOOLS_METRICS_NAMESPACE`
- **Dimensions**: Automatically includes `service`, `function_name`
- **Zero overhead**: Metrics embedded in logs, no CloudWatch API calls

---

## X-Ray Service Map

With Powertools tracing enabled, X-Ray automatically generates:

- **Service map** showing all Lambda functions and their dependencies
- **Trace timeline** with subsegments for each operation
- **Performance analytics** with percentiles and error rates
- **Annotations** for filtering traces by business criteria

---

## Best Practices

### DO

- **Set `POWERTOOLS_SERVICE_NAME`** - Required for proper metric dimensions
- **Use structured logging** - Always include context in log messages
- **Add annotations to traces** - Makes filtering easier in X-Ray
- **Instrument database calls** - Use `@tracer.capture_method` for slow operations
- **Keep layer versions updated** - FsCDK uses latest stable versions

### DON'T

- **Log sensitive data** - Powertools logs everything you pass
- **Call CloudWatch Metrics API** - Use Powertools metrics instead (zero overhead)
- **Disable Powertools in production** - Overhead is minimal (<1%)
- **Mix logging libraries** - Stick with Powertools for consistency
- **Override `_X_AMZN_TRACE_ID`** - Let X-Ray manage trace IDs

---

## Troubleshooting

### Layer Not Added

**Symptom**: Powertools not available in Lambda function

**Causes**:
1. Runtime not supported (check supported runtimes above)
2. `autoAddPowertools false` was set
3. .NET runtime (needs NuGet packages)

**Solution**:
```fsharp
// Verify Powertools is enabled (default)
lambda "MyFunction" {
    handler "app.handler"
    runtime Runtime.PYTHON_3_11
    code "./src"
    autoAddPowertools true  // Explicitly enable
}
```

### Import Errors in Python

**Symptom**: `ModuleNotFoundError: No module named 'aws_lambda_powertools'`

**Solution**: Layer was not added. Check runtime compatibility or verify deployment.

### Metrics Not Appearing

**Symptom**: No custom metrics in CloudWatch

**Causes**:
1. Missing `POWERTOOLS_METRICS_NAMESPACE`
2. Missing `@metrics.log_metrics()` decorator
3. Exception thrown before metrics flushed

**Solution**:
```python
# Always use decorator to ensure metrics are flushed
@metrics.log_metrics(capture_cold_start_metric=True)
def lambda_handler(event, context):
    metrics.add_metric(name="MyMetric", unit=MetricUnit.Count, value=1)
    # Metrics automatically flushed when handler exits
```

---

## Related Documentation

- [Lambda Production Defaults](lambda-production-defaults.html) - Overview of all production features
- [Lambda Quickstart](lambda-quickstart.html) - Basic Lambda usage
- [CloudWatch Dashboard](cloudwatch-dashboard.html) - Visualizing metrics

---

## External Resources

- [AWS Lambda Powertools Python](https://docs.powertools.aws.dev/lambda/python/)
- [AWS Lambda Powertools TypeScript](https://docs.powertools.aws.dev/lambda/typescript/)
- [AWS Lambda Powertools Java](https://docs.powertools.aws.dev/lambda/java/)
- [AWS Lambda Powertools .NET](https://docs.powertools.aws.dev/lambda/dotnet/)

---

## Summary

FsCDK's Lambda Powertools integration provides:

**Zero-configuration** - Layers added automatically  
**Multi-runtime support** - Python, Node.js, Java, .NET  
**Production-grade observability** - Logging, metrics, tracing  
**Zero cold-start impact** - Layers don't increase startup time  
**Cost-effective metrics** - No CloudWatch API calls  
**Easy to override** - Use `autoAddPowertools false` if needed  

**Get production-ready observability with zero extra code!**

*)
()
