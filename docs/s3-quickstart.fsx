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

## Resources

- [FsCDK Documentation](index.html)
- [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)
- [AWS KMS Documentation](https://docs.aws.amazon.com/kms/latest/developerguide/overview.html)
*)
