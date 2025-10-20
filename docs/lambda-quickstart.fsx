(**
---
title: Lambda Quickstart Example
category: docs
index: 5
---

# Lambda Quickstart Example

This example demonstrates how to create AWS Lambda functions using FsCDK with secure defaults and best practices.

## Features Demonstrated

- Lambda function with secure defaults (512MB memory, 30s timeout)
- Environment variable encryption (KMS)
- CloudWatch log retention (90 days default)
- Minimal IAM execution role
- X-Ray tracing (optional)
- Global tagging

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- AWS credentials configured (for deployment)

## Usage

### 1. Synthesize CloudFormation Template

```bash
cd examples/lambda-quickstart
dotnet build
cdk synth
```

This generates a CloudFormation template in `cdk.out/` without requiring AWS credentials.

### 2. Deploy to AWS

```bash
# Bootstrap CDK (first time only)
cdk bootstrap

# Deploy the stack
cdk deploy
```

### 3. Clean Up

```bash
cdk destroy
```

## What's Included

### Default Settings

The Lambda function builder applies these best practices by default:

- **Memory**: 512 MB (balanced performance/cost)
- **Timeout**: 30 seconds
- **Environment Encryption**: KMS with AWS managed key
- **Log Retention**: 90 days
- **IAM Role**: Minimal permissions (CloudWatch Logs + KMS decrypt)
- **X-Ray**: Disabled (opt-in)

### Example 1: Basic Function
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs

lambdaFunction "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    codePath "./lambda-code"
}

(**
Creates a function with all defaults.

### Example 2: Custom Memory and Timeout
*)

lambdaFunction "heavy-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    codePath "./lambda-code"
    memorySize 1024
    timeout 120.0
}

(**
Adjusts memory and timeout for compute-intensive workloads.

### Example 3: Environment Variables
*)

lambdaFunction "api-function" {
    handler "index.handler"
    runtime Runtime.DOTNET_8
    codePath "./publish"
    environment [
        "DATABASE_URL", "postgres://localhost/mydb"
        "API_KEY", "secret-key"
        "LOG_LEVEL", "INFO"
    ]
}

(**
**Security Note**: Environment variables are encrypted at rest using KMS by default.

### Example 4: X-Ray Tracing
*)

lambdaFunction "traced-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_20_X
    codePath "./lambda-code"
    xrayEnabled
    description "Function with X-Ray tracing for debugging"
}

(**
Enables AWS X-Ray for distributed tracing.

### Example 5: Custom Log Retention
*)

lambdaFunction "short-lived-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    codePath "./lambda-code"
    logRetention RetentionDays.ONE_WEEK
}

(**
Reduces log retention for cost savings.

## Complete Example Stack
*)

let config = Config.get ()

stack "LambdaQuickstartStack" {
    environment {
        account config.Account
        region config.Region
    }
    
    stackProps {
        stackEnv
        description "FsCDK Lambda Quickstart Example - demonstrates Lambda functions with security defaults"
        tags [ 
            "Project", "FsCDK-Examples"
            "Example", "Lambda-Quickstart"
            "ManagedBy", "FsCDK"
        ]
    }

    // Example 1: Basic function with all defaults
    lambdaFunction "basic-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        codePath "./dummy-code"
        description "Basic Lambda function with secure defaults"
        // Uses defaults:
        // - memorySize = 512 MB
        // - timeout = 30 seconds
        // - logRetention = 90 days
        // - environment encryption = KMS
    }
    
    // Example 2: Function with custom memory and timeout
    lambdaFunction "compute-intensive-function" {
        handler "process.handler"
        runtime Runtime.PYTHON_3_11
        codePath "./dummy-code"
        memorySize 2048
        timeout 300.0
        description "Compute-intensive function with higher memory and timeout"
    }
    
    // Example 3: Function with environment variables (encrypted by default)
    lambdaFunction "api-handler-function" {
        handler "api.handler"
        runtime Runtime.NODEJS_20_X
        codePath "./dummy-code"
        environment [
            "LOG_LEVEL", "INFO"
            "API_VERSION", "v1"
            "REGION", config.Region
        ]
        description "API handler with encrypted environment variables"
    }
    
    // Example 4: Function with X-Ray tracing enabled
    lambdaFunction "traced-function" {
        handler "traced.handler"
        runtime Runtime.PYTHON_3_11
        codePath "./dummy-code"
        xrayEnabled
        description "Function with X-Ray tracing for debugging"
    }
    
    // Example 5: Function with custom log retention
    lambdaFunction "dev-function" {
        handler "dev.handler"
        runtime Runtime.DOTNET_8
        codePath "./dummy-code"
        logRetention RetentionDays.ONE_WEEK
        timeout 60.0
        description "Development function with shorter log retention"
    }
    
    // Example 6: Function with reserved concurrency
    lambdaFunction "rate-limited-function" {
        handler "ratelimited.handler"
        runtime Runtime.NODEJS_18_X
        codePath "./dummy-code"
        reservedConcurrentExecutions 10
        description "Function with reserved concurrent executions for rate limiting"
    }
}

(**
## IAM Permissions

### Default Execution Role

The builder automatically creates an IAM execution role with:

```fsharp
// Managed policy for CloudWatch Logs
"service-role/AWSLambdaBasicExecutionRole"

// Inline policy for KMS (when environment encryption enabled)
{
    "Effect": "Allow",
    "Action": "kms:Decrypt",
    "Resource": "arn:aws:kms:*:*:key/*"
}
```

### Custom Role

For advanced scenarios, provide your own role:
*)

open FsCDK.Security

let customRole = IAM.createLambdaExecutionRole "my-function" true

lambdaFunction "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    codePath "./code"
    role customRole
}

(**
## Security Considerations

### Environment Variable Encryption

All environment variables are encrypted at rest using KMS. This protects:
- API keys and secrets
- Database connection strings
- Configuration values

**Best Practice**: Use AWS Secrets Manager for highly sensitive secrets.

### Least-Privilege IAM

The execution role includes only:
- CloudWatch Logs write permissions
- KMS decrypt (for environment variables)

Add additional permissions explicitly:

```fsharp
open FsCDK.Security

let role = IAM.createLambdaExecutionRole "my-function" true
// Add S3 read access
IAM.allow ["s3:GetObject"] ["arn:aws:s3:::my-bucket/*"]
|> role.AddToPolicy
```

### Log Retention

Logs are retained for 90 days by default, balancing:
- **Auditability**: Sufficient history for investigation
- **Cost**: Prevents unbounded log storage costs
- **Compliance**: Meets many regulatory requirements

## Performance Optimization

### Memory Configuration

Lambda CPU scales with memory:
- **128-512 MB**: Low-power functions
- **512-1536 MB**: Standard workloads (default: 512 MB)
- **1536-10240 MB**: CPU-intensive tasks

### Timeout

Set timeout based on expected execution time:
- **API handlers**: 5-30 seconds (default: 30s)
- **Batch processing**: 60-900 seconds
- **Max**: 15 minutes (900 seconds)

### Cold Start Optimization

- Use provisioned concurrency for latency-sensitive functions
- Minimize package size
- Avoid large dependencies in handler initialization

## Escape Hatch

For advanced scenarios not covered by the builder:
*)

let funcResource = lambdaFunction "my-function" { 
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    codePath "./code"
}
// Access underlying CDK construct
let cdkFunction = funcResource.Function
// Use any CDK Function methods...

(**
## Next Steps

- Integrate with [S3 Quickstart](s3-quickstart.html) for event-driven processing
- Read [IAM Best Practices](iam-best-practices.html) for advanced permissions
- Review [Lambda Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)

## Resources

- [FsCDK Documentation](index.html)
- [AWS Lambda Documentation](https://docs.aws.amazon.com/lambda/)
- [Lambda Security Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/lambda-security.html)
- [X-Ray Documentation](https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html)
*)
