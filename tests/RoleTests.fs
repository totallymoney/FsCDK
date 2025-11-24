module FsCDK.Tests.RoleTests

open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Lambda
open Expecto
open FsCDK

[<Tests>]
let roleTests =
    testList
        "Role Tests"
        [ test "app synth succeeds with cors via builder" {

              stack "S3StackCorsBuilder" {

                  let! fn =
                      lambda "MyFunctionCorsBuilder" {
                          handler "index.handler"
                          runtime Runtime.NODEJS_18_X
                          code (Code.FromInline("exports.handler = async () => {};"))
                      }

                  let! managedPolicySpec =
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

                  let role =
                      role "MyRoleCorsBuilder" {
                          assumedBy fn.GrantPrincipal
                          description "Role for MyFunctionCorsBuilder"
                          externalIds [ "external-id-1"; "external-id-2" ]
                          managedPolicies [ managedPolicySpec ]
                      }

                  let! expectRole = role

                  Expect.isNotNull expectRole "Role should be created successfully"
                  Expect.equal role.Props.AssumedBy fn.GrantPrincipal "Role should have correct assumedBy principal"

                  Expect.equal
                      role.Props.Description
                      "Role for MyFunctionCorsBuilder"
                      "Role should have correct description"
              }
          } ]
    |> testSequenced
