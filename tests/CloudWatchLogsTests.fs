module FsCDK.Tests.CloudWatchLogsTests

open Amazon.CDK
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.Lambda
open Expecto
open FsCDK

[<Tests>]
let cloudwatch_logs_tests =
    testSequenced
    <| testList
        "CloudWatch Logs DSL"
        [ testList
              "Log Group Tests"
              [ test "creates log group with name" {
                    let logGroupResource = logGroup "/aws/lambda/my-function" { () }

                    Expect.equal logGroupResource.LogGroupName "/aws/lambda/my-function" "Should set log group name"
                }

                test "defaults constructId to log group name" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    Expect.equal logGroupResource.ConstructId "/aws/lambda/test" "ConstructId should default to name"
                }

                test "allows custom constructId" {
                    let logGroupResource = logGroup "/aws/lambda/test" { constructId "MyLogGroup" }

                    Expect.equal logGroupResource.ConstructId "MyLogGroup" "Should use custom constructId"
                } ]

          testList
              "Metric Filter Tests"
              [ test "accepts CloudWatchLogGroupResource" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    let filterResource =
                        metricFilter "ErrorFilter" {
                            logGroup logGroupResource
                            filterPattern (FilterPatterns.errorLogs ())
                            metricName "ErrorCount"
                            metricNamespace "MyApp"
                        }

                    Expect.equal filterResource.FilterName "ErrorFilter" "Should accept CloudWatchLogGroupResource"
                }

                test "defaults constructId to filter name" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    let filterResource =
                        metricFilter "ErrorFilter" {
                            logGroup logGroupResource
                            filterPattern (FilterPatterns.errorLogs ())
                            metricName "ErrorCount"
                            metricNamespace "MyApp"
                        }

                    Expect.equal filterResource.ConstructId "ErrorFilter" "ConstructId should default to filter name"
                }

                test "allows custom constructId" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    let filterResource =
                        metricFilter "ErrorFilter" {
                            constructId "MyCustomId"
                            logGroup logGroupResource
                            filterPattern (FilterPatterns.errorLogs ())
                            metricName "ErrorCount"
                            metricNamespace "MyApp"
                        }

                    Expect.equal filterResource.ConstructId "MyCustomId" "Should use custom constructId"
                }

                test "requires logGroup" {
                    Expect.throws
                        (fun () ->
                            metricFilter "ErrorFilter" {
                                filterPattern (FilterPatterns.errorLogs ())
                                metricName "ErrorCount"
                                metricNamespace "MyApp"
                            }
                            |> ignore)
                        "Should require logGroup"
                }

                test "requires filterPattern" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    Expect.throws
                        (fun () ->
                            metricFilter "ErrorFilter" {
                                logGroup logGroupResource
                                metricName "ErrorCount"
                                metricNamespace "MyApp"
                            }
                            |> ignore)
                        "Should require filterPattern"
                }

                test "requires metricName" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    Expect.throws
                        (fun () ->
                            metricFilter "ErrorFilter" {
                                logGroup logGroupResource
                                filterPattern (FilterPatterns.errorLogs ())
                                metricNamespace "MyApp"
                            }
                            |> ignore)
                        "Should require metricName"
                }

                test "requires metricNamespace" {
                    let logGroupResource = logGroup "/aws/lambda/test" { () }

                    Expect.throws
                        (fun () ->
                            metricFilter "ErrorFilter" {
                                logGroup logGroupResource
                                filterPattern (FilterPatterns.errorLogs ())
                                metricName "ErrorCount"
                            }
                            |> ignore)
                        "Should require metricNamespace"
                } ]

          testList
              "Subscription Filter Tests"
              [ test "subscription filter builder exists" {
                    // Subscription filter tests require actual CDK scope/stack setup
                    // which is complex to test in unit tests
                    // Testing that the builder function exists and returns a builder type
                    let builder = subscriptionFilter "test"
                    Expect.isTrue (builder.GetType().Name.Contains("SubscriptionFilter")) "Subscription filter builder should exist"
                } ]

          testList
              "FilterPatterns Helper Tests"
              [ test "allEvents creates filter pattern" {
                    let pattern = FilterPatterns.allEvents ()
                    Expect.isNotNull pattern "Should create all events pattern"
                }

                test "matchText creates filter pattern" {
                    let pattern = FilterPatterns.matchText "ERROR"
                    Expect.isNotNull pattern "Should create text match pattern"
                }

                test "errorLogs creates filter pattern" {
                    let pattern = FilterPatterns.errorLogs ()
                    Expect.isNotNull pattern "Should create error logs pattern"
                }

                test "warningLogs creates filter pattern" {
                    let pattern = FilterPatterns.warningLogs ()
                    Expect.isNotNull pattern "Should create warning logs pattern"
                }

                test "infoLogs creates filter pattern" {
                    let pattern = FilterPatterns.infoLogs ()
                    Expect.isNotNull pattern "Should create info logs pattern"
                }

                test "http5xxErrors creates filter pattern" {
                    let pattern = FilterPatterns.http5xxErrors ()
                    Expect.isNotNull pattern "Should create HTTP 5xx pattern"
                }

                test "http4xxErrors creates filter pattern" {
                    let pattern = FilterPatterns.http4xxErrors ()
                    Expect.isNotNull pattern "Should create HTTP 4xx pattern"
                } ]

          testList
              "CloudWatchLogsHelpers Tests"
              [ test "RetentionPeriods provides standard values" {
                    Expect.equal
                        CloudWatchLogsHelpers.RetentionPeriods.dev
                        RetentionDays.THREE_DAYS
                        "Should provide dev retention"

                    Expect.equal
                        CloudWatchLogsHelpers.RetentionPeriods.standard
                        RetentionDays.ONE_WEEK
                        "Should provide standard retention"

                    Expect.equal
                        CloudWatchLogsHelpers.RetentionPeriods.production
                        RetentionDays.ONE_MONTH
                        "Should provide production retention"

                    Expect.equal
                        CloudWatchLogsHelpers.RetentionPeriods.compliance
                        RetentionDays.THREE_MONTHS
                        "Should provide compliance retention"

                    Expect.equal
                        CloudWatchLogsHelpers.RetentionPeriods.audit
                        RetentionDays.FIVE_YEARS
                        "Should provide audit retention"
                }

                test "ecsLogGroup creates correct log group name" {
                    let name = CloudWatchLogsHelpers.ecsLogGroup "my-service" "production"
                    Expect.equal name "/aws/ecs/my-service-production" "Should format ECS log group name"
                }

                test "lambdaLogGroup creates correct log group name" {
                    let name = CloudWatchLogsHelpers.lambdaLogGroup "my-function"
                    Expect.equal name "/aws/lambda/my-function" "Should format Lambda log group name"
                }

                test "apiGatewayLogGroup creates correct log group name" {
                    let name = CloudWatchLogsHelpers.apiGatewayLogGroup "my-api" "prod"
                    Expect.equal name "/aws/apigateway/my-api/prod" "Should format API Gateway log group name"
                }

                test "appLogGroup creates correct log group name" {
                    let name = CloudWatchLogsHelpers.appLogGroup "my-app" "staging"
                    Expect.equal name "/my-app/staging" "Should format app log group name"
                } ] ]
