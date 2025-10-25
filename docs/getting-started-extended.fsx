(**
---
title: Getting Started with FsCDK - Extended Features
category: docs
index: 3
---

# Getting Started with FsCDK - Extended Features

Welcome to the extended FsCDK! This guide will help you understand the new features and how to use them effectively.

## What's New?

FsCDK now includes builders for:

- 🌐 **VPC & Networking** - Secure, multi-AZ virtual private clouds
- 🗄️ **RDS PostgreSQL** - Managed databases with automated backups
- ⚡ **CloudFront** - Global CDN for fast content delivery
- 🔐 **Cognito** - User authentication with MFA support

All following AWS Well-Architected Framework best practices!

## Quick Start Examples

### 1. Create a Secure VPC
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK.AWS.EC2

vpc "MyVpc" {
    maxAzs 2 // Multi-AZ for high availability
    natGateways 1 // Cost-optimized
    cidr "10.0.0.0/16" // IP address range
}

(**
**What you get:**

- 2 public subnets (one per AZ)
- 2 private subnets with NAT gateway access
- DNS hostnames and support enabled
- Best practices baked in!

### 2. Add a PostgreSQL Database
*)

open Amazon.CDK.AWS.RDS

(**
Here's how to add a PostgreSQL database (shown in full stack example below):

```fsharp
rdsInstance "MyDatabase" {
    vpc myVpc
    postgresEngine                     // PostgreSQL 15
    instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
    allocatedStorage 20

    // Automatic best practices:
    // ✅ Encrypted storage
    // ✅ 7-day automated backups
    // ✅ Auto minor version upgrades
    // ✅ Private subnet placement

    multiAz true                       // High availability
    databaseName "myapp"
}
```

**What you get:**

- Encrypted database in private subnet
- Automated backups with 7-day retention
- Multi-AZ replication for HA
- Not publicly accessible (secure!)

### 3. Set Up User Authentication
*)

open Amazon.CDK.AWS.Cognito

// Create user pool
let myUserPool =
    userPool "MyUserPool" {
        signInWithEmail // Users sign in with email
        selfSignUpEnabled true // Allow self-registration
        mfa Mfa.OPTIONAL // Users can enable MFA

    // Automatic best practices:
    // ✅ Strong password policy (8+ chars, mixed case, digits, symbols)
    // ✅ Email verification required
    // ✅ Account recovery via email
    }

// Create app client
userPoolClient "MyAppClient" {
    userPool myUserPool
    generateSecret false // For web/mobile apps

// Automatic best practices:
// ✅ SRP authentication flow
// ✅ Prevents user existence errors
// ✅ Reasonable token expiration times
}

(**
**What you get:**

- Secure authentication with strong passwords
- Email verification out of the box
- MFA support for enhanced security
- Industry-standard OAuth flows

### 4. Add a CDN for Fast Delivery
*)

open Amazon.CDK.AWS.CloudFront

(*** hide ***)
let myBehavior =
    CloudFrontBehaviors.httpBehaviorDefault "origin.example.com" (Some true)

cloudFrontDistribution "MyCDN" {
    defaultBehavior myBehavior // Your origin configuration
    defaultRootObject "index.html"

// Automatic best practices:
// ✅ HTTP/2 enabled
// ✅ TLS 1.2 minimum
// ✅ IPv6 enabled
// ✅ Cost-optimized (US/Canada/Europe)
}

(**
**What you get:**

- Global content delivery network
- Modern protocols (HTTP/2, IPv6)
- Secure by default (TLS 1.2+)
- Cost-optimized for common use cases

## Complete Application Stack

Here's how to combine everything into a production-ready stack:
*)

open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.Lambda

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

(*** hide ***)
let cdnBehavior =
    CloudFrontBehaviors.httpBehaviorDefault "origin.example.com" (Some true)

stack "ProductionApp" {

    environment {
        account config.Account
        region config.Region
    }

    description "Production application stack"
    tags [ "environment", "production"; "managed-by", "FsCDK" ]

    // 1. Network layer
    let myVpc =
        vpc "AppVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    // 2. Database layer
    rdsInstance "AppDatabase" {
        vpc myVpc
        postgresEngine
        multiAz true
        allocatedStorage 20
        databaseName "myapp"
    }

    // 3. Storage layer
    bucket "StaticAssets" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
    }

    // 4. Compute layer
    lambda "ApiHandler" {
        runtime Runtime.DOTNET_8
        handler "MyApp.Api::Handler::handle"
        code "../MyApp.Api/bin/Release/net8.0/publish"
        timeout 30.0
        memory 512

        environment [ "DATABASE_HOST", "dbEndpoint"; "DATABASE_NAME", "myapp" ]
    }

    // 5. Auth layer
    userPool "AppUsers" {
        signInWithEmail
        selfSignUpEnabled true
        mfa Mfa.OPTIONAL
    }

    // 6. CDN layer
    cloudFrontDistribution "AppCDN" {
        defaultBehavior cdnBehavior
        defaultRootObject "index.html"
    }
}

(**
## Best Practices Baked In

FsCDK automatically applies AWS best practices:

### Security 🔒
- Encryption enabled by default (S3, RDS)
- Security groups deny all by default
- Strong password policies
- Private subnet placement for databases
- No public database access

### High Availability 🏢
- Multi-AZ VPC configuration
- Multi-AZ database replication
- Automated backups (7-day retention)
- Global CDN distribution

### Cost Optimization 💰
- Right-sized instance defaults (t3.micro)
- Single NAT gateway for dev/test
- Regional CDN pricing (PriceClass100)
- Pay-per-request database options

### Performance ⚡
- HTTP/2 enabled for CDN
- IPv6 support
- Proper subnet segmentation
- Monitoring capabilities built-in

## Common Patterns

### Pattern 1: Web Application with Auth
*)
stack "WebApp" {
    vpc "WebVpc" { () }

    userPool "Users" {
        signInWithEmail
        selfSignUpEnabled true
    }

    lambda "Api" {
        runtime Runtime.DOTNET_8
        handler "Api::Handler"
        code "./publish"
    }

    bucket "Assets" { versioned true }
}

(**
### Pattern 2: Data Processing Pipeline
*)

stack "DataPipeline" {
    let myVpc = vpc "DataVpc" { () }

    rdsInstance "DataWarehouse" {
        vpc myVpc
        postgresEngine
        multiAz true
    }

    lambda "Processor" {
        runtime Runtime.DOTNET_8
        handler "Processor::Handler"
        code "./publish"
        timeout 300.0 // 5 minutes for data processing
        memory 1024 // More memory for processing
    }

    bucket "DataLake" {
        versioned true
    // lifecycleRules [ /* ... */ ]
    }
}

(**
### Pattern 3: Serverless API with CDN
*)

open Amazon.CDK.AWS.DynamoDB

(*** hide ***)
let apiOrigin =
    CloudFrontBehaviors.httpBehaviorDefault "origin.example.com" (Some true)

stack "ServerlessApi" {
    lambda "GetUsers" {
        runtime Runtime.DOTNET_8
        handler "Api::GetUsers"
        code "./publish"
    }

    lambda "CreateUser" {
        runtime Runtime.DOTNET_8
        handler "Api::CreateUser"
        code "./publish"
    }

    table "Users" {
        partitionKey "userId" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
    }

    cloudFrontDistribution "ApiCDN" { defaultBehavior apiOrigin }
}

(**
## Migration from Existing FsCDK

Good news! **No breaking changes!** Your existing code continues to work:
*)

// Your existing code still works!
stack "MyStack" {
    lambda "MyFunction" {
        runtime Runtime.DOTNET_8
        handler "index.handler"
        code "./lambda"
    }

    table "MyTable" { partitionKey "id" AttributeType.STRING }
}

// Just add new features when you need them
stack "EnhancedStack" {
    // Existing services
    lambda "MyFunction" {
        runtime Runtime.DOTNET_8
        handler "index.handler"
        code "./lambda"
    }

    // New services
    let myVpc = vpc "MyVpc" { () }

    rdsInstance "MyDB" {
        vpc myVpc
        postgresEngine
    }

    userPool "MyAuth" { signInWithEmail }
}

(**
## Learning Resources

1. **Examples**: See [Multi-Tier Example](multi-tier-example.html) for a complete application
2. **Security**: Read [IAM Best Practices](iam-best-practices.html) for security guidance
3. **API Docs**: Check XML documentation comments in code
4. **AWS Docs**: [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)

## Next Steps

1. ✅ Install FsCDK: `dotnet add package FsCDK`
2. ✅ Create your first VPC: `vpc "MyVpc" { }`
3. ✅ Add a database: `rdsInstance "MyDB" { vpc myVpc; postgresEngine }`
4. ✅ Deploy: `cdk deploy`
5. ✅ Monitor in AWS Console

## Getting Help

- 📖 Read the examples in the docs
- 🔍 Check the test files for usage patterns
- 💬 Ask questions in GitHub Issues
- 📚 Reference AWS CDK documentation

## Tips for Success

1. **Start Small**: Begin with a simple VPC or database
2. **Use Defaults**: FsCDK defaults are production-ready
3. **Review Generated Templates**: Run `cdk synth` to see CloudFormation
4. **Test Incrementally**: Deploy small changes frequently
5. **Follow Best Practices**: Read the IAM best practices guide

## Cost Management

Monitor your costs:

- Use `aws ce get-cost-and-usage` CLI command
- Set up AWS Budgets
- Review the cost estimates in examples
- Start with smaller instance types
- Use t3.micro for development

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Deployment Issues
```bash
# Verify CDK bootstrap
cdk bootstrap aws://ACCOUNT/REGION

# Check diff before deploying
cdk diff

# Deploy with verbose output
cdk deploy --verbose
```

### Resource Already Exists
```bash
# Import existing resources
cdk import

# Or use different names
vpc "MyVpc-v2" { ... }
```

## What's Coming Next?

Future enhancements may include:

- Application Load Balancer (ALB)
- Network Load Balancer (NLB)
- Route53 DNS zones
- Additional database engines
- ECS/Fargate support

---

Ready to build secure, scalable infrastructure with F#? Let's go! 🚀
*)

(*** hide ***)
()
