namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DocDB
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.SecretsManager

/// <summary>
/// High-level Amazon DocumentDB (MongoDB-compatible) cluster builder following AWS best practices.
///
/// **Default Settings:**
/// - Engine = docdb
/// - Instance class = db.t3.medium
/// - Instances = 1 (single instance for dev)
/// - Port = 27017 (MongoDB default)
/// - Backup retention = 7 days
/// - Preferred backup window = 03:00-04:00 UTC
/// - Encryption at rest = enabled
/// - Deletion protection = disabled (for dev)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework:
/// - MongoDB compatibility for easy migration from Cosmos DB
/// - T3.medium suitable for development and testing
/// - Encryption enabled by default for security
/// - Single instance reduces costs for non-production
///
/// **Use Cases:**
/// - Document database applications
/// - Content management systems
/// - User profiles and catalogs
/// - Mobile and web applications
/// - Migration from MongoDB or Cosmos DB
///
/// **Escape Hatch:**
/// Access the underlying CDK DatabaseCluster via the `Cluster` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type DocumentDBClusterConfig =
    { ClusterName: string
      ConstructId: string option
      MasterUsername: string option
      MasterPassword: ISecret option
      InstanceType: string option
      Instances: int option
      Port: int option
      Vpc: VpcRef option
      VpcSubnets: SubnetSelection option
      SecurityGroup: SecurityGroupRef option
      BackupRetentionDays: int option
      PreferredBackupWindow: string option
      PreferredMaintenanceWindow: string option
      StorageEncrypted: bool option
      DeletionProtection: bool option
      RemovalPolicy: RemovalPolicy option
      Tags: (string * string) list }

type DocumentDBClusterResource =
    {
        ClusterName: string
        ConstructId: string
        /// The underlying CDK DatabaseCluster construct
        Cluster: DatabaseCluster
    }

    /// Gets the cluster endpoint
    member this.ClusterEndpoint = this.Cluster.ClusterEndpoint

    /// Gets the cluster read endpoint
    member this.ClusterReadEndpoint = this.Cluster.ClusterReadEndpoint

    /// Gets the connection string (without credentials)
    member this.ConnectionString =
        sprintf
            "mongodb://%s:%d"
            this.Cluster.ClusterEndpoint.Hostname
            (this.Cluster.ClusterEndpoint.Port |> float |> int)

type DocumentDBClusterBuilder(name: string) =
    member _.Yield _ : DocumentDBClusterConfig =
        { ClusterName = name
          ConstructId = None
          MasterUsername = Some "docdbadmin"
          MasterPassword = None
          InstanceType = Some "db.t3.medium"
          Instances = Some 1
          Port = Some 27017
          Vpc = None
          VpcSubnets = None
          SecurityGroup = None
          BackupRetentionDays = Some 7
          PreferredBackupWindow = Some "03:00-04:00"
          PreferredMaintenanceWindow = None
          StorageEncrypted = Some true
          DeletionProtection = Some false
          RemovalPolicy = Some RemovalPolicy.SNAPSHOT
          Tags = [] }

    member _.Zero() : DocumentDBClusterConfig =
        { ClusterName = name
          ConstructId = None
          MasterUsername = Some "docdbadmin"
          MasterPassword = None
          InstanceType = Some "db.t3.medium"
          Instances = Some 1
          Port = Some 27017
          Vpc = None
          VpcSubnets = None
          SecurityGroup = None
          BackupRetentionDays = Some 7
          PreferredBackupWindow = Some "03:00-04:00"
          PreferredMaintenanceWindow = None
          StorageEncrypted = Some true
          DeletionProtection = Some false
          RemovalPolicy = Some RemovalPolicy.SNAPSHOT
          Tags = [] }

    member _.Combine(state1: DocumentDBClusterConfig, state2: DocumentDBClusterConfig) : DocumentDBClusterConfig =
        { ClusterName = state2.ClusterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          MasterUsername = state2.MasterUsername |> Option.orElse state1.MasterUsername
          MasterPassword = state2.MasterPassword |> Option.orElse state1.MasterPassword
          InstanceType = state2.InstanceType |> Option.orElse state1.InstanceType
          Instances = state2.Instances |> Option.orElse state1.Instances
          Port = state2.Port |> Option.orElse state1.Port
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          VpcSubnets = state2.VpcSubnets |> Option.orElse state1.VpcSubnets
          SecurityGroup = state2.SecurityGroup |> Option.orElse state1.SecurityGroup
          BackupRetentionDays = state2.BackupRetentionDays |> Option.orElse state1.BackupRetentionDays
          PreferredBackupWindow = state2.PreferredBackupWindow |> Option.orElse state1.PreferredBackupWindow
          PreferredMaintenanceWindow =
            state2.PreferredMaintenanceWindow
            |> Option.orElse state1.PreferredMaintenanceWindow
          StorageEncrypted = state2.StorageEncrypted |> Option.orElse state1.StorageEncrypted
          DeletionProtection = state2.DeletionProtection |> Option.orElse state1.DeletionProtection
          RemovalPolicy = state2.RemovalPolicy |> Option.orElse state1.RemovalPolicy
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DocumentDBClusterConfig) : DocumentDBClusterConfig = f ()

    member inline x.For
        (
            config: DocumentDBClusterConfig,
            [<InlineIfLambda>] f: unit -> DocumentDBClusterConfig
        ) : DocumentDBClusterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: DocumentDBClusterConfig) : DocumentDBClusterResource =
        let clusterName = config.ClusterName
        let constructId = config.ConstructId |> Option.defaultValue clusterName

        let props = DatabaseClusterProps()

        match config.Vpc with
        | Some vpc -> props.Vpc <- VpcHelpers.resolveVpcRef vpc
        | None -> failwith "VPC is required for DocumentDB cluster"

        match config.MasterPassword with
        | Some secret ->
            let login = Login()
            login.Username <- config.MasterUsername |> Option.defaultValue "docdbadmin"
            login.Password <- secret.SecretValue
            props.MasterUser <- login
        | None -> failwith "MasterPassword (ISecret) is required for DocumentDB cluster"

        config.InstanceType
        |> Option.iter (fun v -> props.InstanceType <- InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM))

        config.Instances |> Option.iter (fun v -> props.Instances <- v)

        config.Port
        |> Option.iter (fun v -> props.Port <- System.Nullable<float>(float v))

        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)

        config.BackupRetentionDays
        |> Option.iter (fun v -> props.Backup <- BackupProps(Retention = Duration.Days(float v)))

        config.PreferredMaintenanceWindow
        |> Option.iter (fun v -> props.PreferredMaintenanceWindow <- v)

        config.StorageEncrypted |> Option.iter (fun v -> props.StorageEncrypted <- v)

        config.DeletionProtection
        |> Option.iter (fun v -> props.DeletionProtection <- v)

        config.RemovalPolicy |> Option.iter (fun v -> props.RemovalPolicy <- v)

        { ClusterName = clusterName
          ConstructId = constructId
          Cluster = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DocumentDBClusterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("masterUsername")>]
    member _.MasterUsername(config: DocumentDBClusterConfig, username: string) =
        { config with
            MasterUsername = Some username }

    [<CustomOperation("masterPassword")>]
    member _.MasterPassword(config: DocumentDBClusterConfig, password: ISecret) =
        { config with
            MasterPassword = Some password }

    [<CustomOperation("instanceType")>]
    member _.InstanceType(config: DocumentDBClusterConfig, instanceType: string) =
        { config with
            InstanceType = Some instanceType }

    [<CustomOperation("instances")>]
    member _.Instances(config: DocumentDBClusterConfig, count: int) = { config with Instances = Some count }

    [<CustomOperation("port")>]
    member _.Port(config: DocumentDBClusterConfig, port: int) = { config with Port = Some port }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: DocumentDBClusterConfig, vpcSpec: VpcSpec) =
        { config with
            Vpc = Some(VpcSpecRef vpcSpec) }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: DocumentDBClusterConfig, vpc: IVpc) =
        { config with
            Vpc = Some(VpcInterface vpc) }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: DocumentDBClusterConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: DocumentDBClusterConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroup = Some(SecurityGroupRef.SecurityGroupInterface sg) }

    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: DocumentDBClusterConfig, sg: SecurityGroupSpec) =
        { config with
            SecurityGroup = Some(SecurityGroupRef.SecurityGroupSpecRef sg) }

    [<CustomOperation("backupRetentionDays")>]
    member _.BackupRetentionDays(config: DocumentDBClusterConfig, days: int) =
        { config with
            BackupRetentionDays = Some days }

    [<CustomOperation("backupWindow")>]
    member _.BackupWindow(config: DocumentDBClusterConfig, window: string) =
        { config with
            PreferredBackupWindow = Some window }

    [<CustomOperation("maintenanceWindow")>]
    member _.MaintenanceWindow(config: DocumentDBClusterConfig, window: string) =
        { config with
            PreferredMaintenanceWindow = Some window }

    [<CustomOperation("storageEncrypted")>]
    member _.StorageEncrypted(config: DocumentDBClusterConfig, encrypted: bool) =
        { config with
            StorageEncrypted = Some encrypted }

    [<CustomOperation("deletionProtection")>]
    member _.DeletionProtection(config: DocumentDBClusterConfig, enabled: bool) =
        { config with
            DeletionProtection = Some enabled }

    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: DocumentDBClusterConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    [<CustomOperation("tag")>]
    member _.Tag(config: DocumentDBClusterConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: DocumentDBClusterConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// Helper functions for DocumentDB operations
module DocumentDBHelpers =

    /// Common instance types by use case
    module InstanceTypes =
        /// Development and testing
        let t3_medium = "db.t3.medium" // 2 vCPU, 4 GB RAM

        /// Production workloads
        let r5_large = "db.r5.large" // 2 vCPU, 16 GB RAM
        let r5_xlarge = "db.r5.xlarge" // 4 vCPU, 32 GB RAM
        let r5_2xlarge = "db.r5.2xlarge" // 8 vCPU, 64 GB RAM
        let r5_4xlarge = "db.r5.4xlarge" // 16 vCPU, 128 GB RAM

    /// Creates a production-ready DocumentDB cluster configuration
    let productionCluster (instanceType: string) (instanceCount: int) = (instanceType, instanceCount, 35, true) // type, count, 35-day retention, deletion protection

[<AutoOpen>]
module DocumentDBBuilders =
    /// <summary>
    /// Creates a new DocumentDB cluster builder.
    /// Example: documentDBCluster "my-docdb" { vpc myVpc; masterPassword mySecret; instances 3 }
    /// </summary>
    let documentDBCluster name = DocumentDBClusterBuilder name
