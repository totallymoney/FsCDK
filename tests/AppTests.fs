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
                  // Names double as construct IDs unless you override them
                  let usersTable =
                      table {
                          name "users"
                          partitionKey "id" AttributeType.STRING
                          billingMode BillingMode.PAY_PER_REQUEST
                          removalPolicy RemovalPolicy.DESTROY // fine for dev
                      }


                  let dlq =
                      queue {
                          name "users-dlq"
                          messageRetention (7.0 * 24.0 * 3600.0) // 7 days
                      }

                  let mainQueue =
                      queue {
                          name "users-queue"
                          deadLetterQueue "users-dlq" 5
                          visibilityTimeout 30.0
                      }

                  let events =
                      topic {
                          name "user-events"
                          displayName "User events"
                      }

                  stack {
                      name "Dev"

                      props (
                          stackProps {
                              env devEnv
                              description "Developer stack for feature work"
                              tags [ "service", "users"; "env", "dev" ]
                          }
                      )

                      // resources
                      addTable usersTable
                      addQueue dlq
                      addQueue mainQueue
                      addTopic events

                      // wiring
                      subscribe (
                          subscription {
                              topic "user-events"
                              queue "users-queue"
                          }
                      )
                  }

              // 3) A production-leaning stack
              let prodStack =
                  let usersTable =
                      table {
                          name "users"
                          partitionKey "id" AttributeType.STRING
                          billingMode BillingMode.PAY_PER_REQUEST
                          removalPolicy RemovalPolicy.RETAIN // keep data safe
                          pointInTimeRecovery true
                      }


                  stack {
                      name "Prod"

                      props (
                          stackProps {
                              env prodEnv
                              stackName "users-prod"
                              terminationProtection true
                              tags [ "service", "users"; "env", "prod" ]
                          }
                      )

                      addTable usersTable
                  }

              let specs = [ devStack; prodStack ]


              Expect.equal specs.Length 2 "Should create exactly two stack specs"
              Expect.equal specs[0].Name "Dev" "First spec should be Dev"
              Expect.equal specs[1].Name "Prod" "Second spec should be Prod"
          } ]
