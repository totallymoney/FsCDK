module FsCDK.Tests.DynamoDBTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB

[<Tests>]
let dynamo_table_dsl_tests =
    testList
        "DynamoDB table DSL"
        [ test "fails when partition key is missing" {
              let thrower () = table "MyTable" { () } |> ignore
              Expect.throws thrower "Table builder should throw when partition key is missing"
          }

          test "defaults constructId to table name" {
              let spec = table "Users" { partitionKey "pk" AttributeType.STRING }

              Expect.equal spec.TableName "Users" "TableName should be set"
              Expect.equal spec.ConstructId "Users" "ConstructId should default to table name"
          }

          test "uses custom constructId when provided" {
              let spec =
                  table "Users" {
                      constructId "UsersTable"
                      partitionKey "pk" AttributeType.STRING
                  }

              Expect.equal spec.ConstructId "UsersTable" "Custom constructId should be used"
          }

          test "applies sort key when configured" {
              let spec =
                  table "Orders" {
                      partitionKey "orderId" AttributeType.STRING
                      sortKey "createdAt" AttributeType.NUMBER
                  }

              Expect.isNotNull (box spec.Props.SortKey) "SortKey should be set"
              Expect.equal spec.Props.SortKey.Name "createdAt" "SortKey name should match"
              Expect.equal spec.Props.SortKey.Type AttributeType.NUMBER "SortKey type should match"
          }

          test "applies billing mode when configured" {
              let spec =
                  table "Billing" {
                      partitionKey "pk" AttributeType.STRING
                      billingMode BillingMode.PAY_PER_REQUEST
                  }

              Expect.equal spec.Props.BillingMode.Value BillingMode.PAY_PER_REQUEST "Billing mode should be set"
          }

          test "applies removal policy when configured" {
              let spec =
                  table "Tmp" {
                      partitionKey "pk" AttributeType.STRING
                      removalPolicy RemovalPolicy.DESTROY
                  }

              Expect.equal spec.Props.RemovalPolicy.Value RemovalPolicy.DESTROY "Removal policy should be set"
          }

          test "enables point-in-time recovery when set to true" {
              let spec =
                  table "History" {
                      partitionKey "pk" AttributeType.STRING
                      pointInTimeRecovery true
                  }

              Expect.isNotNull (box spec.Props.PointInTimeRecoverySpecification) "PITR spec should be created"

              Expect.isTrue
                  spec.Props.PointInTimeRecoverySpecification.PointInTimeRecoveryEnabled
                  "PITR should be enabled"
          }

          test "optional sort key remains unset when not provided" {
              let spec = table "Simple" { partitionKey "pk" AttributeType.STRING }

              Expect.isNull (box spec.Props.SortKey) "SortKey should be null when not configured"
          } ]
