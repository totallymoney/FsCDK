module FsCDK.Tests.S3BucketMetricsTests

open System.Collections.Generic
open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.S3

[<Tests>]
let s3_bucket_metrics_tests =
    testList
        "S3 Bucket metrics builder"
        [ test "app synth succeeds with basic metrics configuration" {
              let app = App()

              stack "S3StackMetricsBuilder" {
                  bucket "my-bucket-metrics-builder" {
                      constructId "MyBucketMetricsBuilder"

                      // Basic metrics with just ID
                      metrics { id "all-objects-metrics" }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with metrics prefix" {
              let app = App()

              stack "S3StackMetricsBuilder" {
                  bucket "my-bucket-metrics-builder" {
                      constructId "MyBucketMetricsBuilder"

                      metrics {
                          id "uploads-metrics"
                          prefix "uploads/"
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with metrics tag filters" {
              let app = App()

              stack "S3StackMetricsBuilder" {
                  app

                  bucket "my-bucket-metrics-builder" {
                      constructId "MyBucketMetricsBuilder"

                      metrics {
                          id "tagged-metrics"
                          tagFilters [ "env", "prod"; "team", "analytics" ]
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with multiple metrics configurations" {
              let app = App()

              stack "S3StackMetricsBuilder" {
                  app

                  bucket "my-bucket-metrics-builder" {
                      constructId "MyBucketMetricsBuilder"

                      metrics {
                          id "uploads-metrics"
                          prefix "uploads/"
                      }

                      metrics {
                          id "downloads-metrics"
                          prefix "downloads/"
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth fails when metrics id is not provided" {
              Expect.throwsT<System.Exception>
                  (fun () ->
                      let app = App()

                      stack "S3StackMetricsBuilder" {
                          app

                          bucket "my-bucket-metrics-builder" {
                              constructId "MyBucketMetricsBuilder"

                              metrics { prefix "uploads/" }
                          }
                      }

                      app.Synth() |> ignore)
                  "Should throw when metrics ID is not provided"
          }

          test "app synth succeeds with combined metrics configurations" {
              let app = App()

              stack "S3StackMetricsBuilder" {
                  app

                  bucket "my-bucket" {
                      metrics {
                          id "all-objects"
                          prefix "logs/"
                      }

                      metrics {
                          id "tagged-objects"

                          tagFilters [ "env", "prod"; "team", "analytics" ]
                      }
                  }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
