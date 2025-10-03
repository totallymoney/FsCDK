module FsCDK.Tests.S3CorsRuleBuilderTests

open Expecto
open FsCDK
open Amazon.CDK.AWS.S3

[<Tests>]
let s3_cors_rule_builder_tests =
    testList
        "S3 CorsRule builder"
        [ test "app synth succeeds with cors via builder" {
              let s3Stack =
                  stack "S3StackCorsBuilder" {
                      bucket "my-bucket-cors-builder" {
                          constructId "MyBucketCorsBuilder"

                          corsRule {
                              allowedOrigins [ "*" ]
                              allowedMethods [ HttpMethods.GET; HttpMethods.HEAD ]
                              allowedHeaders [ "*" ]
                              id "default"
                              maxAgeSeconds 300
                          }
                      }
                  }

              let application = app { s3Stack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
