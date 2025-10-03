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
              let s3Stack =
                  stack "S3StackMetricsBuilder" {
                      bucket "my-bucket-metrics-builder" {
                          constructId "MyBucketMetricsBuilder"

                          // Basic metrics with just ID
                          metrics { id "all-objects-metrics" }
                      }
                  }

              let application = app { s3Stack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with metrics prefix" {
              let s3Stack =
                  stack "S3StackMetricsBuilder" {
                      bucket "my-bucket-metrics-builder" {
                          constructId "MyBucketMetricsBuilder"

                          metrics {
                              id "uploads-metrics"
                              prefix "uploads/"
                          }
                      }
                  }

              let application = app { s3Stack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with metrics tag filters" {
              let s3Stack =
                  stack "S3StackMetricsBuilder" {
                      bucket "my-bucket-metrics-builder" {
                          constructId "MyBucketMetricsBuilder"

                          metrics {
                              id "tagged-metrics"
                              tagFilters [ "env", "prod"; "team", "analytics" ]
                          }
                      }
                  }

              let application = app { s3Stack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth succeeds with multiple metrics configurations" {
              let s3Stack =
                  stack "S3StackMetricsBuilder" {
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

              let application = app { s3Stack }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          }

          test "app synth fails when metrics id is not provided" {
              Expect.throwsT<System.Exception>
                  (fun () ->
                      let s3Stack =
                          stack "S3StackMetricsBuilder" {
                              bucket "my-bucket-metrics-builder" {
                                  constructId "MyBucketMetricsBuilder"

                                  metrics { prefix "uploads/" }
                              }
                          }

                      let application = app { s3Stack }
                      application.Synth() |> ignore)
                  "Should throw when metrics ID is not provided"
          }

          test "app synth succeeds with combined metrics configurations" {
              let s3Stack =
                  stack "S3StackMetricsBuilder" {
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

              let application = app { s3Stack }
              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 1 "App should synthesize one stack"
          } ]
    |> testSequenced
