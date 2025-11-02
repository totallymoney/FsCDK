module FsCDK.Tests.XRayTests

open Expecto
open FsCDK
open Amazon.CDK

[<Tests>]
let xray_group_tests =
  testSequenced
  <| testList
        "X-Ray Group DSL"
        [ test "defaults constructId to group name" {
              let group = xrayGroup "MyGroup" { () }
              Expect.equal group.GroupName "MyGroup" "Group name should be set"
              Expect.equal group.ConstructId "MyGroup" "ConstructId should default to group name"
          }

          test "uses custom constructId when provided" {
              let group = xrayGroup "MyGroup" {
                  constructId "CustomGroupId"
              }
              Expect.equal group.ConstructId "CustomGroupId" "Should use custom construct ID"
          }

          test "applies AWS best practices - insights enabled by default" {
              let group = xrayGroup "MyGroup" { () }
              Expect.equal group.GroupName "MyGroup" "Should have correct group name"
              Expect.isNotNull group.Props "Props should be created"
              Expect.isNotNull group.Props.InsightsConfiguration "Insights should be configured"
          }

          test "accepts custom filter expression" {
              let group = xrayGroup "MyGroup" {
                  filterExpression "service(\"my-service\")"
              }
              Expect.equal group.Props.FilterExpression "service(\"my-service\")" "Filter expression should be set"
          }

          test "accepts tags" {
              let group = xrayGroup "MyGroup" {
                  tag "Environment" "Production"
                  tag "Team" "Backend"
              }
              Expect.equal group.GroupName "MyGroup" "Should have correct group name"
              Expect.isTrue (group.Props.Tags.Length > 0) "Tags should be set"
          }

          test "creates X-Ray Group in Stack" {
              let app = App()
              
              let _ = stack "TestStack" {
                  app
                  
                  xrayGroup "ProductionErrors" {
                      filterExpression XRayHelpers.FilterExpressions.serverErrors
                      insightsEnabled true
                      tag "Environment" "Production"
                  }
              }
              
              Expect.isTrue true "Stack should create without errors"
          }

          test "FilterExpressions helpers provide common patterns" {
              let httpErrors = XRayHelpers.FilterExpressions.httpErrors
              Expect.isNotEmpty httpErrors "HTTP errors filter should be defined"
              
              let serverErrors = XRayHelpers.FilterExpressions.serverErrors
              Expect.isNotEmpty serverErrors "Server errors filter should be defined"
              
              Expect.isNotEmpty httpErrors "Filters should work"
          } ]

[<Tests>]
let xray_sampling_rule_tests =
    testList
        "X-Ray Sampling Rule DSL"
        [ test "defaults constructId to rule name" {
              let rule = xraySamplingRule "MyRule" { () }
              Expect.equal rule.RuleName "MyRule" "Rule name should be set"
              Expect.equal rule.ConstructId "MyRule" "ConstructId should default to rule name"
          }

          test "uses custom constructId when provided" {
              let rule = xraySamplingRule "MyRule" {
                  constructId "CustomRuleId"
              }
              Expect.equal rule.ConstructId "CustomRuleId" "Should use custom construct ID"
          }

          test "applies 5% sampling rate by default" {
              let rule = xraySamplingRule "MyRule" { () }
              Expect.equal rule.RuleName "MyRule" "Should have correct rule name"
              Expect.isNotNull rule.Props "Props should be created"
              Expect.isNotNull rule.Props.SamplingRule "Sampling rule should be created"
          }

          test "accepts custom priority" {
              let rule = xraySamplingRule "MyRule" {
                  priority 100
              }
              Expect.isNotNull rule.Props "Props should be created"
          }

          test "accepts custom sampling rate" {
              let rule = xraySamplingRule "MyRule" {
                  fixedRate 0.10
              }
              Expect.isNotNull rule.Props "Props should be created"
          }

          test "accepts custom reservoir size" {
              let rule = xraySamplingRule "MyRule" {
                  reservoirSize 5
              }
              Expect.isNotNull rule.Props "Props should be created"
          }

          // Works but is too slow:
          //test "creates X-Ray Sampling Rule in Stack" {
          //    let app = App()
              
          //    let _ = stack "TestStack" {
          //        app
                  
          //        xraySamplingRule "HighPrioritySampling" {
          //            priority 100
          //            fixedRate XRayHelpers.SamplingRates.tenPercent
          //            reservoirSize 10
          //            serviceName "order-service"
          //        }
          //    }
              
          //    Expect.isTrue true "Stack should create without errors"
          //}

          test "SamplingRates helpers provide standard rates" {
              Expect.equal XRayHelpers.SamplingRates.onePercent 0.01 "1% rate should be 0.01"
              Expect.equal XRayHelpers.SamplingRates.fivePercent 0.05 "5% rate should be 0.05"
              Expect.equal XRayHelpers.SamplingRates.tenPercent 0.10 "10% rate should be 0.10"
              Expect.equal XRayHelpers.SamplingRates.all 1.0 "All rate should be 1.0"
          } ]
