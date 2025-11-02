namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.XRay

/// <summary>
/// High-level AWS X-Ray enhanced tracing builder following AWS best practices.
///
/// **Default Settings:**
/// - InsightsEnabled = true (automatic anomaly detection)
/// - NotificationsEnabled = false (opt-in for notifications)
///
/// **Rationale:**
/// These defaults follow Yan Cui's production debugging recommendations:
/// - X-Ray Groups for filtering traces by business logic
/// - Sampling Rules for cost-effective tracing at scale
/// - Insights for automatic anomaly detection
///
/// **Use Cases:**
/// - Production debugging and troubleshooting
/// - Performance optimization
/// - Distributed tracing across services
/// - Error rate analysis
/// - Latency profiling
///
/// **Escape Hatch:**
/// Access the underlying CDK CfnGroup/CfnSamplingRule via properties
/// for advanced scenarios not covered by this builder.
/// </summary>
type XRayGroupConfig =
    { GroupName: string
      ConstructId: string option
      FilterExpression: string option
      InsightsEnabled: bool voption
      Tags: (string * string) list }

type XRayGroupResource =
    {
        GroupName: string
        ConstructId: string
        Props: CfnGroupProps
        /// The underlying CDK CfnGroup construct
        mutable Group: CfnGroup option
    }

    /// Gets the group ARN
    member this.GroupArn =
        match this.Group with
        | Some g -> g.AttrGroupArn
        | None -> null

type XRayGroupBuilder(name: string) =
    member _.Yield _ : XRayGroupConfig =
        { GroupName = name
          ConstructId = None
          FilterExpression = None
          InsightsEnabled = ValueSome true
          Tags = [] }

    member _.Zero() : XRayGroupConfig =
        { GroupName = name
          ConstructId = None
          FilterExpression = None
          InsightsEnabled = ValueSome true
          Tags = [] }

    member _.Combine(state1: XRayGroupConfig, state2: XRayGroupConfig) : XRayGroupConfig =
        { GroupName = state2.GroupName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          FilterExpression = state2.FilterExpression |> Option.orElse state1.FilterExpression
          InsightsEnabled = state2.InsightsEnabled |> ValueOption.orElse state1.InsightsEnabled
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline _.Delay([<InlineIfLambda>] f: unit -> XRayGroupConfig) : XRayGroupConfig = f ()

    member inline x.For(config: XRayGroupConfig, [<InlineIfLambda>] f: unit -> XRayGroupConfig) : XRayGroupConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: XRayGroupConfig) : XRayGroupResource =
        let groupName = config.GroupName
        let constructId = config.ConstructId |> Option.defaultValue groupName

        let props = CfnGroupProps()
        props.GroupName <- groupName

        config.FilterExpression |> Option.iter (fun v -> props.FilterExpression <- v)

        let insightsConfig = CfnGroup.InsightsConfigurationProperty()
        insightsConfig.InsightsEnabled <- config.InsightsEnabled |> ValueOption.defaultValue true
        insightsConfig.NotificationsEnabled <- false
        props.InsightsConfiguration <- insightsConfig

        if not config.Tags.IsEmpty then
            props.Tags <-
                config.Tags
                |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                |> Array.ofList

        { GroupName = groupName
          ConstructId = constructId
          Props = props
          Group = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: XRayGroupConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("filterExpression")>]
    member _.FilterExpression(config: XRayGroupConfig, expression: string) =
        { config with
            FilterExpression = Some expression }

    [<CustomOperation("insightsEnabled")>]
    member _.InsightsEnabled(config: XRayGroupConfig, enabled: bool) =
        { config with
            InsightsEnabled = ValueSome enabled }

    [<CustomOperation("tag")>]
    member _.Tag(config: XRayGroupConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: XRayGroupConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// X-Ray Sampling Rule Configuration
type XRaySamplingRuleConfig =
    { RuleName: string
      ConstructId: string option
      Priority: int voption
      ReservoirSize: int voption
      FixedRate: float voption
      Host: string option
      HttpMethod: string option
      UrlPath: string option
      ServiceName: string option
      ServiceType: string option
      ResourceArn: string option
      Tags: (string * string) list }

type XRaySamplingRuleResource =
    {
        RuleName: string
        ConstructId: string
        Props: CfnSamplingRuleProps
        /// The underlying CDK CfnSamplingRule construct
        mutable SamplingRule: CfnSamplingRule option
    }

    /// Gets the sampling rule ARN
    member this.RuleArn =
        match this.SamplingRule with
        | Some r -> r.AttrRuleArn
        | None -> null

type XRaySamplingRuleBuilder(name: string) =
    member _.Yield _ : XRaySamplingRuleConfig =
        { RuleName = name
          ConstructId = None
          Priority = ValueSome 1000
          ReservoirSize = ValueSome 1
          FixedRate = ValueSome 0.05 // 5% sampling
          Host = Some "*"
          HttpMethod = Some "*"
          UrlPath = Some "*"
          ServiceName = Some "*"
          ServiceType = Some "*"
          ResourceArn = Some "*"
          Tags = [] }

    member _.Zero() : XRaySamplingRuleConfig =
        { RuleName = name
          ConstructId = None
          Priority = ValueSome 1000
          ReservoirSize = ValueSome 1
          FixedRate = ValueSome 0.05
          Host = Some "*"
          HttpMethod = Some "*"
          UrlPath = Some "*"
          ServiceName = Some "*"
          ServiceType = Some "*"
          ResourceArn = Some "*"
          Tags = [] }

    member _.Combine(state1: XRaySamplingRuleConfig, state2: XRaySamplingRuleConfig) : XRaySamplingRuleConfig =
        { RuleName = state2.RuleName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Priority = state2.Priority |> ValueOption.orElse state1.Priority
          ReservoirSize = state2.ReservoirSize |> ValueOption.orElse state1.ReservoirSize
          FixedRate = state2.FixedRate |> ValueOption.orElse state1.FixedRate
          Host = state2.Host |> Option.orElse state1.Host
          HttpMethod = state2.HttpMethod |> Option.orElse state1.HttpMethod
          UrlPath = state2.UrlPath |> Option.orElse state1.UrlPath
          ServiceName = state2.ServiceName |> Option.orElse state1.ServiceName
          ServiceType = state2.ServiceType |> Option.orElse state1.ServiceType
          ResourceArn = state2.ResourceArn |> Option.orElse state1.ResourceArn
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline _.Delay([<InlineIfLambda>] f: unit -> XRaySamplingRuleConfig) : XRaySamplingRuleConfig = f ()

    member inline x.For
        (
            config: XRaySamplingRuleConfig,
            [<InlineIfLambda>] f: unit -> XRaySamplingRuleConfig
        ) : XRaySamplingRuleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: XRaySamplingRuleConfig) : XRaySamplingRuleResource =
        let ruleName = config.RuleName
        let constructId = config.ConstructId |> Option.defaultValue ruleName

        let samplingRule = CfnSamplingRule.SamplingRuleProperty()
        samplingRule.RuleName <- ruleName
        samplingRule.Priority <- config.Priority |> ValueOption.defaultValue 1000 |> float
        samplingRule.ReservoirSize <- config.ReservoirSize |> ValueOption.defaultValue 1 |> float
        samplingRule.FixedRate <- config.FixedRate |> ValueOption.defaultValue 0.05
        samplingRule.Host <- config.Host |> Option.defaultValue "*"
        samplingRule.HttpMethod <- config.HttpMethod |> Option.defaultValue "*"
        samplingRule.UrlPath <- config.UrlPath |> Option.defaultValue "*"
        samplingRule.ServiceName <- config.ServiceName |> Option.defaultValue "*"
        samplingRule.ServiceType <- config.ServiceType |> Option.defaultValue "*"
        samplingRule.ResourceArn <- config.ResourceArn |> Option.defaultValue "*"
        samplingRule.Version <- 1.0

        let props = CfnSamplingRuleProps()
        props.SamplingRule <- samplingRule

        if not config.Tags.IsEmpty then
            props.Tags <-
                config.Tags
                |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                |> Array.ofList

        { RuleName = ruleName
          ConstructId = constructId
          Props = props
          SamplingRule = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: XRaySamplingRuleConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("priority")>]
    member _.Priority(config: XRaySamplingRuleConfig, priority: int) =
        { config with
            Priority = ValueSome priority }

    [<CustomOperation("reservoirSize")>]
    member _.ReservoirSize(config: XRaySamplingRuleConfig, size: int) =
        { config with
            ReservoirSize = ValueSome size }

    [<CustomOperation("fixedRate")>]
    member _.FixedRate(config: XRaySamplingRuleConfig, rate: float) =
        { config with
            FixedRate = ValueSome rate }

    [<CustomOperation("host")>]
    member _.Host(config: XRaySamplingRuleConfig, host: string) = { config with Host = Some host }

    [<CustomOperation("httpMethod")>]
    member _.HttpMethod(config: XRaySamplingRuleConfig, method: string) =
        { config with HttpMethod = Some method }

    [<CustomOperation("urlPath")>]
    member _.UrlPath(config: XRaySamplingRuleConfig, path: string) = { config with UrlPath = Some path }

    [<CustomOperation("serviceName")>]
    member _.ServiceName(config: XRaySamplingRuleConfig, name: string) = { config with ServiceName = Some name }

    [<CustomOperation("serviceType")>]
    member _.ServiceType(config: XRaySamplingRuleConfig, serviceType: string) =
        { config with
            ServiceType = Some serviceType }

    [<CustomOperation("resourceArn")>]
    member _.ResourceArn(config: XRaySamplingRuleConfig, arn: string) = { config with ResourceArn = Some arn }

    [<CustomOperation("tag")>]
    member _.Tag(config: XRaySamplingRuleConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: XRaySamplingRuleConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// Helper functions for X-Ray operations
module XRayHelpers =

    /// Common filter expressions for X-Ray groups
    module FilterExpressions =
        /// Filter for HTTP errors (4xx and 5xx)
        let httpErrors = "fault = true OR error = true"

        /// Filter for 5xx server errors only
        let serverErrors = "fault = true AND http.status >= 500"

        /// Filter for 4xx client errors only
        let clientErrors = "error = true AND http.status >= 400 AND http.status < 500"

        /// Filter for slow requests (> 1 second)
        let slowRequests = "duration > 1"

        /// Filter for very slow requests (> 5 seconds)
        let verySlowRequests = "duration > 5"

        /// Custom filter for specific service
        let forService serviceName = $"service(name = \"{serviceName}\")"

        /// Filter for specific annotation
        let withAnnotation (key: string) (value: string) = $"annotation.{key} = \"{value}\""

    /// Common sampling rates
    module SamplingRates =
        /// Sample 1% of traffic
        let onePercent = 0.01

        /// Sample 5% of traffic (default)
        let fivePercent = 0.05

        /// Sample 10% of traffic
        let tenPercent = 0.10

        /// Sample 25% of traffic
        let twentyFivePercent = 0.25

        /// Sample 50% of traffic
        let fiftyPercent = 0.50

        /// Sample 100% of traffic
        let all = 1.0

    /// Creates a sampling rule for high-traffic endpoints
    let highTrafficRule (name: string) (urlPath: string) = (name, urlPath, 10, 0.01) // Priority 10, 1% sampling

    /// Creates a sampling rule for low-traffic endpoints
    let lowTrafficRule (name: string) (urlPath: string) = (name, urlPath, 100, 0.10) // Priority 100, 10% sampling

    /// Creates a sampling rule for critical endpoints (always sample)
    let criticalEndpointRule (name: string) (urlPath: string) = (name, urlPath, 1, 1.0) // Priority 1 (highest), 100% sampling

[<AutoOpen>]
module XRayBuilders =
    /// <summary>
    /// Creates a new X-Ray Group builder for filtering traces.
    /// Example: xrayGroup "ProductionErrors" { filterExpression "fault = true" }
    /// </summary>
    let xrayGroup name = XRayGroupBuilder name

    /// <summary>
    /// Creates a new X-Ray Sampling Rule builder for cost-effective tracing.
    /// Example: xraySamplingRule "HighTrafficAPI" { urlPath "/api/*"; fixedRate 0.05 }
    /// </summary>
    let xraySamplingRule name = XRaySamplingRuleBuilder name
