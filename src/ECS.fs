namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ECS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM

/// <summary>
/// High-level ECS Cluster builder following AWS best practices.
///
/// **Default Settings:**
/// - Container Insights = ENABLED (monitoring and observability)
/// - Execute command logging = enabled (for debugging)
///
/// **Rationale:**
/// Container Insights provides metrics and logs for troubleshooting.
/// Use ContainerInsights.ENHANCED for more detailed monitoring.
/// </summary>
type ECSClusterConfig =
    { ClusterName: string
      ConstructId: string option
      Vpc: IVpc option
      ContainerInsights: ContainerInsights option
      EnableFargateCapacityProviders: bool option }

type ECSClusterResource =
    {
        ClusterName: string
        ConstructId: string
        /// The underlying CDK Cluster construct
        Cluster: Cluster
    }

type ECSClusterBuilder(name: string) =
    member _.Yield _ : ECSClusterConfig =
        { ClusterName = name
          ConstructId = None
          Vpc = None
          ContainerInsights = Some ContainerInsights.ENABLED
          EnableFargateCapacityProviders = Some true }

    member _.Zero() : ECSClusterConfig =
        { ClusterName = name
          ConstructId = None
          Vpc = None
          ContainerInsights = Some ContainerInsights.ENABLED
          EnableFargateCapacityProviders = Some true }

    member _.Combine(state1: ECSClusterConfig, state2: ECSClusterConfig) : ECSClusterConfig =
        { ClusterName = state2.ClusterName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Vpc = state2.Vpc |> Option.orElse state1.Vpc
          ContainerInsights = state2.ContainerInsights |> Option.orElse state1.ContainerInsights
          EnableFargateCapacityProviders =
            state2.EnableFargateCapacityProviders
            |> Option.orElse state1.EnableFargateCapacityProviders }

    member inline x.For(config: ECSClusterConfig, [<InlineIfLambda>] f: unit -> ECSClusterConfig) : ECSClusterConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ECSClusterConfig) : ECSClusterResource =
        let clusterName = config.ClusterName
        let constructId = config.ConstructId |> Option.defaultValue clusterName

        let props = ClusterProps()
        props.ClusterName <- clusterName

        config.Vpc |> Option.iter (fun v -> props.Vpc <- v)

        config.ContainerInsights
        |> Option.iter (fun v -> props.ContainerInsightsV2 <- v)

        config.EnableFargateCapacityProviders
        |> Option.iter (fun v -> props.EnableFargateCapacityProviders <- v)

        { ClusterName = clusterName
          ConstructId = constructId
          Cluster = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ECSClusterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: ECSClusterConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }

    [<CustomOperation("containerInsights")>]
    member _.ContainerInsights(config: ECSClusterConfig, insights: ContainerInsights) =
        { config with
            ContainerInsights = Some insights }

    [<CustomOperation("enableFargateCapacityProviders")>]
    member _.EnableFargateCapacityProviders(config: ECSClusterConfig, enabled: bool) =
        { config with
            EnableFargateCapacityProviders = Some enabled }

/// <summary>
/// High-level Fargate Service builder for ECS.
///
/// **Default Settings:**
/// - CPU = 256 (.25 vCPU)
/// - Memory = 512 MB
/// - Desired count = 1
/// - Platform version = LATEST
/// - Public IP = false (secure by default)
///
/// **Rationale:**
/// Fargate provides serverless container orchestration.
/// Private networking by default enhances security.
/// </summary>
type ECSFargateServiceConfig =
    { ServiceName: string
      ConstructId: string option
      Cluster: ICluster option
      TaskDefinition: TaskDefinition option
      DesiredCount: int option
      AssignPublicIp: bool option
      SecurityGroups: SecurityGroupRef list
      VpcSubnets: SubnetSelection option }

type ECSFargateServiceResource =
    {
        ServiceName: string
        ConstructId: string
        /// The underlying CDK FargateService construct
        Service: FargateService
    }

type ECSFargateServiceBuilder(name: string) =
    member _.Yield _ : ECSFargateServiceConfig =
        { ServiceName = name
          ConstructId = None
          Cluster = None
          TaskDefinition = None
          DesiredCount = Some 1
          AssignPublicIp = Some false
          SecurityGroups = []
          VpcSubnets = None }

    member _.Zero() : ECSFargateServiceConfig =
        { ServiceName = name
          ConstructId = None
          Cluster = None
          TaskDefinition = None
          DesiredCount = Some 1
          AssignPublicIp = Some false
          SecurityGroups = []
          VpcSubnets = None }

    member _.Combine(state1: ECSFargateServiceConfig, state2: ECSFargateServiceConfig) : ECSFargateServiceConfig =
        { ServiceName = state2.ServiceName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Cluster = state2.Cluster |> Option.orElse state1.Cluster
          TaskDefinition = state2.TaskDefinition |> Option.orElse state1.TaskDefinition
          DesiredCount = state2.DesiredCount |> Option.orElse state1.DesiredCount
          AssignPublicIp = state2.AssignPublicIp |> Option.orElse state1.AssignPublicIp
          SecurityGroups =
            if state2.SecurityGroups.IsEmpty then
                state1.SecurityGroups
            else
                state2.SecurityGroups
          VpcSubnets = state2.VpcSubnets |> Option.orElse state1.VpcSubnets }

    member inline x.For
        (
            config: ECSFargateServiceConfig,
            [<InlineIfLambda>] f: unit -> ECSFargateServiceConfig
        ) : ECSFargateServiceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ECSFargateServiceConfig) : ECSFargateServiceResource =
        let serviceName = config.ServiceName
        let constructId = config.ConstructId |> Option.defaultValue serviceName

        let props = FargateServiceProps()
        props.ServiceName <- serviceName
        config.Cluster |> Option.iter (fun v -> props.Cluster <- v)
        config.TaskDefinition |> Option.iter (fun v -> props.TaskDefinition <- v)

        config.DesiredCount
        |> Option.iter (fun v -> props.DesiredCount <- System.Nullable<float>(float v))

        config.AssignPublicIp |> Option.iter (fun v -> props.AssignPublicIp <- v)

        if not config.SecurityGroups.IsEmpty then
            props.SecurityGroups <-
                config.SecurityGroups
                |> List.map VpcHelpers.resolveSecurityGroupRef
                |> Array.ofList

        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)

        { ServiceName = serviceName
          ConstructId = constructId
          Service = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ECSFargateServiceConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("cluster")>]
    member _.Cluster(config: ECSFargateServiceConfig, cluster: ICluster) = { config with Cluster = Some cluster }

    [<CustomOperation("taskDefinition")>]
    member _.TaskDefinition(config: ECSFargateServiceConfig, taskDef: TaskDefinition) =
        { config with
            TaskDefinition = Some taskDef }

    [<CustomOperation("desiredCount")>]
    member _.DesiredCount(config: ECSFargateServiceConfig, count: int) =
        { config with
            DesiredCount = Some count }

    [<CustomOperation("assignPublicIp")>]
    member _.AssignPublicIp(config: ECSFargateServiceConfig, assign: bool) =
        { config with
            AssignPublicIp = Some assign }

    /// Add groups to securityGroups
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: ECSFargateServiceConfig, sgs: ISecurityGroup list) =
        let sgsRefs = sgs |> List.map SecurityGroupRef.SecurityGroupInterface

        { config with
            SecurityGroups = sgsRefs @ config.SecurityGroups }

    /// Add groups to securityGroups
    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: ECSFargateServiceConfig, sgs: SecurityGroupSpec list) =
        let sgsrefs = sgs |> List.map SecurityGroupRef.SecurityGroupSpecRef

        { config with
            SecurityGroups = sgsrefs @ config.SecurityGroups }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: ECSFargateServiceConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

// ============================================================================
// Fargate Task Definition Configuration DSL
// ============================================================================

/// <summary>
/// High-level Fargate Task Definition builder following AWS best practices.
///
/// **Default Settings:**
/// - CPU = 256 (.25 vCPU)
/// - Memory = 512 MB
/// - Network mode = awsvpc (required for Fargate)
///
/// **Rationale:**
/// Task definitions define the containers that run in your ECS service.
/// Fargate task definitions require awsvpc network mode.
/// </summary>
type FargateTaskDefinitionConfig =
    { TaskDefinitionName: string
      ConstructId: string option
      Cpu: int option
      MemoryLimitMiB: int option
      TaskRole: RoleRef option
      ExecutionRole: RoleRef option
      Family: string option
      RuntimePlatform: RuntimePlatform option
      EphemeralStorageGiB: int option
      Volumes: Amazon.CDK.AWS.ECS.Volume list }

type FargateTaskDefinitionSpec =
    { TaskDefinitionName: string
      ConstructId: string
      Props: FargateTaskDefinitionProps
      mutable TaskDefinition: FargateTaskDefinition option }

    /// Gets the underlying FargateTaskDefinition resource. Must be called after the stack is built.
    member this.Resource =
        match this.TaskDefinition with
        | Some td -> td
        | None ->
            failwith
                $"FargateTaskDefinition '{this.TaskDefinitionName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type FargateTaskDefinitionBuilder(name: string) =

    member _.Yield _ : FargateTaskDefinitionConfig =
        { TaskDefinitionName = name
          ConstructId = None
          Cpu = Some 256
          MemoryLimitMiB = Some 512
          TaskRole = None
          ExecutionRole = None
          Family = None
          RuntimePlatform = None
          EphemeralStorageGiB = None
          Volumes = [] }

    member _.Zero() : FargateTaskDefinitionConfig =
        { TaskDefinitionName = name
          ConstructId = None
          Cpu = Some 256
          MemoryLimitMiB = Some 512
          TaskRole = None
          ExecutionRole = None
          Family = None
          RuntimePlatform = None
          EphemeralStorageGiB = None
          Volumes = [] }

    member _.Combine
        (
            state1: FargateTaskDefinitionConfig,
            state2: FargateTaskDefinitionConfig
        ) : FargateTaskDefinitionConfig =
        { TaskDefinitionName = state2.TaskDefinitionName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Cpu = state2.Cpu |> Option.orElse state1.Cpu
          MemoryLimitMiB = state2.MemoryLimitMiB |> Option.orElse state1.MemoryLimitMiB
          TaskRole = state2.TaskRole |> Option.orElse state1.TaskRole
          ExecutionRole = state2.ExecutionRole |> Option.orElse state1.ExecutionRole
          Family = state2.Family |> Option.orElse state1.Family
          RuntimePlatform = state2.RuntimePlatform |> Option.orElse state1.RuntimePlatform
          EphemeralStorageGiB = state2.EphemeralStorageGiB |> Option.orElse state1.EphemeralStorageGiB
          Volumes =
            if state2.Volumes.IsEmpty then
                state1.Volumes
            else
                state2.Volumes @ state1.Volumes }

    member inline _.Delay([<InlineIfLambda>] f: unit -> FargateTaskDefinitionConfig) : FargateTaskDefinitionConfig =
        f ()

    member inline x.For
        (
            config: FargateTaskDefinitionConfig,
            [<InlineIfLambda>] f: unit -> FargateTaskDefinitionConfig
        ) : FargateTaskDefinitionConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: FargateTaskDefinitionConfig) : FargateTaskDefinitionSpec =
        let taskDefinitionName = config.TaskDefinitionName
        let constructId = config.ConstructId |> Option.defaultValue taskDefinitionName

        let props = FargateTaskDefinitionProps()

        config.Cpu
        |> Option.iter (fun v -> props.Cpu <- System.Nullable<float>(float v))

        config.MemoryLimitMiB
        |> Option.iter (fun v -> props.MemoryLimitMiB <- System.Nullable<float>(float v))

        config.TaskRole
        |> Option.iter (fun v -> props.TaskRole <- RoleHelpers.resolveRoleRef v)

        config.ExecutionRole
        |> Option.iter (fun v -> props.ExecutionRole <- RoleHelpers.resolveRoleRef v)

        config.Family |> Option.iter (fun v -> props.Family <- v)
        config.RuntimePlatform |> Option.iter (fun v -> props.RuntimePlatform <- v)

        config.EphemeralStorageGiB
        |> Option.iter (fun v -> props.EphemeralStorageGiB <- System.Nullable<float>(float v))

        if not config.Volumes.IsEmpty then
            props.Volumes <-
                config.Volumes
                |> List.map (fun v -> v :> Amazon.CDK.AWS.ECS.IVolume)
                |> Array.ofList

        { TaskDefinitionName = taskDefinitionName
          ConstructId = constructId
          Props = props
          TaskDefinition = None }

    /// <summary>Sets a custom construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: FargateTaskDefinitionConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the number of CPU units (256, 512, 1024, 2048, 4096).</summary>
    [<CustomOperation("cpu")>]
    member _.Cpu(config: FargateTaskDefinitionConfig, cpu: int) = { config with Cpu = Some cpu }

    /// <summary>Sets the memory limit in MiB.</summary>
    [<CustomOperation("memory")>]
    member _.Memory(config: FargateTaskDefinitionConfig, memory: int) =
        { config with
            MemoryLimitMiB = Some memory }

    /// <summary>Sets the IAM role for the task (application permissions).</summary>
    [<CustomOperation("taskRole")>]
    member _.TaskRole(config: FargateTaskDefinitionConfig, role: IRole) =
        { config with
            TaskRole = Some(RoleInterface role) }

    /// <summary>Sets the IAM role for the task (application permissions) using a LambdaRoleSpec.</summary>
    [<CustomOperation("taskRole")>]
    member _.TaskRoleSpec(config: FargateTaskDefinitionConfig, roleSpec: LambdaRoleSpec) =
        { config with
            TaskRole = Some(RoleSpecRef roleSpec) }

    /// <summary>Sets the IAM role for the execution (pulls images, writes logs).</summary>
    [<CustomOperation("executionRole")>]
    member _.ExecutionRole(config: FargateTaskDefinitionConfig, role: IRole) =
        { config with
            ExecutionRole = Some(RoleInterface role) }

    /// <summary>Sets the IAM role for the execution (pulls images, writes logs) using a LambdaRoleSpec.</summary>
    [<CustomOperation("executionRole")>]
    member _.ExecutionRoleSpec(config: FargateTaskDefinitionConfig, roleSpec: LambdaRoleSpec) =
        { config with
            ExecutionRole = Some(RoleSpecRef roleSpec) }

    /// <summary>Sets the task definition family name.</summary>
    [<CustomOperation("family")>]
    member _.Family(config: FargateTaskDefinitionConfig, family: string) = { config with Family = Some family }

    /// <summary>Sets the runtime platform (CPU architecture and OS).</summary>
    [<CustomOperation("runtimePlatform")>]
    member _.RuntimePlatform(config: FargateTaskDefinitionConfig, platform: RuntimePlatform) =
        { config with
            RuntimePlatform = Some platform }

    /// <summary>Sets the ephemeral storage size in GiB (default 20, max 200).</summary>
    [<CustomOperation("ephemeralStorageGiB")>]
    member _.EphemeralStorageGiB(config: FargateTaskDefinitionConfig, size: int) =
        { config with
            EphemeralStorageGiB = Some size }

    /// <summary>Adds a volume to the task definition.</summary>
    [<CustomOperation("volume")>]
    member _.Volume(config: FargateTaskDefinitionConfig, volume: Amazon.CDK.AWS.ECS.Volume) =
        { config with
            Volumes = volume :: config.Volumes }

    /// <summary>Adds multiple volumes to the task definition.</summary>
    [<CustomOperation("volumes")>]
    member _.Volumes(config: FargateTaskDefinitionConfig, volumes: Amazon.CDK.AWS.ECS.Volume list) =
        { config with
            Volumes = volumes @ config.Volumes }

// ============================================================================
// Container Definition Configuration DSL
// ============================================================================

type ContainerDefinitionConfig =
    { ContainerName: string
      Image: ContainerImage option
      Cpu: int option
      MemoryLimitMiB: int option
      MemoryReservationMiB: int option
      Essential: bool option
      Environment: Map<string, string>
      Secrets: Map<string, Secret>
      PortMappings: PortMapping list
      Command: string list
      EntryPoint: string list
      WorkingDirectory: string option
      HealthCheck: HealthCheck option
      Logging: LogDriver option
      User: string option
      Privileged: bool option
      ReadonlyRootFilesystem: bool option
      StartTimeout: Duration option
      StopTimeout: Duration option }

type ContainerDefinitionHelper() =
    /// Adds a container definition to a Fargate task definition
    static member AddContainer
        (
            taskDef: FargateTaskDefinition,
            containerName: string,
            config: ContainerDefinitionConfig
        ) : ContainerDefinition =
        let options = ContainerDefinitionOptions()
        options.ContainerName <- containerName

        match config.Image with
        | Some v -> options.Image <- v
        | None -> invalidArg "image" "Container image is required"

        config.Cpu
        |> Option.iter (fun v -> options.Cpu <- System.Nullable<float>(float v))

        config.MemoryLimitMiB
        |> Option.iter (fun v -> options.MemoryLimitMiB <- System.Nullable<float>(float v))

        config.MemoryReservationMiB
        |> Option.iter (fun v -> options.MemoryReservationMiB <- System.Nullable<float>(float v))

        config.Essential |> Option.iter (fun v -> options.Essential <- v)

        if not (Map.isEmpty config.Environment) then
            options.Environment <- System.Collections.Generic.Dictionary<string, string>(config.Environment)

        if not (Map.isEmpty config.Secrets) then
            options.Secrets <- System.Collections.Generic.Dictionary<string, Secret>(config.Secrets)

        if not config.PortMappings.IsEmpty then
            options.PortMappings <- config.PortMappings |> List.map (fun p -> p :> IPortMapping) |> Array.ofList

        if not config.Command.IsEmpty then
            options.Command <- Array.ofList config.Command

        if not config.EntryPoint.IsEmpty then
            options.EntryPoint <- Array.ofList config.EntryPoint

        config.WorkingDirectory |> Option.iter (fun v -> options.WorkingDirectory <- v)
        config.HealthCheck |> Option.iter (fun v -> options.HealthCheck <- v)
        config.Logging |> Option.iter (fun v -> options.Logging <- v)
        config.User |> Option.iter (fun v -> options.User <- v)
        config.Privileged |> Option.iter (fun v -> options.Privileged <- v)

        config.ReadonlyRootFilesystem
        |> Option.iter (fun v -> options.ReadonlyRootFilesystem <- v)

        config.StartTimeout |> Option.iter (fun v -> options.StartTimeout <- v)
        config.StopTimeout |> Option.iter (fun v -> options.StopTimeout <- v)

        taskDef.AddContainer(containerName, options)

[<AutoOpen>]
module ECSBuilders =
    /// <summary>
    /// Creates a new ECS cluster builder with best practices.
    /// Example: ecsCluster "my-cluster" { vpc myVpc }
    /// </summary>
    let ecsCluster name = ECSClusterBuilder name

    /// <summary>
    /// Creates a new Fargate service builder.
    /// Example: ecsFargateService "my-service" { cluster myCluster; taskDefinition myTaskDef }
    /// </summary>
    let ecsFargateService name = ECSFargateServiceBuilder name

    /// <summary>
    /// Creates a new Fargate task definition builder.
    /// Example: fargateTaskDefinition "my-task" { cpu 512; memory 1024 }
    /// </summary>
    let fargateTaskDefinition name = FargateTaskDefinitionBuilder name
