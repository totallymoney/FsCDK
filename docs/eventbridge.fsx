(**
---
title: EventBridge
category: docs
index: 11
---

# Amazon EventBridge

EventBridge is a serverless event bus service that makes it easy to connect applications using events.
Build event-driven architectures with decoupled microservices.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Events
open Amazon.CDK.AWS.Events.Targets
open Amazon.CDK.AWS.Lambda

(**
## Scheduled Events

Run Lambda functions on a schedule using EventBridge rules.
*)

stack "ScheduledEvents" {
    // Lambda function
    let processFunction =
        lambda "ProcessDaily" {
            runtime Runtime.DOTNET_8
            handler "App::Handler"
            code "./lambda"
        }

    // Run daily at midnight UTC
    eventBridgeRule "DailyProcessing" {
        description "Process data daily at midnight"
        schedule (Schedule.Cron(CronOptions(Hour = "0", Minute = "0")))
        target (LambdaFunction(processFunction.Function.Value))
    }
}

(**
## Rate-Based Scheduling

Execute tasks at regular intervals.
*)

stack "RateBasedEvents" {
    let monitorFunction =
        lambda "Monitor" {
            runtime Runtime.DOTNET_8
            handler "App::Monitor"
            code "./lambda"
        }

    // Run every 5 minutes
    eventBridgeRule "HealthCheck" {
        description "Health check every 5 minutes"
        schedule (Schedule.Rate(Duration.Minutes(5.0)))
        target (LambdaFunction(monitorFunction.Function.Value))
        enabled true
    }
}

(**
## Event Pattern Matching

React to specific AWS events using event patterns.
*)

stack "EventPatterns" {
    let alertFunction =
        lambda "AlertHandler" {
            runtime Runtime.DOTNET_8
            handler "App::HandleAlert"
            code "./lambda"
        }

    // React to EC2 instance state changes
    let ec2StateChangePattern =
        let details = System.Collections.Generic.Dictionary<string, obj>()
        details.Add("state", [| "terminated" |] :> obj)

        EventPattern(
            Source = [| "aws.ec2" |],
            DetailType = [| "EC2 Instance State-change Notification" |],
            Detail = details
        )

    eventBridgeRule "EC2Termination" {
        description "Alert on EC2 instance termination"
        eventPattern ec2StateChangePattern
        target (LambdaFunction(alertFunction.Function.Value))
    }
}

(**
## Custom Event Bus

Create a custom event bus for your application events.
Note: Custom event buses are created using the CDK EventBus class directly,
then referenced in rules.
*)

(*
stack "CustomEventBus" {
    // Create custom event bus using CDK directly
    let customBus = EventBus(this, "AppEventBus", EventBusProps(EventBusName = "my-application-events"))

    let orderProcessor =
        lambda "OrderProcessor" {
            runtime Runtime.DOTNET_8
            handler "App::ProcessOrder"
            code "./lambda"
        }

    // Rule on custom bus
    let orderPattern =
        EventPattern(
            Source = [| "my.application" |],
            DetailType = [| "Order Placed" |]
        )

    eventBridgeRule "ProcessOrders" {
        description "Process new orders"
        eventPattern orderPattern
        eventBus customBus
        target (LambdaFunction(orderProcessor.Function.Value))
    }
}
*)

(**
## Multi-Target Rules

Send events to multiple targets.
*)

stack "MultiTarget" {
    let logFunction =
        lambda "Logger" {
            runtime Runtime.DOTNET_8
            handler "App::Log"
            code "./lambda"
        }

    let notifyFunction =
        lambda "Notifier" {
            runtime Runtime.DOTNET_8
            handler "App::Notify"
            code "./lambda"
        }

    eventBridgeRule "CriticalEvents" {
        description "Handle critical system events"
        schedule (Schedule.Rate(Duration.Hours(1.0)))
        target (LambdaFunction(logFunction.Function.Value))
        target (LambdaFunction(notifyFunction.Function.Value))
    }
}

(**
## Cron Expressions

Use cron expressions for complex scheduling.
*)

stack "CronSchedule" {
    let reportFunction =
        lambda "WeeklyReport" {
            runtime Runtime.DOTNET_8
            handler "App::GenerateReport"
            code "./lambda"
        }

    // Run every Monday at 9 AM UTC
    eventBridgeRule "WeeklyReport" {
        description "Generate weekly report"
        schedule (Schedule.Cron(CronOptions(Minute = "0", Hour = "9", WeekDay = "MON")))
        target (LambdaFunction(reportFunction.Function.Value))
    }
}

(**
## Disabled Rules

Create rules that are initially disabled.
*)

stack "DisabledRule" {
    let maintenanceFunction =
        lambda "Maintenance" {
            runtime Runtime.DOTNET_8
            handler "App::RunMaintenance"
            code "./lambda"
        }

    eventBridgeRule "MaintenanceTask" {
        description "Maintenance task (manually enabled)"
        schedule (Schedule.Rate(Duration.Days(1.0)))
        enabled false // Start disabled
        target (LambdaFunction(maintenanceFunction.Function.Value))
    }
}

(**
## Best Practices

### Performance

- ✅ Use event patterns to filter events efficiently
- ✅ Batch events when possible
- ✅ Monitor rule metrics (Invocations, FailedInvocations)

### Reliability

- ✅ Configure dead-letter queues for failed events
- ✅ Use retries with exponential backoff
- ✅ Set appropriate target timeout values

### Security

- ✅ Use IAM policies to control who can put events
- ✅ Encrypt custom event buses at rest
- ✅ Use resource-based policies for cross-account access

### Cost Optimization

- ✅ Use event filtering to reduce unnecessary invocations
- ✅ Consolidate rules where possible
- ✅ Monitor CloudWatch metrics for usage patterns

### Operational Excellence

- ✅ Use descriptive rule names and descriptions
- ✅ Tag rules with project and environment
- ✅ Document event schemas
- ✅ Version your event patterns
*)
