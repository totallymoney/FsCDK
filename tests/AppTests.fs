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

              let application =
                  app {
                      context "environment" "production"
                      context "feature-flag" true
                      context "version" "1.2.3"
                  }

              // 2) A Dev stack you can actually work with
              stack "Dev" application {
                  stackProps {
                      devEnv
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

              stack "Prod" application {
                  stackProps {
                      prodEnv
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

              Expect.equal application.Account null "App account should be null"

              let cloudAssembly = application.Synth()

              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
              Expect.equal cloudAssembly.Stacks[0].DisplayName "Dev" "First spec should be Dev"
              Expect.equal cloudAssembly.Stacks[1].DisplayName "Prod (users-prod)" "Second spec should be Prod"

              Expect.equal
                  (application.Node.TryGetContext("environment"))
                  "production"
                  "App context 'environment' should be 'production'"

              Expect.equal
                  (application.Node.TryGetContext("feature-flag"))
                  true
                  "App context 'feature-flag' should be true"

              Expect.equal (application.Node.TryGetContext("version")) "1.2.3" "App context 'version' should be '1.2.3'"
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
              let application = app { stackTraces true }

              stack "Dev" application {
                  devEnv
                  stackProps { devEnv }

                  table "users" { partitionKey "id" AttributeType.STRING }
              }

              stack "Prod" application {
                  stackProps { prodEnv }

                  table "users" { partitionKey "id" AttributeType.STRING }
              }

              let cloudAssembly = application.Synth()
              Expect.equal cloudAssembly.Stacks.Length 2 "App should have exactly two stacks"
          } ]
    |> testSequenced
