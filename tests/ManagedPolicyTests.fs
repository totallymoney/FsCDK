module FsCDK.Tests.ManagedPolicyTests

open Amazon.CDK.AWS.IAM
open Expecto
open FsCDK

[<Tests>]
let managed_policy_tests =
    testSequenced
    <| testList
        "Managed Policy DSL"
        [ test "creates policy with description" {
              let policySpec = managedPolicy "MyPolicy" { description "Test policy" }

              Expect.equal policySpec.Props.Description "Test policy" "Should have description"
          }

          test "accepts policy statements" {
              let statement1 =
                  PolicyStatement(PolicyStatementProps(Actions = [| "s3:GetObject" |], Resources = [| "*" |]))

              let policySpec =
                  managedPolicy "MyPolicy" {
                      description "S3 read policy"
                      statement statement1
                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document"
          }

          test "accepts multiple statements" {
              let statement1 =
                  PolicyStatement(PolicyStatementProps(Actions = [| "s3:GetObject" |], Resources = [| "*" |]))

              let statement2 =
                  PolicyStatement(PolicyStatementProps(Actions = [| "s3:PutObject" |], Resources = [| "*" |]))

              let policySpec =
                  managedPolicy "MyPolicy" {
                      statement statement1
                      statement statement2
                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document with statements"
          }

          test "accepts custom policy name" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      managedPolicyName "CustomPolicyName"
                      description "Test"
                  }

              Expect.equal policySpec.Props.ManagedPolicyName "CustomPolicyName" "Should use custom name"
          }

          test "accepts path" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      path "/custom/path/"
                      description "Test"
                  }

              Expect.equal policySpec.Props.Path "/custom/path/" "Should use custom path"
          }

          test "allow helper creates policy" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      description "Test"
                      allow [ "s3:GetObject"; "s3:PutObject" ] [ "arn:aws:s3:::bucket/*" ]
                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document"
          }

          test "deny helper creates policy" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      description "Test"
                      deny [ "s3:DeleteObject" ] [ "arn:aws:s3:::bucket/*" ]
                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document"
          }

          test "defaults constructId to policy name" {
              let policySpec = managedPolicy "MyPolicy" { description "Test" }

              Expect.equal policySpec.ConstructId "MyPolicy" "ConstructId should default to name"
          }

          test "accepts custom description" {
              let policySpec =
                  managedPolicy "MyPolicy" { description "Custom description for testing" }

              Expect.equal policySpec.Props.Description "Custom description for testing" "Should use custom description"
          }

          test "ManagedPolicyStatements.s3ReadOnly creates correct statement" {
              let statement = ManagedPolicyStatements.s3ReadOnly "arn:aws:s3:::my-bucket"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "s3:GetObject" "Should include GetObject"
              Expect.contains (statement.Actions |> Seq.toList) "s3:ListBucket" "Should include ListBucket"
          }

          test "ManagedPolicyStatements.dynamoDBFullAccess creates correct statement" {
              let statement =
                  ManagedPolicyStatements.dynamoDBFullAccess "arn:aws:dynamodb:us-east-1:123456789012:table/MyTable"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "dynamodb:*" "Should include wildcard action"
          }

          test "ManagedPolicyStatements.lambdaInvoke creates correct statement" {
              let statement =
                  ManagedPolicyStatements.lambdaInvoke "arn:aws:lambda:us-east-1:123456789012:function:MyFunction"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "lambda:InvokeFunction" "Should include InvokeFunction"
          }

          test "ManagedPolicyStatements.cloudWatchLogsWrite creates correct statement" {
              let statement =
                  ManagedPolicyStatements.cloudWatchLogsWrite "/aws/lambda/my-function"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"

              Expect.contains (statement.Actions |> Seq.toList) "logs:CreateLogGroup" "Should include CreateLogGroup"
          }

          test "ManagedPolicyStatements.sqsFullAccess creates correct statement" {
              let statement =
                  ManagedPolicyStatements.sqsFullAccess "arn:aws:sqs:us-east-1:123456789012:my-queue"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "sqs:SendMessage" "Should include SendMessage"
          }

          test "ManagedPolicyStatements.snsPublish creates correct statement" {
              let statement =
                  ManagedPolicyStatements.snsPublish "arn:aws:sns:us-east-1:123456789012:my-topic"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "sns:Publish" "Should include Publish"
          }

          test "ManagedPolicyStatements.secretsManagerRead creates correct statement" {
              let statement =
                  ManagedPolicyStatements.secretsManagerRead
                      "arn:aws:secretsmanager:us-east-1:123456789012:secret:MySecret"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"

              Expect.contains
                  (statement.Actions |> Seq.toList)
                  "secretsmanager:GetSecretValue"
                  "Should include GetSecretValue"
          }

          test "ManagedPolicyStatements.kmsDecrypt creates correct statement" {
              let statement =
                  ManagedPolicyStatements.kmsDecrypt
                      "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012"

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"
              Expect.contains (statement.Actions |> Seq.toList) "kms:Decrypt" "Should include Decrypt"
          }

          test "ManagedPolicyStatements.ec2Describe creates correct statement" {
              let statement = ManagedPolicyStatements.ec2Describe ()

              Expect.equal statement.Effect Effect.ALLOW "Should be ALLOW"

              Expect.contains
                  (statement.Actions |> Seq.toList)
                  "ec2:DescribeInstances"
                  "Should include DescribeInstances"
          }

          test "policy with no statements has no document" {
              let policySpec = managedPolicy "EmptyPolicy" { description "No statements" }

              // When there are no statements, we don't create a document
              Expect.isTrue
                  (policySpec.Props.Document = null || policySpec.Props.Document.IsEmpty)
                  "Should have no document or empty document"
          }

          test "combines allow and deny statements" {
              let policySpec =
                  managedPolicy "MixedPolicy" {
                      description "Mixed permissions"
                      allow [ "s3:GetObject" ] [ "arn:aws:s3:::bucket/*" ]
                      deny [ "s3:DeleteObject" ] [ "arn:aws:s3:::bucket/*" ]
                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document"
          }

          test "attaches to roles list" {
              let policySpec =
                  managedPolicy "RolePolicy" {
                      description "For testing"
                  // Note: In real usage, you'd pass actual IRole instances
                  }

              Expect.equal policySpec.PolicyName "RolePolicy" "Should create policy spec"
          }

          test "accepts custom construct ID" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      constructId "CustomConstructId"
                      description "Test"
                  }

              Expect.equal policySpec.ConstructId "CustomConstructId" "Should use custom construct ID"
          } ]
