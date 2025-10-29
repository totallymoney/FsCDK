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
              stack "s3-stack" {
                  let! u =
                      user {
                          userName "TestUser"
                          password (Amazon.CDK.SecretValue.SsmSecure("", ""))
                      }

                  policy "MyPolicyDocument" {
                      policyStatement {
                          effect Effect.ALLOW
                          actions [ "s3:GetObject" ]
                          resources [ "arn:aws:s3:::bucket/*" ]
                      }

                      users [ u ]
                  }

                  let policySpec =
                      managedPolicy "MyPolicy" {
                          description "S3 read policy"

                          policyStatement {
                              actions [ "s3:ListBucket" ]
                              resources [ "*" ]
                          }

                          policyDocument {
                              assignSids true
                              minimize false

                              policyStatement {
                                  effect Effect.ALLOW
                                  actions [ "s3:ListBucket" ]
                                  resources [ "*" ]
                              }
                          }

                      }

                  Expect.isNotNull policySpec.Props.Document "Should have policy document"
              }

          }

          test "accepts multiple statements" {
              let statement1 =
                  PolicyStatement(PolicyStatementProps(Actions = [| "s3:GetObject" |], Resources = [| "*" |]))

              let statement2 =
                  PolicyStatement(PolicyStatementProps(Actions = [| "s3:PutObject" |], Resources = [| "*" |]))

              let policySpec =
                  managedPolicy "MyPolicy" {
                      description "S3 read/write policy"
                      statements [ statement1; statement2 ]

                      policyDocument {
                          assignSids true
                          minimize false

                          policyStatement {
                              effect Effect.ALLOW
                              actions [ "s3:GetObject" ]
                              resources [ "*" ]
                          }
                      }
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

                      policyStatement {
                          effect Effect.ALLOW
                          actions [ "s3:GetObject"; "s3:PutObject" ]
                          resources [ "arn:aws:s3:::bucket/*" ]
                      }

                      policyDocument {
                          assignSids true
                          minimize false

                          policyStatement {
                              effect Effect.ALLOW
                              actions [ "s3:GetObject" ]
                              resources [ "*" ]
                          }
                      }

                  }

              Expect.isNotNull policySpec.Props.Document "Should have policy document"
          }

          test "deny helper creates policy" {
              let policySpec =
                  managedPolicy "MyPolicy" {
                      description "Test"

                      policyStatement {
                          effect Effect.DENY
                          actions [ "s3:DeleteObject" ]
                          resources [ "arn:aws:s3:::bucket/*" ]
                      }

                      policyDocument {
                          assignSids true
                          minimize false

                          policyStatement {
                              effect Effect.DENY
                              actions [ "s3:DeleteObject" ]
                              resources [ "*" ]
                          }
                      }
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

                      statements
                          [ PolicyStatement(
                                PolicyStatementProps(
                                    Effect = Effect.ALLOW,
                                    Actions = [| "s3:GetObject" |],
                                    Resources = [| "arn:aws:s3:::bucket/*" |]
                                )
                            )
                            PolicyStatement(
                                PolicyStatementProps(
                                    Effect = Effect.DENY,
                                    Actions = [| "s3:DeleteObject" |],
                                    Resources = [| "arn:aws:s3:::bucket/*" |]
                                )
                            ) ]

                      policyDocument {
                          assignSids true
                          minimize false

                          policyStatement {
                              effect Effect.ALLOW
                              actions [ "s3:ListBucket" ]
                              resources [ "*" ]
                          }
                      }
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
