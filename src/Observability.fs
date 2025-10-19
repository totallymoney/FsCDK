namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.CloudTrail
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.ElasticLoadBalancingV2

/// <summary>
/// Observability helpers for CloudTrail, CloudWatch, GuardDuty, and AWS Config.
///
/// **Rationale:**
/// - CloudTrail provides audit logs for compliance and security investigations
/// - CloudWatch alarms enable proactive incident response
/// - GuardDuty detects threats and anomalous behavior
/// - AWS Config tracks resource configuration changes
///
/// These tools are essential for:
/// - Security monitoring and threat detection
/// - Compliance auditing (SOC2, HIPAA, PCI-DSS)
/// - Operational visibility and troubleshooting
/// - Cost optimization through usage tracking
/// </summary>
module Observability =

    /// <summary>
    /// Creates a CloudTrail with encrypted logs stored in S3
    /// </summary>
    /// <param name="trailName">Name of the CloudTrail</param>
    /// <param name="bucket">Optional S3 bucket for logs (creates one if not provided)</param>
    /// <param name="includeGlobalEvents">Include global service events (IAM, CloudFront, etc.)</param>
    let createCloudTrail (trailName: string) (bucket: IBucket option) (includeGlobalEvents: bool) =
        let props = TrailProps()
        props.TrailName <- trailName
        props.IncludeGlobalServiceEvents <- includeGlobalEvents
        props.IsMultiRegionTrail <- true
        props.EnableFileValidation <- true

        match bucket with
        | Some b -> props.Bucket <- b
        | None -> ()

        props

    /// <summary>
    /// Creates a CloudWatch alarm for Lambda function errors
    /// </summary>
    /// <param name="alarmName">Name of the alarm</param>
    /// <param name="functionName">Lambda function name to monitor</param>
    /// <param name="threshold">Error count threshold (default: 5)</param>
    /// <param name="evaluationPeriods">Number of periods to evaluate (default: 1)</param>
    let createLambdaErrorAlarm
        (alarmName: string)
        (functionName: string)
        (threshold: float option)
        (evaluationPeriods: int option)
        =
        let props = AlarmProps()
        props.AlarmName <- alarmName
        props.AlarmDescription <- sprintf "Alarm when %s has errors" functionName

        props.Metric <-
            Metric(
                MetricProps(
                    Namespace = "AWS/Lambda",
                    MetricName = "Errors",
                    DimensionsMap = dict [ "FunctionName", functionName ],
                    Statistic = "Sum",
                    Period = Duration.Minutes(5.0)
                )
            )

        props.Threshold <- threshold |> Option.defaultValue 5.0
        props.EvaluationPeriods <- evaluationPeriods |> Option.defaultValue 1 |> float
        props.ComparisonOperator <- ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
        props.TreatMissingData <- TreatMissingData.NOT_BREACHING

        props

    /// <summary>
    /// Creates a CloudWatch alarm for RDS CPU utilization
    /// </summary>
    /// <param name="alarmName">Name of the alarm</param>
    /// <param name="dbInstanceIdentifier">RDS instance identifier</param>
    /// <param name="threshold">CPU percentage threshold (default: 80)</param>
    /// <param name="evaluationPeriods">Number of periods to evaluate (default: 2)</param>
    let createRDSCpuAlarm
        (alarmName: string)
        (dbInstanceIdentifier: string)
        (threshold: float option)
        (evaluationPeriods: int option)
        =
        let props = AlarmProps()
        props.AlarmName <- alarmName
        props.AlarmDescription <- sprintf "Alarm when %s CPU is high" dbInstanceIdentifier

        props.Metric <-
            Metric(
                MetricProps(
                    Namespace = "AWS/RDS",
                    MetricName = "CPUUtilization",
                    DimensionsMap = dict [ "DBInstanceIdentifier", dbInstanceIdentifier ],
                    Statistic = "Average",
                    Period = Duration.Minutes(5.0)
                )
            )

        props.Threshold <- threshold |> Option.defaultValue 80.0
        props.EvaluationPeriods <- evaluationPeriods |> Option.defaultValue 2 |> float
        props.ComparisonOperator <- ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
        props.TreatMissingData <- TreatMissingData.NOT_BREACHING

        props

    /// <summary>
    /// Creates a CloudWatch alarm for ALB 5xx errors
    /// </summary>
    /// <param name="alarmName">Name of the alarm</param>
    /// <param name="loadBalancerFullName">ALB full name (e.g., app/my-lb/1234567890abcdef)</param>
    /// <param name="threshold">Error count threshold (default: 10)</param>
    /// <param name="evaluationPeriods">Number of periods to evaluate (default: 2)</param>
    let createALB5xxAlarm
        (alarmName: string)
        (loadBalancerFullName: string)
        (threshold: float option)
        (evaluationPeriods: int option)
        =
        let props = AlarmProps()
        props.AlarmName <- alarmName
        props.AlarmDescription <- sprintf "Alarm when %s has 5xx errors" loadBalancerFullName

        props.Metric <-
            Metric(
                MetricProps(
                    Namespace = "AWS/ApplicationELB",
                    MetricName = "HTTPCode_Target_5XX_Count",
                    DimensionsMap = dict [ "LoadBalancer", loadBalancerFullName ],
                    Statistic = "Sum",
                    Period = Duration.Minutes(5.0)
                )
            )

        props.Threshold <- threshold |> Option.defaultValue 10.0
        props.EvaluationPeriods <- evaluationPeriods |> Option.defaultValue 2 |> float
        props.ComparisonOperator <- ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
        props.TreatMissingData <- TreatMissingData.NOT_BREACHING

        props

    /// <summary>
    /// Template for common CloudWatch alarm configurations
    /// </summary>
    type AlarmTemplate =
        | LambdaErrors of functionName: string * threshold: float option
        | RDSHighCpu of dbIdentifier: string * threshold: float option
        | ALB5xxErrors of loadBalancerName: string * threshold: float option

    /// <summary>
    /// Creates a CloudWatch alarm from a template
    /// </summary>
    let createAlarmFromTemplate (alarmName: string) (template: AlarmTemplate) =
        match template with
        | LambdaErrors(functionName, threshold) -> createLambdaErrorAlarm alarmName functionName threshold None
        | RDSHighCpu(dbIdentifier, threshold) -> createRDSCpuAlarm alarmName dbIdentifier threshold None
        | ALB5xxErrors(loadBalancerName, threshold) -> createALB5xxAlarm alarmName loadBalancerName threshold None

/// <summary>
/// Toggle configurations for optional AWS security services
/// These services have associated costs and should be enabled based on security requirements
/// </summary>
module SecurityToggles =

    /// <summary>
    /// Configuration for enabling GuardDuty threat detection
    ///
    /// **Cost Consideration:**
    /// GuardDuty charges are based on:
    /// - CloudTrail events analyzed
    /// - VPC Flow Logs analyzed
    /// - DNS logs analyzed
    /// Typical cost: $4-5 per million events
    ///
    /// **When to enable:**
    /// - Production environments with sensitive data
    /// - Compliance requirements (PCI-DSS, HIPAA)
    /// - Environments requiring threat detection
    ///
    /// **Note:** GuardDuty must be enabled via AWS Console or CLI, not CloudFormation
    /// Use this as documentation and manual setup guide
    /// </summary>
    type GuardDutyConfig =
        { Enabled: bool
          FindingPublishingFrequency: string option } // FIFTEEN_MINUTES, ONE_HOUR, SIX_HOURS

    let defaultGuardDuty =
        { Enabled = false
          FindingPublishingFrequency = Some "ONE_HOUR" }

    /// <summary>
    /// Configuration for enabling AWS Config
    ///
    /// **Cost Consideration:**
    /// AWS Config charges are based on:
    /// - Configuration items recorded
    /// - Configuration change evaluations
    /// Typical cost: $0.003 per configuration item
    ///
    /// **When to enable:**
    /// - Compliance auditing requirements
    /// - Change tracking for security/operational purposes
    /// - Configuration drift detection
    ///
    /// **Note:** AWS Config must be enabled via AWS Console or CLI
    /// Use this as documentation and manual setup guide
    /// </summary>
    type AWSConfigConfig =
        { Enabled: bool
          IncludeGlobalResources: bool
          AllSupported: bool }

    let defaultAWSConfig =
        { Enabled = false
          IncludeGlobalResources = true
          AllSupported = true }
