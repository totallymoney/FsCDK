(*** hide ***)
#r "nuget: Amazon.CDK.Lib, 2.213.0"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

(**
# AWS CDK Custom Resources

AWS CDK Custom Resources allow you to run custom code during CloudFormation stack lifecycle events (create, update, delete). 
This is useful for tasks like:
- Database migrations and seeding
- Calling third-party APIs
- Certificate validation
- Resource initialization
- Custom DNS record creation

FsCDK provides a high-level builder for creating Custom Resources with sensible defaults following AWS best practices.

## Basic Usage

Here's a simple example of a Custom Resource that puts an object into an S3 bucket during stack creation:

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
## Production Defaults

FsCDK Custom Resources come with production-ready defaults:

- **Timeout**: 5 minutes (configurable)
- **Install Latest AWS SDK**: `true` (ensures latest AWS features)
- **Log Retention**: 1 week (CloudWatch Logs)
- **Auto Policy**: Automatically creates IAM policy from SDK calls

## Helper Functions

FsCDK provides built-in helpers for common operations:

### S3 Operations
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
## Lifecycle Hooks

Custom Resources support three lifecycle hooks:

- **onCreate**: Runs when the resource is created
- **onUpdate**: Runs when the resource is updated
- **onDelete**: Runs when the resource is deleted
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
## Custom SDK Calls

For operations not covered by the helpers, use `createSdkCall`:

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
## Advanced Configuration

### Custom Timeout

For long-running operations:

*)

customResource "LongRunningTask" {
    onCreate (CustomResourceHelpers.s3PutObject "bucket" "key" "value")
    timeout CustomResourceHelpers.Timeouts.fifteenMinutes
}

(**
### Custom IAM Policy

For fine-grained permissions:

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
### Disable Latest SDK

For environments that require specific SDK versions:

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
            tableName "Users"
            partitionKey ("id", Amazon.CDK.AWS.DynamoDB.AttributeType.STRING)
            billingMode Amazon.CDK.AWS.DynamoDB.BillingMode.PAY_PER_REQUEST
        }

        // Seed initial admin user
        let adminUser = Dictionary<string, obj>()
        adminUser.["id"] <- box "admin-001"
        adminUser.["username"] <- box "admin"
        adminUser.["role"] <- box "administrator"
        adminUser.["createdAt"] <- box (System.DateTime.UtcNow.ToString("o"))

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
