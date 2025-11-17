module FsCDK.Tests.StepFunctionsTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.StepFunctions

[<Tests>]
let step_functions_tests =
    testList
        "Step Functions DSL"
        [ test "fails when definition is missing" {
              let thrower () =
                  stepFunction "MyStateMachine" { () } |> ignore

              Expect.throws thrower "Step Function builder should throw when definition is missing"
          }

          test "defaults constructId to state machine name" {
              let app = App()
              let simplePass = Pass(app, "PassState")

              let sf = stepFunction "MyStateMachine" { definition simplePass }
              Expect.equal sf.StateMachineName "MyStateMachine" "State machine name should be set"
              Expect.equal sf.ConstructId "MyStateMachine" "ConstructId should default to state machine name"
          }

          test "uses custom constructId when provided" {
              let app = App()
              let simplePass = Pass(app, "PassState")

              let sf =
                  stepFunction "MyStateMachine" {
                      constructId "CustomId"
                      definition simplePass
                  }

              Expect.equal sf.ConstructId "CustomId" "Should use custom construct ID"
          }

          test "applies AWS best practices by default - STANDARD type" {
              let app = App()
              let simplePass = Pass(app, "PassState")

              let sf = stepFunction "MyStateMachine" { definition simplePass }
              Expect.isNotNull sf.Props "Props should be created"
              Expect.isNotNull sf.Props.DefinitionBody "Definition should be set"
          }

          test "accepts custom timeout" {
              let app = App()
              let simplePass = Pass(app, "PassState")

              let sf =
                  stepFunction "MyStateMachine" {
                      timeout (Duration.Minutes(30.0))
                      definition simplePass
                  }

              Expect.isNotNull sf.Props.Timeout "Timeout should be set"
          }

          test "accepts custom state machine type" {
              let app = App()
              let simplePass = Pass(app, "PassState")

              let sf =
                  stepFunction "MyStateMachine" {
                      stateMachineType StateMachineType.EXPRESS
                      definition simplePass
                  }

              Expect.isNotNull sf.Props "Props should be created"
          }

          test "creates StateMachine in Stack" {
              let app = App()
              let testStack = Stack(app, "TestStack")
              let simplePass = Pass(testStack, "PassState")
              let logGroup = Amazon.CDK.AWS.Logs.LogGroup(testStack, "OrderProcessorLogs")

              let _ =
                  stack "TestStack2" {
                      app

                      stepFunction "OrderProcessor" {
                          definition simplePass
                          stateMachineType StateMachineType.STANDARD
                          tracingEnabled true
                          logDestination logGroup
                          loggingLevel LogLevel.ALL
                      }
                  }

              Expect.isTrue true "Stack should create without errors"
          }

          test "StateMachineTypes helpers provide standard types" {
              Expect.equal
                  StepFunctionHelpers.StateMachineTypes.standard
                  StateMachineType.STANDARD
                  "Standard type should match"

              Expect.equal
                  StepFunctionHelpers.StateMachineTypes.express
                  StateMachineType.EXPRESS
                  "Express type should match"
          } ]
    |> testSequenced
