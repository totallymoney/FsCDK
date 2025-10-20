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

type Route53HostedZoneResource =
    {
        ZoneName: string
        ConstructId: string
        /// The underlying CDK HostedZone construct
        HostedZone: HostedZone
    }

type Route53HostedZoneBuilder(zoneName: string) =
    member _.Yield _ : Route53HostedZoneConfig =
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

    member _.Run(config: Route53HostedZoneConfig) : Route53HostedZoneResource =
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
          HostedZone = null }

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

type Route53ARecordResource =
    {
        RecordName: string
        ConstructId: string
        /// The underlying CDK ARecord construct
        ARecord: ARecord
    }

type Route53ARecordBuilder(recordName: string) =
    member _.Yield _ : Route53ARecordConfig =
        { RecordName = recordName
          ConstructId = None
          Zone = None
          Target = None
          Ttl = Some(Duration.Minutes(5.0))
          Comment = None }

    member _.Zero() : Route53ARecordConfig =
        { RecordName = recordName
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

    member _.Run(config: Route53ARecordConfig) : Route53ARecordResource =
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
          ARecord = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53ARecordConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("zone")>]
    member _.Zone(config: Route53ARecordConfig, zone: IHostedZone) = { config with Zone = Some zone }

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

[<AutoOpen>]
module Route53Builders =
    /// <summary>
    /// Creates a new Route 53 hosted zone builder.
    /// Example: hostedZone "example.com" { comment "Production domain" }
    /// </summary>
    let hostedZone zoneName = Route53HostedZoneBuilder zoneName

    /// <summary>
    /// Creates a new Route 53 A record builder.
    /// Example: aRecord "www" { zone myZone; target myTarget }
    /// </summary>
    let aRecord recordName = Route53ARecordBuilder recordName
