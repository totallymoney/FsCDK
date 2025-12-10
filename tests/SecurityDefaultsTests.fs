module FsCDK.Tests.SecurityDefaultsTests

open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.CloudTrail
open Amazon.CDK.AWS.Logs
open Expecto
open FsCDK

[<Tests>]
let vpcFlowLogsTests =
    testList
        "VPC Flow Logs Security Defaults"
        [ test "VPC has flow logs enabled by default" {
              let spec = vpc "TestVpc" { () }

              Expect.isTrue spec.EnableFlowLogs "Flow logs should be enabled by default"
          }

          test "VPC has ONE_WEEK retention by default" {
              let spec = vpc "TestVpc" { () }

              Expect.equal
                  spec.FlowLogRetention
                  (Some RetentionDays.ONE_WEEK)
                  "Flow logs should have ONE_WEEK retention by default"
          }

          test "VPC allows disabling flow logs" {
              let spec = vpc "TestVpc" { enableFlowLogs false }

              Expect.isFalse spec.EnableFlowLogs "Flow logs should be disabled when explicitly set"
          }

          test "VPC allows custom flow log retention" {
              let spec = vpc "TestVpc" { flowLogRetention RetentionDays.ONE_MONTH }

              Expect.equal spec.FlowLogRetention (Some RetentionDays.ONE_MONTH) "Flow logs should have custom retention"
          } ]
    |> testSequenced

[<Tests>]
let cloudTrailSecurityDefaultsTests =
    testList
        "CloudTrail Security Defaults"
        [ test "CloudTrail is multi-region by default" {
              let spec = cloudTrail "TestTrail" { () }

              Expect.equal
                  (spec.Props.IsMultiRegionTrail |> Option.ofNullable)
                  (Some true)
                  "CloudTrail should be multi-region by default"
          }

          test "CloudTrail includes global service events by default" {
              let spec = cloudTrail "TestTrail" { () }

              Expect.equal
                  (spec.Props.IncludeGlobalServiceEvents |> Option.ofNullable)
                  (Some true)
                  "CloudTrail should include global service events by default"
          }

          test "CloudTrail has file validation enabled by default" {
              let spec = cloudTrail "TestTrail" { () }

              Expect.equal
                  (spec.Props.EnableFileValidation |> Option.ofNullable)
                  (Some true)
                  "CloudTrail should have file validation enabled by default"
          }

          test "CloudTrail sends to CloudWatch Logs by default" {
              let spec = cloudTrail "TestTrail" { () }

              Expect.isTrue spec.SendToCloudWatchLogs "CloudTrail should send to CloudWatch Logs by default"
          }

          test "CloudTrail has ONE_MONTH log retention by default" {
              let spec = cloudTrail "TestTrail" { () }

              Expect.equal
                  spec.CloudWatchLogsRetention
                  (Some RetentionDays.ONE_MONTH)
                  "CloudTrail should have ONE_MONTH retention by default"
          }

          test "CloudTrail allows custom settings" {
              let spec =
                  cloudTrail "CustomTrail" {
                      isMultiRegionTrail false
                      sendToCloudWatchLogs false
                      cloudWatchLogsRetention RetentionDays.THREE_MONTHS
                  }

              Expect.equal
                  (spec.Props.IsMultiRegionTrail |> Option.ofNullable)
                  (Some false)
                  "CloudTrail should allow disabling multi-region"

              Expect.isFalse spec.SendToCloudWatchLogs "CloudTrail should allow disabling CloudWatch Logs"

              Expect.equal
                  spec.CloudWatchLogsRetention
                  (Some RetentionDays.THREE_MONTHS)
                  "CloudTrail should allow custom retention"
          }

          test "CloudTrail defaults constructId to trail name" {
              let spec = cloudTrail "MyTrail" { () }

              Expect.equal spec.ConstructId "MyTrail" "ConstructId should default to trail name"
          }

          test "CloudTrail allows custom constructId" {
              let spec = cloudTrail "MyTrail" { constructId "CustomId" }

              Expect.equal spec.ConstructId "CustomId" "ConstructId should be custom value"
          } ]
    |> testSequenced

[<Tests>]
let dynamoDBSecurityDefaultsTests =
    testList
        "DynamoDB Security Defaults"
        [ test "DynamoDB table has encryption enabled by default" {
              let (spec: FsCDK.TableSpec) =
                  table "TestTable" { partitionKey "Id" AttributeType.STRING }

              // Check via Props - encryption is applied to the CDK TableProps
              Expect.equal
                  (spec.Props.Encryption |> Option.ofNullable)
                  (Some TableEncryption.AWS_MANAGED)
                  "DynamoDB should have AWS_MANAGED encryption by default"
          }

          test "DynamoDB table has PITR enabled by default" {
              let (spec: FsCDK.TableSpec) =
                  table "TestTable" { partitionKey "Id" AttributeType.STRING }

              // Check via Props - PITR is applied to the CDK TableProps
              Expect.isNotNull
                  spec.Props.PointInTimeRecoverySpecification
                  "DynamoDB should have PITR specification set by default"

              Expect.isTrue
                  spec.Props.PointInTimeRecoverySpecification.PointInTimeRecoveryEnabled
                  "DynamoDB should have PITR enabled by default"
          }

          test "DynamoDB allows disabling PITR" {
              let (spec: FsCDK.TableSpec) =
                  table "TestTable" {
                      partitionKey "Id" AttributeType.STRING
                      pointInTimeRecovery false
                  }

              // When PITR is disabled, the specification should be null or have enabled=false
              let pitrEnabled =
                  spec.Props.PointInTimeRecoverySpecification
                  |> Option.ofObj
                  |> Option.map (fun pitr -> pitr.PointInTimeRecoveryEnabled)
                  |> Option.defaultValue false

              Expect.isFalse pitrEnabled "DynamoDB should allow disabling PITR"
          } ]
    |> testSequenced

[<Tests>]
let policyStatementWildcardTests =
    testList
        "PolicyStatement Wildcard Validation"
        [ test "PolicyStatement fails with double wildcards" {
              let thrower () =
                  policyStatement {
                      actions [ "*" ]
                      resources [ "*" ]
                  }
                  |> ignore

              Expect.throws thrower "PolicyStatement should throw on double wildcards"
          } ]
    |> testSequenced
