(**
---
title: S3 Bucket Policy
category: docs
index: 14
---

# S3 Bucket Policy

S3 bucket policies control access to your buckets and objects.
FsCDK provides a first-class DSL for creating secure bucket policies.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.IAM

(**
## Enforce HTTPS Only

Deny all non-HTTPS requests (security best practice).
*)

stack "SecureBucket" {
    let myBucket =
        bucket "MyBucket" {
            versioned true
            encryption BucketEncryption.S3_MANAGED
        }

    bucketPolicy "SecurePolicy" {
        bucket myBucket
        denyInsecureTransport
    }
}

(**
## CloudFront Origin Access Identity

Allow CloudFront to access a private S3 bucket.
*)


open Amazon.CDK.AWS.CloudFront

stack "CloudFrontOrigin" {

    let websiteBucket =
        bucket "Website" {
            blockPublicAccess BlockPublicAccess.BLOCK_ALL
            encryption BucketEncryption.S3_MANAGED
        }

    // CloudFront Origin Access Identity
    let oai = originAccessIdentity "MyOAI" { comment "S3 access for CloudFront" }

    bucketPolicy "CloudFrontAccess" {
        bucket websiteBucket
        allowCloudFrontOAI oai.Identity.Value.OriginAccessIdentityId // CloudFrontOriginAccessIdentityS3CanonicalUserId
        denyInsecureTransport
    }
}


(**
## IP Address Restrictions

Restrict bucket access to specific IP addresses.
*)

stack "IPRestrictedBucket" {
    let privateBucket =
        bucket "PrivateBucket" { blockPublicAccess BlockPublicAccess.BLOCK_ALL }

    bucketPolicy "IPRestriction" {
        bucket privateBucket
        allowFromIpAddresses [ "203.0.113.0/24"; "198.51.100.0/24" ]
        denyInsecureTransport
    }
}

(**
## Deny Specific IP Addresses

Block access from known malicious IPs.
*)

stack "BlockMaliciousIPs" {
    let publicBucket = bucket "PublicBucket" { () }

    bucketPolicy "BlockBadActors" {
        bucket publicBucket
        denyFromIpAddresses [ "192.0.2.0/24" ]
        denyInsecureTransport
    }
}

(**
## Custom Policy Statements

Add custom policy statements for specific requirements.
*)

stack "CustomPolicy" {
    let dataBucket = bucket "DataBucket" { versioned true }

    let readOnlyStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "AllowReadOnly",
                Effect = Effect.ALLOW,
                Principals = [| AccountPrincipal("123456789012") :> IPrincipal |],
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources =
                    [| dataBucket.Bucket.Value.BucketArn
                       dataBucket.Bucket.Value.BucketArn + "/*" |]
            )
        )

    bucketPolicy "CustomPolicy" {
        bucket dataBucket
        statement readOnlyStatement
        denyInsecureTransport
    }
}

(**
## Multi-Statement Policy

Combine multiple security controls in one policy.
*)

stack "ComprehensivePolicy" {
    let secureBucket =
        bucket "SecureBucket" {
            versioned true
            encryption BucketEncryption.KMS_MANAGED
        }

    let adminStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "AdminFullAccess",
                Effect = Effect.ALLOW,
                Principals = [| ArnPrincipal("arn:aws:iam::123456789012:role/AdminRole") :> IPrincipal |],
                Actions = [| "s3:*" |],
                Resources =
                    [| secureBucket.Bucket.Value.BucketArn
                       secureBucket.Bucket.Value.BucketArn + "/*" |]
            )
        )

    bucketPolicy "ComprehensivePolicy" {
        bucket secureBucket
        denyInsecureTransport
        allowFromIpAddresses [ "10.0.0.0/8" ]
        statement adminStatement
    }
}

(**
## Best Practices

### Security

- ✅ Always deny insecure transport (HTTP)
- ✅ Use principle of least privilege
- ✅ Restrict access by IP when possible
- ✅ Enable bucket versioning with policies
- ✅ Use MFA delete for critical buckets

### Operational Excellence

- ✅ Use descriptive Sid values for statements
- ✅ Document policy purpose in comments
- ✅ Test policies before production deployment
- ✅ Version control your policies

### Compliance

- ✅ Audit bucket access regularly
- ✅ Enable S3 access logging
- ✅ Use AWS Config to monitor policy changes
- ✅ Implement encryption requirements in policies

### Performance

- ✅ Minimize policy complexity
- ✅ Use bucket policies over ACLs
- ✅ Cache policy evaluation results when possible
*)
