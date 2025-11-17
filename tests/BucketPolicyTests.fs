module FsCDK.Tests.BucketPolicyTests

open Amazon.CDK.AWS.IAM
open Expecto
open FsCDK

[<Tests>]
let bucket_policy_tests =
    testList
        "Bucket Policy DSL"
        [ test "fails when bucket is missing" {
              let thrower () =
                  bucketPolicy "MyPolicy" { () } |> ignore

              Expect.throws thrower "BucketPolicy builder should throw when bucket is missing"
          }

          test "accepts bucket from spec" {
              let bucketSpec = bucket "TestBucket" { () }
              let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

              Expect.equal policySpec.PolicyName "MyPolicy" "Should accept bucket"
          }

          test "accepts policy statements" {
              let bucketSpec = bucket "TestBucket" { () }

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

          test "provides denyInsecureTransport helper" {
              let bucketSpec = bucket "TestBucket" { () }

              let policySpec =
                  bucketPolicy "MyPolicy" {
                      bucket bucketSpec
                      denyInsecureTransport
                  }

              Expect.equal policySpec.PolicyName "MyPolicy" "Should add deny insecure transport statement"
          }

          test "provides allowFromIpAddresses helper" {
              let bucketSpec = bucket "TestBucket" { () }

              let policySpec =
                  bucketPolicy "MyPolicy" {
                      bucket bucketSpec
                      allowFromIpAddresses [ "203.0.113.0/24"; "198.51.100.0/24" ]
                  }

              Expect.equal policySpec.PolicyName "MyPolicy" "Should add IP restriction statement"
          }

          test "defaults constructId to policy name" {
              let bucketSpec = bucket "TestBucket" { () }
              let policySpec = bucketPolicy "MyPolicy" { bucket bucketSpec }

              Expect.equal policySpec.ConstructId "MyPolicy" "ConstructId should default to name"
          } ]
    |> testSequenced
