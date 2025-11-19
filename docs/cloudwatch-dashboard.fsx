(**
---
title: CloudWatch Monitoring and Dashboards
category: 3. Resources
categoryindex: 6
---

# ![CloudWatch](img/icons/Arch_Amazon-CloudWatch_48.png) CloudWatch Monitoring and Dashboards

CloudWatch provides comprehensive monitoring for your AWS resources with dashboards, log groups, 
metric filters, and subscription filters. Monitor metrics, logs, and alarms in a customizable (and visual) interface.

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
    let myFunction =
        lambda "MyFunction" {
            runtime Runtime.DOTNET_8
            handler "App::Handler"
            code "./lambda"
        }

    let invocationsMetric = myFunction.Function.Value.MetricInvocations()
    let errorsMetric = myFunction.Function.Value.MetricErrors()

    dashboard "LambdaDashboard" {
        dashboardName "lambda-monitoring"

        widgetRow
            [ DashboardWidgets.metricWidget "Invocations" [ invocationsMetric ]
              DashboardWidgets.metricWidget "Errors" [ errorsMetric ] ]
    }
}


(**
## Multi-Resource Dashboard

Monitor multiple services in one dashboard.
*)


stack "MultiResourceDashboard" {
    let apiFunction =
        lambda "API" {
            runtime Runtime.DOTNET_8
            handler "App::API"
            code "./lambda"
        }

    let dataTable =
        table "Data" {
            partitionKey "id" AttributeType.STRING
            billingMode BillingMode.PAY_PER_REQUEST
        }

    let apiMetric = apiFunction.Function.Value.MetricInvocations()
    let apiDuration = apiFunction.Function.Value.MetricDuration()
    let tableReads = dataTable.Table.Value.MetricConsumedReadCapacityUnits()
    let tableWrites = dataTable.Table.Value.MetricConsumedWriteCapacityUnits()

    dashboard "ApplicationDashboard" {
        dashboardName "application-monitoring"
        defaultInterval (Duration.Minutes(5.0))

        // First row: Lambda metrics
        widgetRow
            [ DashboardWidgets.metricWidget "API Invocations" [ apiMetric ]
              DashboardWidgets.metricWidget "API Duration" [ apiDuration ] ]

        // Second row: DynamoDB metrics
        widgetRow
            [ DashboardWidgets.metricWidget "Table Reads" [ tableReads ]
              DashboardWidgets.metricWidget "Table Writes" [ tableWrites ] ]
    }
}


(**
## Dashboard with Alarms

Include CloudWatch alarms for critical metrics.
*)


stack "DashboardWithAlarms" {
    let webFunction =
        lambda "WebApp" {
            runtime Runtime.DOTNET_8
            handler "App::Handle"
            code "./lambda"
        }

    let errorMetric = webFunction.Function.Value.MetricErrors()

    let errorAlarm =
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

    dashboard "AlertingDashboard" {
        dashboardName "web-app-alerts"

        widgetRow [ DashboardWidgets.metricWidget "Errors" [ errorMetric ] ]

        widget (DashboardWidgets.alarmWidgetSpec errorAlarm)
    }
}

(**
## Dashboard with Text Widgets

Add explanatory text and documentation.
*)

stack "DocumentedDashboard" {
    dashboard "ProductionDashboard" {
        dashboardName "production-overview"

        widget (
            DashboardWidgets.textWidget
                "# Production System Overview\n\nThis dashboard monitors critical production metrics.\n\n**Contact:** ops-team@example.com"
        )

    // Add metric widgets here
    }
}

(**
## Custom Time Range

Set specific time ranges for the dashboard.
*)

stack "CustomTimeRange" {
    dashboard "HistoricalDashboard" {
        dashboardName "last-7-days"
        defaultInterval (Duration.Days(7.0))
        startTime "-P7D" // ISO 8601 duration: 7 days ago
        endTime "PT0H" // Now
    }
}

(**
## CloudWatch Log Groups

Create and configure log groups for your applications.
*)

stack "LogGroupsStack" {
    // Lambda function log group with custom retention
    logGroup "/aws/lambda/my-function" {
        retention Amazon.CDK.AWS.Logs.RetentionDays.ONE_MONTH
        removalPolicy RemovalPolicy.DESTROY
    }

    // ECS service log group
    logGroup (CloudWatchLogsHelpers.ecsLogGroup "my-service" "production") {
        retention CloudWatchLogsHelpers.RetentionPeriods.production
    }

    // API Gateway log group
    logGroup (CloudWatchLogsHelpers.apiGatewayLogGroup "my-api" "prod") {
        retention CloudWatchLogsHelpers.RetentionPeriods.audit
        constructId "ApiGatewayLogs"
    }
}

(**
## Metric Filters

Extract custom metrics from log data for monitoring and alerting.
*)

stack "MetricFiltersStack" {
    let appLogGroup =
        logGroup "/aws/application/logs" { retention Amazon.CDK.AWS.Logs.RetentionDays.TWO_WEEKS }

    // Count error occurrences
    metricFilter "ErrorCount" {
        logGroup appLogGroup
        filterPattern (FilterPatterns.errorLogs ())
        metricName "ErrorCount"
        metricNamespace "MyApp"
        metricValue "1"
    }

    // Count HTTP 5xx errors
    metricFilter "ServerErrors" {
        logGroup appLogGroup
        filterPattern (FilterPatterns.http5xxErrors ())
        metricName "ServerErrorCount"
        metricNamespace "MyApp"
    }

    // Extract custom metric value from logs
    metricFilter "ResponseTime" {
        logGroup appLogGroup
        filterPattern (FilterPatterns.matchText "response_time")
        metricName "ResponseTime"
        metricNamespace "MyApp"
        metricValue "$responseTime"
        unit Unit.MILLISECONDS
    }
}

(**
## Log Insights Widget

Query and visualize CloudWatch Logs.
*)


stack "LogsDashboard" {
    let logWidget =
        DashboardWidgets.logQueryWidget
            "Error Logs"
            [ "/aws/lambda/my-function" ]
            "fields @timestamp, @message | filter @message like /ERROR/ | sort @timestamp desc | limit 20"
            (Some 12)
            (Some 6)

    dashboard "LogsDashboard" {
        dashboardName "application-logs"
        widget logWidget
    }
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

    dashboard "CurrentStatsDashboard" {
        dashboardName "current-stats"

        widgetRow
            [ DashboardWidgets.singleValueWidget "Active Users" [ activeUsers ]
              DashboardWidgets.singleValueWidget "Current Requests" [ currentRequests ] ]
    }
}


(**
## CloudWatch Helpers

FsCDK provides helper functions for common CloudWatch patterns.
*)

// Common retention periods
let devRetention = CloudWatchLogsHelpers.RetentionPeriods.dev // 3 days
let prodRetention = CloudWatchLogsHelpers.RetentionPeriods.production // 30 days
let auditRetention = CloudWatchLogsHelpers.RetentionPeriods.audit // 5 years

// Standard log group naming
let lambdaLogGroup = CloudWatchLogsHelpers.lambdaLogGroup "my-function"
let ecsLogGroup = CloudWatchLogsHelpers.ecsLogGroup "my-service" "production"
let apiGatewayLogGroup = CloudWatchLogsHelpers.apiGatewayLogGroup "my-api" "prod"

// Common filter patterns
let allEvents = FilterPatterns.allEvents ()
let errorLogs = FilterPatterns.errorLogs ()
let warningLogs = FilterPatterns.warningLogs ()
let http5xxErrors = FilterPatterns.http5xxErrors ()
let http4xxErrors = FilterPatterns.http4xxErrors ()

(**
## Best Practices

### Design

- Organize related metrics together
- Use consistent time ranges across widgets
- Add text widgets for context and documentation
- Use colors to highlight critical metrics

### Operational Excellence

- Create separate dashboards for different teams
- Include both system and business metrics
- Set appropriate Y-axis ranges for readability
- Use anomaly detection for baseline comparison
- Use metric filters to extract custom metrics from logs
- Set appropriate log retention periods (dev: 3 days, prod: 30 days, audit: 5 years)

### Cost Optimization

- Use 5-minute intervals for most metrics (default)
- Limit the number of custom metrics
- Delete unused dashboards
- Use metric math to reduce API calls
- Set shorter retention for development logs (3-7 days)
- Use filter patterns to reduce unnecessary log processing

### Security

- Control dashboard access via IAM
- Don't expose sensitive data in dashboards
- Use CloudFormation for dashboard as code
- Version control dashboard configurations
- Enable encryption for sensitive log data
- Use DESTROY removal policy for dev/test log groups

*)
