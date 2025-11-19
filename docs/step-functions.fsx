(**
---
title: Step Functions (State Machines)
category: 3. Resources
categoryindex: 25
---

# ![AWS Step Functions](img/icons/Arch_AWS-Step-Functions_48.png) AWS Step Functions

AWS Step Functions is a serverless orchestration service that lets you combine AWS Lambda functions
and other AWS services into business-critical workflows.

## Order Processing Workflow Example

<pre class="mermaid">
stateDiagram-v2
    [*] --> ValidateOrder
    
    ValidateOrder --> CheckInventory: Valid
    ValidateOrder --> SendFailureNotification: Invalid
    
    CheckInventory --> ReserveInventory: In Stock
    CheckInventory --> SendOutOfStockNotification: Out of Stock
    
    ReserveInventory --> ProcessPayment
    
    ProcessPayment --> ShipOrder: Success
    ProcessPayment --> ReleaseInventory: Failed
    
    ShipOrder --> SendConfirmationEmail
    SendConfirmationEmail --> [*]
    
    ReleaseInventory --> SendPaymentFailureNotification
    SendPaymentFailureNotification --> [*]
    
    SendFailureNotification --> [*]
    SendOutOfStockNotification --> [*]
    
    note right of ValidateOrder
        Lambda: Validate order data
        Check customer, items, pricing
    end note
    
    note right of ProcessPayment
        Lambda: Charge customer
        Uses payment gateway API
        Includes retry logic
    end note
    
    note right of ShipOrder
        Lambda: Create shipment
        Integrates with logistics API
    end note
</div>

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
            logDestination logGroup
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
            logDestination logGroup
            timeout (Duration.Days(1.0))
        }

    // Express: Short-lived, at-least-once execution, cheaper
    let expressSM =
        stepFunction "ExpressWorkflow" {
            stateMachineType StateMachineType.EXPRESS
            comment "High-volume, short-duration workflow"
            logDestination logGroup
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
            logDestination logGroup
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
cdkSM.GrantStartExecution myRole
cdkSM.GrantStartSyncExecution myRole

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

## 📚 Learning Resources for AWS Step Functions

### AWS Official Documentation

**Getting Started:**
- [AWS Step Functions Developer Guide](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html) - Complete documentation
- [Step Functions Tutorials](https://docs.aws.amazon.com/step-functions/latest/dg/tutorials.html) - Hands-on learning
- [Amazon States Language (ASL)](https://states-language.net/spec.html) - JSON-based workflow language
- [Step Functions Workflow Studio](https://aws.amazon.com/blogs/aws/new-aws-step-functions-workflow-studio-a-low-code-visual-tool-for-building-state-machines/) - Visual workflow builder

**Core Concepts:**
- [State Types](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-states.html) - Task, Choice, Parallel, Map, Wait, etc.
- [Service Integrations](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-service-integrations.html) - 220+ AWS service integrations
- [Error Handling](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html) - Retry and Catch patterns
- [Standard vs Express Workflows](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-standard-vs-express.html) - When to use each

**Best Practices:**
- [Step Functions Best Practices](https://docs.aws.amazon.com/step-functions/latest/dg/best-practices.html) - Official recommendations
- [Design Patterns](https://docs.aws.amazon.com/step-functions/latest/dg/design-patterns.html) - Common workflow patterns
- [Cost Optimization](https://aws.amazon.com/step-functions/pricing/) - Pricing guide and cost strategies

### Serverless Orchestration Patterns

**Yan Cui (The Burning Monk) - Orchestration Expert:**
- [The Burning Monk Blog](https://theburningmonk.com/) - Yan Cui's Step Functions and orchestration insights, including comprehensive serverless best practices and error handling
- [Saga Pattern with Step Functions](https://theburningmonk.com/2017/07/applying-the-saga-pattern-with-aws-lambda-and-step-functions/) - Distributed transactions
- [Step Functions vs EventBridge](https://theburningmonk.com/2020/08/choreography-vs-orchestration-in-the-land-of-serverless/) - Choreography vs Orchestration

**AWS Compute Blog - Essential Reading:**
- [Event-Driven Orchestration](https://aws.amazon.com/blogs/compute/building-event-driven-architectures-with-step-functions/) - Modern patterns
- [Callback Pattern](https://aws.amazon.com/blogs/compute/using-the-aws-step-functions-callback-pattern/) - Human approval workflows
- [Map State Deep Dive](https://aws.amazon.com/blogs/compute/handling-batch-operations-with-aws-step-functions/) - Process arrays efficiently
- [Wait for Callback with Task Token](https://docs.aws.amazon.com/step-functions/latest/dg/connect-to-resource.html#connect-wait-token) - Integrate with external systems

### Advanced Patterns & Architectures

**Saga Pattern (Distributed Transactions):**
- [Saga Pattern Implementation](https://aws.amazon.com/blogs/compute/implementing-the-saga-pattern-with-aws-step-functions-and-amazon-dynamodb/) - Official AWS guide
- [Compensating Transactions](https://docs.aws.amazon.com/prescriptive-guidance/latest/patterns/implement-the-serverless-saga-pattern-by-using-aws-step-functions.html) - Rollback failed operations
- [Event Sourcing with Step Functions](https://aws.amazon.com/blogs/compute/using-aws-step-functions-and-amazon-eventbridge-to-orchestrate-event-driven-applications/) - Building audit trails

**Parallel & Map State Patterns:**
- [Distributed Map State](https://aws.amazon.com/blogs/aws/step-functions-distributed-map-a-serverless-solution-for-large-scale-parallel-data-processing/) - Process millions of items
- [Dynamic Parallelism](https://docs.aws.amazon.com/step-functions/latest/dg/sample-project-dynamodb-streams.html) - Fan-out patterns
- [Batch Processing](https://aws.amazon.com/blogs/compute/handling-batch-operations-with-aws-step-functions/) - Large-scale data processing

**Choice & Branching:**
- [Choice State Examples](https://docs.aws.amazon.com/step-functions/latest/dg/amazon-states-language-choice-state.html) - Conditional logic
- [InputPath, OutputPath, ResultPath](https://docs.aws.amazon.com/step-functions/latest/dg/input-output-example.html) - Data flow management
- [JSONPath in Step Functions](https://docs.aws.amazon.com/step-functions/latest/dg/amazon-states-language-paths.html) - Extract and transform data

**Wait & Timer Patterns:**
- [Wait State](https://docs.aws.amazon.com/step-functions/latest/dg/amazon-states-language-wait-state.html) - Fixed or dynamic waits
- [Schedule-Based Workflows](https://aws.amazon.com/blogs/compute/scheduling-aws-lambda-functions-with-step-functions/) - Cron-like execution
- [Polling Patterns](https://docs.aws.amazon.com/step-functions/latest/dg/sample-project-job-poller.html) - Wait for external job completion

### Step Functions Service Integrations

**Direct SDK Integrations (220+ Services):**
- [Lambda Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-lambda.html) - Invoke functions sync or async
- [DynamoDB Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-ddb.html) - Read/write tables directly
- [ECS/Fargate Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-ecs.html) - Run containerized tasks
- [SNS/SQS Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-sns.html) - Message pub/sub
- [Glue Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-glue.html) - ETL workflows
- [Athena Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-athena.html) - Query S3 data
- [SageMaker Integration](https://docs.aws.amazon.com/step-functions/latest/dg/connect-sagemaker.html) - ML training/inference

**Optimized Integrations:**
- [Lambda Optimized](https://docs.aws.amazon.com/step-functions/latest/dg/connect-lambda.html) - Automatic payload handling
- [Service Integrations Deep Dive](https://aws.amazon.com/blogs/compute/introducing-aws-step-functions-synchronous-express-workflows/) - Sync vs async patterns

### Error Handling & Resilience

**Retry Strategies:**
- [Retry Configuration](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html#error-handling-retrying-after-an-error) - ErrorEquals, IntervalSeconds, MaxAttempts, BackoffRate
- [Exponential Backoff](https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/) - Prevent thundering herd
- [Error Handling Best Practices](https://docs.aws.amazon.com/step-functions/latest/dg/best-practices.html#bp-error-handling) - Official AWS guidance on retries and error handling

**Catch & Fallback:**
- [Catch Errors](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html#error-handling-catching-errors) - Handle specific errors
- [Fallback Chains](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html#error-handling-fallback-chains) - Multiple catch handlers
- [States.ALL](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html) - Catch-all error handler

**Circuit Breaker Pattern:**
- [Circuit Breaker with Step Functions](https://aws.amazon.com/blogs/compute/building-a-circuit-breaker-for-aws-step-functions/) - Prevent cascading failures
- [Health Checks](https://docs.aws.amazon.com/step-functions/latest/dg/sample-project-health-check.html) - Monitor external dependencies

### Standard vs Express Workflows

**When to Use Standard:**
- Long-running workflows (up to 1 year)
- Exactly-once execution semantics required
- Need full execution history and visual debugging
- Audit trail is critical
- Slower execution rate (< 2,000/second)

**When to Use Express:**
- High-volume, short-duration workflows (< 5 minutes)
- Can tolerate at-least-once execution
- Cost is primary concern (100x cheaper for high volume)
- Need high throughput (100,000/second)
- Streaming data processing

**Cost Comparison Example:**
```
Standard: $0.025 per 1,000 state transitions
Express: $1.00 per 1 million executions + $0.0000167 per GB-second

For 100 million executions/month with 3 states each:
Standard: (100M * 3 * $0.025) / 1000 = $7,500/month
Express: (100M * $1.00) / 1M + compute = ~$100/month
```

### Monitoring & Observability

**CloudWatch Integration:**
- [Step Functions Metrics](https://docs.aws.amazon.com/step-functions/latest/dg/procedure-cw-metrics.html) - Execution metrics
- [CloudWatch Logs](https://docs.aws.amazon.com/step-functions/latest/dg/cw-logs.html) - Detailed execution logs
- [CloudWatch Alarms](https://docs.aws.amazon.com/step-functions/latest/dg/procedure-cw-alarms.html) - Alert on failures

**X-Ray Tracing:**
- [Enable X-Ray](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-xray-tracing.html) - End-to-end tracing
- [Service Map](https://docs.aws.amazon.com/xray/latest/devguide/xray-console.html#xray-console-servicemap) - Visualize workflow dependencies
- [Trace Analysis](https://docs.aws.amazon.com/xray/latest/devguide/xray-console-analytics.html) - Find bottlenecks

**EventBridge Integration:**
- [Execution Events](https://docs.aws.amazon.com/step-functions/latest/dg/cw-events.html) - React to workflow events
- [Failed Execution Alerts](https://aws.amazon.com/blogs/compute/using-amazon-eventbridge-to-capture-aws-step-functions-failures/) - Automated notifications

### Real-World Use Cases

**Order Processing:**
1. Validate order (Lambda)
2. Check inventory (DynamoDB)
3. Charge payment (External API with callback)
4. Update inventory (DynamoDB)
5. Ship order (SQS)
6. Send confirmation (SNS)
7. **On Error:** Refund payment, restore inventory

**ETL Pipeline:**
1. Trigger Glue job (extract)
2. Wait for completion
3. Parallel transform jobs (Map state)
4. Load to Redshift
5. Run validation queries (Athena)
6. Generate reports (Lambda)

**ML Training Workflow:**
1. Prepare data (Glue)
2. Train model (SageMaker)
3. Evaluate model (Lambda)
4. If accuracy > 95%: Deploy (SageMaker endpoint)
5. Else: Tune hyperparameters, retry
6. Send notification (SNS)

**Human Approval Workflow:**
1. Submit expense report (Lambda)
2. Wait for manager approval (callback with task token)
3. If approved: Process payment (Lambda)
4. If rejected: Notify employee (SNS)
5. Archive (S3)

### Video Tutorials

**Beginner:**
- [Step Functions Tutorial](https://www.youtube.com/watch?v=Dh7h3lkpeP4) - AWS official introduction
- [Building Workflows with Workflow Studio](https://www.youtube.com/watch?v=f0maCDqW41k) - Visual builder demo
- [Step Functions for Beginners](https://www.youtube.com/watch?v=8C96jgAj4Es) - Complete walkthrough

**Advanced:**
- [AWS re:Invent - Step Functions Deep Dive](https://www.youtube.com/results?search_query=aws+reinvent+step+functions) - Annual advanced sessions
- [Distributed Map State](https://www.youtube.com/watch?v=6-jfKDJLbVo) - Large-scale processing
- [Step Functions Best Practices](https://www.youtube.com/watch?v=o6-7BAUWaqg) - AWS Serverless Land

### Community Tools & Libraries

**Infrastructure as Code:**
- [CDK Patterns for Step Functions](https://github.com/cdk-patterns/serverless) - Reusable patterns
- [Serverless Framework Plugin](https://www.serverless.com/framework/docs/providers/aws/guide/workflow) - Define workflows in YAML
- [SAM Support](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-statemachine.html) - Step Functions in SAM

**Testing & Development:**
- [Step Functions Local](https://docs.aws.amazon.com/step-functions/latest/dg/sfn-local.html) - Test workflows locally
- [LocalStack](https://localstack.cloud/) - Emulate Step Functions
- [ASL Validator](https://github.com/ChristopheBougere/asl-validator) - Validate state machine definitions

**Visualization:**
- [Step Functions Graph](https://github.com/aws/aws-toolkit-vscode) - VS Code extension
- [Render ASL as SVG](https://github.com/kddejong/cfn-diagram) - Generate diagrams from code

### Workshops & Hands-On Labs

**Official AWS Workshops:**
- [Step Functions Workshop](https://catalog.workshops.aws/stepfunctions/en-US) - Comprehensive hands-on tutorial
- [Serverless Patterns](https://serverlessland.com/patterns?services=step-functions) - Step Functions patterns collection
- [Build a Saga Pattern](https://catalog.workshops.aws/stepfunctions/en-US/module-5) - Distributed transaction workshop

**Community Resources:**
- [Serverless Land](https://serverlessland.com/) - Step Functions examples and patterns
- [AWS Samples GitHub](https://github.com/aws-samples?q=step-functions) - Official code samples

### Recommended Learning Path

**Week 1 - Fundamentals:**
1. Read [Step Functions Developer Guide](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html) - First 5 chapters
2. Watch [Step Functions Tutorial Video](https://www.youtube.com/watch?v=Dh7h3lkpeP4)
3. Build your first workflow with FsCDK (examples above)
4. Explore [Workflow Studio](https://aws.amazon.com/blogs/aws/new-aws-step-functions-workflow-studio-a-low-code-visual-tool-for-building-state-machines/)

**Week 2 - Patterns & Best Practices:**
1. Study [Step Functions Design Patterns](https://docs.aws.amazon.com/step-functions/latest/dg/design-patterns.html)
2. Read [Step Functions Best Practices](https://docs.aws.amazon.com/step-functions/latest/dg/best-practices.html)
3. Implement error handling with Retry and Catch
4. Learn [Service Integrations](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-service-integrations.html)

**Week 3 - Advanced:**
1. Implement [Saga Pattern](https://aws.amazon.com/blogs/compute/implementing-the-saga-pattern-with-aws-step-functions-and-amazon-dynamodb/)
2. Use [Map State for parallel processing](https://aws.amazon.com/blogs/compute/handling-batch-operations-with-aws-step-functions/)
3. Add [X-Ray tracing](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-xray-tracing.html)
4. Take [Step Functions Workshop](https://catalog.workshops.aws/stepfunctions/en-US)

**Ongoing - Mastery:**
- Build complex orchestration patterns
- Optimize costs (Standard vs Express)
- Implement circuit breakers and resilience patterns
- Follow [AWS Compute Blog](https://aws.amazon.com/blogs/compute/) for new features

### AWS Experts to Follow

![AWS Heroes](img/awsheros.png)
*AWS Heroes and community experts who share serverless workflow patterns*

**AWS Heroes & Advocates:**
- **Yan Cui** - Serverless orchestration expert
  - [Twitter/X: @theburningmonk](https://twitter.com/theburningmonk)
- **Ben Kehoe** - Serverless workflow patterns
  - [Twitter/X: @ben11kehoe](https://twitter.com/ben11kehoe)
  - [Mastodon: @ben11kehoe@mastodon.social](https://mastodon.social/@ben11kehoe)
- **Jeremy Daly** - Serverless advocate
  - [Twitter/X: @jeremy_daly](https://twitter.com/jeremy_daly)
- **Danilo Poccia** - AWS Principal Developer Advocate
  - [Twitter/X: @danilop](https://twitter.com/danilop)
  - [Mastodon: @danilop@mastodon.social](https://mastodon.social/@danilop)

**AWS Step Functions Team:**
- Follow [AWS Compute Blog](https://aws.amazon.com/blogs/compute/category/compute/aws-step-functions/) for official updates

### Common Pitfalls & Solutions

**❌ DON'T:**
1. **Use Step Functions for high-frequency loops** → Use Lambda or Fargate
2. **Pass large payloads between states** → Use S3 for data, pass S3 keys
3. **Ignore error handling** → Always add Retry and Catch
4. **Use Standard for high-volume, short tasks** → Use Express workflows
5. **Forget timeouts** → Set realistic TimeoutSeconds for each state

**✅ DO:**
1. **Design for idempotency** → Same input = same output
2. **Use parallel states** → Execute independent tasks concurrently
3. **Implement compensating transactions** → Saga pattern for rollbacks
4. **Monitor execution metrics** → Set up CloudWatch alarms
5. **Use service integrations** → Avoid Lambda for simple AWS API calls

### FsCDK Step Functions Features

- Type-safe state machine definitions
- Production-safe defaults (STANDARD type, ALL logging, X-Ray enabled)
- Helper functions for common patterns
- Seamless integration with Lambda, DynamoDB, and other services

For implementation details, see [src/StepFunctions.fs](../src/StepFunctions.fs) in the FsCDK repository.
*)
