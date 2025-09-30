module FsCDK.Tests.AppTests

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
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

              let dev = stack "Dev" { props (stackProps { env devEnv }) }

              let prodEnv =
                  environment {
                      account "098765432109"
                      region "us-east-1"
                  }

              let prod = stack "Prod" { props (stackProps { env prodEnv }) }

              let specs = [ dev; prod ]

              Expect.equal specs.Length 2 "Should create exactly two stack specs"
              Expect.equal specs[0].Name "Dev" "First spec should be Dev"
              Expect.equal specs[1].Name "Prod" "Second spec should be Prod"
          }

          test "app with no stacks" {
              // 1) Environments
              let devEnv =
                  environment {
                      account "123456789012"
                      region "us-east-1"
                  }

              let prodEnv =
                  environment {
                      account "098765432109"
                      region "us-east-1"
                  }

              // 2) A Dev stack you can actually work with
              let devStack =
                  stack "Dev" {
                      stackProps {
                          env devEnv
                          description "Developer stack for feature work"
                          tags [ "service", "users"; "env", "dev" ]
                      }

                      table "users" {
                          partitionKey "id" AttributeType.STRING
                          billingMode BillingMode.PAY_PER_REQUEST
                          removalPolicy RemovalPolicy.DESTROY // fine for dev
                      }

                      queue "users-dlq" {
                          messageRetention (7.0 * 24.0 * 3600.0) // 7 days
                      }

                      queue "users-queue" {
                          deadLetterQueue "users-dlq" 5
                          visibilityTimeout 30.0
                      }

                      topic "user-events" { displayName "User events" }

                      subscription {
                          topic "user-events"
                          queue "users-queue"
                      }
                  }

              // 3) A production-leaning stack
              let prodStack =
                  stack "Prod" {
                      stackProps {
                          env prodEnv
                          stackName "users-prod"
                          terminationProtection true
                          tags [ "service", "users"; "env", "prod" ]
                      }

                      table "users" {
                          partitionKey "id" AttributeType.STRING
                          billingMode BillingMode.PAY_PER_REQUEST
                          removalPolicy RemovalPolicy.RETAIN // keep data safe
                          pointInTimeRecovery true
                      }
                  }

              // 4) Finally, build the app
              let app = app { stacks [ devStack; prodStack ] }

              Expect.equal app.Account null "App account should be null"

              // 5) Synthesize and validate
              let cloudAssembly = app.Synth()

              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
              Expect.equal cloudAssembly.Stacks[0].DisplayName "Dev" "First spec should be Dev"
              Expect.equal cloudAssembly.Stacks[1].DisplayName "Prod (users-prod)" "Second spec should be Prod"
          }

          test "app with implicit yields" {
              let devEnv =
                  environment {
                      account "123456789012"
                      region "us-east-1"
                  }

              let prodEnv =
                  environment {
                      account "098765432109"
                      region "us-east-1"
                  }

              // Build app with implicit yields (no stacks wrapper)
              let app =
                  app {
                      stack "Dev" {
                          stackProps { env devEnv }

                          Table(table "users" { partitionKey "id" AttributeType.STRING })
                      }

                      stack "Prod" {
                          stackProps { env prodEnv }

                          table "users" { partitionKey "id" AttributeType.STRING }
                      }
                  }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
          } ]
