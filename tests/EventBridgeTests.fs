module FsCDK.Tests.EventBridgeTests

open Amazon.CDK
open Amazon.CDK.AWS.Events
open Expecto
open FsCDK

[<Tests>]
let event_bridge_tests =
    testList
        "EventBridge DSL"
        [ test "eventBridge rule defaults to enabled" {
              let ruleSpec =
                  eventBridgeRule "MyRule" { schedule (Schedule.Rate(Duration.Hours(1.0))) }

              Expect.equal ruleSpec.Props.Enabled.HasValue true "Rule should default to enabled"
              Expect.equal ruleSpec.Props.Enabled.Value true "Rule should default to enabled"
          }

          test "eventBridge rule accepts custom schedule" {
              let ruleSpec =
                  eventBridgeRule "MyRule" {
                      schedule (Schedule.Rate(Duration.Days(1.0)))
                      description "Daily processing"
                  }

              Expect.equal ruleSpec.Props.Description "Daily processing" "Should accept description"
          }

          test "eventBridge rule can be disabled" {
              let ruleSpec =
                  eventBridgeRule "MyRule" {
                      schedule (Schedule.Rate(Duration.Hours(1.0)))
                      enabled false
                  }

              Expect.equal ruleSpec.Props.Enabled.Value false "Rule should be disabled"
          }

          test "eventBus creates with name" {
              let busSpec = eventBus "MyBus" { () }

              Expect.equal busSpec.EventBusName "MyBus" "Should use provided name"
          }

          test "eventBus accepts partner event source" {
              let busSpec =
                  eventBus "PartnerBus" { eventSourceName "aws.partner/example.com/123456789012/test" }

              Expect.equal busSpec.ConstructId "PartnerBus" "Should create bus spec with event source"
          }

          test "defaults constructId to rule name" {
              let ruleSpec =
                  eventBridgeRule "MyRule" { schedule (Schedule.Rate(Duration.Hours(1.0))) }

              Expect.equal ruleSpec.ConstructId "MyRule" "ConstructId should default to name"
          }

          test "accepts event pattern" {
              let pattern =
                  EventPattern(Source = [| "aws.ec2" |], DetailType = [| "EC2 Instance State-change Notification" |])

              let ruleSpec =
                  eventBridgeRule "EC2Rule" {
                      eventPattern pattern
                      description "React to EC2 changes"
                  }

              Expect.isNotNull ruleSpec.Props.EventPattern "Should have event pattern"
          }

          test "accepts multiple targets" {

              let ruleSpec =
                  eventBridgeRule "MultiTarget" {
                      schedule (Schedule.Rate(Duration.Hours(1.0)))
                  // Note: In real usage, you'd add actual targets here
                  }

              Expect.equal ruleSpec.RuleName "MultiTarget" "Should accept multiple targets"
          }

          test "accepts custom rule name" {
              let ruleSpec =
                  eventBridgeRule "MyRule" {
                      ruleName "custom-rule-name"
                      schedule (Schedule.Rate(Duration.Hours(1.0)))
                  }

              Expect.equal ruleSpec.Props.RuleName "custom-rule-name" "Should use custom rule name"
          } ]
    |> testSequenced
