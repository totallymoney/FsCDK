(**
---
title: Local Testing with LocalStack
category: docs
index: 50
---

# Local Testing with LocalStack

Test your FsCDK infrastructure locally without deploying to AWS using [LocalStack](https://localstack.cloud/).

## What is LocalStack?

LocalStack emulates AWS services on your local machine, enabling:
- Fast iteration without AWS deployment delays
- Zero AWS costs during development
- Offline development and testing
- Integration tests in CI/CD pipelines

## Quick Setup

### 1. Install LocalStack

```bash
# Using pip
pip install localstack

# Using Homebrew (macOS)
brew install localstack/tap/localstack-cli

# Using Docker directly
docker run -d -p 4566:4566 localstack/localstack
```

### 2. Install CDK Local Wrapper

```bash
npm install -g aws-cdk-local
```

### 3. Start LocalStack

```bash
localstack start -d
```

## Using FsCDK with LocalStack

Your FsCDK code works unchanged! Just use `cdklocal` instead of `cdk`:

```bash
# Synthesize CloudFormation template
cdklocal synth

# Deploy to LocalStack
cdklocal deploy

# Destroy stack
cdklocal destroy
```

## Example: Testing a Lambda Function

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.DynamoDB

// Define your stack normally
stack "LocalTestStack" {
    description "Stack for local testing with LocalStack"

    table "Users" {
        partitionKey "userId" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
    }

    lambda "UserFunction" {
        runtime Runtime.DOTNET_8
        handler "Handler::process"
        code "./publish"
        environment [ "TABLE_NAME", "Users" ]
    }
}

(**

Deploy to LocalStack:

```bash
cdklocal deploy LocalTestStack
```

Test your function:

```bash
# Invoke Lambda locally
aws --endpoint-url=http://localhost:4566 lambda invoke \
    --function-name UserFunction \
    response.json

# Query DynamoDB locally
aws --endpoint-url=http://localhost:4566 dynamodb scan \
    --table-name Users
```

## Integration Testing Example

Use LocalStack in your test suite:

```fsharp
open Expecto
open System.Diagnostics

[<Tests>]
let integrationTests =
    testList "LocalStack Integration" [
        test "can deploy and invoke lambda" {
            // Deploy to LocalStack
            let deploy = Process.Start("cdklocal", "deploy --require-approval never")
            deploy.WaitForExit()
            Expect.equal deploy.ExitCode 0 "Deployment should succeed"
            
            // Test your resources via AWS CLI with local endpoint
            // ...
        }
    ]
```

## Configuration

LocalStack uses standard AWS environment variables:

```bash
# Point AWS SDK to LocalStack
export AWS_ENDPOINT_URL=http://localhost:4566

# Use dummy credentials (LocalStack doesn't validate)
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1
```

## Supported Services

LocalStack supports 80+ AWS services. Most FsCDK resources work including:
- Lambda, DynamoDB, S3, SQS, SNS
- API Gateway, EventBridge, Step Functions
- VPC, EC2, RDS (basic functionality)
- CloudWatch, IAM, Secrets Manager

Check [LocalStack coverage](https://docs.localstack.cloud/references/coverage/) for specific service support.

## Limitations

- Some advanced features may not be fully emulated
- Performance characteristics differ from real AWS
- Not a replacement for staging environment testing
- Use for local development and unit/integration tests

## CI/CD Integration

Add LocalStack to GitHub Actions:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: LocalStack/setup-localstack@v0.2.0
      - run: cdklocal deploy
      - run: dotnet test
```

## Resources

- [LocalStack Documentation](https://docs.localstack.cloud/)
- [CDK Local GitHub](https://github.com/localstack/aws-cdk-local)
- [AWS CLI with LocalStack](https://docs.localstack.cloud/user-guide/integrations/aws-cli/)

---

**Pro Tip**: Start with LocalStack for rapid iteration, then verify in a real AWS dev account before production deployment.

*)
