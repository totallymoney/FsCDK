module FsCDK.Tests.CloudWatchDashboardTests

open Amazon.CDK
open Amazon.CDK.AWS.CloudWatch
open Expecto
open FsCDK

[<Tests>]
let cloudwatch_dashboard_tests =
    testSequenced
    <| testList
        "CloudWatch Dashboard DSL"
        [ test "creates empty dashboard" {
              let dashboardSpec = dashboard "MyDashboard" { () }

              Expect.equal dashboardSpec.DashboardName "MyDashboard" "Should create dashboard"
          }

          test "accepts custom dashboard name" {
              stack "DashboardStack" {
                  let dashboardSpec = dashboard "production-dashboard" { () }

                  Expect.equal dashboardSpec.DashboardName "production-dashboard" "Should use custom name"
              }
          }

          test "defaults to 5 minute interval" {
              let interval = Duration.Minutes 5.0
              let dashboardSpec = dashboard "MyDashboard" { defaultInterval interval }

              Expect.equal
                  (dashboardSpec.Props.DefaultInterval.ToString())
                  (interval.ToString())
                  "Should default to 5 minutes"
          }

          test "allows custom default interval" {
              let interval = Duration.Minutes 1.0
              let dashboardSpec = dashboard "MyDashboard" { defaultInterval interval }

              Expect.equal dashboardSpec.Props.DefaultInterval interval "Should use custom interval"
          }

          test "accepts time range" {
              let dashboardSpec =
                  dashboard "MyDashboard" {
                      startTime "-PT3H"
                      endTime "PT0H"
                  }

              Expect.equal dashboardSpec.Props.Start "-PT3H" "Should set start time"
              Expect.equal dashboardSpec.Props.End "PT0H" "Should set end time"
          }

          test "defaults constructId to dashboard name" {
              let dashboardSpec = dashboard "MyDashboard" { () }

              Expect.equal dashboardSpec.ConstructId "MyDashboard" "ConstructId should default to name"
          } ]
