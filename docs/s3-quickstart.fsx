(**
---
title: S3 Quickstart Example
category: docs
index: 6
---

# S3 Quickstart Example

This example demonstrates how to create an S3 bucket using FsCDK with secure defaults and optional configuration.

## Features Demonstrated

- S3 bucket with KMS encryption (default)
- Block public access (default)
- Optional versioning
- Lifecycle rules for cost optimization
- Global tagging

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- AWS credentials configured (for deployment)

## Usage

### 1. Synthesize CloudFormation Template

```bash
cd examples/s3-quickstart
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

### Default Security Settings

The S3 bucket builder applies these security best practices by default:

- **Block Public Access**: All public access blocked
- **Encryption**: SSE-KMS with AWS managed key (aws/s3)
- **SSL/TLS**: Requires HTTPS for all requests
- **Versioning**: Disabled by default (opt-in)

### Example 1: Basic Bucket

```fsharp
s3Bucket "my-secure-bucket" { }
```

Creates a bucket with all security defaults.

### Example 2: Versioned Bucket

```fsharp
s3Bucket "my-versioned-bucket" {
    versioned true
}
```

Enables versioning for data protection.

##Complete Example Stack

See the actual runnable example code in [examples/s3-quickstart](https://github.com/Thorium/FsCDK/tree/main/examples/s3-quickstart).

## Security Considerations

### Encryption at Rest
All buckets use KMS encryption by default. This provides:
- Audit trails in CloudTrail
- Key rotation capabilities
- Fine-grained access control

### Blocking Public Access
Public access is blocked at the bucket level by default. 

**Warning**: Only disable public access blocking if absolutely necessary.

### SSL/TLS Enforcement
All bucket operations require HTTPS. HTTP requests are rejected.

## Cost Optimization

### Lifecycle Rules

Use lifecycle rules to reduce storage costs by transitioning objects to cheaper storage classes or expiring them after a certain time.

### Storage Classes

- **Standard**: Frequently accessed data
- **Glacier**: Archival data (retrieval in minutes to hours)
- **Glacier Deep Archive**: Long-term archival (retrieval in hours)

## Next Steps

- Explore [Lambda Quickstart](lambda-quickstart.html) to integrate S3 with Lambda
- Read [IAM Best Practices](iam-best-practices.html) for access control
- Review [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)

## 📚 Learning Resources for Amazon S3

### AWS Official Documentation

**Getting Started:**
- [Amazon S3 User Guide](https://docs.aws.amazon.com/AmazonS3/latest/userguide/Welcome.html) - Complete S3 documentation
- [S3 Getting Started Guide](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html) - First steps with S3
- [S3 Storage Classes](https://aws.amazon.com/s3/storage-classes/) - Standard, Glacier, Intelligent-Tiering, etc.
- [S3 Pricing](https://aws.amazon.com/s3/pricing/) - Understand storage and request costs

**Security Best Practices:**
- [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html) - Official AWS security guide
- [Block Public Access](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html) - Prevent accidental public exposure
- [S3 Bucket Policies](https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucket-policies.html) - Control access with policies
- [S3 Access Points](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-points.html) - Simplified access management for shared buckets
- [S3 Object Lock](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lock.html) - WORM (Write Once Read Many) compliance

**Encryption:**
- [S3 Encryption](https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingEncryption.html) - Server-side and client-side encryption
- [AWS KMS with S3](https://docs.aws.amazon.com/kms/latest/developerguide/services-s3.html) - Customer-managed encryption keys
- [S3 Bucket Keys](https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucket-key.html) - Reduce KMS costs by 99%

### Cost Optimization

**Storage Classes & Lifecycle:**
- [S3 Intelligent-Tiering](https://aws.amazon.com/blogs/aws/new-automatic-cost-optimization-for-amazon-s3-via-intelligent-tiering/) - Automatic cost optimization
- [S3 Lifecycle Policies](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lifecycle-mgmt.html) - Automate transitions and expirations
- [S3 Glacier Deep Archive](https://aws.amazon.com/s3/storage-classes/glacier/) - Lowest-cost archival storage ($1/TB/month)
- [S3 Storage Lens](https://docs.aws.amazon.com/AmazonS3/latest/userguide/storage_lens.html) - Organization-wide storage visibility

**Cost Optimization Strategies:**
- [Optimizing S3 Costs](https://aws.amazon.com/s3/cost-optimization/) - Official AWS cost optimization guide
- [S3 Request Pricing](https://aws.amazon.com/s3/pricing/) - Understand GET, PUT, LIST costs
- [Transfer Acceleration Cost](https://aws.amazon.com/s3/transfer-acceleration/pricing/) - When it's worth it
- [Reduce S3 Costs Blog](https://aws.amazon.com/blogs/storage/reduce-amazon-s3-storage-costs/) - Real-world strategies

**Storage Class Comparison:**

| Storage Class | Use Case | Durability | Availability | Cost ($/GB/month) |
|---------------|----------|------------|--------------|-------------------|
| **Standard** | Frequently accessed data | 99.999999999% | 99.99% | ~$0.023 |
| **Intelligent-Tiering** | Unknown/changing patterns | 99.999999999% | 99.9% | ~$0.023 + monitoring |
| **Standard-IA** | Infrequent access | 99.999999999% | 99.9% | ~$0.0125 |
| **One Zone-IA** | Infrequent, non-critical | 99.999999999% | 99.5% | ~$0.01 |
| **Glacier Instant** | Archive with instant retrieval | 99.999999999% | 99.9% | ~$0.004 |
| **Glacier Flexible** | Archive (minutes-hours) | 99.999999999% | 99.99% | ~$0.0036 |
| **Glacier Deep Archive** | Long-term archive (hours) | 99.999999999% | 99.99% | ~$0.00099 |

### Performance Optimization

**Transfer Acceleration:**
- [S3 Transfer Acceleration](https://docs.aws.amazon.com/AmazonS3/latest/userguide/transfer-acceleration.html) - Fast uploads via CloudFront edge locations
- [When to Use Transfer Acceleration](https://aws.amazon.com/s3/transfer-acceleration/) - Global uploads, large files

**Multipart Upload:**
- [Multipart Upload Overview](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html) - Upload large objects in parts
- [Performance Guidelines](https://docs.aws.amazon.com/AmazonS3/latest/userguide/optimizing-performance.html) - Maximize throughput
- [Request Rate Performance](https://docs.aws.amazon.com/AmazonS3/latest/userguide/optimizing-performance-design-patterns.html) - Handle high request rates

**Best Practices:**
- [Performance Design Patterns](https://docs.aws.amazon.com/AmazonS3/latest/userguide/optimizing-performance-design-patterns.html) - Naming, prefixes, parallelization
- [CloudFront with S3](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/DownloadDistS3AndCustomOrigins.html) - CDN for faster global access
- [Byte-Range Fetches](https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html) - Download only what you need

### Event-Driven Architecture with S3

**S3 Event Notifications:**
- [S3 Event Notifications](https://docs.aws.amazon.com/AmazonS3/latest/userguide/NotificationHowTo.html) - Trigger Lambda, SNS, SQS on S3 events
- [S3 to Lambda Tutorial](https://docs.aws.amazon.com/lambda/latest/dg/with-s3-example.html) - Process uploaded files automatically
- [EventBridge Integration](https://aws.amazon.com/blogs/aws/new-use-amazon-s3-event-notifications-with-amazon-eventbridge/) - Advanced event filtering and routing

**Common Patterns:**
- [Image Processing Pipeline](https://aws.amazon.com/blogs/compute/resize-images-on-the-fly-with-amazon-s3-aws-lambda-and-amazon-api-gateway/) - Thumbnail generation with Lambda
- [Video Transcoding](https://aws.amazon.com/solutions/implementations/video-on-demand-on-aws/) - Automated video processing
- [Data Lake Architecture](https://aws.amazon.com/big-data/datalakes-and-analytics/datalakes/) - S3 as centralized data repository

### S3 Security Deep Dives

**Access Control Models:**
- [IAM Policies vs Bucket Policies](https://aws.amazon.com/blogs/security/iam-policies-and-bucket-policies-and-acls-oh-my-controlling-access-to-s3-resources/) - When to use each
- [S3 Access Analyzer](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-analyzer.html) - Find unintended access
- [Cross-Account Access](https://aws.amazon.com/blogs/security/how-to-restrict-amazon-s3-bucket-access-to-a-specific-iam-role/) - Secure sharing between AWS accounts
- [Presigned URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html) - Temporary access without credentials

**Compliance & Auditing:**
- [S3 Access Logging](https://docs.aws.amazon.com/AmazonS3/latest/userguide/ServerLogs.html) - Track all requests to your bucket
- [CloudTrail S3 Data Events](https://docs.aws.amazon.com/awscloudtrail/latest/userguide/logging-data-events-with-cloudtrail.html) - Audit object-level operations
- [S3 Inventory](https://docs.aws.amazon.com/AmazonS3/latest/userguide/storage-inventory.html) - Scheduled reports of objects and metadata
- [Macie](https://aws.amazon.com/macie/) - Discover and protect sensitive data in S3

**Ransomware Protection:**
- [S3 Object Versioning](https://docs.aws.amazon.com/AmazonS3/latest/userguide/Versioning.html) - Keep multiple versions to recover from deletions
- [S3 Object Lock](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lock.html) - Prevent object deletion or modification
- [MFA Delete](https://docs.aws.amazon.com/AmazonS3/latest/userguide/MultiFactorAuthenticationDelete.html) - Require MFA for permanent deletions
- [S3 Backup Best Practices](https://aws.amazon.com/blogs/storage/protecting-data-with-amazon-s3-versioning-object-lock-and-replication/) - Multi-layer protection

### Static Website Hosting

**Setup & Configuration:**
- [S3 Static Website Hosting](https://docs.aws.amazon.com/AmazonS3/latest/userguide/WebsiteHosting.html) - Host websites directly from S3
- [CloudFront + S3 for Static Sites](https://aws.amazon.com/blogs/networking-and-content-delivery/amazon-s3-amazon-cloudfront-a-match-made-in-the-cloud/) - Add CDN and custom domain
- [Route 53 with S3 Website](https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/RoutingToS3Bucket.html) - Custom domain setup

**Modern Web Frameworks:**
- [Deploy React/Vue/Angular to S3](https://docs.aws.amazon.com/prescriptive-guidance/latest/patterns/deploy-a-react-based-single-page-application-to-amazon-s3-and-cloudfront.html) - SPA deployment guide
- [Next.js on AWS](https://aws.amazon.com/blogs/mobile/host-a-next-js-ssr-app-with-real-time-data-on-aws-amplify/) - Server-side rendering patterns
- [JAMstack on AWS](https://aws.amazon.com/blogs/compute/building-a-jamstack-site-with-aws-amplify/) - Static site generation

### Video Tutorials

**Beginner:**
- [S3 Fundamentals](https://www.youtube.com/watch?v=77lMCiiMilo) - Complete beginner's guide
- [S3 Security Best Practices](https://www.youtube.com/watch?v=yRBzRcHhpGg) - AWS re:Inforce session
- [S3 Storage Classes Explained](https://www.youtube.com/watch?v=9HwbjA_R5Xg) - When to use each class

**Advanced:**
- [AWS re:Invent - Deep Dive on S3](https://www.youtube.com/results?search_query=aws+reinvent+s3+deep+dive) - Annual deep dive sessions
- [S3 Security Masterclass](https://www.youtube.com/watch?v=DJMGLs-1_Xs) - Advanced security patterns
- [S3 Performance Optimization](https://www.youtube.com/watch?v=rHeTn9pHNKo) - Maximize throughput

### Tools & Utilities

**Command-Line Tools:**
- [AWS CLI S3 Commands](https://docs.aws.amazon.com/cli/latest/reference/s3/) - s3 cp, sync, mb, rb commands
- [S3 Sync Command](https://docs.aws.amazon.com/cli/latest/reference/s3/sync.html) - Sync local directories with S3
- [s5cmd](https://github.com/peak/s5cmd) - Faster S3 client for large transfers

**GUI Clients:**
- [S3 Browser](https://s3browser.com/) - Windows GUI for S3
- [Cyberduck](https://cyberduck.io/) - Cross-platform S3 client
- [Transmit](https://panic.com/transmit/) - Mac S3 client with excellent UI

**Developer Tools:**
- [boto3 (Python SDK)](https://boto3.amazonaws.com/v1/documentation/api/latest/reference/services/s3.html) - AWS SDK for Python
- [AWS SDK for JavaScript](https://docs.aws.amazon.com/AWSJavaScriptSDK/v3/latest/clients/client-s3/) - Node.js S3 client
- [S3 Select](https://docs.aws.amazon.com/AmazonS3/latest/userguide/selecting-content-from-objects.html) - Query data in S3 objects directly

### Common Use Cases

**Data Lake:**
- [Building Data Lakes on AWS](https://aws.amazon.com/big-data/datalakes-and-analytics/) - S3 as central data repository
- [AWS Lake Formation](https://aws.amazon.com/lake-formation/) - Simplify data lake setup
- [Athena](https://aws.amazon.com/athena/) - Query S3 data with SQL

**Backup & Archive:**
- [AWS Backup with S3](https://aws.amazon.com/backup/) - Centralized backup management
- [S3 Batch Operations](https://docs.aws.amazon.com/AmazonS3/latest/userguide/batch-ops.html) - Perform actions on billions of objects
- [Cross-Region Replication](https://docs.aws.amazon.com/AmazonS3/latest/userguide/replication.html) - Disaster recovery

**Content Distribution:**
- [CloudFront Origin Access Identity](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-s3.html) - Secure S3 content delivery
- [Signed URLs and Cookies](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/PrivateContent.html) - Restrict content access
- [Video Streaming from S3](https://aws.amazon.com/blogs/media/part-1-back-to-basics-streaming-delivery/) - HTTP streaming patterns

### Recommended Learning Path

**Week 1 - Basics:**
1. Read [S3 User Guide](https://docs.aws.amazon.com/AmazonS3/latest/userguide/Welcome.html) - First 5 chapters
2. Watch [S3 Fundamentals Video](https://www.youtube.com/watch?v=77lMCiiMilo)
3. Create buckets with FsCDK (examples above)
4. Practice with AWS CLI s3 commands

**Week 2 - Security:**
1. Study [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)
2. Learn [Bucket Policies vs IAM Policies](https://aws.amazon.com/blogs/security/iam-policies-and-bucket-policies-and-acls-oh-my-controlling-access-to-s3-resources/)
3. Enable [S3 Access Logging](https://docs.aws.amazon.com/AmazonS3/latest/userguide/ServerLogs.html)
4. Configure [Versioning and Object Lock](https://docs.aws.amazon.com/AmazonS3/latest/userguide/Versioning.html)

**Week 3 - Cost Optimization:**
1. Understand [S3 Storage Classes](https://aws.amazon.com/s3/storage-classes/)
2. Create [Lifecycle Policies](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lifecycle-mgmt.html)
3. Enable [S3 Intelligent-Tiering](https://aws.amazon.com/s3/storage-classes/intelligent-tiering/)
4. Use [S3 Storage Lens](https://docs.aws.amazon.com/AmazonS3/latest/userguide/storage_lens.html) for cost visibility

**Ongoing - Advanced:**
- Implement [event-driven processing](https://docs.aws.amazon.com/AmazonS3/latest/userguide/NotificationHowTo.html) with Lambda
- Build [data lakes](https://aws.amazon.com/big-data/datalakes-and-analytics/) with S3 and Athena
- Master [S3 performance optimization](https://docs.aws.amazon.com/AmazonS3/latest/userguide/optimizing-performance.html)
- Explore [S3 Access Points](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-points.html) for shared datasets

### AWS Community Experts

**Security Experts:**
- [Scott Piper (@0xdabbad00)](https://twitter.com/0xdabbad00) - Cloud security, S3 misconfigurations
- [Chris Farris (@jcfarris)](https://twitter.com/jcfarris) - AWS security and compliance
- [Mark Nunnikhoven (@marknca)](https://twitter.com/marknca) - Cloud security best practices

**Storage Experts:**
- [Jeff Barr (@jeffbarr)](https://twitter.com/jeffbarr) - VP AWS, announces S3 features
- [Martin Beeby (@thebeebs)](https://twitter.com/thebeebs) - AWS Principal Advocate

### FsCDK S3 Features

- Security-first defaults (block public access, enforce SSL, KMS encryption)
- Type-safe bucket configuration with computation expressions
- Built-in lifecycle rule helpers
- CORS configuration support
- Versioning and removal policy controls

For implementation details, see [src/S3.fs](../src/S3.fs) in the FsCDK repository.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
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

envProps.Description <- "FsCDK S3 Quickstart Example - demonstrates S3 bucket with security defaults"

// Create the stack
let stack = Stack(app, "S3QuickstartStack", envProps)

// Apply tags
Tags.Of(stack).Add("Project", "FsCDK-Examples")
Tags.Of(stack).Add("Example", "S3-Quickstart")
Tags.Of(stack).Add("ManagedBy", "FsCDK")

// Example 1: Basic bucket with all security defaults
let basicBucket = s3Bucket "basic-secure-bucket" { () }
// Uses defaults:
// - BlockPublicAccess = BLOCK_ALL
// - Encryption = KMS_MANAGED
// - EnforceSSL = true
// - Versioned = false

// Example 2: Versioned bucket for data protection
let versionedBucket = s3Bucket "versioned-bucket" { versioned true }

// Example 3: Bucket with lifecycle rules for cost optimization
let lifecycleBucket =
    s3Bucket "lifecycle-bucket" {
        versioned true

        // Expire old objects after 30 days
        LifecycleRuleHelpers.expireAfter 30 "expire-old-objects"

        // Transition to Glacier for archival after 90 days
        LifecycleRuleHelpers.transitionToGlacier 90 "archive-to-glacier"

        // Delete non-current versions after 180 days
        LifecycleRuleHelpers.deleteNonCurrentVersions 180 "cleanup-old-versions"
    }

// Example 4: Bucket with custom removal policy for dev/test
let tempBucket =
    s3Bucket "temporary-bucket" {
        removalPolicy RemovalPolicy.DESTROY
        autoDeleteObjects true
    }

app.Synth() |> ignore
