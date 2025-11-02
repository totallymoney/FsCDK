module FsCDK.Tests.ProductionDefaultsTests

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Expecto
open FsCDK

[<Tests>]
let production_defaults_tests =
    testList
        "Production Defaults Tests"
        [ test "lambda applies Yan Cui production defaults" {
              // Need to create App first to initialize JSII runtime
              let app = App()
              let _ = Stack(app, "TestStack")
              
              let spec =
                  lambda "TestFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory()))
                  }

              // Test reserved concurrency default
              let rceObj = box spec.Props.ReservedConcurrentExecutions

              match rceObj with
              | :? int as i -> Expect.equal i 10 "Reserved concurrency should default to 10"
              | :? System.Nullable<double> as n when n.HasValue ->
                  Expect.equal (int n.Value) 10 "Reserved concurrency should default to 10"
              | :? System.Nullable<int> as n when n.HasValue ->
                  Expect.equal n.Value 10 "Reserved concurrency should default to 10"
              | _ -> failtestf $"Unexpected ReservedConcurrentExecutions type/value: %A{rceObj}"

              // Test tracing default
              let tracingObj = box spec.Props.Tracing

              match tracingObj with
              | :? System.Nullable<Tracing> as n when n.HasValue ->
                  Expect.equal n.Value Tracing.ACTIVE "Tracing should default to ACTIVE"
              | :? Tracing as t -> Expect.equal t Tracing.ACTIVE "Tracing should default to ACTIVE"
              | _ -> failtestf $"Unexpected Tracing type/value: %A{tracingObj}"

              // Test logging format default
              let fmtObj = box spec.Props.LoggingFormat

              match fmtObj with
              | :? System.Nullable<LoggingFormat> as n when n.HasValue ->
                  Expect.equal n.Value LoggingFormat.JSON "LoggingFormat should default to JSON"
              | :? LoggingFormat as f -> Expect.equal f LoggingFormat.JSON "LoggingFormat should default to JSON"
              | _ -> failtestf $"Unexpected LoggingFormat type/value: %A{fmtObj}"

              // Test retry attempts default
              let retryObj = box spec.Props.RetryAttempts

              match retryObj with
              | :? int as i -> Expect.equal i 2 "RetryAttempts should default to 2"
              | :? System.Nullable<double> as n when n.HasValue ->
                  Expect.equal (int n.Value) 2 "RetryAttempts should default to 2"
              | :? System.Nullable<int> as n when n.HasValue ->
                  Expect.equal n.Value 2 "RetryAttempts should default to 2"
              | _ -> failtestf $"Unexpected RetryAttempts type/value: %A{retryObj}"

              // Test MaxEventAge default (6 hours)
              Expect.isNotNull spec.Props.MaxEventAge "MaxEventAge should be set"
              Expect.equal
                  (spec.Props.MaxEventAge.ToHours())
                  6.0
                  "MaxEventAge should default to 6 hours"
          }

          test "lambda allows overriding production defaults" {
              // Need to create App first to initialize JSII runtime
              let app = App()
              let _ = Stack(app, "TestStack")
              
              let spec =
                  lambda "TestFn" {
                      handler "Program::Handler"
                      runtime Runtime.DOTNET_8
                      code (Code.FromAsset(System.IO.Directory.GetCurrentDirectory()))
                      reservedConcurrentExecutions 100
                      tracing Tracing.PASS_THROUGH
                      loggingFormat LoggingFormat.TEXT
                      retryAttempts 0
                      maxEventAge (Duration.Hours(1.0))
                      autoCreateDLQ false
                      autoAddPowertools false
                  }

              // Test overridden values
              let rceObj = box spec.Props.ReservedConcurrentExecutions

              match rceObj with
              | :? int as i -> Expect.equal i 100 "Reserved concurrency should be overridden"
              | :? System.Nullable<double> as n when n.HasValue ->
                  Expect.equal (int n.Value) 100 "Reserved concurrency should be overridden"
              | :? System.Nullable<int> as n when n.HasValue ->
                  Expect.equal n.Value 100 "Reserved concurrency should be overridden"
              | _ -> failtestf $"Unexpected ReservedConcurrentExecutions type/value: %A{rceObj}"

              let tracingObj = box spec.Props.Tracing

              match tracingObj with
              | :? System.Nullable<Tracing> as n when n.HasValue ->
                  Expect.equal n.Value Tracing.PASS_THROUGH "Tracing should be overridden"
              | :? Tracing as t -> Expect.equal t Tracing.PASS_THROUGH "Tracing should be overridden"
              | _ -> failtestf $"Unexpected Tracing type/value: %A{tracingObj}"

              let fmtObj = box spec.Props.LoggingFormat

              match fmtObj with
              | :? System.Nullable<LoggingFormat> as n when n.HasValue ->
                  Expect.equal n.Value LoggingFormat.TEXT "LoggingFormat should be overridden"
              | :? LoggingFormat as f -> Expect.equal f LoggingFormat.TEXT "LoggingFormat should be overridden"
              | _ -> failtestf $"Unexpected LoggingFormat type/value: %A{fmtObj}"

              Expect.equal (spec.Props.MaxEventAge.ToHours()) 1.0 "MaxEventAge should be overridden"
          } ]
    |> testSequenced
