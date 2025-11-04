(**
---
title: Managed IAM Policy
category: docs
index: 16
---

# AWS IAM Managed Policy

AWS Managed Policies are standalone identity-based policies that you can attach to multiple users, groups, and roles.
They provide reusable permission sets following the principle of least privilege.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.S3

(**
## Basic Managed Policy

Create a managed policy with explicit permissions.
*)

stack "BasicManagedPolicy" {
    let readOnlyStatement =
        PolicyStatement(
            PolicyStatementProps(
                Effect = Effect.ALLOW,
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources = [| "arn:aws:s3:::my-bucket"; "arn:aws:s3:::my-bucket/*" |]
            )
        )

    managedPolicy "S3ReadPolicy" {
        description "Read-only access to S3 bucket"
        managedPolicyName "S3ReadOnlyPolicy"
        statements [ readOnlyStatement ]
    }
}

(**
## Policy with Multiple Statements

Combine multiple permissions in a single policy.
*)

stack "MultiStatementPolicy" {
    let s3Statement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "S3Access",
                Effect = Effect.ALLOW,
                Actions = [| "s3:GetObject"; "s3:PutObject" |],
                Resources = [| "arn:aws:s3:::data-bucket/*" |]
            )
        )

    let dynamoStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "DynamoDBAccess",
                Effect = Effect.ALLOW,
                Actions = [| "dynamodb:GetItem"; "dynamodb:PutItem" |],
                Resources = [| "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable" |]
            )
        )

    managedPolicy "DataAccessPolicy" {
        description "Access to S3 and DynamoDB"
        statements [ s3Statement; dynamoStatement ]
    }
}

(**
## Using Helper Methods

FsCDK provides convenient helpers for common permission patterns.
*)

stack "HelperMethodsPolicy" {
    managedPolicy "QuickAccessPolicy" {
        description "Quick policy using helpers"

        statements
            [ policyStatement {
                  sid "S3ReadOnly"
                  effect Effect.ALLOW
                  actions [ "s3:GetObject"; "s3:ListBucket" ]
                  resources [ "arn:aws:s3:::my-bucket"; "arn:aws:s3:::my-bucket/*" ]
              }

              policyStatement {
                  sid "S3DenyDelete"
                  effect Effect.DENY
                  actions [ "s3:DeleteObject" ]
                  resources [ "arn:aws:s3:::my-bucket/*" ]
              }

              ]
    }
}

(**
## Using Pre-Built Statements

Use the `ManagedPolicyStatements` module for common scenarios.
*)

stack "PreBuiltStatements" {
    let bucketArn = "arn:aws:s3:::my-bucket"
    let tableArn = "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable"

    managedPolicy "ApplicationPolicy" {
        description "Standard application permissions"

        statements
            [ policyStatement {
                  sid "S3ReadOnlyAccess"
                  effect Effect.ALLOW
                  actions [ "s3:GetObject"; "s3:ListBucket" ]
                  resources [ bucketArn; bucketArn + "/*" ]
              }

              policyStatement {
                  sid "DynamoDBFullAccess"
                  effect Effect.ALLOW
                  actions [ "dynamodb:*" ]
                  resources [ tableArn ]
              }

              policyStatement {
                  sid "CloudWatchLogsWrite"
                  effect Effect.ALLOW
                  actions [ "logs:CreateLogGroup"; "logs:CreateLogStream"; "logs:PutLogEvents" ]
                  resources [ "/aws/lambda/my-function" ]
              } ]
    }
}

(**
## Attaching to Roles

Attach policies to IAM roles for EC2, Lambda, or other services.
*)

stack "PolicyWithRole" {
    let! basicLambdaRole = managedPolicy "LambdaBasicExecution" { managedPolicyName "AWSLambdaBasicExecutionRole" }

    let! s3ReadOnlyPolicy = managedPolicy "S3ReadOnlyAccess" { managedPolicyName "AmazonS3ReadOnlyAccess" }

    let! vpcExecutionPolicy =
        managedPolicy "LambdaVPCAccessExecution" { managedPolicyName "AWSLambdaVPCAccessExecutionRole" }

    let! kmsDecryptPolicy = managedPolicy "KMSDecryptAccess" { managedPolicyName "AWSKeyManagementServicePowerUser" }

    let! xrayWritePolicy = managedPolicy "XRayDaemonWriteAccess" { managedPolicyName "AWSXRayDaemonWriteAccess" }

    // Create a role for Lambda
    let! lambdaRole1 =
        role "my-function-role" {
            assumedBy (ServicePrincipal("lambda.amazonaws.com"))
            managedPolicies [ basicLambdaRole; s3ReadOnlyPolicy; vpcExecutionPolicy; xrayWritePolicy ]

            inlinePolicies
                [ "MyPolicy",
                  policyDocument {
                      statements
                          [ policyStatement {
                                sid "DynamoDBQueryAccess"
                                effect Effect.ALLOW
                                actions [ "dynamodb:Query" ]
                                resources [ "arn:aws:dynamodb:*:*:table/MyTable" ]
                            } ]
                  } ]
        }

    let policy2 =
        policyStatement {
            sid "KMSDecryptAccess"
            effect Effect.ALLOW
            actions [ "kms:Decrypt"; "kms:DescribeKey" ]
            resources [ "*" ]
        }

    (lambdaRole1 :?> Role).AddToPolicy(policy2) |> ignore

    // Create and attach policy
    managedPolicy "LambdaS3Policy" {
        description "Lambda S3 access"

        policyStatement {
            sid "S3FullAccess"
            effect Effect.ALLOW
            actions [ "s3:*" ]
            resources [ "arn:aws:s3:::lambda-bucket"; "arn:aws:s3:::lambda-bucket/*" ]
        }

        roles [ lambdaRole1 ]
    }
}

(**
## Cross-Account Access Policy

Grant permissions for cross-account access.
*)

stack "CrossAccountPolicy" {
    let crossAccountStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "CrossAccountS3Access",
                Effect = Effect.ALLOW,
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources = [| "arn:aws:s3:::shared-bucket"; "arn:aws:s3:::shared-bucket/*" |],
                Principals = [| AccountPrincipal("123456789012") :> IPrincipal |]
            )
        )

    managedPolicy "CrossAccountPolicy" {
        description "Allow access from partner account"
        statements [ crossAccountStatement ]
    }
}

(**
## Conditional Permissions

Use conditions to restrict permissions based on context.
*)

stack "ConditionalPolicy" {
    let conditions = System.Collections.Generic.Dictionary<string, obj>()
    conditions.Add("aws:SourceIp", [| "203.0.113.0/24" |] :> obj)

    let conditionalStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "IPRestrictedAccess",
                Effect = Effect.ALLOW,
                Actions = [| "s3:*" |],
                Resources = [| "arn:aws:s3:::secure-bucket/*" |],
                Conditions = dict [ "IpAddress", conditions ]
            )
        )

    managedPolicy "IPRestrictedPolicy" {
        description "S3 access only from specific IP range"
        statements [ conditionalStatement ]
    }
}

(**
## Secrets Manager Access

Grant access to AWS Secrets Manager secrets.
*)

stack "SecretsPolicy" {
    let secretArn =
        "arn:aws:secretsmanager:us-east-1:123456789012:secret:MySecret-AbCdEf"

    let secretsManagerRead (secretArn: string) =
        PolicyStatement(
            props =
                PolicyStatementProps(
                    Sid = "SecretsManagerRead",
                    Effect = System.Nullable Effect.ALLOW,
                    Actions = [| "secretsmanager:GetSecretValue"; "secretsmanager:DescribeSecret" |],
                    Resources = [| secretArn |]
                )
        )

    managedPolicy "SecretsAccessPolicy" {
        description "Access to application secrets"
        statements [ secretsManagerRead secretArn ]
    }
}

(**
## KMS Encryption Policy

Grant permissions to use KMS keys for encryption/decryption.
*)

stack "KMSPolicy" {
    let kmsKeyArn =
        "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012"

    let kmsDecrypt (keyArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "KMSDecrypt",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "kms:Decrypt"; "kms:DescribeKey" |],
                Resources = [| keyArn |]
            )
        )

    managedPolicy "KMSAccessPolicy" {
        description "KMS key usage for encryption"
        statements [ kmsDecrypt kmsKeyArn ]
    }
}

(**
## Lambda Execution Policy

Complete policy for Lambda function execution.
*)

stack "LambdaExecutionPolicy" {
    let functionName = "my-function"
    let logGroupArn = $"/aws/lambda/{functionName}"
    let bucketArn = "arn:aws:s3:::lambda-data"
    let tableArn = "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable"

    /// Creates a statement for CloudWatch Logs write access
    let cloudWatchLogsWrite (logGroupArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "CloudWatchLogsWrite",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "logs:CreateLogGroup"
                       "logs:CreateLogStream"
                       "logs:PutLogEvents"
                       "logs:DescribeLogStreams" |],
                Resources = [| logGroupArn; logGroupArn + ":*" |]
            )
        )

    /// Creates a statement for SQS full access
    let sqsFullAccess (queueArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "SQSFullAccess",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "sqs:SendMessage"
                       "sqs:ReceiveMessage"
                       "sqs:DeleteMessage"
                       "sqs:GetQueueAttributes"
                       "sqs:GetQueueUrl" |],
                Resources = [| queueArn |]
            )
        )

    /// Creates a statement for S3 full access
    let s3FullAccess (bucketArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "S3FullAccess",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "s3:*" |],
                Resources = [| bucketArn; bucketArn + "/*" |]
            )
        )

    /// Creates a statement for DynamoDB full access
    let dynamoDBFullAccess (tableArn: string) =
        policyStatement {
            sid "DynamoDBFullAccess"
            effect Effect.ALLOW
            actions [ "dynamodb:*" ]
            resources [ tableArn; tableArn + "/index/*" ]
        }

    managedPolicy "LambdaFullAccessPolicy" {
        description "Complete Lambda execution permissions"

        statements
            [ cloudWatchLogsWrite logGroupArn
              s3FullAccess bucketArn
              dynamoDBFullAccess tableArn ]
    }
}

(**
## Read-Only Policy

Create a read-only policy for auditors or monitoring.
*)

stack "ReadOnlyPolicy" {

    /// Creates a statement for EC2 describe permissions (read-only)
    let ec2Describe =
        policyStatement {
            sid "EC2Describe"
            effect Effect.ALLOW

            actions
                [ "ec2:DescribeInstances"
                  "ec2:DescribeImages"
                  "ec2:DescribeKeyPairs"
                  "ec2:DescribeSecurityGroups"
                  "ec2:DescribeAvailabilityZones"
                  "ec2:DescribeSubnets"
                  "ec2:DescribeVpcs" ]

            resources [ "*" ]
        }

    /// Creates a statement for S3 read-only access
    let s3ReadOnly (bucketArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "S3ReadOnly",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources = [| bucketArn; bucketArn + "/*" |]
            )
        )

    let dynamoDBReadOnly (tableArn: string) =
        policyStatement {
            sid "DynamoDBReadOnly"
            effect Effect.ALLOW

            actions
                [ "dynamodb:GetItem"
                  "dynamodb:Query"
                  "dynamodb:Scan"
                  "dynamodb:BatchGetItem"
                  "dynamodb:DescribeTable" ]

            resources [ tableArn; tableArn + "/index/*" ]
        }

    managedPolicy "AuditorPolicy" {
        description "Read-only access for auditing"

        statements
            [ ec2Describe
              s3ReadOnly "arn:aws:s3:::audit-logs"
              dynamoDBReadOnly "arn:aws:dynamodb:us-east-1:123456789012:table/*" ]
    }
}

(**
## Deny Override Policy

Use deny statements to prevent specific actions (overrides allows).
*)

stack "DenyOverridePolicy" {
    let allowStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "AllowS3Access",
                Effect = Effect.ALLOW,
                Actions = [| "s3:*" |],
                Resources = [| "arn:aws:s3:::my-bucket/*" |]
            )
        )

    let denyStatement =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "DenyDeleteActions",
                Effect = Effect.DENY,
                Actions = [| "s3:DeleteObject"; "s3:DeleteBucket" |],
                Resources = [| "arn:aws:s3:::my-bucket"; "arn:aws:s3:::my-bucket/*" |]
            )
        )

    managedPolicy "SafeS3Policy" {
        description "S3 access without delete permissions"
        statements [ allowStatement; denyStatement ]
    }
}

(**
## Best Practices

### Security

- ✅ Follow principle of least privilege
- ✅ Use specific resources instead of wildcards when possible
- ✅ Add explicit deny statements for sensitive actions
- ✅ Use conditions to limit scope (IP, time, MFA)
- ✅ Regularly audit and review policies

### Operational Excellence

- ✅ Use descriptive Sid values for each statement
- ✅ Add meaningful descriptions to policies
- ✅ Group related permissions together
- ✅ Version policies using Git
- ✅ Test policies in non-production first

### Organization

- ✅ Create reusable policies for common patterns
- ✅ Use consistent naming conventions
- ✅ Organize policies by service or team
- ✅ Use paths to organize policies hierarchically
- ✅ Attach policies to roles, not users directly

### Compliance

- ✅ Document why each permission is needed
- ✅ Set up AWS Config rules to monitor policies
- ✅ Enable CloudTrail to audit policy usage
- ✅ Review policies during security audits
- ✅ Implement policy change approval workflows

### Maintenance

- ✅ Remove unused policies regularly
- ✅ Consolidate duplicate policies
- ✅ Update policies when AWS introduces new services
- ✅ Monitor for policy changes in CloudTrail
- ✅ Use IAM Access Analyzer to validate policies

*)
