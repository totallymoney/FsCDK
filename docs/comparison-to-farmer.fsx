(**
---
title: Comparison with Azure (Farmer F#) to AWS (FsCDK)
category: docs
index: 2
---

# Comparison with Azure (Farmer F#) to AWS (FsCDK)

This guide helps developers familiar with [Farmer](https://compositionalit.github.io/farmer/) (the F# DSL for Azure) transition to FsCDK for AWS infrastructure.

## Philosophy

Both Farmer and FsCDK share similar goals:
- **Type Safety**: Catch configuration errors at compile time
- **Security by Default**: Follow cloud provider best practices
- **Composability**: Build reusable infrastructure components
- **F# Idioms**: Use computation expressions and functional patterns

## Conceptual Mapping

### Azure → AWS Service Mapping

| Azure Service | Farmer | AWS Service | FsCDK |
|---------------|--------|-------------|-------|
| **Storage** |
| Blob Storage | `storageAccount` | S3 | `s3Bucket` |
| Storage Queue | `storageQueue` | SQS | `queue` |
| Table Storage | `table` | DynamoDB | `table` |
| **Compute** |
| App Service | `webApp` | Elastic Beanstalk | `eb` |
| Azure Functions | `functions` | Lambda | `lambdaFunction` |
| Container Instances | `containerGroup` | ECS/Fargate | `ecs` |
| Virtual Machine | `vm` | EC2 | `ec2` |
| AKS | `aks` | EKS | (planned) |
| **Database** |
| Cosmos DB | `cosmosDb` | DynamoDB | `table` |
| Azure SQL | `sqlServer` | RDS | `rds` |
| PostgreSQL | `postgreSql` | RDS PostgreSQL | `rds` |
| **Networking** |
| Virtual Network | `vnet` | VPC | `vpc` |
| Load Balancer | `loadBalancer` | ALB/NLB | `applicationLoadBalancer` |
| Application Gateway | `appGateway` | ALB | `applicationLoadBalancer` |
| DNS | `dns` | Route53 | Route53 |
| **Security** |
| Key Vault | `keyVault` | Secrets Manager | `secret` |
| Managed Identity | `identity` | IAM Role | IAM helpers |
| **Messaging** |
| Event Hub | `eventHub` | Kinesis | (planned) |
| Service Bus | `serviceBus` | SNS/SQS | `topic`/`queue` |

## Code Examples

### Storage: Blob Storage → S3

**Farmer (Azure Blob Storage):**
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

(**
```fsharp
open Farmer
open Farmer.Builders

let storage = storageAccount {
    name "mystorageaccount"
    sku Storage.Sku.Standard_LRS
}

let deployment = arm {
    location Location.EastUS
    add_resource storage
}
```

**FsCDK (AWS S3):**
*)

open FsCDK
open Amazon.CDK.AWS.S3

stack "MyStack" {
    s3Bucket "my-bucket" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
    }
}

(**
### Compute: Azure Functions → Lambda

**Farmer (Azure Functions):**
```fsharp
let myFunction = functions {
    name "myfunctionapp"
    service_plan_name "myserviceplan"
    runtime_stack Runtime.DotNet80
    operating_system OS.Linux
    
    setting "StorageConnection" storageConnection
}
```

**FsCDK (AWS Lambda):**
*)

open Amazon.CDK.AWS.Lambda

lambdaFunction "my-function" {
    handler "MyApp::MyApp.Handler::FunctionHandler"
    runtime Runtime.DOTNET_8
    codePath "./publish"
    memorySize 512
    timeout 30.0
    environment [ "KEY", "value" ]
}

(**
### Database: Cosmos DB → DynamoDB

**Farmer (Cosmos DB):**
```fsharp
let cosmos = cosmosDb {
    name "mycosmosdb"
    account_name "mycosmosaccount"
    throughput 400<RUs>
    failover_policy NoFailover
    consistency_policy Eventual
}
```

**FsCDK (DynamoDB):**
*)

open Amazon.CDK.AWS.DynamoDB

table "my-table" {
    partitionKey "id" AttributeType.STRING
    sortKey "timestamp" AttributeType.NUMBER
    billingMode BillingMode.PAY_PER_REQUEST
}

(**
### Networking: VNet → VPC

**Farmer (Azure VNet):**
```fsharp
let vnet = vnet {
    name "myvnet"
    add_address_spaces [ "10.0.0.0/16" ]
    
    add_subnets [
        subnet {
            name "webapp-subnet"
            prefix "10.0.1.0/24"
        }
        subnet {
            name "db-subnet"
            prefix "10.0.2.0/24"
        }
    ]
}
```

**FsCDK (AWS VPC):**
*)

open Amazon.CDK.AWS.EC2

vpc "my-vpc" {
    cidr "10.0.0.0/16"
    maxAzs 2
    natGateways 1
}

(**
### Security: Managed Identity → IAM Role

**Farmer (Managed Identity):**
```fsharp
let identity = createUserAssignedIdentity "myidentity"

let webApp = webApp {
    name "mywebapp"
    add_identity identity
}
```

**FsCDK (IAM Role):**
```fsharp
open FsCDK

// Create execution role for Lambda
let role = IAM.createLambdaExecutionRole "my-function" true

// Or create custom role
let customRole = IAM.createRole "lambda.amazonaws.com" "my-custom-role"
```

## Key Differences

### 1. Stack Model

**Farmer:**
- Uses ARM templates
- Deployment at resource group level
- Single region per deployment

**FsCDK:**
- Uses AWS CloudFormation
- Stack-based deployment
- Multi-region support

### 2. Naming Conventions

**Farmer:**
- Resource names are globally unique or scoped to resource group
- Naming restrictions vary by service

**FsCDK:**
- Most resources autogenerate unique names
- Logical IDs separate from physical names
- More flexible naming

### 3. Security Defaults

**Farmer:**
- Follows Azure best practices
- Managed identities for authentication
- Network security groups

**FsCDK:**
- Follows AWS Well-Architected Framework
- IAM roles and policies
- Security groups and NACLs

### 4. Deployment Process

**Farmer:**
```bash
# Generate ARM template
dotnet run

# Deploy with Azure CLI
az deployment group create \
    --resource-group mygroup \
    --template-file output.json
```

**FsCDK:**
```bash
# Synthesize CloudFormation
cdk synth

# Deploy with CDK
cdk deploy
```

## Migration Checklist

- [ ] Map Azure services to AWS equivalents
- [ ] Update authentication (Managed Identity → IAM Role)
- [ ] Adapt networking concepts (VNet → VPC, NSG → Security Group)
- [ ] Modify storage patterns (Blob → S3, Queue → SQS)
- [ ] Update database configurations (Cosmos → DynamoDB, SQL → RDS)
- [ ] Review security defaults and adjust as needed
- [ ] Test with `cdk synth` before deploying
- [ ] Update CI/CD pipelines for AWS deployment

## Common Patterns

### Environment Configuration

**Farmer:**
```fsharp
let env = Environment.GetEnvironmentVariable

let config = {
    Environment = env "ENVIRONMENT" |> Option.defaultValue "dev"
    Location = Location.EastUS
}
```

**FsCDK:**
*)

open Amazon.CDK

// Use environment variables or defaults for AWS account/region
let accountId = 
    System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") 
    |> Option.ofObj 
    |> Option.defaultValue "000000000000"
let regionName = 
    System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") 
    |> Option.ofObj 
    |> Option.defaultValue "us-east-1"

stack "MyStack" {
    environment {
        account accountId
        region regionName
    }
}

(**
### Resource Composition

**Farmer:**
```fsharp
let storage = storageAccount { name "storage" }
let webApp = webApp {
    name "webapp"
    depends_on storage
}

let deployment = arm {
    add_resources [ storage; webApp ]
}
```

**FsCDK:**
*)

stack "MyStack" {
    let bucket = s3Bucket "my-bucket" { }
    
    lambdaFunction "my-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        codePath "./code"
        // Lambda can read from bucket (configure IAM separately)
    }
}

(**
### Tagging

**Farmer:**
```fsharp
let deployment = arm {
    add_resource myResource
    add_tags [ "Environment", "Production"; "Owner", "Team" ]
}
```

**FsCDK:**
```fsharp
open FsCDK

stack "MyStack" {
    Tags.tagStack this "my-project" "production" (Some "team@example.com")
    
    // Resources...
}
```

## Best Practices

### 1. Start Small
Begin with a single service or component, test thoroughly, then expand.

### 2. Use Existing Patterns
Study the examples in `/examples` directory for common patterns.

### 3. Security First
Review FsCDK's security defaults and adjust for your requirements.

### 4. Test Locally
Use `cdk synth` to generate CloudFormation templates and review them before deploying.

### 5. Leverage Escape Hatches
When FsCDK builders don't cover your use case, access underlying CDK constructs.

## Resources

- [FsCDK Documentation](https://totallymoney.github.io/FsCDK/)
- [FsCDK Examples](lambda-quickstart.html)
- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/latest/guide/home.html)
- [Farmer Documentation](https://compositionalit.github.io/farmer/)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)

## Getting Help

- GitHub Issues: Bug reports and feature requests
- GitHub Discussions: Questions and community support
- Stack Overflow: Tag questions with `fscdk` or `aws-cdk`

---

Welcome to FsCDK! We're excited to have Farmer users in the community. 🎉
*)
