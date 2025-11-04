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
    { ZoneName: string
      ConstructId: string
      Props: IHostedZoneProps
      mutable HostedZone: IHostedZone }


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
/// **Use Case: **
/// Private hosted zones are used for DNS resolution within VPCs only.
/// They are not accessible from the public internet.
///
/// **Rationale: **
/// Private zones are ideal for internal service discovery and
/// microservices architectures within AWS.
/// </summary>
type Route53PrivateHostedZoneConfig =
    { ZoneName: string
      ConstructId: string option
      Comment: string option
      Vpc: IVpc option }

type Route53PrivateHostedZoneSpec =
    { ZoneName: string
      ConstructId: string
      mutable HostedZone: IHostedZone }

type Route53PrivateHostedZoneBuilder(zoneName: string) =
    member _.Yield _ : Route53PrivateHostedZoneConfig =
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

    member _.Run(config: Route53PrivateHostedZoneConfig) : Route53PrivateHostedZoneSpec =
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
          HostedZone = null }

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
    member _.Yield _ : Route53ARecordConfig =
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

    [<CustomOperation("target")>]
    member _.Target(config: Route53ARecordConfig, target: RecordTarget) = { config with Target = Some target }

    [<CustomOperation("ttl")>]
    member _.Ttl(config: Route53ARecordConfig, ttl: Duration) = { config with Ttl = Some ttl }

    [<CustomOperation("comment")>]
    member _.Comment(config: Route53ARecordConfig, comment: string) = { config with Comment = Some comment }


[<AutoOpen>]
module Route53Builders =
    /// <summary>
    /// Creates a new Route 53 hosted zone builder.
    /// Example: hostedZone "example.com" { comment "Production domain" }
    /// </summary>
    let hostedZone name = Route53HostedZoneBuilder(name)

    /// <summary>
    /// Creates a new Route 53 private hosted zone builder.
    /// Example: privateHostedZone "internal.example.com" { vpc myVpc; comment "Internal DNS" }
    /// </summary>
    let privateHostedZone name = Route53PrivateHostedZoneBuilder(name)

    /// <summary>
    /// Creates a new Route 53 A record builder.
    /// Example: aRecord "www" { zone myZone; target myTarget }
    /// </summary>
    let aRecord name = Route53ARecordBuilder(name)

type HostedZoneConfig =
    { Vpcs: IVpc seq
      ZoneName: string
      AddTrailingDot: bool option
      Comment: string option
      ConstructId: string option
      QueryLogsLogGroupArn: string option }

type HostedZoneSpec =
    { ZoneName: string
      ConstructId: string
      Props: IHostedZoneProps
      mutable HostedZone: IHostedZone }

type HostedZoneBuilder(zoneName: string) =
    member _.Yield(_: unit) : HostedZoneConfig =
        { Vpcs = []
          ZoneName = zoneName
          AddTrailingDot = None
          ConstructId = None
          Comment = None
          QueryLogsLogGroupArn = None }

    member _.Yield(vpc: IVpc) : HostedZoneConfig =
        { Vpcs = [ vpc ]
          ZoneName = zoneName
          AddTrailingDot = None
          ConstructId = None
          Comment = None
          QueryLogsLogGroupArn = None }

    member _.Zero() : HostedZoneConfig =
        { Vpcs = []
          ZoneName = zoneName
          ConstructId = None
          AddTrailingDot = None
          Comment = None
          QueryLogsLogGroupArn = None }

    member _.Combine(state1: HostedZoneConfig, state2: HostedZoneConfig) : HostedZoneConfig =
        { ZoneName = state2.ZoneName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Comment = state2.Comment |> Option.orElse state1.Comment
          AddTrailingDot = state2.AddTrailingDot |> Option.orElse state1.AddTrailingDot
          QueryLogsLogGroupArn = state2.QueryLogsLogGroupArn |> Option.orElse state1.QueryLogsLogGroupArn
          Vpcs = if Seq.isEmpty state2.Vpcs then state1.Vpcs else state2.Vpcs }

    member inline x.For(config: HostedZoneConfig, [<InlineIfLambda>] f: unit -> HostedZoneConfig) : HostedZoneConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: HostedZoneConfig) : HostedZoneSpec =
        let zoneName = config.ZoneName
        let constructId = config.ConstructId |> Option.defaultValue zoneName

        let props = HostedZoneProps()
        props.ZoneName <- zoneName
        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        config.AddTrailingDot |> Option.iter (fun v -> props.AddTrailingDot <- v)

        config.Comment |> Option.iter (fun v -> props.Comment <- v)

        config.QueryLogsLogGroupArn
        |> Option.iter (fun v -> props.QueryLogsLogGroupArn <- v)

        if not (Seq.isEmpty config.Vpcs) then
            props.Vpcs <- Array.ofSeq config.Vpcs

        { ZoneName = zoneName
          ConstructId = constructId
          Props = props
          HostedZone = null }


    /// <summary>Sets the construct ID for the hosted zone.</summary>
    /// <param name="config">The current hosted zone configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     constructId "MyHostedZone"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: HostedZoneConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the comment for the hosted zone.</summary>
    /// <param name="config">The current hosted zone configuration.</param>
    /// <param name="comment">The comment string.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     comment "This is my hosted zone"
    /// }
    /// </code>
    [<CustomOperation("comment")>]
    member _.Comment(config: HostedZoneConfig, comment: string) = { config with Comment = Some comment }

    /// <summary>Specifies whether to add a trailing dot to the zone name.</summary>
    /// <param name="config">The current hosted zone configuration.</param>
    /// <param name="addTrailingDot">True to add a trailing dot, false otherwise.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     addTrailingDot true
    /// }
    /// </code>
    [<CustomOperation("addTrailingDot")>]
    member _.AddTrailingDot(config: HostedZoneConfig, addTrailingDot: bool) =
        { config with
            AddTrailingDot = Some addTrailingDot }

    /// <summary>Sets the ARN of the CloudWatch Log Group for query logging.</summary>
    /// <param name="config">The current hosted zone configuration.</param>
    /// <param name="arn">The ARN string.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     queryLogsLogGroupArn "arn:aws:logs:region:account-id:log-group:log-group-name"
    /// }
    /// </code>
    [<CustomOperation("queryLogsLogGroupArn")>]
    member _.QueryLogsLogGroupArn(config: HostedZoneConfig, arn: string) =
        { config with
            QueryLogsLogGroupArn = Some arn }

    /// <summary>Adds VPCs to associate with the hosted zone (for private zones).</summary>
    /// <param name="config">The current hosted zone configuration.</param>
    /// <param name="vpcs">The sequence of VPCs.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     vpcs [ myVpc1; myVpc2 ]
    /// }
    /// </code>
    [<CustomOperation("vpcs")>]
    member _.Vpcs(config: HostedZoneConfig, vpcs: IVpc seq) = { config with Vpcs = vpcs }


[<AutoOpen>]
module HostedZoneBuilders =
    /// <summary>Creates a new Route 53 hosted zone builder.</summary>
    /// <param name="name">The zone name.</param>
    /// <code lang="fsharp">
    /// hostedZone "example.com" {
    ///     comment "Production domain"
    /// }
    /// </code>
    let hostedZone name = HostedZoneBuilder(name)
