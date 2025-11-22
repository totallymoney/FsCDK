namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Route53
open Amazon.CDK.AWS.Route53.Targets
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.EC2

/// <summary>
/// High-level Route 53 Hosted Zone builder following AWS best practices.
///
/// **Default Settings:**
/// - Query logging = disabled (opt-in via logging operation)
/// - DNSSEC = disabled (opt-in, requires KMS key)
///
/// **Rationale:**
/// Hosted zones manage DNS records for your domain.
/// DNSSEC and logging are opt-in features with additional costs.
///
/// **Escape Hatch:**
/// Access the underlying CDK HostedZone via the `HostedZone` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type Route53HostedZoneConfig =
    { ZoneName: string
      ConstructId: string option
      Comment: string option
      QueryLogsLogGroupArn: string option
      Vpcs: IVpc list }

and Route53HostedZoneSpec =
    {
        ZoneName: string
        ConstructId: string
        Props: IHostedZoneProps
        /// The underlying CDK HostedZone construct
        mutable HostedZone: IHostedZone option
    }

    /// Gets the underlying IHostedZone resource. Must be called after the stack is built.
    member this.Resource =
        match this.HostedZone with
        | Some zone -> zone
        | None ->
            failwith
                $"HostedZone '{this.ZoneName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type Route53HostedZoneBuilder(zoneName: string) =
    member _.Yield(_: unit) : Route53HostedZoneConfig =
        { ZoneName = zoneName
          ConstructId = None
          Comment = None
          QueryLogsLogGroupArn = None
          Vpcs = [] }

    member _.Zero() : Route53HostedZoneConfig =
        { ZoneName = zoneName
          ConstructId = None
          Comment = None
          QueryLogsLogGroupArn = None
          Vpcs = [] }

    member _.Combine(state1: Route53HostedZoneConfig, state2: Route53HostedZoneConfig) : Route53HostedZoneConfig =
        { ZoneName = state2.ZoneName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Comment = state2.Comment |> Option.orElse state1.Comment
          QueryLogsLogGroupArn = state2.QueryLogsLogGroupArn |> Option.orElse state1.QueryLogsLogGroupArn
          Vpcs = if state2.Vpcs.IsEmpty then state1.Vpcs else state2.Vpcs }

    member inline x.For
        (
            config: Route53HostedZoneConfig,
            [<InlineIfLambda>] f: unit -> Route53HostedZoneConfig
        ) : Route53HostedZoneConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: Route53HostedZoneConfig) : Route53HostedZoneSpec =
        let zoneName = config.ZoneName
        let constructId = config.ConstructId |> Option.defaultValue zoneName

        let props = HostedZoneProps()
        props.ZoneName <- zoneName
        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        config.QueryLogsLogGroupArn
        |> Option.iter (fun v -> props.QueryLogsLogGroupArn <- v)

        if not config.Vpcs.IsEmpty then
            props.Vpcs <- Array.ofList config.Vpcs

        { ZoneName = zoneName
          ConstructId = constructId
          Props = props
          HostedZone = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53HostedZoneConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("comment")>]
    member _.Comment(config: Route53HostedZoneConfig, comment: string) = { config with Comment = Some comment }

    [<CustomOperation("queryLogsLogGroupArn")>]
    member _.QueryLogsLogGroupArn(config: Route53HostedZoneConfig, arn: string) =
        { config with
            QueryLogsLogGroupArn = Some arn }

    [<CustomOperation("vpcs")>]
    member _.Vpcs(config: Route53HostedZoneConfig, vpcs: IVpc list) = { config with Vpcs = vpcs }

    /// <summary>Adds a single VPC to the hosted zone (for private zones).</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: Route53HostedZoneConfig, vpc: IVpc) =
        { config with
            Vpcs = vpc :: config.Vpcs }

// ============================================================================
// Private Hosted Zone Configuration DSL
// ============================================================================

/// <summary>
/// High-level Route 53 Private Hosted Zone builder.
///
/// **Use Case:**
/// Private hosted zones are used for DNS resolution within VPCs only.
/// They are not accessible from the public internet.
///
/// **Rationale:**
/// Private zones are ideal for internal service discovery and
/// microservices architectures within AWS.
/// </summary>
type Route53PrivateHostedZoneConfig =
    { ZoneName: string
      ConstructId: string option
      Comment: string option
      Vpc: IVpc option }

type Route53PrivateHostedZoneResource =
    {
        ZoneName: string
        ConstructId: string
        /// The underlying CDK PrivateHostedZone construct
        mutable HostedZone: IHostedZone option
    }

    /// Gets the underlying IHostedZone resource. Must be called after the stack is built.
    member this.Resource =
        match this.HostedZone with
        | Some zone -> zone
        | None ->
            failwith
                $"PrivateHostedZone '{this.ZoneName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type Route53PrivateHostedZoneBuilder(zoneName: string) =
    member _.Yield(_: unit) : Route53PrivateHostedZoneConfig =
        { ZoneName = zoneName
          ConstructId = None
          Comment = None
          Vpc = None }

    member _.Zero() : Route53PrivateHostedZoneConfig =
        { ZoneName = zoneName
          ConstructId = None
          Comment = None
          Vpc = None }

    member _.Combine
        (
            state1: Route53PrivateHostedZoneConfig,
            state2: Route53PrivateHostedZoneConfig
        ) : Route53PrivateHostedZoneConfig =
        { ZoneName = state2.ZoneName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Comment = state2.Comment |> Option.orElse state1.Comment
          Vpc = state2.Vpc |> Option.orElse state1.Vpc }

    member inline x.For
        (
            config: Route53PrivateHostedZoneConfig,
            [<InlineIfLambda>] f: unit -> Route53PrivateHostedZoneConfig
        ) : Route53PrivateHostedZoneConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: Route53PrivateHostedZoneConfig) : Route53PrivateHostedZoneResource =
        let zoneName = config.ZoneName
        let constructId = config.ConstructId |> Option.defaultValue zoneName

        let props = PrivateHostedZoneProps()
        props.ZoneName <- zoneName

        props.Vpc <-
            match config.Vpc with
            | Some vpc -> vpc
            | None -> invalidArg "vpc" "VPC is required for Private Hosted Zone"

        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        { ZoneName = zoneName
          ConstructId = constructId
          HostedZone = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53PrivateHostedZoneConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("comment")>]
    member _.Comment(config: Route53PrivateHostedZoneConfig, comment: string) = { config with Comment = Some comment }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: Route53PrivateHostedZoneConfig, vpc: IVpc) = { config with Vpc = Some vpc }

// ============================================================================
// A Record Configuration (existing code unchanged)
// ============================================================================

/// <summary>
/// High-level Route 53 A Record builder.
///
/// **Rationale:**
/// A records map domain names to IP addresses or AWS resources.
/// Supports alias records for AWS resources like ALB, CloudFront, etc.
/// </summary>
type Route53ARecordConfig =
    { RecordName: string
      ConstructId: string option
      Zone: IHostedZone option
      Target: RecordTarget option
      Ttl: Duration option
      Comment: string option }

type Route53ARecordSpec =
    { RecordName: string
      ConstructId: string
      mutable ARecord: ARecord
      Props: ARecordProps }

type Route53ARecordBuilder(name: string) =
    member _.Yield(_: unit) : Route53ARecordConfig =
        { RecordName = name
          ConstructId = None
          Zone = None
          Target = None
          Ttl = Some(Duration.Minutes(5.0))
          Comment = None }

    member _.Zero() : Route53ARecordConfig =
        { RecordName = name
          ConstructId = None
          Zone = None
          Target = None
          Ttl = Some(Duration.Minutes(5.0))
          Comment = None }

    member _.Combine(state1: Route53ARecordConfig, state2: Route53ARecordConfig) : Route53ARecordConfig =
        { RecordName = state2.RecordName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Zone = state2.Zone |> Option.orElse state1.Zone
          Target = state2.Target |> Option.orElse state1.Target
          Ttl = state2.Ttl |> Option.orElse state1.Ttl
          Comment = state2.Comment |> Option.orElse state1.Comment }

    member inline x.For
        (
            config: Route53ARecordConfig,
            [<InlineIfLambda>] f: unit -> Route53ARecordConfig
        ) : Route53ARecordConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: Route53ARecordConfig) : Route53ARecordSpec =
        let recordName = config.RecordName
        let constructId = config.ConstructId |> Option.defaultValue recordName

        let props = ARecordProps()
        props.RecordName <- recordName

        config.Zone |> Option.iter (fun v -> props.Zone <- v)

        config.Target |> Option.iter (fun v -> props.Target <- v)
        config.Ttl |> Option.iter (fun v -> props.Ttl <- v)
        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        { RecordName = recordName
          ConstructId = constructId
          ARecord = null
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53ARecordConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("zone")>]
    member _.Zone(config: Route53ARecordConfig, zone: IHostedZone) = { config with Zone = Some(zone) }

    [<CustomOperation("zone")>]
    member _.Zone(config: Route53ARecordConfig, zone: IHostedZone option) = { config with Zone = zone }

    [<CustomOperation("target")>]
    member _.Target(config: Route53ARecordConfig, target: RecordTarget) = { config with Target = Some target }

    [<CustomOperation("ttl")>]
    member _.Ttl(config: Route53ARecordConfig, ttl: Duration) = { config with Ttl = Some ttl }

    [<CustomOperation("comment")>]
    member _.Comment(config: Route53ARecordConfig, comment: string) = { config with Comment = Some comment }

/// <summary>
/// Helper functions for creating Route 53 record targets
/// </summary>
module Route53Helpers =
    /// <summary>
    /// Creates a record target for an Application Load Balancer
    /// </summary>
    let albTarget (alb: IApplicationLoadBalancer) =
        RecordTarget.FromAlias(LoadBalancerTarget alb)

    /// <summary>
    /// Creates a record target for a CloudFront distribution
    /// </summary>
    let cloudFrontTarget (distribution: IDistribution) =
        RecordTarget.FromAlias(CloudFrontTarget distribution)

// ============================================================================
// Health Check Configuration DSL
// ============================================================================

/// <summary>
/// High-level Route 53 Health Check builder following AWS best practices.
///
/// **Default Settings:**
/// - Type = HTTPS (more secure than HTTP)
/// - Port = 443
/// - Request interval = 30 seconds (standard)
/// - Failure thresholds = 3 (balanced sensitivity)
///
/// **Rationale:**
/// Health checks monitor endpoint availability for failover routing.
/// HTTPS health checks verify both connectivity and SSL/TLS validity.
///
/// **Use Cases:**
/// - Failover routing policies
/// - Weighted routing with health-based failover
/// - CloudWatch alarms for endpoint health
/// </summary>
type Route53HealthCheckConfig =
    { HealthCheckName: string
      ConstructId: string option
      Type: string option
      ResourcePath: string option
      FullyQualifiedDomainName: string option
      Port: int voption
      RequestInterval: int voption
      FailureThreshold: int voption
      MeasureLatency: bool voption
      EnableSNI: bool voption }

type Route53HealthCheckResource =
    {
        HealthCheckName: string
        ConstructId: string
        /// The underlying CDK CfnHealthCheck construct
        HealthCheck: CfnHealthCheck
    }

    /// Gets the health check ID
    member this.HealthCheckId = this.HealthCheck.AttrHealthCheckId

type Route53HealthCheckBuilder(name: string) =
    member _.Yield(_: unit) : Route53HealthCheckConfig =
        { HealthCheckName = name
          ConstructId = None
          Type = Some "HTTPS"
          ResourcePath = Some "/"
          FullyQualifiedDomainName = None
          Port = ValueSome 443
          RequestInterval = ValueSome 30
          FailureThreshold = ValueSome 3
          MeasureLatency = ValueSome false
          EnableSNI = ValueSome true }

    member _.Zero() : Route53HealthCheckConfig =
        { HealthCheckName = name
          ConstructId = None
          Type = Some "HTTPS"
          ResourcePath = Some "/"
          FullyQualifiedDomainName = None
          Port = ValueSome 443
          RequestInterval = ValueSome 30
          FailureThreshold = ValueSome 3
          MeasureLatency = ValueSome false
          EnableSNI = ValueSome true }

    member _.Combine(state1: Route53HealthCheckConfig, state2: Route53HealthCheckConfig) : Route53HealthCheckConfig =
        { HealthCheckName = state2.HealthCheckName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Type = state2.Type |> Option.orElse state1.Type
          ResourcePath = state2.ResourcePath |> Option.orElse state1.ResourcePath
          FullyQualifiedDomainName = state2.FullyQualifiedDomainName |> Option.orElse state1.FullyQualifiedDomainName
          Port = state2.Port |> ValueOption.orElse state1.Port
          RequestInterval = state2.RequestInterval |> ValueOption.orElse state1.RequestInterval
          FailureThreshold = state2.FailureThreshold |> ValueOption.orElse state1.FailureThreshold
          MeasureLatency = state2.MeasureLatency |> ValueOption.orElse state1.MeasureLatency
          EnableSNI = state2.EnableSNI |> ValueOption.orElse state1.EnableSNI }

    member inline x.For
        (
            config: Route53HealthCheckConfig,
            [<InlineIfLambda>] f: unit -> Route53HealthCheckConfig
        ) : Route53HealthCheckConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: Route53HealthCheckConfig) : Route53HealthCheckResource =
        let healthCheckName = config.HealthCheckName
        let constructId = config.ConstructId |> Option.defaultValue healthCheckName

        let healthCheckConfig = CfnHealthCheck.HealthCheckConfigProperty()
        config.Type |> Option.iter (fun v -> healthCheckConfig.Type <- v)

        config.ResourcePath
        |> Option.iter (fun v -> healthCheckConfig.ResourcePath <- v)

        config.FullyQualifiedDomainName
        |> Option.iter (fun v -> healthCheckConfig.FullyQualifiedDomainName <- v)

        config.Port
        |> ValueOption.iter (fun v -> healthCheckConfig.Port <- System.Nullable<float>(float v))

        config.RequestInterval
        |> ValueOption.iter (fun v -> healthCheckConfig.RequestInterval <- System.Nullable<float>(float v))

        config.FailureThreshold
        |> ValueOption.iter (fun v -> healthCheckConfig.FailureThreshold <- System.Nullable<float>(float v))

        config.MeasureLatency
        |> ValueOption.iter (fun v -> healthCheckConfig.MeasureLatency <- System.Nullable<bool>(v))

        config.EnableSNI
        |> ValueOption.iter (fun v -> healthCheckConfig.EnableSni <- System.Nullable<bool>(v))

        let props = CfnHealthCheckProps()
        props.HealthCheckConfig <- healthCheckConfig

        { HealthCheckName = healthCheckName
          ConstructId = constructId
          HealthCheck = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53HealthCheckConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("healthCheckType")>]
    member _.HealthCheckType(config: Route53HealthCheckConfig, hcType: string) = { config with Type = Some hcType }

    [<CustomOperation("resourcePath")>]
    member _.ResourcePath(config: Route53HealthCheckConfig, path: string) =
        { config with ResourcePath = Some path }

    [<CustomOperation("domainName")>]
    member _.DomainName(config: Route53HealthCheckConfig, domain: string) =
        { config with
            FullyQualifiedDomainName = Some domain }

    [<CustomOperation("port")>]
    member _.Port(config: Route53HealthCheckConfig, port: int) = { config with Port = ValueSome port }

    [<CustomOperation("requestInterval")>]
    member _.RequestInterval(config: Route53HealthCheckConfig, interval: int) =
        { config with
            RequestInterval = ValueSome interval }

    [<CustomOperation("failureThreshold")>]
    member _.FailureThreshold(config: Route53HealthCheckConfig, threshold: int) =
        { config with
            FailureThreshold = ValueSome threshold }

    [<CustomOperation("measureLatency")>]
    member _.MeasureLatency(config: Route53HealthCheckConfig, measure: bool) =
        { config with
            MeasureLatency = ValueSome measure }

    [<CustomOperation("enableSNI")>]
    member _.EnableSNI(config: Route53HealthCheckConfig, enable: bool) =
        { config with
            EnableSNI = ValueSome enable }

[<AutoOpen>]
module Route53Builders =
    /// <summary>
    /// Creates a new Route 53 hosted zone builder.
    /// Example: hostedZone "example.com" { comment "Production domain" }
    /// </summary>
    let hostedZone zoneName = Route53HostedZoneBuilder zoneName

    /// <summary>
    /// Creates a new Route 53 private hosted zone builder.
    /// Example: privateHostedZone "internal.example.com" { vpc myVpc; comment "Internal DNS" }
    /// </summary>
    let privateHostedZone zoneName =
        Route53PrivateHostedZoneBuilder zoneName

    /// <summary>
    /// Creates a new Route 53 A record builder.
    /// Example: aRecord "www" { zone myZone; target myTarget }
    /// </summary>
    let aRecord name = Route53ARecordBuilder(name)

    /// <summary>
    /// Creates a new Route 53 health check builder.
    /// Example: healthCheck "my-endpoint" { domainName "api.example.com"; resourcePath "/health" }
    /// </summary>
    let healthCheck name = Route53HealthCheckBuilder name
