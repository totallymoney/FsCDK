module FsCdk.Tests.AppTests

open Expecto
open FsCDK

[<Tests>]
let appTests =
    testList
        "FsCDK DSL"
        [ test "app synth returns a cloud assembly" {
              let app = app { stacks [ stack { name "TestStack" } ] }

              Expect.equal app.Stacks[0].StackName "TestStack" "Stack name should match"
              Expect.equal app.Version "48.0.0" "CloudAssembly version should match"
          } ]
