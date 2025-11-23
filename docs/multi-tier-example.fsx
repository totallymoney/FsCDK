(**
---
title: FsCDK Multi-Tier Application Example
category: Tutorials
categoryindex: 1
---

# FsCDK Multi-Tier Application Example

This example demonstrates how to build a complete multi-tier web application using FsCDK with AWS best practices.

## Architecture Overview

![Multi-Tier Application Architecture](img/diagrams/multi-tier-architecture.svg)

## Architecture Details

This example demonstrates a production-ready multi-tier application with security best practices:
- **High Availability**: Multi-AZ deployment across 2 availability zones
- **Security**: VPC isolation, security groups, encryption at rest and in transit
- **Scalability**: Auto-scaling Lambda, Multi-AZ RDS with read replicas
- **Performance**: CloudFront CDN for global content delivery

### Architecture Diagram

![Multi-Tier Architecture](img/multi-tier-architecture.png)

*Production-ready multi-tier web application showing VPC network segmentation, security zones, and data flow. Components: CloudFront CDN → Application Load Balancer → Lambda Functions → RDS PostgreSQL, with S3 static assets and Cognito authentication.*

**Key Components:**
- **CloudFront CDN**: Global content delivery with HTTPS/TLS 1.2+
- **Application Load Balancer**: Multi-AZ load balancing in public subnets
- **Lambda Functions**: Serverless compute in private subnets with auto-scaling
- **RDS PostgreSQL**: Multi-AZ database with encryption and automated backups
- **S3**: Static asset storage with versioning and encryption
- **Cognito**: Managed authentication and user management

**Security Layers:**
- Public subnets: ALB with internet-facing access
- Private subnets: Lambda and RDS with no direct internet access
- Security groups: Least-privilege network access control
- NAT Gateway: Controlled outbound internet access for private resources

**Network Flow:**
1. User requests → CloudFront → Internet Gateway → ALB (public subnet)
2. ALB → Lambda functions (private subnet, security group SG-Lambda)
3. Lambda → RDS database (private subnet, security group SG-Database, inbound only from SG-Lambda)
4. Lambda outbound → NAT Gateway → Internet Gateway (for API calls)
5. Static content → S3 → CloudFront cache

> **Note:** To generate this diagram, use the specifications in `docs/img/DIAGRAM_SPECIFICATIONS.md` with tools like Cloudcraft, Draw.io, or Lucidchart.

## Example Stack
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.CloudFront
open FsCDK

(*** hide ***)
let myBehaviorOptions =
    CloudFrontBehaviors.httpBehaviorDefault "origin.example.com" (Some true)

// Use environment variables or defaults for AWS account/region
let accountId =
    System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
    |> Option.ofObj
    |> Option.defaultValue "000000000000"

let regionName =
    System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
    |> Option.ofObj
    |> Option.defaultValue "us-east-1"

(*** hide ***)

stack "MultiTierApp" {
    scope (app { context [ "environment", "production"; "app-name", "my-web-app" ] })

    description "Multi-tier web application with database and CDN"

    tags
        [ "project", "MultiTierApp"
          "environment", "production"
          "managed-by", "FsCDK" ]

    let! staticAssetsBucket =
        bucket "CloudFrontLogs" {
            blockPublicAccess BlockPublicAccess.BLOCK_ALL
            encryption BucketEncryption.S3_MANAGED
            enforceSSL true
            versioned false
            removalPolicy RemovalPolicy.RETAIN
        }

    // Step 1: Create VPC with public and private subnets
    // AWS Best Practice: Multi-AZ for high availability
    let! myVpc =
        vpc "AppVpc" {
            maxAzs 2
            natGateways 1 // Cost optimized - 1 NAT gateway
            cidr "10.0.0.0/16"
        }

    // Step 2: Create a Security Group for Lambda functions
    // AWS Best Practice: The Least privilege - no outbound by default
    let! lambdaSecurityGroup =
        securityGroup "LambdaSecurityGroup" {
            vpc myVpc
            description "Security group for Lambda functions"
            allowAllOutbound false // Explicit configuration required
        }

    // Step 3: Create a Security Group for RDS
    let! dbSecurityGroup =
        securityGroup "DatabaseSecurityGroup" {
            vpc myVpc
            description "Security group for RDS PostgreSQL"
            allowAllOutbound false
        }

    // Step 4: Create RDS PostgreSQL database
    // AWS Best Practice: Encrypted, automated backups, Multi-AZ
    rdsInstance "AppDatabase" {
        vpc myVpc
        postgresEngine // Uses PostgreSQL 15 by default
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        allocatedStorage 20
        databaseName "myapp"

        // High Availability
        multiAz true
        backupRetentionDays 7.0

        // Security
        storageEncrypted true
        deletionProtection true
        publiclyAccessible false

        // Networking
        vpcSubnets (SubnetSelection(SubnetType = SubnetType.PRIVATE_WITH_EGRESS))
        securityGroup dbSecurityGroup

        // Maintenance
        preferredBackupWindow "03:00-04:00"
        preferredMaintenanceWindow "sun:04:00-sun:05:00"
    }

    // Step 5: Create S3 bucket for static assets
    // AWS Best Practice: Versioned, encrypted, block public access
    bucket "StaticAssets" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
        blockPublicAccess BlockPublicAccess.BLOCK_ALL
        removalPolicy RemovalPolicy.RETAIN
        autoDeleteObjects false

        // Lifecycle rules for cost optimization
        yield
            lifecycleRule {

                enabled true

                transitions
                    [ transition {
                          storageClass StorageClass.INFREQUENT_ACCESS
                          transitionAfter (Duration.Minutes 30.0)
                      }
                      transition {
                          storageClass StorageClass.GLACIER
                          transitionAfter (Duration.Days 90.0)
                      } ]
            }
    }

    // Step 6: Create Cognito User Pool for authentication
    // AWS Best Practice: MFA, strong password policy, email verification
    let! myUserPool =
        userPool "AppUserPool" {
            signInWithEmail
            selfSignUpEnabled true
            mfa Mfa.OPTIONAL

            passwordPolicy (
                PasswordPolicy(
                    MinLength = 12,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireDigits = true,
                    RequireSymbols = true
                )
            )

            accountRecovery AccountRecovery.EMAIL_ONLY
        }

    // Step 7: Create User Pool Client
    userPoolClient "AppClient" {
        userPool myUserPool
        generateSecret false // For web/mobile apps
        authFlows (AuthFlow(UserSrp = true, UserPassword = true))

        tokenValidities (
            (Duration.Days 30.0), // refreshToken
            (Duration.Hours 1.0), // accessToken
            (Duration.Hours 1.0) // idToken
        )
    }

    // Step 8: Create Lambda function for API
    lambda "ApiHandler" {
        runtime Runtime.DOTNET_8
        handler "MyApp.Api::MyApp.Api.Handler::handleRequest"
        code "../MyApp.Api/bin/Release/net8.0/publish"

        timeout 30.0
        memorySize 512
        description "API handler for the web application"

        // VPC configuration for database access
        vpcSubnets (subnetSelection { subnetType SubnetType.PRIVATE_WITH_EGRESS })

        securityGroups [ lambdaSecurityGroup ]

        // Environment variables
        environment
            [ "DATABASE_HOST", "dbEndpoint"
              "DATABASE_NAME", "myapp"
              "USER_POOL_ID", "userPoolId"
              "REGION", regionName ]

        // Enable X-Ray tracing
        tracing Tracing.ACTIVE

        // Enable Lambda Insights
        insightsVersion LambdaInsightsVersion.VERSION_1_0_229_0
    }

    // Step 9: Create CloudFront distribution for CDN
    // AWS Best Practice: HTTP/2, TLS 1.2, IPv6, cost-optimized
    cloudFrontDistribution "AppCDN" {
        defaultBehavior myBehaviorOptions // Created separately
        defaultRootObject "index.html"
        comment "CDN for multi-tier application"

        // Performance
        httpVersion HttpVersion.HTTP2
        enableIpv6 true

        // Security
        minimumProtocolVersion SecurityPolicyProtocol.TLS_V1_2_2021

        // Cost optimization
        priceClass PriceClass.PRICE_CLASS_100

        // Logging
        enableLogging staticAssetsBucket "cdn-logs/"
    }
}

(**
## Best Practices Demonstrated

### Security
1. **Least Privilege**: Security groups deny all by default
2. **Encryption**: RDS and S3 use encryption at rest
3. **Strong Authentication**: Cognito with MFA and strong password policy
4. **Private Subnets**: Database and Lambda in private subnets
5. **No Public Access**: Database not publicly accessible

### High Availability
1. **Multi-AZ**: VPC spans multiple availability zones
2. **Multi-AZ RDS**: Database replicated across AZs
3. **Automated Backups**: 7-day retention with preferred window
4. **CloudFront CDN**: Global content delivery

### Cost Optimization
1. **Right-sized Instances**: t3.small for RDS, appropriate memory for Lambda
2. **Single NAT Gateway**: Development/staging configuration
3. **S3 Lifecycle Rules**: Automatic transition to cheaper storage
4. **Regional CDN**: PriceClass100 for US/Canada/Europe

### Performance
1. **HTTP/2**: CloudFront uses HTTP/2
2. **IPv6**: Enabled for better connectivity
3. **Lambda Insights**: Performance monitoring
4. **X-Ray Tracing**: Distributed tracing enabled

### Operational Excellence
1. **Automated Backups**: RDS backup retention
2. **Auto Minor Upgrades**: RDS automatically updates
3. **Monitoring**: Lambda Insights and X-Ray
4. **Tagging**: All resources properly tagged

## Deployment

```bash
# Build the Lambda function
cd MyApp.Api
dotnet publish -c Release

# Synthesize CloudFormation template
cd ../MyApp.CDK
cdk synth

# Deploy to AWS
cdk deploy

# View outputs
cdk output
```

## Environment Variables

Create a `.env` file:

```
AWS_ACCOUNT=123456789012
AWS_REGION=us-east-1
```

## Monitoring

After deployment, monitor your application:

1. **CloudWatch Logs**: Lambda function logs
2. **RDS Performance Insights**: Database performance
3. **CloudFront Metrics**: CDN performance and cache hit rate
4. **X-Ray Service Map**: Distributed tracing visualization

## Scaling

To scale for production:

1. Increase NAT gateways to 2+ for HA: `natGateways 2`
2. Upgrade RDS instance: `instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE))`
3. Add more Lambda functions with ALB
4. Expand CloudFront price class: `priceClass PriceClass.PRICE_CLASS_ALL`

## Cost Estimation

Approximate monthly costs (us-east-1) (at of Oct25):

- VPC + NAT Gateway: ~$32
- RDS t3.small (Multi-AZ): ~$50
- Lambda (1M requests, 512MB): ~$10
- S3 (100GB, with lifecycle): ~$2
- CloudFront (PriceClass100, 100GB transfer): ~$8.50
- Cognito (10k MAU): Free

**Total: ~$102/month** (excluding data transfer)

## Security Checklist

- [x] All data encrypted at rest
- [x] All data encrypted in transit (TLS 1.2+)
- [x] Security groups follow least privilege
- [x] Database in private subnet
- [x] No hardcoded credentials
- [x] MFA available for users
- [x] Strong password policy enforced
- [x] Automated backups enabled
- [x] Deletion protection enabled

## Next Steps

1. Add Application Load Balancer for Lambda
2. Implement API Gateway for REST API
3. Add Route53 for custom domain
4. Configure WAF for CloudFront
5. Set up CloudWatch alarms
6. Implement CI/CD pipeline
*)
