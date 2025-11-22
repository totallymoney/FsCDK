module FsCDK.Tests.S3LifecycleRuleBuilderTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.S3

[<Tests>]
let s3_lifecycle_rule_builder_tests =
    testList
        "S3 LifecycleRule builder"
        [ test "app synth succeeds with basic lifecycle rule via builder" {
              let app = App()

              stack "S3StackLifecycleBuilder" {
                  scope app

                  bucket "my-bucket-lifecycle-builder" {
                      constructId "MyBucketLifecycleBuilder"

                      lifecycleRule {
                          id "test-lifecycle-rule"
                          enabled true
                          prefix "logs/"
                          expiredObjectDeleteMarker true
                          abortIncompleteMultipartUploadAfter (Duration.Days(7.))
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with transitions via nested builders" {
              let app = App()

              stack "S3StackLifecycleBuilder" {
                  scope app

                  bucket "my-bucket-lifecycle-builder" {
                      constructId "MyBucketLifecycleBuilder"

                      lifecycleRule {
                          id "transition-rule"
                          enabled true
                          prefix "data/"

                          transition {
                              storageClass StorageClass.INTELLIGENT_TIERING
                              transitionAfter (Duration.Days(90.))
                          }

                          transition {
                              storageClass StorageClass.GLACIER
                              transitionAfter (Duration.Days(180.))
                          }
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with noncurrent version transitions via nested builders" {
              let app = App()

              stack "S3StackLifecycleBuilder" {
                  scope app

                  bucket "my-bucket-lifecycle-builder" {
                      constructId "MyBucketLifecycleBuilder"
                      versioned true

                      lifecycleRule {
                          id "noncurrent-transition-rule"
                          enabled true

                          noncurrentVersionTransition {
                              storageClass StorageClass.GLACIER
                              transitionAfter (Duration.Days(90.))
                              noncurrentVersionsToRetain 5.0
                          }
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
