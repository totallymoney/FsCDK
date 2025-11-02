(**
---
title: Step Functions (State Machines)
category: docs
index: 22
---

# AWS Step Functions

AWS Step Functions is a serverless orchestration service that lets you combine AWS Lambda functions
and other AWS services to build business-critical applications. Step Functions provides a graphical
console to arrange and visualize the components of your application as a series of steps.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.StepFunctions
open Amazon.CDK.AWS.StepFunctions.Tasks
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs

(**
## Basic State Machine

Create a simple state machine that orchestrates Lambda functions.
*)

stack "BasicStateMachine" {
    // Create Lambda functions
    let processFunc =
        lambda "ProcessData" {
            runtime Runtime.DOTNET_8
            handler "App::Process"
            code "./lambda"
            ()
        }

    let validateFunc =
        lambda "ValidateData" {
            runtime Runtime.DOTNET_8
            handler "App::Validate"
            code "./lambda"
        }

    // Create log group for state machine
    let logGroup =
        logGroup "/aws/vendedlogs/states/MyStateMachine" { retention RetentionDays.ONE_MONTH }

    // Create state machine
    // Note: State definitions must be created using CDK Tasks
    // Example:
    //   let validateTask = LambdaInvoke(scope, "ValidateTask", ...)
    //   let processTask = LambdaInvoke(scope, "ProcessTask", ...)
    //   let definition = Chain.Start(validateTask).Next(processTask)

    let stateMachine =
        stepFunction "DataPipeline" {
            comment "Validates and processes data"
            // definition definition
            logDestination logGroup.LogGroup.Value
            timeout (Duration.Hours(2.0))
        }

    ()
}

(**
## Standard vs Express State Machines

Step Functions offers two types of state machines:
*)

stack "StateMachineTypes" {
    let logGroup =
        logGroup "Logs" {
            retention RetentionDays.ONE_WEEK
            ()
        }

    // Standard: Long-running, exactly-once execution
    let standardSM =
        stepFunction "StandardWorkflow" {
            stateMachineType StateMachineType.STANDARD
            comment "Long-running workflow with exactly-once semantics"
            logDestination logGroup.LogGroup.Value
            timeout (Duration.Days(1.0))
        }

    // Express: Short-lived, at-least-once execution, cheaper
    let expressSM =
        stepFunction "ExpressWorkflow" {
            stateMachineType StateMachineType.EXPRESS
            comment "High-volume, short-duration workflow"
            logDestination logGroup.LogGroup.Value
            timeout (Duration.Minutes(5.0))
        }

    ()
}

(**
## Error Handling and Retry Logic

Implement robust error handling with retries and fallbacks.

Note: Error handling and retry logic must be configured on individual tasks using CDK directly.
*)

(**
## Parallel Execution

Execute multiple tasks concurrently.

Note: Parallel states must be created using CDK Parallel construct.
*)

(**
## Choice States

Implement conditional branching in workflows.

Note: Choice states must be created using CDK Choice construct.
*)

(**
## Map States

Process arrays of items in parallel.

Note: Map states must be created using CDK Map construct.
*)

(**
## Wait States

Add delays between workflow steps.

Note: Wait states must be created using CDK Wait construct.
*)

(**
## Integration with AWS Services

Step Functions integrates with many AWS services beyond Lambda:
*)

(**
### DynamoDB Integration

Read/write to DynamoDB tables directly from state machines.
*)

(**
### SQS Integration

Send messages to SQS queues.
*)

(**
### SNS Integration

Publish messages to SNS topics.
*)

(**
### ECS/Fargate Integration

Run containerized tasks.
*)

(**
## Human Approval Workflow

Implement workflows that require human approval.
*)

(**
## Saga Pattern for Distributed Transactions

Implement compensating transactions for microservices.
*)

(**
## Monitoring and Observability

Step Functions provides comprehensive monitoring capabilities.
*)

stack "MonitoredStateMachine" {
    let logGroup =
        logGroup "DetailedLogs" {
            retention RetentionDays.THREE_MONTHS
            ()
        }

    let sm =
        stepFunction "MonitoredWorkflow" {
            comment "Workflow with full logging and tracing"
            // Full logging (ALL events)
            loggingLevel LogLevel.ALL
            logDestination logGroup.LogGroup.Value
            // X-Ray tracing enabled by default
            tracingEnabled true
        }

    ()
}

(**
## Best Practices

### Performance

- Use Express workflows for high-volume, short-duration tasks
- Use Standard workflows for long-running processes
- Execute independent tasks in parallel using Parallel states
- Use Map state for processing arrays
- Consider Lambda performance optimizations (memory, cold starts)
- Minimize data passing between states (use S3 for large payloads)

### Security

- Use IAM roles with least-privilege permissions
- Enable X-Ray tracing for distributed system visibility
- Encrypt state machine data at rest
- Use VPC endpoints for private AWS service access
- Audit state machine executions in CloudTrail
- Implement input validation in Lambda functions

### Cost Optimization

- Use Express workflows for cost-sensitive, high-volume workloads
  - 100x cheaper than Standard for high-volume scenarios
  - Charged per execution and duration (not per state transition)
- Use Standard workflows only when you need:
  - Exactly-once execution semantics
  - Long-running workflows (> 5 minutes)
  - Visual workflow history
- Optimize Lambda function execution time and memory
- Use Step Functions instead of polling loops
- Set appropriate timeouts to prevent runaway executions

### Reliability

- Implement retry logic with exponential backoff
- Use error handling (Catch) for graceful degradation
- Set realistic timeouts for all tasks
- Use DLQ for failed executions
- Test failure scenarios thoroughly
- Implement circuit breaker patterns for external dependencies
- Use CloudWatch alarms for execution failures

### Operational Excellence

- Use descriptive state machine and task names
- Add comments to explain workflow logic
- Enable full logging (LogLevel.ALL) for debugging
- Tag state machines with project, environment, team
- Version your state machine definitions
- Monitor execution metrics (duration, success rate)
- Set up alerts for failed executions
- Document workflow diagrams and business logic

## State Machine Types Comparison

| Feature | Standard | Express |
|---------|----------|---------|
| **Max Duration** | 1 year | 5 minutes |
| **Execution Rate** | 2,000/second | 100,000/second |
| **Pricing** | Per state transition | Per execution + duration |
| **Execution Semantics** | Exactly-once | At-least-once |
| **Execution History** | Full history (90 days) | CloudWatch Logs only |
| **Best For** | Long-running, critical | High-volume, short tasks |

## Default Settings

The Step Functions builder applies these production-safe defaults:

- **State Machine Type**: STANDARD (reliable, exactly-once)
- **Logging Level**: ALL (full execution logging)
- **Tracing**: Enabled (X-Ray integration)
- **Timeout**: 1 hour (configurable)

Note: Logging requires a CloudWatch Log Group destination.

## Logging Levels

- **OFF**: No logging (not recommended)
- **ERROR**: Log errors only
- **ALL**: Log all events (recommended for debugging)

## Helper Functions

FsCDK provides helper functions for common Step Functions patterns:
*)

open StepFunctionHelpers

// State machine types
let standardType = StateMachineTypes.standard
let expressType = StateMachineTypes.express

// Common timeouts
let fiveMin = Timeouts.fiveMinutes
let thirtyMin = Timeouts.thirtyMinutes
let oneHour = Timeouts.oneHour
let oneDay = Timeouts.oneDay

// Logging levels
let allLogs = LoggingLevels.all
let errorLogs = LoggingLevels.error
let noLogs = LoggingLevels.off

(**
## Escape Hatch

For advanced scenarios, access the underlying CDK StateMachine:

`fsharp
let smResource = stepFunction "MyWorkflow" {
    comment "My workflow"
    logDestination myLogGroup`
}

// Access the CDK StateMachine for advanced configuration
let cdkSM = smResource.StateMachine.Value

// Grant execution permissions
cdkSM.GrantStartExecution(myRole)
cdkSM.GrantStartSyncExecution(myRole)

// Get state machine ARN
let arn = cdkSM.StateMachineArn
`

## Use Cases

### Order Processing
- Validate order
- Check inventory
- Process payment
- Ship order
- Send confirmation
- Handle failures with compensating transactions

### ETL Pipelines
- Extract data from sources
- Transform data in parallel
- Load to data warehouse
- Validate results
- Generate reports

### Machine Learning Workflows
- Prepare training data
- Train model
- Evaluate model
- Deploy if accuracy threshold met
- Monitor model performance

### Human Approval Workflows
- Submit request
- Wait for approval (callback pattern)
- Process approved requests
- Notify outcome

### Microservice Orchestration
- Coordinate multiple microservices
- Handle partial failures
- Implement saga pattern
- Ensure data consistency

## Resources

- [AWS Step Functions Documentation](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html)
- [Step Functions Best Practices](https://docs.aws.amazon.com/step-functions/latest/dg/best-practices.html)
- [Amazon States Language](https://states-language.net/spec.html)
- [Step Functions Integrations](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-service-integrations.html)
- [Error Handling](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html)
*)
