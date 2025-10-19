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
      IamAuthentication: bool option }

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
          StorageType = None
          BackupRetention = None
          DeleteAutomatedBackups = None
          RemovalPolicy = None
          DeletionProtection = None
          MultiAz = None
          PubliclyAccessible = None
          ParameterGroup = None
          DatabaseName_ = None
          MasterUsername = None
          Credentials = None
          PreferredBackupWindow = None
          PreferredMaintenanceWindow = None
          StorageEncrypted = None
          MonitoringInterval = None
          EnablePerformanceInsights = None
          PerformanceInsightRetention = None
          AutoMinorVersionUpgrade = None
          IamAuthentication = None }

    member _.Zero() : DatabaseInstanceConfig =
        { DatabaseName = name
          ConstructId = None
          Engine = None
          InstanceType = None
          Vpc = None
          VpcSubnets = None
          SecurityGroups = []
          AllocatedStorage = None
          StorageType = None
          BackupRetention = None
          DeleteAutomatedBackups = None
          RemovalPolicy = None
          DeletionProtection = None
          MultiAz = None
          PubliclyAccessible = None
          ParameterGroup = None
          DatabaseName_ = None
          MasterUsername = None
          Credentials = None
          PreferredBackupWindow = None
          PreferredMaintenanceWindow = None
          StorageEncrypted = None
          MonitoringInterval = None
          EnablePerformanceInsights = None
          PerformanceInsightRetention = None
          AutoMinorVersionUpgrade = None
          IamAuthentication = None }

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
            | None -> b.IamAuthentication }

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
        // Default to false for cost optimization in dev/test
        props.MultiAz <- config.MultiAz |> Option.defaultValue false

        // AWS Best Practice: Do not make databases publicly accessible
        props.PubliclyAccessible <- config.PubliclyAccessible |> Option.defaultValue false

        // AWS Best Practice: Enable storage encryption by default
        props.StorageEncrypted <- config.StorageEncrypted |> Option.defaultValue true

        // AWS Best Practice: Enable deletion protection for production
        // Default to false for flexibility in dev/test
        props.DeletionProtection <- config.DeletionProtection |> Option.defaultValue false

        // AWS Best Practice: Enable auto minor version upgrades
        props.AutoMinorVersionUpgrade <- config.AutoMinorVersionUpgrade |> Option.defaultValue true

        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.AllocatedStorage
        |> Option.iter (fun s -> props.AllocatedStorage <- float s)

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

        config.IamAuthentication |> Option.iter (fun i -> props.IamAuthentication <- i)

        { DatabaseName = config.DatabaseName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the database instance.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DatabaseInstanceConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the database engine.</summary>
    [<CustomOperation("engine")>]
    member _.Engine(config: DatabaseInstanceConfig, engine: IInstanceEngine) = { config with Engine = Some engine }

    /// <summary>Sets PostgreSQL as the database engine with specific version.</summary>
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
    member _.Vpc(config: DatabaseInstanceConfig, vpc: IVpc) = { config with Vpc = Some vpc }

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
