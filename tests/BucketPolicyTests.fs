module FsCDK.Tests.BucketPolicyTests

open Amazon.CDK.AWS.IAM
open Expecto
open FsCDK

[<Tests>]
let bucket_policy_tests =
    testList
        "Bucket Policy DSL"
        [ test "accepts bucket from spec" {
              stack "TestStack" {
                  let! bucketSpec = bucket "s3-test" { () }
                  let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should accept bucket"
              }
          }

          test "accepts policy statements" {

              stack "TestStack" {
                  let! bucketSpec = bucket "s3-test" { () }

                  let statement1 =
                      PolicyStatement(
                          props =
                              PolicyStatementProps(
                                  Effect = System.Nullable Effect.ALLOW,
                                  Actions = [| "s3:GetObject" |],
                                  Resources = [| "arn:aws:s3:::test/*" |]
                              )
                      )

                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          statement statement1
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should accept statements"
              }
          }

          test "provides denyInsecureTransport helper" {
              stack "TestStack" {
                  let! bucketSpec = bucket "s3-test" { () }

                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          denyInsecureTransport
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should add deny insecure transport statement"

              }
          }

          test "provides allowFromIpAddresses helper" {
              stack "TestStack" {
                  let! bucketSpec = bucket "s3-test" { () }

                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          allowFromIpAddresses [ "203.0.113.0/24"; "198.51.100.0/24" ]
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should add IP restriction statement"
              }
          }

          test "defaults constructId to policy name" {
              stack "TestStack" {
                  let! bucketSpec = bucket "s3-test" { () }
                  let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

                  Expect.equal policySpec.ConstructId "MyPolicy" "ConstructId should default to name"
              }
          } ]
    |> testSequenced
