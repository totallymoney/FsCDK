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
                  scope app

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
          }

          test "app synth succeeds with bucket using secure defaults" {
              let app = App()

              stack "S3DefaultsStack" {
                  scope app
                  // Uses secure defaults:
                  // - BlockPublicAccess = BLOCK_ALL
                  // - Encryption = KMS_MANAGED
                  // - EnforceSSL = true
                  // - Versioned = false
                  bucket "secure-bucket" { () }
              }

              let cloudAssembly = app.Synth()

              // Basic assertion: we produced exactly one stack in this app
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack with secure defaults"
          }

          test "bucket and s3Bucket are aliases" {
              let bucketResult = bucket "test-bucket" { () }
              let s3BucketResult = s3Bucket "test-bucket" { () }

              // Both should have the same properties
              Expect.equal bucketResult.BucketName s3BucketResult.BucketName "Bucket names should match"
              Expect.equal bucketResult.ConstructId s3BucketResult.ConstructId "Construct IDs should match"
          }

          test "settings before a yielded rule are kept" {
              let spec =
                  bucket "audit-logs" {
                      versioned true
                      encryption BucketEncryption.S3_MANAGED
                      enforceSSL false
                      blockPublicAccess BlockPublicAccess.BLOCK_ACLS_ONLY

                      LifecycleRule(Id = "expire", Enabled = true, Expiration = Duration.Days(30.0))
                      CorsRule(AllowedOrigins = [| "*" |], AllowedMethods = [| HttpMethods.GET |])
                      BucketMetrics(Id = "all-objects")
                  }

              Expect.equal spec.Props.Versioned.Value true "versioned should survive the rules"
              Expect.equal spec.Props.Encryption.Value BucketEncryption.S3_MANAGED "encryption should survive the rules"
              Expect.equal spec.Props.EnforceSSL.Value false "enforceSSL should survive the rules"

              Expect.equal
                  spec.Props.BlockPublicAccess
                  BlockPublicAccess.BLOCK_ACLS_ONLY
                  "blockPublicAccess should survive the rules"

              Expect.equal spec.Props.LifecycleRules.Length 1 "The lifecycle rule should be kept"
          }

          test "secure defaults apply with and without rules" {
              let plain = bucket "plain-bucket" { () }

              let withRule =
                  bucket "rule-bucket" {
                      LifecycleRule(Id = "expire", Enabled = true, Expiration = Duration.Days(30.0))
                  }

              for spec in [ plain; withRule ] do
                  Expect.equal spec.Props.Encryption.Value BucketEncryption.KMS_MANAGED "Encryption defaults to KMS"
                  Expect.equal spec.Props.EnforceSSL.Value true "EnforceSSL defaults to true"
                  Expect.equal spec.Props.Versioned.Value false "Versioned defaults to false"
                  Expect.equal spec.Props.BlockPublicAccess BlockPublicAccess.BLOCK_ALL "Public access is blocked"
          } ]
    |> testSequenced
