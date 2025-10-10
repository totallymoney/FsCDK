module FsCDK.Tests.S3Tests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.S3

[<Tests>]
let s3_bucket_happy_path_tests =
    testList
        "S3 Bucket DSL"
        [ test "app synth succeeds with bucket all-common-properties" {
              let app = App()

              stack "S3Stack" {
                  app

                  bucket "my-bucket" {
                      constructId "MyBucket"
                      blockPublicAccess BlockPublicAccess.BLOCK_ALL
                      encryption BucketEncryption.S3_MANAGED
                      enforceSSL true
                      versioned true
                      removalPolicy RemovalPolicy.DESTROY
                      autoDeleteObjects true
                      websiteIndexDocument "index.html"
                      websiteErrorDocument "error.html"

                      LifecycleRule(Id = "expire-30-days", Enabled = true, Expiration = Duration.Days(30.0))

                      CorsRule(
                          AllowedOrigins = [| "*" |],
                          AllowedMethods = [| HttpMethods.GET; HttpMethods.HEAD |],
                          AllowedHeaders = [| "*" |]
                      )

                      // Metrics configuration
                      BucketMetrics(Id = "all-objects")
                  }
              }

              let cloudAssembly = app.Synth()

              // Basic assertion: we produced exactly one stack in this app
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
