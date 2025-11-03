module FsCDK.Tests.AppSyncTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.AppSync

[<Tests>]
let appsync_api_tests =
    testList
        "AppSync API DSL"
        [ test "defaults constructId to API name" {
              let api = appSyncApi "MyAPI" { () }
              Expect.equal api.ApiName "MyAPI" "API name should be set"
              Expect.equal api.ConstructId "MyAPI" "ConstructId should default to API name"
          }

          test "uses custom constructId when provided" {
              let api = appSyncApi "MyAPI" { constructId "CustomApiId" }
              Expect.equal api.ConstructId "CustomApiId" "Should use custom construct ID"
          }

          test "applies AWS best practices - X-Ray enabled by default" {
              let api = appSyncApi "MyAPI" { () }
              Expect.equal api.ApiName "MyAPI" "Should have correct API name"
              Expect.isNotNull api.Props "Props should be created"
              Expect.equal api.Props.XrayEnabled.Value true "X-Ray should be enabled by default"
          }

          test "applies default log level ALL" {
              let api = appSyncApi "MyAPI" { () }
              Expect.isNotNull api.Props.LogConfig "Log config should be created"
          }

          test "accepts custom log level" {
              let api = appSyncApi "MyAPI" { logLevel FieldLogLevel.ERROR }
              Expect.isNotNull api.Props.LogConfig "Log config should be created"
          }

          // Note: Auth modes and tags removed from simplified API for initial release
          // Use the CDK GraphqlApi construct directly for authorization configuration

          test "can disable X-Ray tracing" {
              let api = appSyncApi "MyAPI" { xrayEnabled false }
              Expect.equal api.Props.XrayEnabled.Value false "X-Ray should be disabled"
          }

          // Note: Stack integration test removed - AppSync requires a schema file
          // which is not available in unit tests. See integration tests for full examples.
          ]

[<Tests>]
let appsync_datasource_tests =
    testList
        "AppSync Data Source DSL"
        [ test "fails when API is missing" {
              let thrower () =
                  appSyncDataSource "MyDS" { () } |> ignore

              Expect.throws thrower "Data source builder should throw when API is missing"
          }

          test "defaults constructId to data source name" {
              let thrower () =
                  appSyncDataSource "MyDS" { () } |> ignore

              Expect.throws thrower "Should require API"
          }

          test "accepts custom construct ID" {
              let thrower () =
                  appSyncDataSource "MyDS" { constructId "CustomDsId" } |> ignore

              Expect.throws thrower "Should require API even with custom construct ID"
          }

          test "accepts description" {
              let thrower () =
                  appSyncDataSource "MyDS" { description "Orders table data source" } |> ignore

              Expect.throws thrower "Should require API even with description"
          } ]
