(**
---
title: Lambda Quickstart Example
category: Resources
categoryindex: 18
---

# ![AWS Lambda](img/icons/Arch_AWS-Lambda_48.png) Lambda quickstart

Spin up your first Lambda function with FsCDK using the same production-minded defaults promoted by AWS Heroes **Yan Cui** and **Heitor Lessa**. This quickstart walks through essential variations—memory, timeouts, environment variables, tracing—so you can go from “hello world” to secure, observable functions in minutes.

## What you’ll practice

- Creating Lambda functions with sensible defaults (512 MB memory, 30 s timeout)
- Encrypting environment variables with KMS automatically
- Controlling log retention using logGroup builder (defaults to 1 week) and ephemeral storage (512 MB)
- Operating with minimal IAM permissions
- Enabling X-Ray tracing and Powertools utilities for observability
- Applying consistent tagging across resources

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- AWS credentials configured for deployment (use an isolated sandbox account, as recommended in the **AWS Lambda Operator Guide** )

## Usage

### 1. Synthesize the CloudFormation template

```bash
cd examples/lambda-quickstart
dotnet build
cdk synth
```

This generates a CloudFormation template in `cdk.out/` without requiring AWS credentials.

### 2. Deploy to AWS (sandbox account)

```bash
# Bootstrap CDK (first time only)
cdk bootstrap

# Deploy the stack
cdk deploy
```

### 3. Clean up

```bash
cdk destroy
```

## What’s included

### Default settings

The FsCDK Lambda builder mirrors the defaults promoted in **Production-Ready Serverless**:

- **Memory**: 512 MB (balanced cost/performance baseline)
- **Timeout**: 30 seconds
- **Environment encryption**: KMS (AWS managed key)
- **Log retention**: 1 week via logGroup builder (Corey Quinn cost optimization)
- **Ephemeral storage**: 512 MB (free tier, increase only when needed)
- **IAM role**: Minimal permissions (CloudWatch Logs + KMS decrypt)
- **X-Ray**: Opt-in (enable when you’re ready for tracing)

### Example 1: Basic function
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

lambda "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    code "./lambda-code"
}

(**
Creates a function with all defaults.

### Example 2: Custom memory and timeout
*)

lambda "heavy-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./lambda-code"
    memory 1024
    timeout 120.0
}

(**
Adjusts memory and timeout for compute-intensive workloads.

### Example 3: Environment variables
*)

lambda "api-function" {
    handler "index.handler"
    runtime Runtime.DOTNET_8
    code "./publish"

    environment
        [ "DATABASE_URL", "postgres://localhost/mydb"
          "API_KEY", "secret-key"
          "LOG_LEVEL", "INFO" ]
}

(**
**Security Note**: Environment variables are encrypted at rest using KMS by default.

### Example 4: X-Ray tracing
*)

lambda "traced-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_20_X
    code "./lambda-code"
    xrayEnabled
    description "Function with X-Ray tracing for debugging"
}

(**
Enables AWS X-Ray for distributed tracing.

### Example 5: Cost optimization with custom ephemeral storage
*)

let logGroupItm =
    logGroup "optimized-function-logs" { retention RetentionDays.THREE_DAYS }

lambda "optimized-function" {
    handler "index.handler"
    runtime Runtime.PYTHON_3_11
    code "./lambda-code"
    ephemeralStorageSize 1024 // Increase /tmp storage to 1 GB

    // For custom log retention, use logGroup builder:
    logGroup logGroupItm
}

(**
Fine-tunes cost with custom ephemeral storage for workloads needing more than the default 512 MB /tmp space. Log retention is controlled via the logGroup builder.

## Complete Example Stack
*)

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "LambdaQuickstartStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "FsCDK Lambda Quickstart Example - demonstrates Lambda functions with security defaults"

    tags
        [ "Project", "FsCDK-Examples"
          "Example", "Lambda-Quickstart"
          "ManagedBy", "FsCDK" ]


    // Example 1: Basic function with all defaults
    lambda "basic-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        code "./dummy-code"
        description "Basic Lambda function with secure defaults"
    // Uses defaults:
    // - memory = 512 MB
    // - timeout = 30 seconds
    // - log retention = 1 week (via default logGroup)
    // - ephemeralStorageSize = 512 MB (free tier)
    // - environment encryption = KMS
    }

    // Example 2: Function with custom memory and timeout
    lambda "compute-intensive-function" {
        handler "process.handler"
        runtime Runtime.PYTHON_3_11
        code "./dummy-code"
        memory 2048
        timeout 300.0
        description "Compute-intensive function with higher memory and timeout"
    }

    // Example 3: Function with environment variables (encrypted by default)
    lambda "api-handler-function" {
        handler "api.handler"
        runtime Runtime.NODEJS_20_X
        code "./dummy-code"
        environment [ "LOG_LEVEL", "INFO"; "API_VERSION", "v1"; "REGION", config.Region ]
        description "API handler with encrypted environment variables"
    }

    // Example 4: Function with X-Ray tracing enabled
    lambda "traced-function" {
        handler "traced.handler"
        runtime Runtime.PYTHON_3_11
        code "./dummy-code"
        xrayEnabled
        description "Function with X-Ray tracing for debugging"
    }

    // Example 5: Function with custom log retention
    lambda "dev-function" {
        handler "dev.handler"
        runtime Runtime.DOTNET_8
        code "./dummy-code"
        timeout 60.0
        description "Development function with shorter log retention"
    }

    // Example 6: Function with reserved concurrency
    lambda "rate-limited-function" {
        handler "ratelimited.handler"
        runtime Runtime.NODEJS_18_X
        code "./dummy-code"
        reservedConcurrentExecutions 10
        description "Function with reserved concurrent executions for rate limiting"
    }
}

(**
## IAM Permissions

### Default execution role

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

### Custom role

For advanced scenarios, bring your own execution role—handy when integrating with existing IAM governance models.
*)

let customRole = IAM.createLambdaExecutionRole "my-function" true

lambda "my-function" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    code "./code"
    role customRole
}

(**
## Security Considerations

### Environment variable encryption

All environment variables are encrypted at rest with KMS. This protects:

- API keys and secrets
- Database connection strings
- Configuration values

**Best practice:** Use AWS Secrets Manager or Parameter Store for highly sensitive secrets, matching the approach outlined in the **AWS Security Blog**.

### Least-privilege IAM

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

### Log retention

Logs are retained for 1 week by default (via CloudWatch Log Groups) following Corey Quinn's cost optimization principle: "Never store logs forever." Balance retention with your needs:

- **Development**: 3–7 days (lower cost)
- **Production**: 1–4 weeks (operational visibility)
- **Compliance**: 90 days or longer (regulatory requirements)

To customize, use the `logGroup` builder:
**)

let logGrp = logGroup "MyFunction-logs" { retention RetentionDays.ONE_MONTH }

lambda "MyFunction" {
    handler "index.handler"
    runtime Runtime.NODEJS_18_X
    code "./code"

    logGroup logGrp
}

(*
## Performance Optimization

### Memory configuration

Lambda CPU scales with memory:

- **128-512 MB**: Low-power functions
- **512-1536 MB**: Standard workloads (default: 512 MB)
- **1536-10240 MB**: CPU-intensive tasks

### Ephemeral storage (/tmp)

Lambda provides 512 MB of /tmp storage for free. Increase when processing large files or caching data between invocations (cold starts reuse /tmp):

- **Default**: 512 MB (free)
- **Maximum**: 10,240 MB (charges apply above 512 MB)

Use `ephemeralStorageSize 1024` to customize.

### Timeout

Align timeouts with the latency guidance from **Yan Cui’s Production-Ready Serverless** series:

- **API handlers**: 5–30 seconds (FsCDK default is 30 s)
- **Batch processing**: 60–900 seconds
- **Upper bound**: 15 minutes (Lambda hard limit)

Always keep downstream service timeouts shorter, so the handler fails fast rather than waiting on hung dependencies.

### Cold-start optimisation

Adopt the techniques from **Alex Casalboni’s Lambda Power Tuning** workshop:
- Enable provisioned concurrency for latency-critical APIs.
- Keep deployment packages slim (leverage Lambda layers or bundlers like esbuild).
- Lazy-load heavy dependencies inside the handler instead of at module import time.

## Escape hatch

Need to drop down to raw CDK? `FunctionSpec` exposes the underlying props, so you can opt into niche configurations while still benefiting from FsCDK defaults.
*)

let funcSpec =
    lambda "my-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        code "./code"
    }
// Access props to see configuration
// The actual Function is created by the stack builder

(**
## Next steps

- Pair this quickstart with the [S3 Quickstart](s3-quickstart.html) to build an end-to-end ingestion flow.
- Dive into [IAM Best Practices](iam-best-practices.html) to grant least-privilege permissions.
- Review [Lambda Production Defaults](lambda-production-defaults.html) to understand the guard rails FsCDK applies automatically.

## 📚 Learning resources

All resources below are curated for quality (4.5★+ ratings or repeated recommendations by AWS Heroes).

### Foundation (Week 0)

- **AWS Lambda Developer Guide** – Core concepts straight from the Lambda team.
- **Lambda Operator Guide** – Operational runbooks for scaling and resilience.
- **Getting Started video (Danilo Poccia)** – Step-by-step walkthrough for your first function.

### Hero insights & advanced reading

- **Yan Cui – Production-Ready Serverless** (course) and blog series on concurrency, cold starts, and cost control.
- **Heitor Lessa – Powertools Live Workshop** – Hands-on observability patterns.
- **Alex Casalboni – Lambda Power Tuning** – Automated memory/performance optimisation.
- **AWS Compute Blog – Event-driven design principles** – Official best practices for building reactive systems.

### Performance & cost

- **Lambda Power Tuning** (open source) – Benchmark memory settings automatically.
- **Provisioned Concurrency** deep dive – Keep latency predictable for mission-critical APIs.
- **SnapStart for Java** – Near-zero cold starts for JVM workloads.

### Security & IAM

- **Lambda execution roles** – Official guide to least privilege.
- **Secrets Manager patterns** – Store and refresh credentials securely.
- **VPC networking for Lambda** – Understand ENIs, private subnets, and egress controls.

### Observability

- **Structured logging best practices** (Yan Cui) – Why JSON logs matter.
- **CloudWatch Logs Insights** – Query examples for rapid debugging.
- **Lambda Insights & X-Ray** – Monitor runtime performance and dependencies.

### Suggested learning path
1. Build this quickstart and review the generated CloudFormation.
2. Enable Powertools and explore the tracing/logging features in [Lambda Production Defaults](lambda-production-defaults.html).
3. Model event-driven architectures with [EventBridge](eventbridge.html) and [SNS SQS Messaging](sns-sqs-messaging.html).
4. Subscribe to **Off-by-none** (Jeremy Daly) and watch the latest **re:Invent serverless** sessions to stay current.

### Community hubs

- **Serverless Stack Discord** – Practitioner Q&A and showcase.
- **AWS re:Post (Lambda tag)** – Official support channel.
- **Serverless Chats podcast (Jeremy Daly)** – Interviews with AWS Heroes and product teams.

Continue practising by wiring these Lambdas into S3, DynamoDB, and EventBridge using the other FsCDK notebooks in this portal.
*)


open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open FsCDK

let app = App()

// Get environment configuration from environment variables
let accountId = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
let region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")

// Create stack props with environment
let envProps = StackProps()

if
    not (System.String.IsNullOrEmpty(accountId))
    && not (System.String.IsNullOrEmpty(region))
then
    envProps.Env <- Amazon.CDK.Environment(Account = accountId, Region = region)

envProps.Description <- "FsCDK Lambda Quickstart Example - demonstrates Lambda functions with security defaults"

// Create the stack
let stack = Stack(app, "LambdaQuickstartStack", envProps)

// Apply tags
Tags.Of(stack).Add("Project", "FsCDK-Examples")
Tags.Of(stack).Add("Example", "Lambda-Quickstart")
Tags.Of(stack).Add("ManagedBy", "FsCDK")

// Example 1: Basic function with all defaults
// Note: In a real scenario, provide actual code path
let basicFunc =
    lambda "basic-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        code "./dummy-code"
        description "Basic Lambda function with secure defaults"
    // Uses defaults:
    // - memorySize = 512 MB
    // - timeout = 30 seconds
    // - log retention = 1 week (via default logGroup)
    // - ephemeralStorageSize = 512 MB (free tier)
    // - environment encryption = KMS
    }

// Example 2: Function with custom memory and timeout
let computeFunc =
    lambda "compute-intensive-function" {
        handler "process.handler"
        runtime Runtime.PYTHON_3_11
        code "./dummy-code"
        memory 2048
        timeout 300.0
        description "Compute-intensive function with higher memory and timeout"
    }

// Example 3: Function with environment variables (encrypted by default)
let apiFunc =
    lambda "api-handler-function" {
        handler "api.handler"
        runtime Runtime.NODEJS_20_X
        code "./dummy-code"

        environment
            [ "LOG_LEVEL", "INFO"
              "API_VERSION", "v1"
              "REGION",
              (if System.String.IsNullOrEmpty(region) then
                   "us-east-1"
               else
                   region) ]

        description "API handler with encrypted environment variables"
    }

// Example 4: Function with X-Ray tracing enabled
let tracedFunc =
    lambda "traced-function" {
        handler "traced.handler"
        runtime Runtime.PYTHON_3_11
        code "./dummy-code"
        xrayEnabled
        description "Function with X-Ray tracing for debugging"
    }

// Example 5: Function with reserved concurrency
let rateLimitedFunc =
    lambda "rate-limited-function" {
        handler "ratelimited.handler"
        runtime Runtime.NODEJS_18_X
        code "./dummy-code"
        reservedConcurrentExecutions 10
        description "Function with reserved concurrent executions for rate limiting"
    }

app.Synth() |> ignore
