module FsCDK.Tests.AppTests

open Expecto
open FsCDK

[<Tests>]
let appTests =
    testList
        "FsCDK DSL"
        [ test "app with Dev and Prod stacks" {
              let devEnv =
                  environment {
                      account "123456789012"
                      region "us-east-1"
                  }

              let dev =
                  stack {
                      name "Dev"
                      props (stackProps { env devEnv })
                  }

              let prodEnv =
                  environment {
                      account "098765432109"
                      region "us-east-1"
                  }

              let prod =
                  stack {
                      name "Prod"
                      props (stackProps { env prodEnv })
                  }

              let specs = [ dev; prod ]

              Expect.equal specs.Length 2 "Should create exactly two stack specs"
              Expect.equal specs[0].Name "Dev" "First spec should be Dev"
              Expect.equal specs[1].Name "Prod" "Second spec should be Prod"
          } ]
