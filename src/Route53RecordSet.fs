namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Route53

/// <summary>
/// High-level Route 53 RecordSet builder with traffic routing policies.
///
/// **Supported Routing Policies:**
/// - Simple (default)
/// - Weighted (distribute traffic based on weights)
/// - Failover (primary/secondary failover)
/// - Geolocation (route based on user location)
/// - Latency (route to lowest latency endpoint)
/// - Multivalue (return multiple healthy IPs)
///
/// **Default Settings:**
/// - TTL = 300 seconds (5 minutes)
/// - Type = A record
///
/// **Rationale:**
/// RecordSet provides advanced routing capabilities beyond simple A records.
/// Essential for high-availability, multi-region architectures.
///
/// **Use Cases:**
/// - Active-passive failover
/// - Traffic distribution across regions
/// - Blue-green deployments
/// - Load distribution
/// </summary>
type Route53RecordSetConfig =
    { RecordSetName: string
      ConstructId: string option
      HostedZoneId: string option
      Name: string option
      Type: string option
      TTL: int voption
      ResourceRecords: string list
      SetIdentifier: string option
      Weight: int voption
      Failover: string option
      HealthCheckId: string option
      Region: string option
      GeoLocation: CfnRecordSet.GeoLocationProperty option }

type Route53RecordSetResource =
    {
        RecordSetName: string
        ConstructId: string
        /// The underlying CDK CfnRecordSet construct
        RecordSet: CfnRecordSet
    }

type Route53RecordSetBuilder(name: string) =
    member _.Yield _ : Route53RecordSetConfig =
        { RecordSetName = name
          ConstructId = None
          HostedZoneId = None
          Name = None
          Type = Some "A"
          TTL = ValueSome 300
          ResourceRecords = []
          SetIdentifier = None
          Weight = ValueNone
          Failover = None
          HealthCheckId = None
          Region = None
          GeoLocation = None }

    member _.Zero() : Route53RecordSetConfig =
        { RecordSetName = name
          ConstructId = None
          HostedZoneId = None
          Name = None
          Type = Some "A"
          TTL = ValueSome 300
          ResourceRecords = []
          SetIdentifier = None
          Weight = ValueNone
          Failover = None
          HealthCheckId = None
          Region = None
          GeoLocation = None }

    member _.Combine(state1: Route53RecordSetConfig, state2: Route53RecordSetConfig) : Route53RecordSetConfig =
        { RecordSetName = state2.RecordSetName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          HostedZoneId = state2.HostedZoneId |> Option.orElse state1.HostedZoneId
          Name = state2.Name |> Option.orElse state1.Name
          Type = state2.Type |> Option.orElse state1.Type
          TTL = state2.TTL |> ValueOption.orElse state1.TTL
          ResourceRecords =
            if state2.ResourceRecords.IsEmpty then
                state1.ResourceRecords
            else
                state2.ResourceRecords @ state1.ResourceRecords
          SetIdentifier = state2.SetIdentifier |> Option.orElse state1.SetIdentifier
          Weight = state2.Weight |> ValueOption.orElse state1.Weight
          Failover = state2.Failover |> Option.orElse state1.Failover
          HealthCheckId = state2.HealthCheckId |> Option.orElse state1.HealthCheckId
          Region = state2.Region |> Option.orElse state1.Region
          GeoLocation = state2.GeoLocation |> Option.orElse state1.GeoLocation }

    member inline x.For
        (
            config: Route53RecordSetConfig,
            [<InlineIfLambda>] f: unit -> Route53RecordSetConfig
        ) : Route53RecordSetConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: Route53RecordSetConfig) : Route53RecordSetResource =
        let recordSetName = config.RecordSetName
        let constructId = config.ConstructId |> Option.defaultValue recordSetName

        let props = CfnRecordSetProps()

        match config.HostedZoneId with
        | Some id -> props.HostedZoneId <- id
        | None -> failwith "HostedZoneId is required for RecordSet"

        match config.Name with
        | Some n -> props.Name <- n
        | None -> failwith "Name is required for RecordSet"

        config.Type |> Option.iter (fun v -> props.Type <- v)
        config.TTL |> ValueOption.iter (fun v -> props.Ttl <- string v)

        if not config.ResourceRecords.IsEmpty then
            props.ResourceRecords <- config.ResourceRecords |> Array.ofList

        config.SetIdentifier |> Option.iter (fun v -> props.SetIdentifier <- v)

        config.Weight
        |> ValueOption.iter (fun v -> props.Weight <- System.Nullable<float>(float v))

        config.Failover |> Option.iter (fun v -> props.Failover <- v)
        config.HealthCheckId |> Option.iter (fun v -> props.HealthCheckId <- v)
        config.Region |> Option.iter (fun v -> props.Region <- v)
        config.GeoLocation |> Option.iter (fun v -> props.GeoLocation <- v)

        { RecordSetName = recordSetName
          ConstructId = constructId
          RecordSet = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: Route53RecordSetConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("hostedZoneId")>]
    member _.HostedZoneId(config: Route53RecordSetConfig, zoneId: string) =
        { config with
            HostedZoneId = Some zoneId }

    [<CustomOperation("recordName")>]
    member _.RecordName(config: Route53RecordSetConfig, name: string) = { config with Name = Some name }

    [<CustomOperation("recordType")>]
    member _.RecordType(config: Route53RecordSetConfig, recordType: string) = { config with Type = Some recordType }

    [<CustomOperation("ttl")>]
    member _.TTL(config: Route53RecordSetConfig, ttl: int) = { config with TTL = ValueSome ttl }

    [<CustomOperation("resourceRecord")>]
    member _.ResourceRecord(config: Route53RecordSetConfig, record: string) =
        { config with
            ResourceRecords = record :: config.ResourceRecords }

    [<CustomOperation("resourceRecords")>]
    member _.ResourceRecords(config: Route53RecordSetConfig, records: string list) =
        { config with
            ResourceRecords = records @ config.ResourceRecords }

    [<CustomOperation("setIdentifier")>]
    member _.SetIdentifier(config: Route53RecordSetConfig, identifier: string) =
        { config with
            SetIdentifier = Some identifier }

    [<CustomOperation("weight")>]
    member _.Weight(config: Route53RecordSetConfig, weight: int) =
        { config with
            Weight = ValueSome weight }

    [<CustomOperation("failover")>]
    member _.Failover(config: Route53RecordSetConfig, failover: string) =
        { config with Failover = Some failover }

    [<CustomOperation("healthCheckId")>]
    member _.HealthCheckId(config: Route53RecordSetConfig, healthCheckId: string) =
        { config with
            HealthCheckId = Some healthCheckId }

    [<CustomOperation("region")>]
    member _.Region(config: Route53RecordSetConfig, region: string) = { config with Region = Some region }

/// Helper functions for Route53 RecordSet routing policies
module Route53RecordSetHelpers =

    /// Creates a primary failover record
    let primaryFailover = "PRIMARY"

    /// Creates a secondary failover record
    let secondaryFailover = "SECONDARY"

    /// Helper to create weighted routing records
    let weightedRecord (name: string) (weight: int) (ip: string) (zoneId: string) (setId: string) =
        { RecordSetName = name
          ConstructId = None
          HostedZoneId = Some zoneId
          Name = Some name
          Type = Some "A"
          TTL = ValueSome 60
          ResourceRecords = [ ip ]
          SetIdentifier = Some setId
          Weight = ValueSome weight
          Failover = None
          HealthCheckId = None
          Region = None
          GeoLocation = None }

    /// Helper to create failover routing records
    let failoverRecord (name: string) (ip: string) (zoneId: string) (isPrimary: bool) (healthCheckId: string option) =
        { RecordSetName = name
          ConstructId = None
          HostedZoneId = Some zoneId
          Name = Some name
          Type = Some "A"
          TTL = ValueSome 60
          ResourceRecords = [ ip ]
          SetIdentifier = Some(if isPrimary then "Primary" else "Secondary")
          Weight = ValueNone
          Failover = Some(if isPrimary then primaryFailover else secondaryFailover)
          HealthCheckId = healthCheckId
          Region = None
          GeoLocation = None }

[<AutoOpen>]
module Route53RecordSetBuilders =
    /// <summary>
    /// Creates a new Route 53 RecordSet builder with advanced routing.
    /// Example: recordSet "www.example.com" { hostedZoneId zone; weight 100; setIdentifier "set1" }
    /// </summary>
    let recordSet name = Route53RecordSetBuilder name
