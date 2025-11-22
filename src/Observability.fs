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

// ============================================================================
// CloudWatch Alarm Builder DSL
// ============================================================================

/// <summary>
/// High-level CloudWatch Alarm builder for monitoring AWS resources.
/// Supports common metrics and custom metric creation.
/// </summary>
type CloudWatchAlarmConfig =
    { AlarmName: string
      ConstructId: string option
      Description: string option
      MetricNamespace: string option
      MetricName: string option
      Metric: IMetric option
      Dimensions: (string * string) list
      Statistic: string option
      Period: Duration option
      Threshold: float option
      EvaluationPeriods: int option
      ComparisonOperator: ComparisonOperator option
      TreatMissingData: TreatMissingData option
      ActionsEnabled: bool option }

type CloudWatchAlarmSpec =
    { AlarmName: string
      ConstructId: string
      Props: AlarmProps
      mutable Alarm: IAlarm option }

type CloudWatchAlarmBuilder(name: string) =
    member _.Yield(_: unit) : CloudWatchAlarmConfig =
        { AlarmName = name
          ConstructId = None
          Description = None
          MetricNamespace = None
          MetricName = None
          Metric = None
          Dimensions = []
          Statistic = Some "Average"
          Period = Some(Duration.Minutes(5.0))
          Threshold = None
          EvaluationPeriods = Some 1
          ComparisonOperator = Some ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
          TreatMissingData = Some TreatMissingData.NOT_BREACHING
          ActionsEnabled = Some true }

    member _.Zero() : CloudWatchAlarmConfig =
        { AlarmName = name
          ConstructId = None
          Description = None
          MetricNamespace = None
          MetricName = None
          Metric = None
          Dimensions = []
          Statistic = Some "Average"
          Period = Some(Duration.Minutes(5.0))
          Threshold = None
          EvaluationPeriods = Some 1
          ComparisonOperator = Some ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
          TreatMissingData = Some TreatMissingData.NOT_BREACHING
          ActionsEnabled = Some true }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudWatchAlarmConfig) : CloudWatchAlarmConfig = f ()

    member _.Combine(state1: CloudWatchAlarmConfig, state2: CloudWatchAlarmConfig) : CloudWatchAlarmConfig =
        { AlarmName = state1.AlarmName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description
          MetricNamespace = state2.MetricNamespace |> Option.orElse state1.MetricNamespace
          MetricName = state2.MetricName |> Option.orElse state1.MetricName
          Metric = state2.Metric |> Option.orElse state1.Metric
          Dimensions =
            if state2.Dimensions.IsEmpty then
                state1.Dimensions
            else
                state2.Dimensions
          Statistic = state2.Statistic |> Option.orElse state1.Statistic
          Period = state2.Period |> Option.orElse state1.Period
          Threshold = state2.Threshold |> Option.orElse state1.Threshold
          EvaluationPeriods = state2.EvaluationPeriods |> Option.orElse state1.EvaluationPeriods
          ComparisonOperator = state2.ComparisonOperator |> Option.orElse state1.ComparisonOperator
          TreatMissingData = state2.TreatMissingData |> Option.orElse state1.TreatMissingData
          ActionsEnabled = state2.ActionsEnabled |> Option.orElse state1.ActionsEnabled }

    member inline x.For
        (
            config: CloudWatchAlarmConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchAlarmConfig
        ) : CloudWatchAlarmConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchAlarmConfig) : CloudWatchAlarmSpec =
        let constructId = config.ConstructId |> Option.defaultValue config.AlarmName

        match config.Metric, config.MetricNamespace, config.MetricName with
        | Some _, Some _, _
        | Some _, _, Some _ -> failwith "CloudWatch alarm requires only Metric, or namespace+name be defined, not both."
        | Some metric, None, None ->
            let props = AlarmProps()
            props.AlarmName <- config.AlarmName
            config.Description |> Option.iter (fun d -> props.AlarmDescription <- d)

            props.Metric <- metric
            props.Threshold <- config.Threshold |> Option.defaultValue 1.0
            props.EvaluationPeriods <- config.EvaluationPeriods |> Option.defaultValue 1 |> float

            props.ComparisonOperator <-
                config.ComparisonOperator
                |> Option.defaultValue ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD

            props.TreatMissingData <- config.TreatMissingData |> Option.defaultValue TreatMissingData.NOT_BREACHING
            props.ActionsEnabled <- config.ActionsEnabled |> Option.defaultValue true

            { AlarmName = config.AlarmName
              ConstructId = constructId
              Props = props
              Alarm = None }

        | None, None, _
        | None, _, None -> failwith "CloudWatch alarm requires both metricNamespace and metricName"
        | None, Some ns, Some mn ->
            let props = AlarmProps()
            props.AlarmName <- config.AlarmName
            config.Description |> Option.iter (fun d -> props.AlarmDescription <- d)

            let metricProps = MetricProps()
            metricProps.Namespace <- ns
            metricProps.MetricName <- mn
            metricProps.Statistic <- config.Statistic |> Option.defaultValue "Average"
            metricProps.Period <- config.Period |> Option.defaultValue (Duration.Minutes(5.0))

            if not config.Dimensions.IsEmpty then
                metricProps.DimensionsMap <- dict config.Dimensions

            props.Metric <- Metric(metricProps)
            props.Threshold <- config.Threshold |> Option.defaultValue 1.0
            props.EvaluationPeriods <- config.EvaluationPeriods |> Option.defaultValue 1 |> float

            props.ComparisonOperator <-
                config.ComparisonOperator
                |> Option.defaultValue ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD

            props.TreatMissingData <- config.TreatMissingData |> Option.defaultValue TreatMissingData.NOT_BREACHING
            props.ActionsEnabled <- config.ActionsEnabled |> Option.defaultValue true

            { AlarmName = config.AlarmName
              ConstructId = constructId
              Props = props
              Alarm = None }

    /// <summary>Sets the construct ID for the alarm.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchAlarmConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the alarm description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: CloudWatchAlarmConfig, desc: string) = { config with Description = Some desc }

    /// <summary>Sets the CloudWatch metric namespace (e.g., "AWS/Lambda", "AWS/RDS").</summary>
    [<CustomOperation("metricNamespace")>]
    member _.MetricNamespace(config: CloudWatchAlarmConfig, ns: string) =
        { config with
            MetricNamespace = Some ns }

    /// <summary>Sets the metric name (e.g., "Errors", "CPUUtilization").</summary>
    [<CustomOperation("metricName")>]
    member _.MetricName(config: CloudWatchAlarmConfig, name: string) = { config with MetricName = Some name }

    /// <summary>Sets the IMetric. If you use this, don't define metricName or metricNamespace.</summary>
    [<CustomOperation("metric")>]
    member _.Metric(config: CloudWatchAlarmConfig, metric: IMetric) = { config with Metric = Some metric }

    /// <summary>Sets the metric dimensions for filtering (e.g., FunctionName, DBInstanceIdentifier).</summary>
    [<CustomOperation("dimensions")>]
    member _.Dimensions(config: CloudWatchAlarmConfig, dims: (string * string) list) = { config with Dimensions = dims }

    /// <summary>Sets the statistic (Average, Sum, Minimum, Maximum, SampleCount).</summary>
    [<CustomOperation("statistic")>]
    member _.Statistic(config: CloudWatchAlarmConfig, stat: string) = { config with Statistic = Some stat }

    /// <summary>Sets the evaluation period.</summary>
    [<CustomOperation("period")>]
    member _.Period(config: CloudWatchAlarmConfig, period: Duration) = { config with Period = Some period }

    /// <summary>Sets the alarm threshold value.</summary>
    [<CustomOperation("threshold")>]
    member _.Threshold(config: CloudWatchAlarmConfig, threshold: float) =
        { config with
            Threshold = Some threshold }

    /// <summary>Sets the number of periods to evaluate.</summary>
    [<CustomOperation("evaluationPeriods")>]
    member _.EvaluationPeriods(config: CloudWatchAlarmConfig, periods: int) =
        { config with
            EvaluationPeriods = Some periods }

    /// <summary>Sets the comparison operator.</summary>
    [<CustomOperation("comparisonOperator")>]
    member _.ComparisonOperator(config: CloudWatchAlarmConfig, op: ComparisonOperator) =
        { config with
            ComparisonOperator = Some op }

    /// <summary>Sets how to treat missing data.</summary>
    [<CustomOperation("treatMissingData")>]
    member _.TreatMissingData(config: CloudWatchAlarmConfig, treatment: TreatMissingData) =
        { config with
            TreatMissingData = Some treatment }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CloudWatchBuilders =
    let cloudwatchAlarm name = CloudWatchAlarmBuilder(name)
