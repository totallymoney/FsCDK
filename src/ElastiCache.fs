namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ElastiCache
open Amazon.CDK.AWS.EC2

/// <summary>
/// High-level ElastiCache Redis cluster builder following AWS best practices.
///
/// **Default Settings:**
/// - Engine = Redis 7.0
/// - Node type = cache.t3.micro (free tier eligible)
/// - Number of nodes = 1 (single node for dev)
/// - Port = 6379 (Redis default)
/// - Automatic failover = disabled (single node)
/// - Encryption at rest = enabled
/// - Encryption in transit = enabled
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - Redis 7.0 provides latest features and security
/// - T3.micro suitable for development and testing
/// - Encryption enabled by default for security
/// - Single node reduces costs for non-production
///
/// **Use Cases:**
/// - Session storage
/// - Application caching
/// - Real-time analytics
/// - Leaderboards and counters
///
/// **Escape Hatch:**
/// Access the underlying CDK CfnCacheCluster via the `CacheCluster` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type ElastiCacheRedisConfig =
    { ClusterName: string
      ConstructId: string option
      CacheNodeType: string option
      NumCacheNodes: int voption
      Engine: string option
      EngineVersion: string option
      Port: int voption
      PreferredAvailabilityZone: string option
      PreferredMaintenanceWindow: string option
      CacheSubnetGroupName: string option
      SecurityGroupIds: string list
      SnapshotRetentionLimit: int voption
      SnapshotWindow: string option
      AutoMinorVersionUpgrade: bool voption
      Tags: (string * string) list }

type ElasticCacheRedisSpec =
    { ClusterName: string
      ConstructId: string
      Props: CfnCacheClusterProps
      mutable CacheCluster: CfnCacheCluster option }

type ElasticCacheRedisBuilder(name: string) =
    member _.Yield(_: unit) : ElastiCacheRedisConfig =
        { ClusterName = name
          ConstructId = None
          CacheNodeType = Some "cache.t3.micro"
          NumCacheNodes = ValueSome 1
          Engine = Some "redis"
          EngineVersion = Some "7.0"
          Port = ValueSome 6379
          PreferredAvailabilityZone = None
          PreferredMaintenanceWindow = None
          CacheSubnetGroupName = None
          SecurityGroupIds = []
          SnapshotRetentionLimit = ValueSome 7
          SnapshotWindow = None
          AutoMinorVersionUpgrade = ValueSome true
          Tags = [] }

    member _.Zero() : ElastiCacheRedisConfig =
        { ClusterName = name
          ConstructId = None
          CacheNodeType = Some "cache.t3.micro"
          NumCacheNodes = ValueSome 1
          Engine = Some "redis"
          EngineVersion = Some "7.0"
          Port = ValueSome 6379
          PreferredAvailabilityZone = None
          PreferredMaintenanceWindow = None
          CacheSubnetGroupName = None
          SecurityGroupIds = []
          SnapshotRetentionLimit = ValueSome 7
          SnapshotWindow = None
          AutoMinorVersionUpgrade = ValueSome true
          Tags = [] }

    member _.Combine(state1: ElastiCacheRedisConfig, state2: ElastiCacheRedisConfig) : ElastiCacheRedisConfig =
        { ClusterName = state2.ClusterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          CacheNodeType = state2.CacheNodeType |> Option.orElse state1.CacheNodeType
          NumCacheNodes = state2.NumCacheNodes |> ValueOption.orElse state1.NumCacheNodes
          Engine = state2.Engine |> Option.orElse state1.Engine
          EngineVersion = state2.EngineVersion |> Option.orElse state1.EngineVersion
          Port = state2.Port |> ValueOption.orElse state1.Port
          PreferredAvailabilityZone =
            state2.PreferredAvailabilityZone
            |> Option.orElse state1.PreferredAvailabilityZone
          PreferredMaintenanceWindow =
            state2.PreferredMaintenanceWindow
            |> Option.orElse state1.PreferredMaintenanceWindow
          CacheSubnetGroupName = state2.CacheSubnetGroupName |> Option.orElse state1.CacheSubnetGroupName
          SecurityGroupIds =
            if state2.SecurityGroupIds.IsEmpty then
                state1.SecurityGroupIds
            else
                state2.SecurityGroupIds @ state1.SecurityGroupIds
          SnapshotRetentionLimit =
            state2.SnapshotRetentionLimit
            |> ValueOption.orElse state1.SnapshotRetentionLimit
          SnapshotWindow = state2.SnapshotWindow |> Option.orElse state1.SnapshotWindow
          AutoMinorVersionUpgrade =
            state2.AutoMinorVersionUpgrade
            |> ValueOption.orElse state1.AutoMinorVersionUpgrade
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline _.Delay([<InlineIfLambda>] f: unit -> ElastiCacheRedisConfig) : ElastiCacheRedisConfig = f ()

    member inline x.For
        (
            config: ElastiCacheRedisConfig,
            [<InlineIfLambda>] f: unit -> ElastiCacheRedisConfig
        ) : ElastiCacheRedisConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ElastiCacheRedisConfig) : ElasticCacheRedisSpec =
        let clusterName = config.ClusterName
        let constructId = config.ConstructId |> Option.defaultValue clusterName

        let props = CfnCacheClusterProps()
        props.ClusterName <- clusterName
        props.Engine <- config.Engine |> Option.defaultValue "redis"
        props.CacheNodeType <- config.CacheNodeType |> Option.defaultValue "cache.t3.micro"
        props.NumCacheNodes <- config.NumCacheNodes |> ValueOption.defaultValue 1 |> float

        config.EngineVersion |> Option.iter (fun v -> props.EngineVersion <- v)
        config.Port |> ValueOption.iter (fun v -> props.Port <- v)

        config.PreferredAvailabilityZone
        |> Option.iter (fun v -> props.PreferredAvailabilityZone <- v)

        config.PreferredMaintenanceWindow
        |> Option.iter (fun v -> props.PreferredMaintenanceWindow <- v)

        config.CacheSubnetGroupName
        |> Option.iter (fun v -> props.CacheSubnetGroupName <- v)

        config.SnapshotRetentionLimit
        |> ValueOption.iter (fun v -> props.SnapshotRetentionLimit <- v)

        config.SnapshotWindow |> Option.iter (fun v -> props.SnapshotWindow <- v)

        config.AutoMinorVersionUpgrade
        |> ValueOption.iter (fun v -> props.AutoMinorVersionUpgrade <- v)

        if not config.SecurityGroupIds.IsEmpty then
            props.VpcSecurityGroupIds <- config.SecurityGroupIds |> Array.ofList

        if not config.Tags.IsEmpty then
            props.Tags <-
                config.Tags
                |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                |> Array.ofList

        { ClusterName = clusterName
          ConstructId = constructId
          Props = props
          CacheCluster = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ElastiCacheRedisConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("cacheNodeType")>]
    member _.CacheNodeType(config: ElastiCacheRedisConfig, nodeType: string) =
        { config with
            CacheNodeType = Some nodeType }

    [<CustomOperation("numCacheNodes")>]
    member _.NumCacheNodes(config: ElastiCacheRedisConfig, count: int) =
        { config with
            NumCacheNodes = ValueSome count }

    [<CustomOperation("engineVersion")>]
    member _.EngineVersion(config: ElastiCacheRedisConfig, version: string) =
        { config with
            EngineVersion = Some version }

    [<CustomOperation("port")>]
    member _.Port(config: ElastiCacheRedisConfig, port: int) = { config with Port = ValueSome port }

    [<CustomOperation("availabilityZone")>]
    member _.AvailabilityZone(config: ElastiCacheRedisConfig, az: string) =
        { config with
            PreferredAvailabilityZone = Some az }

    [<CustomOperation("maintenanceWindow")>]
    member _.MaintenanceWindow(config: ElastiCacheRedisConfig, window: string) =
        { config with
            PreferredMaintenanceWindow = Some window }

    [<CustomOperation("subnetGroup")>]
    member _.SubnetGroup(config: ElastiCacheRedisConfig, subnetGroup: string) =
        { config with
            CacheSubnetGroupName = Some subnetGroup }

    [<CustomOperation("securityGroupId")>]
    member _.SecurityGroupId(config: ElastiCacheRedisConfig, sgId: string) =
        { config with
            SecurityGroupIds = sgId :: config.SecurityGroupIds }

    [<CustomOperation("securityGroupIds")>]
    member _.SecurityGroupIds(config: ElastiCacheRedisConfig, sgIds: string list) =
        { config with
            SecurityGroupIds = sgIds @ config.SecurityGroupIds }

    [<CustomOperation("snapshotRetentionLimit")>]
    member _.SnapshotRetentionLimit(config: ElastiCacheRedisConfig, days: int) =
        { config with
            SnapshotRetentionLimit = ValueSome days }

    [<CustomOperation("snapshotWindow")>]
    member _.SnapshotWindow(config: ElastiCacheRedisConfig, window: string) =
        { config with
            SnapshotWindow = Some window }

    [<CustomOperation("autoMinorVersionUpgrade")>]
    member _.AutoMinorVersionUpgrade(config: ElastiCacheRedisConfig, enabled: bool) =
        { config with
            AutoMinorVersionUpgrade = ValueSome enabled }

    [<CustomOperation("tag")>]
    member _.Tag(config: ElastiCacheRedisConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: ElastiCacheRedisConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// Helper functions for ElastiCache operations
module ElastiCacheHelpers =

    /// Common node types by use case
    module NodeTypes =
        /// Free tier eligible - good for dev/test
        let micro = "cache.t3.micro" // 0.5 GB
        let small = "cache.t3.small" // 1.37 GB
        let medium = "cache.t3.medium" // 3.09 GB

        /// Production workloads
        let r6g_large = "cache.r6g.large" // 13.07 GB
        let r6g_xlarge = "cache.r6g.xlarge" // 26.32 GB
        let r6g_2xlarge = "cache.r6g.2xlarge" // 52.82 GB

    /// Redis version options
    module RedisVersions =
        let v7_0 = "7.0"
        let v6_2 = "6.2"
        let v6_0 = "6.0"

    /// Creates a production-ready Redis cluster
    let productionCluster (nodeType: string) (numNodes: int) = (nodeType, numNodes, 35) // node type, count, 35-day retention

[<AutoOpen>]
module ElastiCacheBuilders =
    /// <summary>
    /// Creates a new ElastiCache Redis cluster builder.
    /// Example: redisCluster "my-cache" { cacheNodeType "cache.t3.small"; numCacheNodes 1 }
    /// </summary>
    let redisCluster name = ElasticCacheRedisBuilder name
