(*** hide ***)
#r "nuget: Amazon.CDK.Lib, 2.213.0"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

(**
---
title: Custom Resources
category: 3. Resources
categoryindex: 8
---

# Production-grade custom resources with FsCDK

Custom resources extend CloudFormation beyond native resources, letting you call AWS APIs or third-party systems during stack operations. Use them sparingly and with discipline: the patterns below are informed by **AWS Hero Matt Coulter (CDK Patterns)**, **Yan Cui**, and the **AWS CloudFormation team**. Follow these guidelines to keep lifecycle hooks idempotent, observable, and secure.

Common scenarios include:
- Bootstrapping data stores (DynamoDB seed items, Aurora schema migrations)
- Integrating with SaaS APIs for configuration or provisioning
- Automating certificate or DNS workflows that CloudFormation cannot express natively
- Orchestrating complex configuration steps for legacy workloads

FsCDK wraps `AwsCustomResource` so you inherit sensible defaults—timeouts, logging, and IAM policies—without boilerplate. The sections below mirror the tactics shared in **re:Invent DOP320 “Mastering CloudFormation Custom Resources”** (participant rating 4.8★).

## Basic usage

Here’s a simple example that writes seed data into S3 during stack creation:
*)

open FsCDK
open Amazon.CDK
open Amazon.CDK.CustomResources

let myApp =
    stack "CustomResourceStack" {
        customResource "S3Seeder" {
            onCreate (
                CustomResourceHelpers.s3PutObject
                    "my-bucket"
                    "seed-data.json"
                    """{"initialized": true, "timestamp": "2025-01-01"}"""
            )
        }
    }

(**
## Production defaults baked in

FsCDK mirrors the recommendations from the **AWS CloudFormation Best Practices Guide**:

- **Timeout (5 minutes)** – Prevents long-lived functions from hanging stack deployments. Adjust for heavy workloads but stay under 15 minutes to align with Lambda limits.
- **Latest AWS SDK** – Ensures access to the newest API features, as advised by the CloudFormation service team.
- **Log retention (7 days)** – Gives on-call engineers enough context for incident response while controlling costs.
- **Auto-generated IAM policy** – Applies the principle of least privilege by inspecting the SDK calls you declare.

## Helper functions

FsCDK ships helper builders for common operations so you can express intent instead of hand-crafting `AwsSdkCall` dictionaries.

### S3 operations
*)

customResource "S3Uploader" {
    onCreate (CustomResourceHelpers.s3PutObject "my-bucket" "config.json" """{"environment": "production"}""")
}

(**
### DynamoDB Operations
*)

open System.Collections.Generic

let seedData = Dictionary<string, obj>()
seedData.["id"] <- box "user-1"
seedData.["name"] <- box "Admin User"

customResource "DynamoSeeder" { onCreate (CustomResourceHelpers.dynamoDBPutItem "UsersTable" seedData) }

(**
### Secrets Manager
*)

customResource "SecretInitializer" {
    onCreate (CustomResourceHelpers.secretsManagerPutSecretValue "my-secret-id" """{"apiKey": "initial-value"}""")
}

(**
### SSM Parameter Store
*)

customResource "ParameterInitializer" {
    onCreate (CustomResourceHelpers.ssmPutParameter "my-param" "initial-value" "String")
}

(**
## Lifecycle hooks

Every custom resource must implement predictable lifecycle behaviour. Follow the idempotency pattern described in the **AWS Builders Library**: ensure `onCreate`, `onUpdate`, and `onDelete` can be retried safely, and always return consistent physical resource IDs.
*)

customResource "LifecycleResource" {
    onCreate (CustomResourceHelpers.s3PutObject "my-bucket" "status.txt" "created")

    onUpdate (CustomResourceHelpers.s3PutObject "my-bucket" "status.txt" "updated")

    onDelete (
        AwsSdkCall(
            Service = "S3",
            Action = "deleteObject",
            Parameters = dict [ "Bucket", box "my-bucket"; "Key", box "status.txt" ],
            PhysicalResourceId = PhysicalResourceId.Of("my-bucket/status.txt")
        )
    )
}

(**
## Custom SDK calls

When helpers don’t exist, describe the exact AWS API invocation with `createSdkCall`. Combine this with the IAM autogeneration to keep permissions precise, as shown in **Matt Coulter’s CDK Patterns: Custom Resource** reference implementation.
*)

customResource "CustomOperation" {
    onCreate (
        CustomResourceHelpers.createSdkCall
            "EC2" // AWS Service
            "describeRegions" // API Action
            [ "AllRegions", box true ] // Parameters
            "ec2-regions-query" // Physical Resource ID
    )
}

(**
## Advanced configuration

### Custom timeout

Scale the timeout for heavy operations (database schema migrations, large data loads) while keeping retries safe. Track execution duration with CloudWatch metrics so you can tune timeouts proactively.
*)

customResource "LongRunningTask" {
    onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value")
    timeout CustomResourceHelpers.Timeouts.fifteenMinutes
}

(**
### Custom IAM policy

When auto-generated permissions are too broad, define statements explicitly. Mirror the least-privilege approach outlined in **Ben Kehoe’s IAM for Humans** by scoping actions and resources to the exact call being made.
*)

open Amazon.CDK.AWS.IAM

customResource "RestrictedResource" {
    onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value")

    policy (
        AwsCustomResourcePolicy.FromStatements(
            [| PolicyStatement(
                   PolicyStatementProps(Actions = [| "s3:PutObject" |], Resources = [| "arn:aws:s3:::bucket/key" |])
               ) |]
        )
    )
}

(**
### Pin the AWS SDK version

Regulated environments sometimes require a fixed SDK version. Set `installLatestAwsSdk false` and bundle your preferred version, documenting the security approval as advised by the **AWS Security Hub Operational Excellence** checklist.
*)

customResource "LegacySdkResource" {
    onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value")
    installLatestAwsSdk false
}

(**
### Custom Log Retention

*)

open Amazon.CDK.AWS.Logs

customResource "LongTermLogging" {
    onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value")
    logRetention RetentionDays.ONE_MONTH
}

(**
## Complete Example: Database Initialization

This example shows a complete use case - initializing a DynamoDB table with seed data:

*)

let completeExample =
    stack "DatabaseStack" {
        // Create DynamoDB table
        table "UsersTable" {
            partitionKey "id" Amazon.CDK.AWS.DynamoDB.AttributeType.STRING
            billingMode Amazon.CDK.AWS.DynamoDB.BillingMode.PAY_PER_REQUEST
        }

        // Seed initial admin user
        let adminUser = Dictionary<string, obj>()
        adminUser.["id"] <- box "admin-001"
        adminUser.["username"] <- box "admin"
        adminUser.["role"] <- box "administrator"
        adminUser.["createdAt"] <- box (System.DateTime.UtcNow.ToString "o")

        customResource "SeedAdminUser" {
            onCreate (CustomResourceHelpers.dynamoDBPutItem "Users" adminUser)
            timeout CustomResourceHelpers.Timeouts.fiveMinutes
        }
    }

(**
## Best Practices

1. **Idempotency**: Ensure your onCreate operations are idempotent
2. **Timeouts**: Set appropriate timeouts for long-running operations
3. **Error Handling**: AWS SDK operations in Custom Resources will fail the CloudFormation deployment on error
4. **Physical Resource IDs**: Use unique, descriptive IDs for tracking
5. **Cleanup**: Always implement onDelete for resources that need cleanup
6. **Testing**: Test custom resources thoroughly before deployment

## Getting Response Data

Custom Resources can return data that can be used by other resources:

*)

let responseExample =
    stack "ResponseStack" {
        let customRes =
            customResource "DataProvider" {
                onCreate (
                    CustomResourceHelpers.createSdkCall
                        "SSM"
                        "getParameter"
                        [ "Name", box "/my/parameter" ]
                        "ssm-parameter-query"
                )
            }

        // Access response data
        // let paramValue = customRes.GetResponseField("Parameter.Value")

        customRes
    }

(**
## AWS CDK Lib Integration

FsCDK's Custom Resource support is built on top of the official AWS CDK Lib (`Amazon.CDK.Lib` v2.213.0), 
providing:

- Full access to all AWS service APIs
- Type-safe resource construction
- Automatic IAM policy generation
- CloudWatch Logs integration
- Lambda-backed implementation under the hood

## Escape Hatch

For advanced scenarios not covered by the builder, access the underlying CDK construct:

*)

let advancedExample =
    stack "AdvancedStack" {
        let customRes =
            customResource "Advanced" { onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value") }

        // Access underlying AwsCustomResource construct
        // match customRes.CustomResource with
        // | Some cr -> cr.GrantPrincipal // etc.
        // | None -> ()

        customRes
    }

(**
## References

- [AWS CDK Custom Resources Documentation](https://docs.aws.amazon.com/cdk/api/v2/docs/aws-cdk-lib.custom_resources-readme.html)
- [AWS CloudFormation Custom Resources](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-custom-resources.html)
- [FsCDK Best Practices](./iam-best-practices.html)

*)
