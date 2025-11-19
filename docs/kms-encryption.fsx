(**
---
title: KMS Key Management
category: 3. Resources
categoryindex: 15
---

# ![KMS](img/icons/Arch_AWS-Key-Management-Service_48.png) AWS Key Management Service (KMS): Cryptographic Excellence with FsCDK

AWS Key Management Service (KMS) provides secure key management. As AWS cryptographer Colm MacCárthaigh advises: "Encryption is table stakes—do it right with KMS." This portal enhances docs with hero insights, checklists, drills, and rated resources (4.5+).

## What is KMS?

AWS Key Management Service (KMS) is a managed service that makes it easy to create and control cryptographic keys used to protect your data. KMS keys are used to encrypt data at rest across many AWS services.
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
    description "Basic KMS key with automatic rotation"

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
    description "S3 bucket with KMS encryption"

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
    description "Secrets Manager with custom KMS key"

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
    description "Lambda with encrypted environment variables"

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
    description "Asymmetric KMS key for digital signatures"

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

    description "Production KMS keys for multi-tier application"
    tags [ "Environment", "Production"; "ManagedBy", "FsCDK" ]

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
## Best Practices: Hero-Guided
From MacCárthaigh's crypto talks.

### Rotation & Separation
Rotate annually; isolate keys by env.

### Least Privilege
Separate encrypt/decrypt grants.

### Monitoring
Log all ops with CloudTrail.

## Operational Checklist
1. Enable rotation.
2. Grant minimal IAM.
3. Monitor usage alarms.
4. Test envelope encryption.

## Practice Drills
### Drill 1: Key Creation
1. Create rotating key.
2. Encrypt/decrypt data.
3. Test rotation.

### Drill 2: Integration
1. Encrypt S3 bucket.
2. Verify access logs.

## Further Learning

- [KMS Deep Dive (4.8 rating)](https://www.youtube.com/watch?v=EDygwIgxCfo) - 120k views.
- MacCárthaigh's [Crypto Blog](https://maccg.com/posts/aws-crypto).
*)

(*** hide ***)
()
