module FsCDK.Tests.StackPropsTests

open Expecto
open FsCDK

[<Tests>]
let stackPropsTests =
    testList
        "StackProps DSL"
        [ test "sets env account and region" {
              let envProps =
                  environment {
                      account "123456789012"
                      region "us-east-1"
                  }

              let props =
                  stackProps {
                      envProps
                      stackName "MyStack"
                  }

              Expect.isNotNull (box props.Env) "Env should be set"
              Expect.equal props.Env.Account "123456789012" "Account should match"
              Expect.equal props.Env.Region "us-east-1" "Region should match"
              Expect.equal props.StackName "MyStack" "StackName should match"
          }

          test "applies tags and boolean flags" {
              let props =
                  stackProps {
                      tags [ "env", "dev"; "service", "api" ]
                      terminationProtection true
                      analyticsReporting true
                      crossRegionReferences true
                      suppressTemplateIndentation true
                      notificationArns [ "arn:aws:sns:us-east-1:111122223333:topic1" ]
                  }

              // Tags
              Expect.sequenceEqual
                  (props.Tags |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Seq.toList)
                  [ "env", "dev"; "service", "api" ]
                  "Tags should contain provided items"

              // Flags
              Expect.isTrue (props.TerminationProtection.HasValue && props.TerminationProtection.Value) "TP true"
              Expect.isTrue (props.AnalyticsReporting.HasValue && props.AnalyticsReporting.Value) "AR true"
              Expect.isTrue (props.CrossRegionReferences.HasValue && props.CrossRegionReferences.Value) "CRR true"

              Expect.isTrue
                  (props.SuppressTemplateIndentation.HasValue
                   && props.SuppressTemplateIndentation.Value)
                  "STI true"

              // Notification ARNs
              Expect.equal props.NotificationArns.Length 1 "One ARN expected"
              Expect.equal props.NotificationArns[0] "arn:aws:sns:us-east-1:111122223333:topic1" "ARN should match"
          } ]
