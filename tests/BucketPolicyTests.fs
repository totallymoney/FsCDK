module FsCDK.Tests.BucketPolicyTests

open Amazon.CDK.AWS.IAM
open Expecto
open FsCDK

[<Tests>]
let bucket_policy_tests =
    testSequenced
    <| testList
        "Bucket Policy DSL"
        [ test "fails when bucket is missing" {
              let thrower () =
                  bucketPolicy "MyPolicy" { () } |> ignore

              Expect.throws thrower "BucketPolicy builder should throw when bucket is missing"
          }

          test "accepts bucket from spec" {
              stack "TestStack" {
                  let! bucketSpec = bucket "test-bucket" { () }
                  let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should accept bucket from spec"
              }
          }

          test "accepts policy statements" {
              stack "TestStack" {
                  let! bucketSpec = bucket "test-bucket" { () }

                  let statement1 =
                      policyStatement {
                          effect Effect.ALLOW
                          actions [ "s3:GetObject" ]
                          resources [ "arn:aws:s3:::test/*" ]
                      }

                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          statements [ statement1 ]
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should accept statements"
              }
          }

          test "provides denyInsecureTransport helper" {
              stack "TestStack" {
                  let! bucketSpec = bucket "test-bucket" { () }

                  let denyInsecureTransport =
                      policyStatement {
                          sid "DenyInsecureTransport"
                          effect Effect.DENY
                          principals [ AnyPrincipal() :> IPrincipal ]
                          actions [ "s3:*" ]
                          resources [ bucketSpec.BucketArn + "/*" ]
                          conditions [ "Bool", box (dict [ "aws:SecureTransport", box "false" ]) ]
                      }


                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          statements [ denyInsecureTransport ]
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should add deny insecure transport statement"
              }
          }

          test "provides allowFromIpAddresses helper" {

              stack "test-bucket" {
                  let! bucketSpec = bucket "test-bucket" { () }

                  let allowFromIpAddresses =
                      policyStatement {
                          sid "AllowFromSpecificIPs"
                          effect Effect.ALLOW
                          principals [ AnyPrincipal() :> IPrincipal ]
                          actions [ "s3:GetObject" ]
                          resources [ bucketSpec.BucketArn + "/*" ]

                          conditions
                              [ "IpAddress",
                                box (dict [ "aws:SourceIp", box [| "203.0.113.0/24"; "198.51.100.0/24" |] ]) ]
                      }

                  let policySpec =
                      bucketPolicy "MyPolicy" {
                          bucket bucketSpec
                          statements [ allowFromIpAddresses ]
                      }

                  Expect.equal policySpec.PolicyName "MyPolicy" "Should add IP restriction statement"
              }
          }

          test "defaults constructId to policy name" {
              stack "TestStack" {
                  let! bucketSpec = bucket "test-bucket" { () }
                  let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

                  Expect.equal policySpec.ConstructId "MyPolicy" "ConstructId should default to name"
              }
          } ]
