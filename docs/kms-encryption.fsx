(**
---
title: KMS Key Management
category: docs
index: 11
---

# KMS Key Management

This guide demonstrates how to create and manage AWS KMS (Key Management Service) keys using FsCDK for encryption at rest.

## What is KMS?

AWS Key Management Service (KMS) is a managed service that makes it easy to create and control cryptographic keys used to protect your data. KMS keys are used to encrypt data at rest across many AWS services.

## Basic KMS Key
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.KMS
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.SecretsManager
open FsCDK

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "BasicKMSStack" {
    stackProps { description "Basic KMS key with automatic rotation" }

    // Create a KMS key with secure defaults
    let myKey =
        kmsKey "my-encryption-key" {
            description "Encrypts sensitive application data"
            alias "alias/my-app-key"
            enableKeyRotation
        }

    ()
}

(**
## Use Cases

### S3 Bucket Encryption
*)

stack "S3EncryptionStack" {
    stackProps { description "S3 bucket with KMS encryption" }

    // Create KMS key for S3
    let s3Key =
        kmsKey "s3-encryption-key" {
            description "KMS key for S3 bucket encryption"
            alias "alias/s3-data-encryption"
            enableKeyRotation
        }

    // Create S3 bucket with KMS encryption
    let encryptedBucket =
        s3Bucket "encrypted-data-bucket" {
            encryption BucketEncryption.KMS
            encryptionKey s3Key
            versioned true
        }

    ()
}

(**
### Secrets Manager Encryption
*)

stack "SecretsEncryptionStack" {
    stackProps { description "Secrets Manager with custom KMS key" }

    // Create KMS key for secrets
    let secretsKey =
        kmsKey "secrets-encryption-key" {
            description "KMS key for Secrets Manager"
            alias "alias/secrets-encryption"
            enableKeyRotation
        }

    // Create secret with KMS encryption
    let apiSecret =
        secret "api-credentials" {
            description "API credentials for external service"
            encryptionKey secretsKey
            generateSecretString (SecretsManagerHelpers.generatePassword 32 None)
        }

    ()
}

(**
### Lambda Environment Variables
*)

stack "LambdaEncryptionStack" {
    stackProps { description "Lambda with encrypted environment variables" }

    // Create KMS key for Lambda
    let lambdaKey =
        kmsKey "lambda-env-key" {
            description "Encrypts Lambda environment variables"
            alias "alias/lambda-env-encryption"
            enableKeyRotation
        }

    // Create Lambda with encrypted env vars
    let myFunction =
        lambda "my-secure-function" {
            handler "index.handler"
            runtime Amazon.CDK.AWS.Lambda.Runtime.NODEJS_18_X
            code "./lambda-code"

            environment [ "API_KEY", "super-secret-key"; "DATABASE_URL", "postgres://..." ]

            environmentEncryption lambdaKey
        }

    ()
}

(**
## Asymmetric Keys for Signing
*)

stack "SigningKeyStack" {
    stackProps { description "Asymmetric KMS key for digital signatures" }

    // Create signing key
    let signingKey =
        kmsKey "code-signing-key" {
            description "Signs application artifacts"
            alias "alias/code-signing"
            keySpec KeySpec.RSA_2048
            keyUsage KeyUsage.SIGN_VERIFY
            disableKeyRotation // Asymmetric keys don't support automatic rotation
        }

    ()
}

(**
## Complete Production Example
*)

stack "ProductionKMSStack" {
    environment {
        account config.Account
        region config.Region
    }

    stackProps {
        description "Production KMS keys for multi-tier application"

        tags [ "Environment", "Production"; "ManagedBy", "FsCDK" ]
    }

    // Application data encryption key
    let appDataKey =
        kmsKey "app-data-key" {
            description "Encrypts application data at rest"
            alias "alias/prod-app-data"
            enableKeyRotation
            pendingWindow (Duration.Days(30.0))
        }

    // Database encryption key
    let dbKey =
        kmsKey "database-key" {
            description "Encrypts RDS database"
            alias "alias/prod-database"
            enableKeyRotation
        }

    // Secrets encryption key
    let secretsKey =
        kmsKey "secrets-key" {
            description "Encrypts secrets and credentials"
            alias "alias/prod-secrets"
            enableKeyRotation
        }

    // S3 bucket with custom KMS key
    let dataBucket =
        s3Bucket "production-data" {
            encryption BucketEncryption.KMS
            encryptionKey appDataKey
            versioned true
        }

    // CloudWatch alarm for key usage
    cloudwatchAlarm "kms-key-usage-alarm" {
        description "Alert on unusual KMS key usage"
        metricNamespace "AWS/KMS"
        metricName "NumberOfOperations"
        dimensions [ "KeyId", appDataKey.Key.Value.KeyId ]
        statistic "Sum"
        threshold 1000.0
        evaluationPeriods 1
        period (Duration.Minutes(5.0))
    }

    ()
}

(**
## Security Best Practices

### 1. Enable Key Rotation
Automatically rotate keys yearly to reduce risk.

### 2. Use Separate Keys per Environment
- Development: `alias/dev-app-key`
- Staging: `alias/staging-app-key`
- Production: `alias/prod-app-key`

### 3. Least Privilege Access
Grant only required permissions:

```fsharp
// Allow encryption only
let encryptStmt = IAM.allow 
    ["kms:Encrypt"; "kms:GenerateDataKey"] 
    [appDataKey.Key.Value.KeyArn]

// Allow decryption only
let decryptStmt = IAM.allow 
    ["kms:Decrypt"] 
    [appDataKey.Key.Value.KeyArn]
```

### 4. Monitor Key Usage
Use CloudWatch and CloudTrail to monitor all key operations.

## Cost Optimization

### KMS Pricing
- **Key Storage**: $1/month per key
- **API Requests**: 
  - Free tier: 20,000 requests/month
  - Beyond free tier: $0.03 per 10,000 requests

### Cost Savings Tips
1. Use AWS-managed keys when custom rotation not needed
2. Share keys across resources in same trust boundary
3. Cache data keys in application (envelope encryption)
4. Monitor usage to avoid unnecessary requests

## Resources

- [AWS KMS Documentation](https://docs.aws.amazon.com/kms/)
- [KMS Best Practices](https://docs.aws.amazon.com/kms/latest/developerguide/best-practices.html)
- [Envelope Encryption](https://docs.aws.amazon.com/kms/latest/developerguide/concepts.html#enveloping)
*)

(*** hide ***)
()
