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

```fsharp
lambdaFunction "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    codePath "./lambda-code"
}
```

Creates a function with all defaults.

### Example 2: Custom Memory and Timeout

```fsharp
lambdaFunction "heavy-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    codePath "./lambda-code"
    memorySize 1024
    timeout 120.0
}
```

Adjusts memory and timeout for compute-intensive workloads.

### Example 3: Environment Variables

```fsharp
lambdaFunction "api-function" {
    handler "index.handler"
    runtime Runtime.DOTNET_8
    codePath "./publish"
    environment [
        "DATABASE_URL", databaseUrl
        "API_KEY", apiKey
        "LOG_LEVEL", "INFO"
    ]
}
```

**Security Note**: Environment variables are encrypted at rest using KMS by default.

### Example 4: X-Ray Tracing

```fsharp
lambdaFunction "traced-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_20_X
    codePath "./lambda-code"
    xrayEnabled
    description "Function with X-Ray tracing for debugging"
}
```

Enables AWS X-Ray for distributed tracing.

### Example 5: Custom Log Retention

```fsharp
lambdaFunction "short-lived-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    codePath "./lambda-code"
    logRetention RetentionDays.ONE_WEEK
}
```

Reduces log retention for cost savings.

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

```fsharp
open FsCDK.Security

let customRole = IAM.createLambdaExecutionRole "my-function" true

lambdaFunction "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    codePath "./code"
    role customRole
}
```

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

```fsharp
let funcResource = lambdaFunction "my-function" { ... }
// Access underlying CDK construct
let cdkFunction = funcResource.Function
// Use any CDK Function methods...
```

## Next Steps

- Integrate with [S3 Quickstart](../s3-quickstart/) for event-driven processing
- Read [IAM Best Practices](../../docs/IAM_BEST_PRACTICES.md) for advanced permissions
- Review [Lambda Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)

## Resources

- [FsCDK Documentation](https://totallymoney.github.io/FsCDK/)
- [AWS Lambda Documentation](https://docs.aws.amazon.com/lambda/)
- [Lambda Security Best Practices](https://docs.aws.amazon.com/lambda/latest/dg/lambda-security.html)
- [X-Ray Documentation](https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html)
