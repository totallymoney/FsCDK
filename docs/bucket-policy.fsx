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
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.IAM

(**
## Enforce HTTPS Only

Deny all non-HTTPS requests (security best practice).
*)

stack "SecureBucket" {
    let! myBucket =
        bucket "MyBucket" {
            versioned true
            encryption BucketEncryption.S3_MANAGED
        }

    bucketPolicy "SecurePolicy" {
        bucket myBucket

        statements
            [ policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ myBucket.BucketArn; myBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              } ]
    }
}

(**
## CloudFront Origin Access Identity

Allow CloudFront to access a private S3 bucket.
*)


stack "CloudFrontOrigin" {

    let! websiteBucket =
        bucket "Website" {
            blockPublicAccess BlockPublicAccess.BLOCK_ALL
            encryption BucketEncryption.S3_MANAGED
        }

    let! oai = originAccessIdentity "MyOAI" { comment "S3 access for CloudFront" }

    bucketPolicy "CloudFrontAccess" {
        bucket websiteBucket

        statements
            [ policyStatement {
                  sid "AllowCloudFrontAccess"
                  effect Effect.ALLOW
                  principals [ CanonicalUserPrincipal(oai.OriginAccessIdentityId) :> IPrincipal ]
                  actions [ "s3:GetObject" ]
                  resources [ websiteBucket.BucketArn + "/*" ]
              }

              policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ websiteBucket.BucketArn; websiteBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              } ]
    }
}


(**
## IP Address Restrictions

Restrict bucket access to specific IP addresses.
*)

stack "IPRestrictedBucket" {
    let! privateBucket = bucket "PrivateBucket" { blockPublicAccess BlockPublicAccess.BLOCK_ALL }

    bucketPolicy "IPRestriction" {
        bucket privateBucket

        statements
            [ policyStatement {
                  sid "AllowFromSpecificIPs"
                  effect Effect.ALLOW
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ privateBucket.BucketArn; privateBucket.BucketArn + "/*" ]

                  conditions
                      [ "IpAddress", box (dict [ "aws:SourceIp", box [| "203.0.113.0/24"; "198.51.100.0/24" |] ]) ]
              }
              policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ privateBucket.BucketArn; privateBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              } ]
    }
}

(**
## Deny Specific IP Addresses

Block access from known malicious IPs.
*)

stack "BlockMaliciousIPs" {
    let! publicBucket = bucket "PublicBucket" { () }

    bucketPolicy "BlockBadActors" {
        bucket publicBucket

        statements
            [ policyStatement {
                  sid "DenyFromMaliciousIPs"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ publicBucket.BucketArn; publicBucket.BucketArn + "/*" ]
                  conditions [ "IpAddress", box (dict [ "aws:SourceIp", box [| "192.0.2.0/24" |] ]) ]

              }

              policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ publicBucket.BucketArn; publicBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              } ]
    }
}

(**
## Custom Policy Statements

Add custom policy statements for specific requirements.
*)

stack "CustomPolicy" {
    let! dataBucket = bucket "DataBucket" { versioned true }

    let readOnlyStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "AllowReadOnly",
                Effect = Effect.ALLOW,
                Principals = [| AccountPrincipal("123456789012") :> IPrincipal |],
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources = [| dataBucket.BucketArn; dataBucket.BucketArn + "/*" |]
            )
        )

    bucketPolicy "CustomPolicy" {
        bucket dataBucket

        statements
            [ readOnlyStatement
              policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ dataBucket.BucketArn; dataBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              } ]

    }
}

(**
## Multi-Statement Policy

Combine multiple security controls in one policy.
*)

stack "ComprehensivePolicy" {
    let! secureBucket =
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
                Resources = [| secureBucket.BucketArn; secureBucket.BucketArn + "/*" |]
            )
        )

    bucketPolicy "ComprehensivePolicy" {
        bucket secureBucket

        statements
            [ adminStatement
              policyStatement {
                  sid "DenyInsecureTransport"
                  effect Effect.DENY
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ secureBucket.BucketArn; secureBucket.BucketArn + "/*" ]
                  conditions [ ("Bool", Map.ofList [ "aws:SecureTransport", false ]) ]
              }

              policyStatement {
                  sid "AllowFromInternalNetwork"
                  effect Effect.ALLOW
                  principals [ AnyPrincipal() :> IPrincipal ]
                  actions [ "s3:*" ]
                  resources [ secureBucket.BucketArn; secureBucket.BucketArn + "/*" ]
                  conditions [ "IpAddress", box (dict [ "aws:SourceIp", box [| "10.0.0.0/8" |] ]) ]
              } ]
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
