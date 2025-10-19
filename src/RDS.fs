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

    member inline x.For(config: DatabaseInstanceConfig, [<InlineIfLambda>] f: unit -> DatabaseInstanceConfig) : DatabaseInstanceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: DatabaseInstanceConfig, state2: DatabaseInstanceConfig) : DatabaseInstanceConfig =
        { DatabaseName = state1.DatabaseName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          Engine =
            if state1.Engine.IsSome then
                state1.Engine
            else
                state2.Engine
          InstanceType =
            if state1.InstanceType.IsSome then
                state1.InstanceType
            else
                state2.InstanceType
          Vpc = if state1.Vpc.IsSome then state1.Vpc else state2.Vpc
          VpcSubnets =
            if state1.VpcSubnets.IsSome then
                state1.VpcSubnets
            else
                state2.VpcSubnets
          SecurityGroups = state1.SecurityGroups @ state2.SecurityGroups
          AllocatedStorage =
            if state1.AllocatedStorage.IsSome then
                state1.AllocatedStorage
            else
                state2.AllocatedStorage
          StorageType =
            if state1.StorageType.IsSome then
                state1.StorageType
            else
                state2.StorageType
          BackupRetention =
            if state1.BackupRetention.IsSome then
                state1.BackupRetention
            else
                state2.BackupRetention
          DeleteAutomatedBackups =
            if state1.DeleteAutomatedBackups.IsSome then
                state1.DeleteAutomatedBackups
            else
                state2.DeleteAutomatedBackups
          RemovalPolicy =
            if state1.RemovalPolicy.IsSome then
                state1.RemovalPolicy
            else
                state2.RemovalPolicy
          DeletionProtection =
            if state1.DeletionProtection.IsSome then
                state1.DeletionProtection
            else
                state2.DeletionProtection
          MultiAz =
            if state1.MultiAz.IsSome then
                state1.MultiAz
            else
                state2.MultiAz
          PubliclyAccessible =
            if state1.PubliclyAccessible.IsSome then
                state1.PubliclyAccessible
            else
                state2.PubliclyAccessible
          ParameterGroup =
            if state1.ParameterGroup.IsSome then
                state1.ParameterGroup
            else
                state2.ParameterGroup
          DatabaseName_ =
            if state1.DatabaseName_.IsSome then
                state1.DatabaseName_
            else
                state2.DatabaseName_
          MasterUsername =
            if state1.MasterUsername.IsSome then
                state1.MasterUsername
            else
                state2.MasterUsername
          Credentials =
            if state1.Credentials.IsSome then
                state1.Credentials
            else
                state2.Credentials
          PreferredBackupWindow =
            if state1.PreferredBackupWindow.IsSome then
                state1.PreferredBackupWindow
            else
                state2.PreferredBackupWindow
          PreferredMaintenanceWindow =
            if state1.PreferredMaintenanceWindow.IsSome then
                state1.PreferredMaintenanceWindow
            else
                state2.PreferredMaintenanceWindow
          StorageEncrypted =
            if state1.StorageEncrypted.IsSome then
                state1.StorageEncrypted
            else
                state2.StorageEncrypted
          MonitoringInterval =
            if state1.MonitoringInterval.IsSome then
                state1.MonitoringInterval
            else
                state2.MonitoringInterval
          EnablePerformanceInsights =
            if state1.EnablePerformanceInsights.IsSome then
                state1.EnablePerformanceInsights
            else
                state2.EnablePerformanceInsights
          PerformanceInsightRetention =
            if state1.PerformanceInsightRetention.IsSome then
                state1.PerformanceInsightRetention
            else
                state2.PerformanceInsightRetention
          AutoMinorVersionUpgrade =
            if state1.AutoMinorVersionUpgrade.IsSome then
                state1.AutoMinorVersionUpgrade
            else
                state2.AutoMinorVersionUpgrade
          IamAuthentication =
            if state1.IamAuthentication.IsSome then
                state1.IamAuthentication
            else
                state2.IamAuthentication }

    member _.Run(config: DatabaseInstanceConfig) : DatabaseInstanceSpec =
        let props = DatabaseInstanceProps()
        let constructId = config.ConstructId |> Option.defaultValue config.DatabaseName

        // VPC is required
        props.Vpc <-
            match config.Vpc with
            | Some vpc -> vpc
            | None -> failwith "VPC is required for RDS Database Instance"

        // Engine is required
        props.Engine <-
            match config.Engine with
            | Some engine -> engine
            | None -> failwith "Database engine is required for RDS Database Instance"

        // AWS Best Practice: Default to t3.micro for cost optimization in dev/test
        // Users should explicitly choose larger instances for production
        props.InstanceType <-
            config.InstanceType
            |> Option.defaultValue (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))

        // AWS Best Practice: Enable automated backups with 7-day retention
        props.BackupRetention <-
            config.BackupRetention |> Option.defaultValue (Duration.Days(7.0))

        // AWS Best Practice: Delete automated backups when instance is deleted
        props.DeleteAutomatedBackups <-
            config.DeleteAutomatedBackups |> Option.defaultValue true

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
        props.AutoMinorVersionUpgrade <-
            config.AutoMinorVersionUpgrade |> Option.defaultValue true

        config.VpcSubnets |> Option.iter (fun s -> props.VpcSubnets <- s)

        if not (List.isEmpty config.SecurityGroups) then
            props.SecurityGroups <- config.SecurityGroups |> List.toArray

        config.AllocatedStorage
        |> Option.iter (fun s -> props.AllocatedStorage <- float s)

        config.StorageType |> Option.iter (fun t -> props.StorageType <- t)

        config.RemovalPolicy
        |> Option.iter (fun r -> props.RemovalPolicy <- r)

        config.ParameterGroup
        |> Option.iter (fun p -> props.ParameterGroup <- p)

        config.DatabaseName_
        |> Option.iter (fun d -> props.DatabaseName <- d)

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

        config.IamAuthentication
        |> Option.iter (fun i -> props.IamAuthentication <- i)

        { DatabaseName = config.DatabaseName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the database instance.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: DatabaseInstanceConfig, id: string) =
        { config with ConstructId = Some id }

    /// <summary>Sets the database engine.</summary>
    [<CustomOperation("engine")>]
    member _.Engine(config: DatabaseInstanceConfig, engine: IInstanceEngine) =
        { config with Engine = Some engine }

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
    member _.MultiAz(config: DatabaseInstanceConfig, enabled: bool) =
        { config with MultiAz = Some enabled }

    /// <summary>Sets whether the database is publicly accessible.</summary>
    [<CustomOperation("publiclyAccessible")>]
    member _.PubliclyAccessible(config: DatabaseInstanceConfig, accessible: bool) =
        { config with
            PubliclyAccessible = Some accessible }

    /// <summary>Sets the database name.</summary>
    [<CustomOperation("databaseName")>]
    member _.DatabaseName(config: DatabaseInstanceConfig, dbName: string) =
        { config with DatabaseName_ = Some dbName }

    /// <summary>Sets the master username.</summary>
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

    /// <summary>Enables performance insights.</summary>
    [<CustomOperation("enablePerformanceInsights")>]
    member _.EnablePerformanceInsights(config: DatabaseInstanceConfig, enabled: bool) =
        { config with
            EnablePerformanceInsights = Some enabled }

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
