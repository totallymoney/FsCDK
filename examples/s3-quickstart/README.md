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

### Example 3: Lifecycle Rules

```fsharp
open FsCDK.Storage

s3Bucket "my-bucket-with-lifecycle" {
    versioned true
    LifecycleRuleHelpers.expireAfter 30 "expire-old-objects"
    LifecycleRuleHelpers.transitionToGlacier 90 "archive-to-glacier"
}
```

Configures lifecycle rules for cost optimization:
- Deletes objects after 30 days
- Archives to Glacier after 90 days

### Example 4: Custom Encryption Key

```fsharp
// Create a KMS key
let key = Key(stack, "MyKey", KeyProps(
    Description = "KMS key for S3 encryption"
))

s3Bucket "my-bucket-custom-kms" {
    encryption BucketEncryption.KMS
    encryptionKey key
}
```

Uses a customer-managed KMS key for encryption.

## Security Considerations

### Encryption at Rest
All buckets use KMS encryption by default. This provides:
- Audit trails in CloudTrail
- Key rotation capabilities
- Fine-grained access control

### Blocking Public Access
Public access is blocked at the bucket level by default. To allow public access (not recommended):

```fsharp
s3Bucket "public-bucket" {
    blockPublicAccess BlockPublicAccess.NONE
}
```

**Warning**: Only disable public access blocking if absolutely necessary.

### SSL/TLS Enforcement
All bucket operations require HTTPS. HTTP requests are rejected.

## Cost Optimization

### Lifecycle Rules

Use lifecycle rules to reduce storage costs:

```fsharp
// Transition to cheaper storage classes
LifecycleRuleHelpers.transitionToGlacier 90 "archive"
LifecycleRuleHelpers.transitionToDeepArchive 365 "deep-archive"

// Delete old objects
LifecycleRuleHelpers.expireAfter 30 "cleanup"

// Delete old versions
LifecycleRuleHelpers.deleteNonCurrentVersions 90 "cleanup-versions"
```

### Storage Classes

- **Standard**: Frequently accessed data
- **Glacier**: Archival data (retrieval in minutes to hours)
- **Glacier Deep Archive**: Long-term archival (retrieval in hours)

## Escape Hatch

For advanced scenarios not covered by the builder:

```fsharp
let bucketResource = s3Bucket "my-bucket" { }
// Access underlying CDK construct
let cdkBucket = bucketResource.Bucket
// Use any CDK Bucket methods...
```

## Next Steps

- Explore [Lambda Quickstart](../lambda-quickstart/) to integrate S3 with Lambda
- Read [IAM Best Practices](../../docs/IAM_BEST_PRACTICES.md) for access control
- Review [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)

## Resources

- [FsCDK Documentation](https://totallymoney.github.io/FsCDK/)
- [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)
- [AWS KMS Documentation](https://docs.aws.amazon.com/kms/latest/developerguide/overview.html)
