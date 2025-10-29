namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ECS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.ElasticLoadBalancingV2

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

type ECSClusterSpec =
    { ClusterName: string
      ConstructId: string
      Props: ClusterProps
      mutable Cluster: Cluster }

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

    member _.Run(config: ECSClusterConfig) : ECSClusterSpec =
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
          Cluster = null
          Props = props }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ECSClusterConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("vpc")>]
    member _.Vpc(config: ECSClusterConfig, vpc: IVpc) = { config with Vpc = Some(vpc) }
    //
    // [<CustomOperation("vpc")>]
    // member _.Vpc(config: ECSClusterConfig, vpcSpec: FsCDK.VpcSpec) =
    //     { config with
    //         Vpc = Some(FsCDK.VpcSpecRef vpcSpec) }

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
      SecurityGroups: ISecurityGroup list
      VpcSubnets: SubnetSelection option }

type ECSFargateServiceSpec =
    { ServiceName: string
      ConstructId: string
      Props: FargateServiceProps
      mutable Service: FargateService }

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

    member _.Run(config: ECSFargateServiceConfig) : ECSFargateServiceSpec =
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
            props.SecurityGroups <- config.SecurityGroups |> List.map id |> Array.ofList

        config.VpcSubnets |> Option.iter (fun v -> props.VpcSubnets <- v)

        { ServiceName = serviceName
          ConstructId = constructId
          Service = null
          Props = props }

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

    [<CustomOperation("securityGroups")>]
    member _.SecurityGroups(config: ECSFargateServiceConfig, sgs: ISecurityGroup list) =
        let sgsrefs = sgs |> List.map id

        { config with
            SecurityGroups = sgsrefs @ config.SecurityGroups }

    // [<CustomOperation("securityGroups")>]
    // member _.SecurityGroups(config: ECSFargateServiceConfig, sgs: SecurityGroupSpec list) =
    //     let sgsrefs = sgs |> List.map SecurityGroupRef.SecurityGroupSpecRef
    //
    //     { config with
    //         SecurityGroups = sgsrefs @ config.SecurityGroups }

    [<CustomOperation("vpcSubnets")>]
    member _.VpcSubnets(config: ECSFargateServiceConfig, subnets: SubnetSelection) =
        { config with
            VpcSubnets = Some subnets }

[<AutoOpen>]
module ECSBuilders =
    /// <summary>
    /// Creates a new ECS cluster builder with best practices.
    /// Example: ecsCluster "my-cluster" { vpc myVpc }
    /// </summary>
    let ecsCluster name = ECSClusterBuilder(name)

    /// <summary>
    /// Creates a new Fargate service builder.
    /// Example: ecsFargateService "my-service" { cluster myCluster; taskDefinition myTaskDef }
    /// </summary>
    let ecsFargateService name = ECSFargateServiceBuilder(name)
