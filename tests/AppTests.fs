module FsCDK.Tests.AppTests

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Expecto
open FsCDK


[<Tests>]
let appTests =
    testList
        "FsCDK App Tests"
        [ test "App with no stacks" {
              let application = App()
              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 0 "App should have no stacks"
          }

          test "App with stacks" {
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

              let app =
                  app { context [ "environment", "production"; "feature-flag", true; "version", "1.2.3" ] }

              // 2) A Dev stack you can actually work with
              stack "Dev" {
                  scope app

                  env devEnv

                  description "Developer stack for feature work"
                  tags [ "service", "users"; "env", "dev" ]

                  table "users" {
                      partitionKey "id" AttributeType.STRING
                      billingMode BillingMode.PAY_PER_REQUEST
                      removalPolicy RemovalPolicy.DESTROY // fine for dev
                  }

                  let! dlqQueue =
                      queue "users-dlq" {
                          retentionPeriod (7.0 * 24.0 * 3600.0) // 7 days
                      }

                  let dlq =
                      deadLetterQueue {
                          queue dlqQueue
                          maxReceiveCount 5
                      }

                  queue "users-queue" {
                      deadLetterQueue dlq
                      visibilityTimeout 30.0
                  }

                  topic "user-events" { displayName "User events" }
              }

              stack "Prod" {
                  scope app
                  env prodEnv
                  terminationProtection true
                  tags [ "service", "users"; "env", "prod" ]

                  table "users" {
                      partitionKey "id" AttributeType.STRING
                      billingMode BillingMode.PAY_PER_REQUEST
                      removalPolicy RemovalPolicy.RETAIN // keep data safe
                      pointInTimeRecovery true
                  }
              }

              Expect.equal app.Account null "App account should be null"

              let cloudAssembly = app.Synth()

              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
              Expect.equal cloudAssembly.Stacks[0].DisplayName "Dev" "First spec should be Dev"
              Expect.equal cloudAssembly.Stacks[1].DisplayName "Prod" "Second spec should be Prod"

              Expect.equal
                  (app.Node.TryGetContext("environment"))
                  "production"
                  "App context 'environment' should be 'production'"

              Expect.equal (app.Node.TryGetContext("feature-flag")) true "App context 'feature-flag' should be true"

              Expect.equal (app.Node.TryGetContext("version")) "1.2.3" "App context 'version' should be '1.2.3'"
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
              let app = app { stackTraces true }

              stack "Dev" {
                  scope app
                  env devEnv
                  table "users" { partitionKey "id" AttributeType.STRING }
              }

              stack "Prod" {
                  scope app
                  env prodEnv
                  table "users" { partitionKey "id" AttributeType.STRING }
              }

              let cloudAssembly = app.Synth()
              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
          } ]
    |> testSequenced
