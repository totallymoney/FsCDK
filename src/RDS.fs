namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2

// ============================================================================
// RDS Database Configuration DSL
// ============================================================================

type DatabaseInstanceConfig =
    { DatabaseName: string
      ConstructId: string option
      Engine: IInstanceEngine option
      InstanceType: InstanceType option
      Vpc: IVpc option
      VpcSubnets: SubnetSelection option
      SecurityGroups: ISecurityGroup list
      AllocatedStorage: int option
      MaxAllocatedStorage: int option
      StorageType: StorageType option
      BackupRetention: Duration option
      DeleteAutomatedBackups: bool option
      RemovalPolicy: RemovalPolicy option
      DeletionProtection: bool option
      MultiAz: bool option
      PubliclyAccessible: bool option
      ParameterGroup: IParameterGroup option
      DatabaseName_: string option
      MasterUsername: string option
      Credentials: Credentials option
      PreferredBackupWindow: string option
      PreferredMaintenanceWindow: string option
      StorageEncrypted: bool option
      MonitoringInterval: Duration option
      EnablePerformanceInsights: bool option
      PerformanceInsightRetention: PerformanceInsightRetention option
      AutoMinorVersionUpgrade: bool option
      IamAuthentication: bool option
      CloudwatchLogsExports: string list option }

type DatabaseInstanceSpec =
    { DatabaseName: string
      ConstructId: string
      Props: DatabaseInstanceProps }

type DatabaseInstanceBuilder(name: string) =

    member _.Yield _ : DatabaseInstanceConfig =
        { DatabaseName = name
          ConstructId = None
          Engine = None
          InstanceType = None
          Vpc = None
          VpcSubnets = None
          SecurityGroups = []
          AllocatedStorage = None
          MaxAllocatedStorage = None
          StorageType = None
          BackupRetention = None
          DeleteAutomatedBackups = None
          RemovalPolicy = None
          DeletionProtection = Some true // Security: Prevent accidental deletion
          MultiAz = None
          PubliclyAccessible = Some false // Security: Never expose database to internet
          ParameterGroup = None
          DatabaseName_ = None
          MasterUsername = None
          Credentials = None
          PreferredBackupWindow = None
          PreferredMaintenanceWindow = None
          StorageEncrypted = Some true // Security: Always encrypt at rest
          MonitoringInterval = None
          EnablePerformanceInsights = None
          PerformanceInsightRetention = None
          AutoMinorVersionUpgrade = None
          IamAuthentication = Some true // Security: Use IAM authentication when possible
          CloudwatchLogsExports = None }

    member _.Zero() : DatabaseInstanceConfig =
        { DatabaseName = name
          ConstructId = None
          Engine = None
          InstanceType = None
          Vpc = None
          VpcSubnets = None
          SecurityGroups = []
          AllocatedStorage = None
          MaxAllocatedStorage = None
          StorageType = None
          BackupRetention = None
          DeleteAutomatedBackups = None
          RemovalPolicy = None
          DeletionProtection = Some true // Security: Prevent accidental deletion
          MultiAz = None
          PubliclyAccessible = Some false // Security: Never expose database to internet
          ParameterGroup = None
          DatabaseName_ = None
          MasterUsername = None
          Credentials = None
          PreferredBackupWindow = None
          PreferredMaintenanceWindow = None
          StorageEncrypted = Some true // Security: Always encrypt at rest
          MonitoringInterval = None
          EnablePerformanceInsights = None
          PerformanceInsightRetention = None
          AutoMinorVersionUpgrade = None
          IamAuthentication = Some true // Security: Use IAM authentication when possible
          CloudwatchLogsExports = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DatabaseInstanceConfig) : DatabaseInstanceConfig = f ()

    member inline x.For
        (
            config: DatabaseInstanceConfig,
            [<InlineIfLambda>] f: unit -> DatabaseInstanceConfig
        ) : DatabaseInstanceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: DatabaseInstanceConfig, b: DatabaseInstanceConfig) : DatabaseInstanceConfig =
        { DatabaseName = a.DatabaseName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Engine =
            match a.Engine with
            | Some _ -> a.Engine
            | None -> b.Engine
          InstanceType =
            match a.InstanceType with
            | Some _ -> a.InstanceType
            | None -> b.InstanceType
          Vpc =
            match a.Vpc with
            | Some _ -> a.Vpc
            | None -> b.Vpc
          VpcSubnets =
            match a.VpcSubnets with
            | Some _ -> a.VpcSubnets
            | None -> b.VpcSubnets
          SecurityGroups = a.SecurityGroups @ b.SecurityGroups
          AllocatedStorage =
            match a.AllocatedStorage with
            | Some _ -> a.AllocatedStorage
            | None -> b.AllocatedStorage
          MaxAllocatedStorage =
            match a.MaxAllocatedStorage with
            | Some _ -> a.MaxAllocatedStorage
            | None -> b.MaxAllocatedStorage
          StorageType =
            match a.StorageType with
            | Some _ -> a.StorageType
            | None -> b.StorageType
          BackupRetention =
            match a.BackupRetention with
            | Some _ -> a.BackupRetention
            | None -> b.BackupRetention
          DeleteAutomatedBackups =
            match a.DeleteAutomatedBackups with
            | Some _ -> a.DeleteAutomatedBackups
            | None -> b.DeleteAutomatedBackups
          RemovalPolicy =
            match a.RemovalPolicy with
            | Some _ -> a.RemovalPolicy
            | None -> b.RemovalPolicy
          DeletionProtection =
            match a.DeletionProtection with
            | Some _ -> a.DeletionProtection
            | None -> b.DeletionProtection
          MultiAz =
            match a.MultiAz with
            | Some _ -> a.MultiAz
            | None -> b.MultiAz
          PubliclyAccessible =
            match a.PubliclyAccessible with
            | Some _ -> a.PubliclyAccessible
            | None -> b.PubliclyAccessible
          ParameterGroup =
            match a.ParameterGroup with
            | Some _ -> a.ParameterGroup
            | None -> b.ParameterGroup
          DatabaseName_ =
            match a.DatabaseName_ with
            | Some _ -> a.DatabaseName_
            | None -> b.DatabaseName_
          MasterUsername =
            match a.MasterUsername with
            | Some _ -> a.MasterUsername
            | None -> b.MasterUsername
          Credentials =
            match a.Credentials with
            | Some _ -> a.Credentials
            | None -> b.Credentials
          PreferredBackupWindow =
            match a.PreferredBackupWindow with
            | Some _ -> a.PreferredBackupWindow
            | None -> b.PreferredBackupWindow
          PreferredMaintenanceWindow =
            match a.PreferredMaintenanceWindow with
            | Some _ -> a.PreferredMaintenanceWindow
            | None -> b.PreferredMaintenanceWindow
          StorageEncrypted =
            match a.StorageEncrypted with
            | Some _ -> a.StorageEncrypted
            | None -> b.StorageEncrypted
          MonitoringInterval =
            match a.MonitoringInterval with
            | Some _ -> a.MonitoringInterval
            | None -> b.MonitoringInterval
          EnablePerformanceInsights =
            match a.EnablePerformanceInsights with
            | Some _ -> a.EnablePerformanceInsights
            | None -> b.EnablePerformanceInsights
          PerformanceInsightRetention =
            match a.PerformanceInsightRetention with
            | Some _ -> a.PerformanceInsightRetention
            | None -> b.PerformanceInsightRetention
          AutoMinorVersionUpgrade =
            match a.AutoMinorVersionUpgrade with
            | Some _ -> a.AutoMinorVersionUpgrade
            | None -> b.AutoMinorVersionUpgrade
          IamAuthentication =
            match a.IamAuthentication with
            | Some _ -> a.IamAuthentication
            | None -> b.IamAuthentication
          CloudwatchLogsExports =
            match a.CloudwatchLogsExports with
            | Some _ -> a.CloudwatchLogsExports
            | None -> b.CloudwatchLogsExports }

    member _.Run(config: DatabaseInstanceConfig) : DatabaseInstanceSpec =
        let props = DatabaseInstanceProps()
        let constructId = config.ConstructId |> Option.defaultValue config.DatabaseName

        // VPC is required
        props.Vpc <-
            match config.Vpc with
            | Some vpc -> vpc
            | None -> invalidArg "vpc" "VPC is required for RDS Database Instance"

        props.Engine <-
            match config.Engine with
            | Some engine -> engine
            | None -> invalidArg "engine" "Database engine is required for RDS Database Instance"

        // AWS Best Practice: Default to t3.micro for cost optimization in dev/test
        // Users should explicitly choose larger instances for production
        props.InstanceType <-
            config.InstanceType
            |> Option.defaultValue (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))

        // AWS Best Practice: Enable automated backups with 7-day retention
        props.BackupRetention <- config.BackupRetention |> Option.defaultValue (Duration.Days(7.0))

        // AWS Best Practice: Delete automated backups when instance is deleted
        props.DeleteAutomatedBackups <- config.DeleteAutomatedBackups |> Option.defaultValue true

        // AWS Best Practice: Enable Multi-AZ for production databases
        // Default to false for cost optimization in dev/test (can be overridden)
        props.MultiAz <- config.MultiAz |> Option.defaultValue false

        // AWS Security Best Practice: Never expose databases to internet (set in Yield)
        props.PubliclyAccessible <- config.PubliclyAccessible |> Option.defaultValue false

        // AWS Security Best Practice: Always encrypt at rest (set in Yield)
        props.StorageEncrypted <- config.StorageEncrypted |> Option.defaultValue true

        // AWS Security Best Practice: Prevent accidental deletion (set in Yield)
        // Note: Set to true by default. Override with `deletionProtection false` only for dev/test
        props.DeletionProtection <- config.DeletionProtection |> Option.defaultValue true

        // AWS Best Practice: Enable auto minor version upgrades
        props.AutoMinorVersionUpgrade <- config.AutoMinorVersionUpgrade |> Option.defaultValue true

        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.AllocatedStorage
        |> Option.iter (fun s -> props.AllocatedStorage <- float s)

        config.MaxAllocatedStorage
        |> Option.iter (fun s -> props.MaxAllocatedStorage <- float s)

        config.StorageType |> Option.iter (fun t -> props.StorageType <- t)
        config.RemovalPolicy |> Option.iter (fun r -> props.RemovalPolicy <- r)
        config.ParameterGroup |> Option.iter (fun p -> props.ParameterGroup <- p)
        config.DatabaseName_ |> Option.iter (fun d -> props.DatabaseName <- d)
        config.Credentials |> Option.iter (fun c -> props.Credentials <- c)

        config.PreferredBackupWindow
        |> Option.iter (fun w -> props.PreferredBackupWindow <- w)

        config.PreferredMaintenanceWindow
        |> Option.iter (fun w -> props.PreferredMaintenanceWindow <- w)

        config.MonitoringInterval
        |> Option.iter (fun i -> props.MonitoringInterval <- i)

        config.EnablePerformanceInsights
        |> Option.iter (fun e -> props.EnablePerformanceInsights <- e)

        config.PerformanceInsightRetention
        |> Option.iter (fun r -> props.PerformanceInsightRetention <- r)

        // AWS Security Best Practice: Use IAM authentication when possible (set in Yield)
        props.IamAuthentication <- config.IamAuthentication |> Option.defaultValue true

        // AWS Security Best Practice: Export logs to CloudWatch for audit trail
        config.CloudwatchLogsExports
        |> Option.iter (fun logs ->
            if not (List.isEmpty logs) then
                props.CloudwatchLogsExports <- logs |> List.toArray)

        { DatabaseName = config.DatabaseName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the database instance.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DatabaseInstanceConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the database engine.</summary>
    [<CustomOperation("engine")>]
    member _.Engine(config: DatabaseInstanceConfig, engine: IInstanceEngine) = { config with Engine = Some engine }

    /// <summary>Sets PostgreSQL as the database engine with a specific version.</summary>
    [<CustomOperation("postgresEngine")>]
    member _.PostgresEngine(config: DatabaseInstanceConfig, ?version: PostgresEngineVersion) =
        let pgVersion = version |> Option.defaultValue PostgresEngineVersion.VER_15

        { config with
            Engine = Some(DatabaseInstanceEngine.Postgres(PostgresInstanceEngineProps(Version = pgVersion))) }

    /// <summary>Sets the instance type.</summary>
    [<CustomOperation("instanceType")>]
    member _.InstanceType(config: DatabaseInstanceConfig, instanceType: InstanceType) =
        { config with
            InstanceType = Some instanceType }

    /// <summary>Sets the VPC.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: DatabaseInstanceConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Sets the VPC subnets.</summary>
    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: DatabaseInstanceConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    /// <summary>Adds a security group.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: DatabaseInstanceConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroups = sg :: config.SecurityGroups }

    /// <summary>Sets the allocated storage in GB.</summary>
    [<CustomOperation("allocatedStorage")>]
    member _.AllocatedStorage(config: DatabaseInstanceConfig, gb: int) =
        { config with
            AllocatedStorage = Some gb }

    /// <summary>Sets the maximum allocated storage in GB for autoscaling.</summary>
    [<CustomOperation("maxAllocatedStorage")>]
    member _.MaxAllocatedStorage(config: DatabaseInstanceConfig, gb: int) =
        { config with
            MaxAllocatedStorage = Some gb }

    /// <summary>Sets the storage type.</summary>
    [<CustomOperation("storageType")>]
    member _.StorageType(config: DatabaseInstanceConfig, storageType: StorageType) =
        { config with
            StorageType = Some storageType }

    /// <summary>Sets the backup retention period in days.</summary>
    [<CustomOperation("backupRetentionDays")>]
    member _.BackupRetentionDays(config: DatabaseInstanceConfig, days: float) =
        { config with
            BackupRetention = Some(Duration.Days(days)) }

    /// <summary>Sets whether to delete automated backups.</summary>
    [<CustomOperation("deleteAutomatedBackups")>]
    member _.DeleteAutomatedBackups(config: DatabaseInstanceConfig, delete: bool) =
        { config with
            DeleteAutomatedBackups = Some delete }

    /// <summary>Sets the removal policy.</summary>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: DatabaseInstanceConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

    /// <summary>Enables or disables deletion protection.</summary>
    [<CustomOperation("deletionProtection")>]
    member _.DeletionProtection(config: DatabaseInstanceConfig, enabled: bool) =
        { config with
            DeletionProtection = Some enabled }

    /// <summary>Enables or disables Multi-AZ deployment.</summary>
    [<CustomOperation("multiAz")>]
    member _.MultiAz(config: DatabaseInstanceConfig, enabled: bool) = { config with MultiAz = Some enabled }

    /// <summary>Sets whether the database is publicly accessible.</summary>
    [<CustomOperation("publiclyAccessible")>]
    member _.PubliclyAccessible(config: DatabaseInstanceConfig, accessible: bool) =
        { config with
            PubliclyAccessible = Some accessible }

    /// <summary>Sets the database name.</summary>
    [<CustomOperation("databaseName")>]
    member _.DatabaseName(config: DatabaseInstanceConfig, dbName: string) =
        { config with
            DatabaseName_ = Some dbName }

    /// <summary>Sets the master username (note: credentials typically encapsulate username/password/secret).</summary>
    [<CustomOperation("masterUsername")>]
    member _.MasterUsername(config: DatabaseInstanceConfig, username: string) =
        { config with
            MasterUsername = Some username }

    /// <summary>Sets the credentials.</summary>
    [<CustomOperation("credentials")>]
    member _.Credentials(config: DatabaseInstanceConfig, credentials: Credentials) =
        { config with
            Credentials = Some credentials }

    /// <summary>Sets the preferred backup window.</summary>
    [<CustomOperation("preferredBackupWindow")>]
    member _.PreferredBackupWindow(config: DatabaseInstanceConfig, window: string) =
        { config with
            PreferredBackupWindow = Some window }

    /// <summary>Sets the preferred maintenance window.</summary>
    [<CustomOperation("preferredMaintenanceWindow")>]
    member _.PreferredMaintenanceWindow(config: DatabaseInstanceConfig, window: string) =
        { config with
            PreferredMaintenanceWindow = Some window }

    /// <summary>Enables storage encryption.</summary>
    [<CustomOperation("storageEncrypted")>]
    member _.StorageEncrypted(config: DatabaseInstanceConfig, encrypted: bool) =
        { config with
            StorageEncrypted = Some encrypted }

    /// <summary>Sets the CloudWatch monitoring interval.</summary>
    [<CustomOperation("monitoringInterval")>]
    member _.MonitoringInterval(config: DatabaseInstanceConfig, interval: Duration) =
        { config with
            MonitoringInterval = Some interval }

    /// <summary>Enables performance insights.</summary>
    [<CustomOperation("enablePerformanceInsights")>]
    member _.EnablePerformanceInsights(config: DatabaseInstanceConfig, enabled: bool) =
        { config with
            EnablePerformanceInsights = Some enabled }

    /// <summary>Sets performance insights retention.</summary>
    [<CustomOperation("performanceInsightRetention")>]
    member _.PerformanceInsightRetention(config: DatabaseInstanceConfig, retention: PerformanceInsightRetention) =
        { config with
            PerformanceInsightRetention = Some retention }

    /// <summary>Enables or disables auto minor version upgrades.</summary>
    [<CustomOperation("autoMinorVersionUpgrade")>]
    member _.AutoMinorVersionUpgrade(config: DatabaseInstanceConfig, enabled: bool) =
        { config with
            AutoMinorVersionUpgrade = Some enabled }

    /// <summary>Enables IAM authentication.</summary>
    [<CustomOperation("iamAuthentication")>]
    member _.IamAuthentication(config: DatabaseInstanceConfig, enabled: bool) =
        { config with
            IamAuthentication = Some enabled }

    /// <summary>
    /// Enables CloudWatch Logs export for database audit and error logs.
    ///
    /// **Security Best Practice:** Export logs to CloudWatch for:
    /// - Audit trails and compliance requirements
    /// - Security incident investigation
    /// - Performance troubleshooting
    /// - Anomaly detection
    ///
    /// **Log Types by Engine:**
    /// - PostgreSQL: ["postgresql", "upgrade"]
    /// - MySQL: ["error", "general", "slowquery", "audit"]
    /// - MariaDB: ["error", "general", "slowquery", "audit"]
    /// - Oracle: ["alert", "audit", "trace", "listener"]
    /// - SQL Server: ["error", "agent"]
    ///
    /// **Default:** None (opt-in for cost considerations)
    /// </summary>
    /// <param name="logTypes">List of log types to export (engine-specific).</param>
    /// <code lang="fsharp">
    /// rdsInstance "ProductionDB" {
    ///     postgresEngine
    ///     cloudwatchLogsExports ["postgresql", "upgrade"]  // PostgreSQL logs
    /// }
    ///
    /// rdsInstance "MySQLDB" {
    ///     engine mySqlEngine
    ///     cloudwatchLogsExports ["error", "slowquery"]  // MySQL logs
    /// }
    /// </code>
    [<CustomOperation("cloudwatchLogsExports")>]
    member _.CloudwatchLogsExports(config: DatabaseInstanceConfig, logTypes: string list) =
        { config with
            CloudwatchLogsExports = Some logTypes }

// ============================================================================
// RDS Proxy Configuration DSL
// ============================================================================

open Amazon.CDK.AWS.SecretsManager

/// <summary>
/// High-level RDS Proxy builder following AWS best practices.
///
/// **Default Settings:**
/// - Debug logging = false (opt-in for troubleshooting)
/// - IAM authentication = true (recommended for security)
/// - Require TLS = true (enforce encrypted connections)
/// - Idle timeout = 30 minutes
///
/// **Rationale:**
/// RDS Proxy provides connection pooling and improves failover times.
/// IAM authentication removes the need for password management.
/// </summary>
type DatabaseProxyConfig =
    { ProxyName: string
      ConstructId: string option
      Vpc: IVpc option
      VpcSubnets: SubnetSelection option
      SecurityGroups: ISecurityGroup list
      Secrets: ISecret list
      DbProxyName: string option
      BorrowTimeout: Duration option
      DebugLogging: bool option
      IamAuth: bool option
      IdleClientTimeout: Duration option
      MaxConnectionsPercent: int option
      MaxIdleConnectionsPercent: int option
      RequireTLS: bool option
      ProxyTarget: DatabaseProxyProxyTargetConfig option }

and DatabaseProxyProxyTargetConfig =
    | InstanceProxyTarget of IDatabaseInstance
    | ClusterProxyTarget of IDatabaseCluster

type DatabaseProxySpec =
    { ProxyName: string
      ConstructId: string
      Props: DatabaseProxyProps
      mutable DatabaseProxy: IDatabaseProxy option }

    /// Gets the underlying IDatabaseProxy resource. Must be called after the stack is built.
    member this.Resource =
        match this.DatabaseProxy with
        | Some proxy -> proxy
        | None ->
            failwith
                $"DatabaseProxy '{this.ProxyName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type DatabaseProxyBuilder(name: string) =

    member _.Yield _ : DatabaseProxyConfig =
        { ProxyName = name
          ConstructId = None
          Vpc = None
          VpcSubnets = None
          SecurityGroups = []
          Secrets = []
          DbProxyName = None
          BorrowTimeout = None
          DebugLogging = Some false
          IamAuth = Some true
          IdleClientTimeout = Some(Duration.Minutes(30.0))
          MaxConnectionsPercent = None
          MaxIdleConnectionsPercent = None
          RequireTLS = Some true
          ProxyTarget = None }

    member _.Zero() : DatabaseProxyConfig =
        { ProxyName = name
          ConstructId = None
          Vpc = None
          VpcSubnets = None
          SecurityGroups = []
          Secrets = []
          DbProxyName = None
          BorrowTimeout = None
          DebugLogging = Some false
          IamAuth = Some true
          IdleClientTimeout = Some(Duration.Minutes(30.0))
          MaxConnectionsPercent = None
          MaxIdleConnectionsPercent = None
          RequireTLS = Some true
          ProxyTarget = None }

    member _.Combine(state1: DatabaseProxyConfig, state2: DatabaseProxyConfig) : DatabaseProxyConfig =
        { ProxyName = state2.ProxyName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          VpcSubnets = state2.VpcSubnets |> Option.orElse state1.VpcSubnets
          SecurityGroups = state1.SecurityGroups @ state2.SecurityGroups
          Secrets =
            if state2.Secrets.IsEmpty then
                state1.Secrets
            else
                state2.Secrets @ state1.Secrets
          DbProxyName = state2.DbProxyName |> Option.orElse state1.DbProxyName
          BorrowTimeout = state2.BorrowTimeout |> Option.orElse state1.BorrowTimeout
          DebugLogging = state2.DebugLogging |> Option.orElse state1.DebugLogging
          IamAuth = state2.IamAuth |> Option.orElse state1.IamAuth
          IdleClientTimeout = state2.IdleClientTimeout |> Option.orElse state1.IdleClientTimeout
          MaxConnectionsPercent = state2.MaxConnectionsPercent |> Option.orElse state1.MaxConnectionsPercent
          MaxIdleConnectionsPercent =
            state2.MaxIdleConnectionsPercent
            |> Option.orElse state1.MaxIdleConnectionsPercent
          RequireTLS = state2.RequireTLS |> Option.orElse state1.RequireTLS
          ProxyTarget = state2.ProxyTarget |> Option.orElse state1.ProxyTarget }

    member inline _.Delay([<InlineIfLambda>] f: unit -> DatabaseProxyConfig) : DatabaseProxyConfig = f ()

    member inline x.For
        (
            config: DatabaseProxyConfig,
            [<InlineIfLambda>] f: unit -> DatabaseProxyConfig
        ) : DatabaseProxyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: DatabaseProxyConfig) : DatabaseProxySpec =
        let proxyName = config.ProxyName
        let constructId = config.ConstructId |> Option.defaultValue proxyName

        let props = DatabaseProxyProps()

        match config.Vpc with
        | Some vpc -> props.Vpc <- vpc
        | None -> invalidArg "vpc" "VPC is required for Database Proxy"

        match config.ProxyTarget with
        | Some target ->
            match target with
            | InstanceProxyTarget instance ->
                let instanceTarget = ProxyTarget.FromInstance(instance)
                props.ProxyTarget <- instanceTarget
            | ClusterProxyTarget cluster ->
                let clusterTarget = ProxyTarget.FromCluster(cluster)
                props.ProxyTarget <- clusterTarget
        | None -> invalidArg "proxyTarget" "Proxy target (instance or cluster) is required"

        if config.Secrets.IsEmpty then
            invalidArg "secrets" "At least one secret is required for Database Proxy"
        else
            props.Secrets <- Array.ofList config.Secrets

        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)

        if not config.SecurityGroups.IsEmpty then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.DbProxyName |> Option.iter (fun v -> props.DbProxyName <- v)
        config.BorrowTimeout |> Option.iter (fun v -> props.BorrowTimeout <- v)
        config.DebugLogging |> Option.iter (fun v -> props.DebugLogging <- v)
        config.IamAuth |> Option.iter (fun v -> props.IamAuth <- v)
        config.IdleClientTimeout |> Option.iter (fun v -> props.IdleClientTimeout <- v)

        config.MaxConnectionsPercent
        |> Option.iter (fun v -> props.MaxConnectionsPercent <- System.Nullable<float>(float v))

        config.MaxIdleConnectionsPercent
        |> Option.iter (fun v -> props.MaxIdleConnectionsPercent <- System.Nullable<float>(float v))

        config.RequireTLS |> Option.iter (fun v -> props.RequireTLS <- v)
        // Note: RoleArn is automatically created by CDK, not set manually

        { ProxyName = proxyName
          ConstructId = constructId
          Props = props
          DatabaseProxy = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DatabaseProxyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the VPC for the proxy.</summary>
    [<CustomOperation("vpc")>]
    member _.Vpc(config: DatabaseProxyConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    /// <summary>Sets the VPC subnets for the proxy.</summary>
    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: DatabaseProxyConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

    /// <summary>Adds a security group to the proxy.</summary>
    [<CustomOperation("securityGroup")>]
    member _.SecurityGroup(config: DatabaseProxyConfig, sg: ISecurityGroup) =
        { config with
            SecurityGroups = sg :: config.SecurityGroups }

    /// <summary>Adds multiple security groups to the proxy.</summary>
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: DatabaseProxyConfig, sgs: ISecurityGroup list) =
        { config with
            SecurityGroups = sgs @ config.SecurityGroups }

    /// <summary>Adds a secret for database credentials.</summary>
    [<CustomOperation("secret")>]
    member _.Secret(config: DatabaseProxyConfig, secret: ISecret) =
        { config with
            Secrets = secret :: config.Secrets }

    /// <summary>Adds multiple secrets for database credentials.</summary>
    [<CustomOperation("secrets")>]
    member _.Secrets(config: DatabaseProxyConfig, secrets: ISecret list) =
        { config with
            Secrets = secrets @ config.Secrets }

    /// <summary>Sets the DB proxy name.</summary>
    [<CustomOperation("dbProxyName")>]
    member _.DbProxyName(config: DatabaseProxyConfig, name: string) = { config with DbProxyName = Some name }

    /// <summary>Sets the maximum time a connection can be borrowed before being returned.</summary>
    [<CustomOperation("borrowTimeout")>]
    member _.BorrowTimeout(config: DatabaseProxyConfig, timeout: Duration) =
        { config with
            BorrowTimeout = Some timeout }

    /// <summary>Enables or disables debug logging.</summary>
    [<CustomOperation("debugLogging")>]
    member _.DebugLogging(config: DatabaseProxyConfig, enabled: bool) =
        { config with
            DebugLogging = Some enabled }

    /// <summary>Enables or disables IAM authentication.</summary>
    [<CustomOperation("iamAuth")>]
    member _.IamAuth(config: DatabaseProxyConfig, enabled: bool) = { config with IamAuth = Some enabled }

    /// <summary>Sets the idle client timeout.</summary>
    [<CustomOperation("idleClientTimeout")>]
    member _.IdleClientTimeout(config: DatabaseProxyConfig, timeout: Duration) =
        { config with
            IdleClientTimeout = Some timeout }

    /// <summary>Sets the maximum percentage of database connections to use.</summary>
    [<CustomOperation("maxConnectionsPercent")>]
    member _.MaxConnectionsPercent(config: DatabaseProxyConfig, percent: int) =
        { config with
            MaxConnectionsPercent = Some percent }

    /// <summary>Sets the maximum percentage of idle connections.</summary>
    [<CustomOperation("maxIdleConnectionsPercent")>]
    member _.MaxIdleConnectionsPercent(config: DatabaseProxyConfig, percent: int) =
        { config with
            MaxIdleConnectionsPercent = Some percent }

    /// <summary>Requires TLS for connections.</summary>
    [<CustomOperation("requireTLS")>]
    member _.RequireTLS(config: DatabaseProxyConfig, require: bool) =
        { config with
            RequireTLS = Some require }

    /// <summary>Sets the proxy target to a database instance.</summary>
    [<CustomOperation("proxyTarget")>]
    member _.ProxyTargetInstance(config: DatabaseProxyConfig, instance: IDatabaseInstance) =
        { config with
            ProxyTarget = Some(InstanceProxyTarget instance) }

    /// <summary>Sets the proxy target to a database cluster.</summary>
    [<CustomOperation("proxyTarget")>]
    member _.ProxyTargetCluster(config: DatabaseProxyConfig, cluster: IDatabaseCluster) =
        { config with
            ProxyTarget = Some(ClusterProxyTarget cluster) }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module RdsBuilders =
    /// <summary>Creates an RDS Database Instance with AWS best practices.</summary>
    /// <param name="name">The database instance name.</param>
    /// <code lang="fsharp">
    /// rdsInstance "MyDatabase" {
    ///     vpc myVpc
    ///     postgresEngine PostgresEngineVersion.VER_15
    ///     instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
    ///     multiAz true
    ///     backupRetentionDays 7.0
    /// }
    /// </code>
    let rdsInstance (name: string) = DatabaseInstanceBuilder(name)

    /// <summary>Creates an RDS Proxy with AWS best practices.</summary>
    /// <param name="name">The proxy name.</param>
    /// <code lang="fsharp">
    /// rdsProxy "MyProxy" {
    ///     vpc myVpc
    ///     proxyTarget dbInstance
    ///     secrets [ dbSecret ]
    ///     iamAuth true
    /// }
    /// </code>
    let rdsProxy (name: string) = DatabaseProxyBuilder(name)
