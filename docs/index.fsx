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
    account "123456789012"
    region "us-east-1"
  }

// 2) A Dev stack you can actually work with
stack "Dev" {
  stackProps {
    devEnv
    description "Developer stack for feature work"
    tags [ "service", "users"; "env", "dev" ]
  }

  // resources
  table "users" {
    partitionKey "id" AttributeType.STRING
    billingMode BillingMode.PAY_PER_REQUEST
    removalPolicy RemovalPolicy.DESTROY
  }

  lambda "users-api-dev" {
    handler "Users::Handler::FunctionHandler"
    runtime Runtime.DOTNET_8
    code "./examples/lambdas/users"
    memory 512
    timeout 10.0
    description "CRUD over the users table"
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

  grant {
    table "users"
    lambda "users-api-dev"
    readWriteAccess
  }
}

stack "Prod" {
  stackProps {
    prodEnv
    stackName "users-prod"
    terminationProtection true
    tags [ "service", "users"; "env", "prod" ]
  }

  table "users" {
    partitionKey "id" AttributeType.STRING
    billingMode BillingMode.PAY_PER_REQUEST
    removalPolicy RemovalPolicy.RETAIN
    pointInTimeRecovery true
  }

  lambda "users-api" {
    handler "Users::Handler::FunctionHandler"
    runtime Runtime.DOTNET_8
    code "./examples/lambdas/users"
    memory 1024
    timeout 15.0
    description "CRUD over the users table"
  }

  grant {
    table "users"
    lambda "users-api"
    readWriteAccess
  }
}
