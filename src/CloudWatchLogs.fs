namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.KMS

/// <summary>
/// High-level CloudWatch Log Group builder following AWS best practices.
///
/// **Default Security Settings:**
/// - Retention = 7 days (cost-effective default, change for compliance)
/// - Removal policy = DESTROY (logs cleaned up on stack deletion)
/// - Encryption = AWS managed key (optional customer managed key)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - 7-day retention balances observability and cost
/// - DESTROY removal policy prevents orphaned log groups
/// - Encryption at rest protects sensitive log data
///
/// **Escape Hatch:**
/// Access the underlying CDK LogGroup via the `LogGroup` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type CloudWatchLogGroupConfig =
    { LogGroupName: string
      ConstructId: string option
      Retention: RetentionDays voption
      RemovalPolicy: RemovalPolicy voption
      EncryptionKey: IKey option
      LogGroupClass: LogGroupClass voption }

type CloudWatchLogGroupSpec =
    { LogGroupName: string
      ConstructId: string
      Props: LogGroupProps
      mutable LogGroup: LogGroup option }

type CloudWatchLogGroupBuilder(name: string) =
    member _.Yield _ : CloudWatchLogGroupConfig =
        { LogGroupName = name
          ConstructId = None
          Retention = ValueSome RetentionDays.ONE_WEEK
          RemovalPolicy = ValueSome RemovalPolicy.DESTROY
          EncryptionKey = None
          LogGroupClass = ValueNone }

    member _.Zero() : CloudWatchLogGroupConfig =
        { LogGroupName = name
          ConstructId = None
          Retention = ValueSome RetentionDays.ONE_WEEK
          RemovalPolicy = ValueSome RemovalPolicy.DESTROY
          EncryptionKey = None
          LogGroupClass = ValueNone }

    member _.Combine(state1: CloudWatchLogGroupConfig, state2: CloudWatchLogGroupConfig) : CloudWatchLogGroupConfig =
        { LogGroupName = state2.LogGroupName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Retention = state2.Retention |> ValueOption.orElse state1.Retention
          RemovalPolicy = state2.RemovalPolicy |> ValueOption.orElse state1.RemovalPolicy
          EncryptionKey = state2.EncryptionKey |> Option.orElse state1.EncryptionKey
          LogGroupClass = state2.LogGroupClass |> ValueOption.orElse state1.LogGroupClass }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudWatchLogGroupConfig) : CloudWatchLogGroupConfig = f ()

    member inline x.For
        (
            config: CloudWatchLogGroupConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchLogGroupConfig
        ) : CloudWatchLogGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchLogGroupConfig) : CloudWatchLogGroupSpec =
        let logGroupName = config.LogGroupName
        let constructId = config.ConstructId |> Option.defaultValue logGroupName

        let props = LogGroupProps()
        props.LogGroupName <- logGroupName

        config.Retention |> ValueOption.iter (fun v -> props.Retention <- v)
        config.RemovalPolicy |> ValueOption.iter (fun v -> props.RemovalPolicy <- v)
        config.LogGroupClass |> ValueOption.iter (fun v -> props.LogGroupClass <- v)

        config.EncryptionKey |> Option.iter (fun key -> props.EncryptionKey <- key)

        { LogGroupName = logGroupName
          ConstructId = constructId
          Props = props
          LogGroup = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchLogGroupConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("retention")>]
    member _.Retention(config: CloudWatchLogGroupConfig, retention: RetentionDays) =
        { config with
            Retention = ValueSome retention }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: CloudWatchLogGroupConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = ValueSome policy }

    [<CustomOperation("encryptionKey")>]
    member _.EncryptionKey(config: CloudWatchLogGroupConfig, key: IKey) =
        { config with EncryptionKey = Some key }

    [<CustomOperation("logGroupClass")>]
    member _.LogGroupClass(config: CloudWatchLogGroupConfig, logClass: LogGroupClass) =
        { config with
            LogGroupClass = ValueSome logClass }

/// Helper functions for CloudWatch Logs
module CloudWatchLogsHelpers =

    /// Common retention periods for different use cases
    module RetentionPeriods =
        /// Development/testing: 3 days
        let dev = RetentionDays.THREE_DAYS

        /// Standard retention: 7 days
        let standard = RetentionDays.ONE_WEEK

        /// Production: 30 days
        let production = RetentionDays.ONE_MONTH

        /// Compliance: 90 days
        let compliance = RetentionDays.THREE_MONTHS

        /// Long-term: 1 year
        let longTerm = RetentionDays.ONE_YEAR

        /// Audit logs: 5 years
        let audit = RetentionDays.FIVE_YEARS

        /// Permanent: Infinite retention
        let permanent = RetentionDays.INFINITE

    /// Creates a log group name for ECS services
    let ecsLogGroup (serviceName: string) (environment: string) =
        sprintf "/aws/ecs/%s-%s" serviceName environment

    /// Creates a log group name for Lambda functions
    let lambdaLogGroup (functionName: string) = sprintf "/aws/lambda/%s" functionName

    /// Creates a log group name for API Gateway
    let apiGatewayLogGroup (apiName: string) (stage: string) =
        sprintf "/aws/apigateway/%s/%s" apiName stage

    /// Creates a log group name for application logs
    let appLogGroup (appName: string) (environment: string) = sprintf "/%s/%s" appName environment

[<AutoOpen>]
module CloudWatchLogsBuilders =
    /// <summary>
    /// Creates a new CloudWatch Log Group builder with sensible defaults.
    /// Example: logGroup "/aws/ecs/my-service" { retention RetentionDays.ONE_MONTH }
    /// </summary>
    let logGroup name = CloudWatchLogGroupBuilder name

// ============================================================================
// CloudWatch Metric Filter Builder
// ============================================================================

/// <summary>
/// Configuration for CloudWatch Metric Filter.
/// Extracts metrics from log events based on pattern matching.
///
/// **Use Cases:**
/// - Extract custom business metrics from application logs
/// - Monitor error patterns and frequencies
/// - Track specific application events
/// - Create alarms on log-based metrics
///
/// **Best Practices:**
/// - Use structured logging (JSON) for easier pattern matching
/// - Test filter patterns with CloudWatch Logs Insights
/// - Set appropriate metric units
/// - Use metric namespaces to organize custom metrics
/// </summary>
type CloudWatchMetricFilterConfig =
    { FilterName: string
      ConstructId: string option
      LogGroup: LogGroup option
      LogGroupResource: CloudWatchLogGroupSpec option // Store the resource for later resolution
      FilterPattern: IFilterPattern option
      MetricName: string option
      MetricNamespace: string option
      MetricValue: string option
      DefaultValue: float option
      Unit: Amazon.CDK.AWS.CloudWatch.Unit voption }

type CloudWatchMetricFilterSpec =
    { FilterName: string
      ConstructId: string
      LogGroupToAttach: LogGroup option
      LogGroupResource: CloudWatchLogGroupSpec option
      FilterOptions: MetricFilterOptions
      mutable MetricFilter: MetricFilter option }

type CloudWatchMetricFilterBuilder(name: string) =
    member _.Yield _ : CloudWatchMetricFilterConfig =
        { FilterName = name
          ConstructId = None
          LogGroup = None
          LogGroupResource = None
          FilterPattern = None
          MetricName = None
          MetricNamespace = None
          MetricValue = Some "1"
          DefaultValue = None
          Unit = ValueNone }

    member _.Zero() : CloudWatchMetricFilterConfig =
        { FilterName = name
          ConstructId = None
          LogGroup = None
          LogGroupResource = None
          FilterPattern = None
          MetricName = None
          MetricNamespace = None
          MetricValue = Some "1"
          DefaultValue = None
          Unit = ValueNone }

    member _.Combine
        (
            state1: CloudWatchMetricFilterConfig,
            state2: CloudWatchMetricFilterConfig
        ) : CloudWatchMetricFilterConfig =
        { FilterName = state2.FilterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          LogGroup = state2.LogGroup |> Option.orElse state1.LogGroup
          LogGroupResource = state2.LogGroupResource |> Option.orElse state1.LogGroupResource
          FilterPattern = state2.FilterPattern |> Option.orElse state1.FilterPattern
          MetricName = state2.MetricName |> Option.orElse state1.MetricName
          MetricNamespace = state2.MetricNamespace |> Option.orElse state1.MetricNamespace
          MetricValue = state2.MetricValue |> Option.orElse state1.MetricValue
          DefaultValue = state2.DefaultValue |> Option.orElse state1.DefaultValue
          Unit = state2.Unit |> ValueOption.orElse state1.Unit }

    member inline _.Delay([<InlineIfLambda>] f: unit -> CloudWatchMetricFilterConfig) : CloudWatchMetricFilterConfig =
        f ()

    member inline x.For
        (
            config: CloudWatchMetricFilterConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchMetricFilterConfig
        ) : CloudWatchMetricFilterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchMetricFilterConfig) : CloudWatchMetricFilterSpec =
        let filterName = config.FilterName
        let constructId = config.ConstructId |> Option.defaultValue filterName

        // Validation - at least one log group reference must be provided
        if config.LogGroup.IsNone && config.LogGroupResource.IsNone then
            failwith "LogGroup is required for MetricFilter"

        let filterPattern =
            config.FilterPattern
            |> Option.defaultWith (fun () -> failwith "FilterPattern is required for MetricFilter")

        let metricName =
            config.MetricName
            |> Option.defaultWith (fun () -> failwith "MetricName is required for MetricFilter")

        let metricNamespace =
            config.MetricNamespace
            |> Option.defaultWith (fun () -> failwith "MetricNamespace is required for MetricFilter")

        let options = MetricFilterOptions()
        options.FilterName <- filterName
        options.FilterPattern <- filterPattern
        options.MetricName <- metricName
        options.MetricNamespace <- metricNamespace
        options.MetricValue <- config.MetricValue |> Option.defaultValue "1"

        config.DefaultValue
        |> Option.iter (fun v -> options.DefaultValue <- System.Nullable(v))

        config.Unit
        |> ValueOption.iter (fun u -> options.Unit <- System.Nullable<Amazon.CDK.AWS.CloudWatch.Unit>(u))

        // Store configuration but don't resolve LogGroup here - that happens in Stack.fs
        { FilterName = filterName
          ConstructId = constructId
          LogGroupToAttach = config.LogGroup // Direct LogGroup or None
          LogGroupResource = config.LogGroupResource // Store resource reference for Stack.fs to resolve
          FilterOptions = options
          MetricFilter = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchMetricFilterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: CloudWatchMetricFilterConfig, logGroup: LogGroup) =
        { config with LogGroup = Some logGroup }

    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: CloudWatchMetricFilterConfig, logGroupResource: CloudWatchLogGroupSpec) =
        // Store the resource reference - actual LogGroup will be resolved in Stack.fs
        { config with
            LogGroupResource = Some logGroupResource }

    [<CustomOperation("filterPattern")>]
    member _.FilterPattern(config: CloudWatchMetricFilterConfig, pattern: IFilterPattern) =
        { config with
            FilterPattern = Some pattern }

    [<CustomOperation("metricName")>]
    member _.MetricName(config: CloudWatchMetricFilterConfig, name: string) = { config with MetricName = Some name }

    [<CustomOperation("metricNamespace")>]
    member _.MetricNamespace(config: CloudWatchMetricFilterConfig, ns: string) =
        { config with
            MetricNamespace = Some ns }

    [<CustomOperation("metricValue")>]
    member _.MetricValue(config: CloudWatchMetricFilterConfig, value: string) =
        { config with MetricValue = Some value }

    [<CustomOperation("defaultValue")>]
    member _.DefaultValue(config: CloudWatchMetricFilterConfig, value: float) =
        { config with
            DefaultValue = Some value }

    [<CustomOperation("unit")>]
    member _.Unit(config: CloudWatchMetricFilterConfig, unit: Amazon.CDK.AWS.CloudWatch.Unit) =
        { config with Unit = ValueSome unit }

// ============================================================================
// CloudWatch Subscription Filter Builder
// ============================================================================

/// <summary>
/// Configuration for CloudWatch Subscription Filter.
/// Streams log events to destinations in real-time.
///
/// **Common Destinations:**
/// - Lambda functions (for log processing)
/// - Kinesis streams (for log aggregation)
/// - Kinesis Firehose (for S3 archival)
/// - OpenSearch (for log analytics)
///
/// **Production Use Cases:**
/// - Stream logs to centralized logging (ELK, Splunk)
/// - Real-time log processing and alerting
/// - Security event monitoring (SIEM integration)
/// - Compliance log archival
///
/// **Best Practices:**
/// - Use filter patterns to reduce unnecessary data transfer
/// - Consider costs for high-volume log streaming
/// - Set appropriate IAM permissions for destinations
/// - Monitor subscription filter delivery failures
/// </summary>
type CloudWatchSubscriptionFilterConfig =
    { FilterName: string
      ConstructId: string option
      LogGroup: LogGroup option
      LogGroupResource: CloudWatchLogGroupSpec option
      Destination: ILogSubscriptionDestination option
      FilterPattern: IFilterPattern option }

type CloudWatchSubscriptionFilterSpec =
    { FilterName: string
      ConstructId: string
      LogGroupToAttach: LogGroup option
      LogGroupResource: CloudWatchLogGroupSpec option
      Props: SubscriptionFilterProps
      mutable SubscriptionFilter: SubscriptionFilter option }

type CloudWatchSubscriptionFilterBuilder(name: string) =
    member _.Yield _ : CloudWatchSubscriptionFilterConfig =
        { FilterName = name
          ConstructId = None
          LogGroup = None
          LogGroupResource = None
          Destination = None
          FilterPattern = None }

    member _.Zero() : CloudWatchSubscriptionFilterConfig =
        { FilterName = name
          ConstructId = None
          LogGroup = None
          LogGroupResource = None
          Destination = None
          FilterPattern = None }

    member _.Combine
        (
            state1: CloudWatchSubscriptionFilterConfig,
            state2: CloudWatchSubscriptionFilterConfig
        ) : CloudWatchSubscriptionFilterConfig =
        { FilterName = state2.FilterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          LogGroup = state2.LogGroup |> Option.orElse state1.LogGroup
          LogGroupResource = state2.LogGroupResource |> Option.orElse state1.LogGroupResource
          Destination = state2.Destination |> Option.orElse state1.Destination
          FilterPattern = state2.FilterPattern |> Option.orElse state1.FilterPattern }

    member inline _.Delay
        ([<InlineIfLambda>] f: unit -> CloudWatchSubscriptionFilterConfig)
        : CloudWatchSubscriptionFilterConfig =
        f ()

    member inline x.For
        (
            config: CloudWatchSubscriptionFilterConfig,
            [<InlineIfLambda>] f: unit -> CloudWatchSubscriptionFilterConfig
        ) : CloudWatchSubscriptionFilterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: CloudWatchSubscriptionFilterConfig) : CloudWatchSubscriptionFilterSpec =
        let filterName = config.FilterName
        let constructId = config.ConstructId |> Option.defaultValue filterName

        // Validation
        if config.LogGroup.IsNone && config.LogGroupResource.IsNone then
            failwith "LogGroup is required for SubscriptionFilter"

        let destination =
            config.Destination
            |> Option.defaultWith (fun () -> failwith "Destination is required for SubscriptionFilter")

        let filterPattern =
            config.FilterPattern
            |> Option.defaultWith (fun () -> failwith "FilterPattern is required for SubscriptionFilter")

        let props = SubscriptionFilterProps()
        props.FilterName <- filterName
        // LogGroup will be set in Stack.fs after resolution
        props.Destination <- destination
        props.FilterPattern <- filterPattern

        { FilterName = filterName
          ConstructId = constructId
          LogGroupToAttach = config.LogGroup
          LogGroupResource = config.LogGroupResource
          Props = props
          SubscriptionFilter = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: CloudWatchSubscriptionFilterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: CloudWatchSubscriptionFilterConfig, logGroup: LogGroup) =
        { config with LogGroup = Some logGroup }

    [<CustomOperation("logGroup")>]
    member _.LogGroup(config: CloudWatchSubscriptionFilterConfig, logGroupResource: CloudWatchLogGroupSpec) =
        // Store the resource reference - actual LogGroup will be resolved in Stack.fs
        { config with
            LogGroupResource = Some logGroupResource }

    [<CustomOperation("destination")>]
    member _.Destination(config: CloudWatchSubscriptionFilterConfig, destination: ILogSubscriptionDestination) =
        { config with
            Destination = Some destination }

    [<CustomOperation("filterPattern")>]
    member _.FilterPattern(config: CloudWatchSubscriptionFilterConfig, pattern: IFilterPattern) =
        { config with
            FilterPattern = Some pattern }

/// Helper module for common filter patterns
module FilterPatterns =
    /// Match all log events
    let allEvents () = FilterPattern.AllEvents()

    /// Match log events containing specific text
    let matchText (text: string) = FilterPattern.Literal(text)

    /// Match JSON log events with specific field values
    let matchJson (jsonPattern: string) =
        FilterPattern.SpaceDelimited(jsonPattern)

    /// Match log events by severity level
    let errorLogs () = FilterPattern.Literal("ERROR")

    let warningLogs () = FilterPattern.Literal("WARN")

    let infoLogs () = FilterPattern.Literal("INFO")

    /// Match HTTP 5xx errors
    let http5xxErrors () = FilterPattern.Literal("5??")

    /// Match HTTP 4xx errors
    let http4xxErrors () = FilterPattern.Literal("4??")

[<AutoOpen>]
module CloudWatchLogsFilterBuilders =
    /// <summary>
    /// Creates a CloudWatch Metric Filter to extract metrics from logs.
    /// Example:
    /// metricFilter "ErrorCount" {
    ///     logGroup myLogGroup
    ///     filterPattern (FilterPatterns.errorLogs())
    ///     metricName "ErrorCount"
    ///     metricNamespace "MyApp"
    /// }
    /// </summary>
    let metricFilter name = CloudWatchMetricFilterBuilder name

    /// <summary>
    /// Creates a CloudWatch Subscription Filter to stream logs to a destination.
    /// Example:
    /// subscriptionFilter "LogsToLambda" {
    ///     logGroup myLogGroup
    ///     destination lambdaDestination
    ///     filterPattern (FilterPatterns.allEvents())
    /// }
    /// </summary>
    let subscriptionFilter name =
        CloudWatchSubscriptionFilterBuilder name
