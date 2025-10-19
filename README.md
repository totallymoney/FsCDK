<div align="center">
  <img src="assets/logo/fscdn-logo-constructs.svg" alt="FsCDK Logo" width="800" />
</div>

<div align="center">

[![Build](https://github.com/totallymoney/FsCDK/actions/workflows/build.yml/badge.svg)](https://github.com/totallymoney/FsCDK/actions/workflows/build.yml)

</div>

FsCDK is a robust F# library for AWS Cloud Development Kit (CDK), enabling you to define cloud infrastructure using F#'s type safety and functional programming features. It provides a natural F# interface to AWS CDK, allowing you to build reliable and maintainable cloud infrastructure as code.

## Features

- **Type-Safe Infrastructure**: Leverage F#'s strong type system to catch configuration errors at compile time
- **Functional-First Approach**: Use F#'s functional programming features to create reusable and composable infrastructure components
- **Native AWS CDK Integration**: Full access to AWS CDK constructs and patterns with F#-friendly APIs
- **Immutable Infrastructure**: Define your infrastructure using immutable constructs, promoting reliable and predictable deployments
- **IDE Support**: Excellent tooling support with type hints and IntelliSense in your favorite F# IDE
- **AWS Best Practices**: Built-in defaults following AWS Well-Architected Framework principles

### Supported AWS Services

- **Compute**: Lambda functions (including Docker), Lambda layers, EC2 instances
- **Containers**: ECS clusters, Fargate services
- **Load Balancing**: Application Load Balancer (ALB)
- **Storage**: S3 buckets with lifecycle rules, versioning, and CORS
- **Database**: DynamoDB tables, RDS PostgreSQL with automated backups and encryption
- **Networking**: VPC with multi-AZ support, Security Groups with least-privilege defaults
- **CDN**: CloudFront distributions with HTTP/2 and IPv6
- **Authentication**: Cognito User Pools and Clients with MFA support
- **Messaging**: SNS topics, SQS queues with dead-letter queue support
- **Secrets**: Secrets Manager for secure credential storage
- **DNS**: Route 53 hosted zones and record sets
- **Platform**: Elastic Beanstalk applications and environments
- **IAM**: Policy statements and permission management

## Quick Start

### New High-Level Builders (Recommended)

FsCDK now includes enhanced builders with security defaults following AWS Well-Architected Framework:

```fsharp
open FsCDK
open FsCDK.Storage
open FsCDK.Compute

let config = Config.get ()

stack "MyStack" {
    app {
        context "environment" "production"
    }

    environment {
        account config.Account
        region config.Region
    }
    
    stackProps {
        stackEnv
        description "My secure infrastructure"
    }

    // S3 bucket with KMS encryption and blocked public access (defaults)
    s3Bucket "my-secure-bucket" {
        versioned true
        LifecycleRuleHelpers.expireAfter 30 "cleanup-old-data"
    }

    // Lambda with encrypted env vars and minimal IAM permissions (defaults)
    lambdaFunction "my-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_20_X
        codePath "./code"
        memorySize 512
        timeout 30.0
        environment [ "KEY", "value" ]
    }
}
```

### Original Builders (Still Supported)

1. Install the package:
```fsharp
dotnet add package FsCDK
```

1. Create your first stack:
```fsharp
open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2
open FsCDK

let config = Config.get () // Load Environment Variables

stack "MyFirstStack" {
    app {
        context "environment" "production"
        context "feature-flag" true
        context "version" "1.2.3"
    }

    environment {
        account config.Account
        region config.Region
    }
    
    stackProps {
        stackEnv
        description "My first CDK stack in F#"
        tags [ "project", "FsCDK"; "owner", "me" ]
    }

    // VPC with AWS best practices (Multi-AZ, DNS enabled)
    vpc "MyVpc" {
        maxAzs 2
        natGateways 1
        cidr "10.0.0.0/16"
    }

    // Lambda function
    lambda "Playground-SayHello" {
        runtime Runtime.DOTNET_8
        handler "Playground::Playground.Handlers::sayHello"
        code "../Playground/bin/Release/net8.0/publish"
        timeout 30.0
        memory 256
        description "A simple hello world lambda"
    }

    // S3 bucket with encryption
    bucket "MyBucket" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
    }

    // DynamoDB table
    table "MyTable" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
    }

    // Cognito User Pool with security defaults
    userPool "MyUserPool" {
        signInWithEmail
        selfSignUpEnabled true
        mfa Mfa.OPTIONAL
    }
    
    // EC2 instance (virtual machine)
    ec2Instance "MyWebServer" {
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        machineImage (MachineImage.LatestAmazonLinux2())
        vpc myVpc
    }
    
    // ECS cluster for container orchestration
    ecsCluster "MyCluster" {
        vpc myVpc
        containerInsights ContainerInsights.ENABLED
    }
    
    // Application Load Balancer
    applicationLoadBalancer "MyALB" {
        vpc myVpc
        internetFacing true
    }
    
    // Secrets Manager secret
    secret "MyApiKey" {
        description "API key for external service"
        generateSecretString (SecretsManagerHelpers.generatePassword 32 None)
    }
    
    // Route 53 DNS zone
    hostedZone "example.com" {
        comment "Production domain"
    }
}
```

1. Deploy your infrastructure:
```bash
cdk synth   # Review the generated CloudFormation template
cdk deploy  # Deploy to AWS
```

## Documentation

For detailed documentation, examples, and best practices, visit our [Documentation Site](https://totallymoney.github.io/FsCDK/).

## AWS Best Practices Built-In

FsCDK follows AWS Well-Architected Framework principles with sensible defaults:

### Security
- **Encryption by default**: S3 buckets, RDS databases use encryption
- **Least privilege**: Security groups deny all outbound by default
- **Strong authentication**: Cognito with 8+ character passwords, MFA support
- **Prevent user enumeration**: Cognito clients prevent user existence errors

### High Availability  
- **Multi-AZ by default**: VPCs span multiple availability zones (default: 2)
- **Automated backups**: RDS with 7-day retention by default
- **Versioning support**: S3 bucket versioning available

### Cost Optimization
- **Right-sized defaults**: RDS t3.micro, single NAT gateway for dev/test
- **Pay-per-request**: DynamoDB billing mode options
- **Regional distribution**: CloudFront PriceClass100 (US/Canada/Europe)

### Performance
- **HTTP/2**: CloudFront distributions use HTTP/2 by default
- **IPv6**: Enabled by default for CloudFront and VPC
- **Caching**: CloudFront with optimized cache policies

### Operational Excellence
- **Auto minor upgrades**: RDS databases auto-upgrade minor versions
- **DNS enabled**: VPC DNS hostnames and support enabled
- **Monitoring ready**: Performance Insights support for RDS

## Examples

### Quickstart Examples

- **[S3 Quickstart](./examples/s3-quickstart/)**: Create secure S3 buckets with encryption, lifecycle rules, and versioning
- **[Lambda Quickstart](./examples/lambda-quickstart/)**: Deploy Lambda functions with encrypted environment variables and minimal IAM permissions

### Additional Examples

Check out the [samples directory](./samples) for complete examples of common infrastructure patterns implemented with FsCDK.

## Contributing

Contributions are welcome! Whether it's:
- Reporting a bug
- Submitting a fix
- Proposing new features

Please check out our [Contributing Guide](CONTRIBUTING.md) for guidelines about how to proceed.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
