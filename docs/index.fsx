(**
---
title: FsCDK
category: docs
index: 0
---

FsCDK lets you describe AWS infrastructure with a small, expressive F# DSL built on top of the AWS Cloud Development Kit (CDK). If you like computation expressions, immutability, and readable diffs, you’ll feel right at home.

This page gives you a quick, human-sized tour. No buzzwords, just a couple of realistic stacks you can read end-to-end.

What you’ll see below:
- Define per-environment settings once and reuse them.
- Declare DynamoDB tables, Lambdas, queues and topics with intent, not boilerplate.
- Wire resources together (grants and subscriptions) without hunting for ARNs.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda

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

  let usersApi =
    lambda {
      name "users-api-dev"
      handler "Users::Handler::FunctionHandler"
      runtime Runtime.DOTNET_8
      code "./examples/lambdas/users" // any folder with your code bundle
      memory 512
      timeout 10.0
      description "CRUD over the users table"
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
    addLambda usersApi
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

    addGrant (
      grant {
        table "users"
        lambda "users-api-dev"
        readWriteAccess
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

  let usersApi =
    lambda {
      name "users-api"
      handler "Users::Handler::FunctionHandler"
      runtime Runtime.DOTNET_8
      code "./examples/lambdas/users"
      memory 1024
      timeout 15.0
      description "CRUD over the users table"
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
    addLambda usersApi

    addGrant (
      grant {
        table "users"
        lambda "users-api"
        readWriteAccess
      }
    )
  }

// 4) Build an in-memory CDK app (no deploy here). We create stacks into an App
// and ignore the synthesized assembly; we just want to show what's inside.
app {
  // You can pass context values here if needed
  // context [ "key", "value" ]

  // Build both stacks into the same app
  stacks [ devStack; prodStack ]
}

(*** include-output ***)
