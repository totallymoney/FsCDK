(**
---
title: CloudWatch Dashboard
category: docs
index: 15
---

# CloudWatch Dashboard

CloudWatch Dashboards provide at-a-glance views of your AWS resources and applications.
Monitor metrics, logs, and alarms in a customizable visual interface.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.DynamoDB

(**
## Basic Dashboard

Create a simple dashboard to monitor Lambda functions.
*)


stack "BasicDashboard" {
    let! myFunction =
        lambda "MyFunction" {
            runtime Runtime.DOTNET_8
            handler "App::Handler"
            code "./lambda"
        }

    let invocationsMetric = myFunction.MetricInvocations()
    let errorsMetric = myFunction.MetricErrors()

    dashboard "lambda-monitoring" {

        widgets
            [ graphWidget "Invocations" { left [ invocationsMetric ] }
              graphWidget "Errors" { left [ errorsMetric ] } ]

    // widgets
    //     [ DashboardWidgets.metricWidget "Invocations" [ invocationsMetric ]
    //       DashboardWidgets.metricWidget "Errors" [ errorsMetric ] ]
    }
}


(**
## Multi-Resource Dashboard

Monitor multiple services in one dashboard.
*)


stack "MultiResourceDashboard" {
    let! apiFunction =
        lambda "API" {
            runtime Runtime.DOTNET_8
            handler "App::API"
            code "./lambda"
        }

    let! dataTable =
        table "Data" {
            partitionKey "id" AttributeType.STRING
            billingMode BillingMode.PAY_PER_REQUEST
        }

    let apiMetric = apiFunction.MetricInvocations()
    let apiDuration = apiFunction.MetricDuration()
    let tableReads = dataTable.MetricConsumedReadCapacityUnits()
    let tableWrites = dataTable.MetricConsumedWriteCapacityUnits()

    dashboard "application-monitoring" {
        defaultInterval (Duration.Minutes(5.0))

        widgets
            [
              // First row: Lambda metrics
              graphWidget "API Invocations and Duration" {
                  left [ apiMetric ]
                  right [ apiDuration ]
              }

              //Second row: DynamoDB metrics
              graphWidget "DynamoDB Read/Write Capacity" {
                  left [ tableReads ]
                  right [ tableWrites ]
              } ]
    }
}


(**
## Dashboard with Alarms

Include CloudWatch alarms for critical metrics.
*)


stack "DashboardWithAlarms" {
    let! webFunction =
        lambda "WebApp" {
            runtime Runtime.DOTNET_8
            handler "App::Handle"
            code "./lambda"
        }

    let errorMetric = webFunction.MetricErrors()

    let! errorAlarm =
        // CloudWatch Alarm for Lambda errors
        cloudwatchAlarm "lambda-error-alarm" {
            description "Alert when error rate is high"
            metric errorMetric
            dimensions [ "FunctionName", "my-function" ]
            statistic "Sum"
            threshold 10.0
            evaluationPeriods 2
            period (Duration.Minutes 5.0)
        }

    dashboard "web-app-alerts" {

        widgets
            [ graphWidget "Lambda Errors" { left [ errorMetric ] }
              alarmWidget "Lambda Errors Alarm" { alarm errorAlarm } ]
    }
}

(**
## Dashboard with Text Widgets

Add explanatory text and documentation.
*)

stack "DocumentedDashboard" {
    let mainText =
        "# Production System Overview\n\nThis dashboard monitors critical production metrics.\n\n**Contact:**"

    dashboard "production-overview" {
        widgets
            [ textWidget {
                  markdown mainText
                  width 24
                  height 4
                  background TextWidgetBackground.SOLID
              } ]
    }
}

(**
## Custom Time Range

Set specific time ranges for the dashboard.
*)

stack "CustomTimeRange" {
    dashboard "HistoricalDashboard last-7-days" {
        defaultInterval (Duration.Days(7.0))
        startTime "-P7D" // ISO 8601 duration: 7 days ago
        endTime "PT0H" // Now
    }
}

(**
## Log Insights Widget

Query and visualize CloudWatch Logs.
*)


stack "LogsDashboard" {
    let logWidget =
        logQueryWidget "Error Logs" {
            queryLines
                [ "fields @timestamp, @message"
                  " | filter @message like /ERROR/"
                  " | sort @timestamp desc"
                  " | limit 20" ]

            logGroupNames [ "/aws/lambda/my-function" ]
            width 12
            height 6
        }

    dashboard "LogsDashboard application-logs" { widgets [ logWidget ] }
}


(**
## Single Value Widgets

Display current metric values prominently.
*)


stack "SingleValueDashboard" {
    let activeUsers =
        Metric(MetricProps(Namespace = "MyApp", MetricName = "ActiveUsers"))

    let currentRequests =
        Metric(MetricProps(Namespace = "MyApp", MetricName = "CurrentRequests"))

    dashboard "CurrentStatsDashboard current-stats" {
        widgets
            [ singleValueWidget "Active Users" { metrics [ activeUsers ] }
              singleValueWidget "Current Requests" { metrics [ currentRequests ] } ]
    }
}


(**
## Best Practices

### Design

- ✅ Organize related metrics together
- ✅ Use consistent time ranges across widgets
- ✅ Add text widgets for context and documentation
- ✅ Use colors to highlight critical metrics

### Operational Excellence

- ✅ Create separate dashboards for different teams
- ✅ Include both system and business metrics
- ✅ Set appropriate Y-axis ranges for readability
- ✅ Use anomaly detection for baseline comparison

### Cost Optimization

- ✅ Use 5-minute intervals for most metrics (default)
- ✅ Limit the number of custom metrics
- ✅ Delete unused dashboards
- ✅ Use metric math to reduce API calls

### Security

- ✅ Control dashboard access via IAM
- ✅ Don't expose sensitive data in dashboards
- ✅ Use CloudFormation for dashboard as code
- ✅ Version control dashboard configurations

*)
