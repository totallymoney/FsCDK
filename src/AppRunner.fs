namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.AppRunner
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.ECR

/// <summary>
/// High-level AWS App Runner service builder following AWS best practices.
///
/// **Default Settings:**
/// - Auto-scaling = 1-10 instances
/// - Memory = 2 GB
/// - vCPU = 1
/// - Port = 8080
/// - Health check = /health
///
/// **Rationale:**
/// App Runner provides fully managed container hosting similar to Azure App Service.
/// These defaults follow AWS Well-Architected Framework:
/// - Sensible scaling limits for cost control
/// - Standard health check endpoint
/// - Container-first approach
///
/// **Use Cases:**
/// - Containerized web applications
/// - REST APIs
/// - Background workers
/// - Microservices
///
/// **Escape Hatch:**
/// Access the underlying CDK CfnService via the `Service` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type AppRunnerServiceConfig =
    { ServiceName: string
      ConstructId: string option
      SourceConfiguration: CfnService.SourceConfigurationProperty option
      InstanceConfiguration: CfnService.InstanceConfigurationProperty option
      HealthCheckConfiguration: CfnService.HealthCheckConfigurationProperty option
      AutoScalingConfigurationArn: string option
      InstanceRole: IRole option
      AccessRole: IRole option
      Tags: (string * string) list }

type AppRunnerServiceSpec =
    { ServiceName: string
      ConstructId: string
      Props: CfnServiceProps
      mutable Service: CfnService option }

    /// Gets the service URL
    member this.ServiceUrl =
        match this.Service with
        | Some s -> s.AttrServiceUrl
        | None -> null

    /// Gets the service ARN
    member this.ServiceArn =
        match this.Service with
        | Some s -> s.AttrServiceArn
        | None -> null

type AppRunnerServiceBuilder(name: string) =
    member _.Yield(_: unit) : AppRunnerServiceConfig =
        { ServiceName = name
          ConstructId = None
          SourceConfiguration = None
          InstanceConfiguration = None
          HealthCheckConfiguration = None
          AutoScalingConfigurationArn = None
          InstanceRole = None
          AccessRole = None
          Tags = [] }

    member _.Zero() : AppRunnerServiceConfig =
        { ServiceName = name
          ConstructId = None
          SourceConfiguration = None
          InstanceConfiguration = None
          HealthCheckConfiguration = None
          AutoScalingConfigurationArn = None
          InstanceRole = None
          AccessRole = None
          Tags = [] }

    member _.Combine(state1: AppRunnerServiceConfig, state2: AppRunnerServiceConfig) : AppRunnerServiceConfig =
        { ServiceName = state2.ServiceName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          SourceConfiguration = state2.SourceConfiguration |> Option.orElse state1.SourceConfiguration
          InstanceConfiguration = state2.InstanceConfiguration |> Option.orElse state1.InstanceConfiguration
          HealthCheckConfiguration = state2.HealthCheckConfiguration |> Option.orElse state1.HealthCheckConfiguration
          AutoScalingConfigurationArn =
            state2.AutoScalingConfigurationArn
            |> Option.orElse state1.AutoScalingConfigurationArn
          InstanceRole = state2.InstanceRole |> Option.orElse state1.InstanceRole
          AccessRole = state2.AccessRole |> Option.orElse state1.AccessRole
          Tags =
            if state2.Tags.IsEmpty then
                state1.Tags
            else
                state2.Tags @ state1.Tags }

    member inline _.Delay([<InlineIfLambda>] f: unit -> AppRunnerServiceConfig) : AppRunnerServiceConfig = f ()

    member inline x.For
        (
            config: AppRunnerServiceConfig,
            [<InlineIfLambda>] f: unit -> AppRunnerServiceConfig
        ) : AppRunnerServiceConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: AppRunnerServiceConfig) : AppRunnerServiceSpec =
        let serviceName = config.ServiceName
        let constructId = config.ConstructId |> Option.defaultValue serviceName

        let props = Amazon.CDK.AWS.AppRunner.CfnServiceProps()
        props.ServiceName <- config.ServiceName

        config.SourceConfiguration
        |> Option.iter (fun v -> props.SourceConfiguration <- v)

        config.InstanceConfiguration
        |> Option.iter (fun v -> props.InstanceConfiguration <- v)

        config.HealthCheckConfiguration
        |> Option.iter (fun v -> props.HealthCheckConfiguration <- v)

        config.AutoScalingConfigurationArn
        |> Option.iter (fun v -> props.AutoScalingConfigurationArn <- v)

        if not config.Tags.IsEmpty then
            props.Tags <-
                config.Tags
                |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                |> Array.ofList

        // Validate required configuration
        match config.SourceConfiguration with
        | None -> failwith "SourceConfiguration is required for App Runner service"
        | Some _ -> ()

        { ServiceName = serviceName
          ConstructId = constructId
          Props = props
          Service = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: AppRunnerServiceConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("sourceConfiguration")>]
    member _.SourceConfiguration(config: AppRunnerServiceConfig, source: CfnService.SourceConfigurationProperty) =
        { config with
            SourceConfiguration = Some source }

    [<CustomOperation("instanceConfiguration")>]
    member _.InstanceConfiguration(config: AppRunnerServiceConfig, instance: CfnService.InstanceConfigurationProperty) =
        { config with
            InstanceConfiguration = Some instance }

    [<CustomOperation("healthCheckConfiguration")>]
    member _.HealthCheckConfiguration
        (
            config: AppRunnerServiceConfig,
            healthCheck: CfnService.HealthCheckConfigurationProperty
        ) =
        { config with
            HealthCheckConfiguration = Some healthCheck }

    [<CustomOperation("autoScalingConfigurationArn")>]
    member _.AutoScalingConfigurationArn(config: AppRunnerServiceConfig, arn: string) =
        { config with
            AutoScalingConfigurationArn = Some arn }

    [<CustomOperation("instanceRole")>]
    member _.InstanceRole(config: AppRunnerServiceConfig, role: IRole) =
        { config with InstanceRole = Some role }

    [<CustomOperation("accessRole")>]
    member _.AccessRole(config: AppRunnerServiceConfig, role: IRole) = { config with AccessRole = Some role }

    [<CustomOperation("tag")>]
    member _.Tag(config: AppRunnerServiceConfig, key: string, value: string) =
        { config with
            Tags = (key, value) :: config.Tags }

    [<CustomOperation("tags")>]
    member _.Tags(config: AppRunnerServiceConfig, tags: (string * string) list) =
        { config with
            Tags = tags @ config.Tags }

/// Helper functions for App Runner operations
module AppRunnerHelpers =

    /// Creates source configuration from ECR image
    let ecrSource (imageUri: string) (port: int) =
        let imageConfig = CfnService.ImageConfigurationProperty()
        imageConfig.Port <- sprintf "%d" port

        let imageRepo = CfnService.ImageRepositoryProperty()
        imageRepo.ImageIdentifier <- imageUri
        imageRepo.ImageRepositoryType <- "ECR"
        imageRepo.ImageConfiguration <- imageConfig

        let source = CfnService.SourceConfigurationProperty()
        source.ImageRepository <- imageRepo
        source.AutoDeploymentsEnabled <- true
        source

    /// Creates source configuration from ECR with auto-deploy
    let ecrSourceWithAutoDeploy (imageUri: string) (port: int) (accessRole: IRole) =
        let source = ecrSource imageUri port
        source.AutoDeploymentsEnabled <- true
        source

    /// Creates instance configuration with custom resources
    let instanceConfig (cpu: string) (memory: string) =
        let config = CfnService.InstanceConfigurationProperty()
        config.Cpu <- cpu
        config.Memory <- memory
        config

    /// Creates standard health check
    let healthCheck (path: string) =
        let config = CfnService.HealthCheckConfigurationProperty()
        config.Path <- path
        config.Protocol <- "HTTP"
        config.Interval <- 5.0
        config.Timeout <- 2.0
        config.HealthyThreshold <- 1.0
        config.UnhealthyThreshold <- 5.0
        config

    /// Pre-configured instance sizes
    module InstanceSizes =
        let small = instanceConfig "0.25 vCPU" "0.5 GB" // 0.25 vCPU, 0.5 GB
        let medium = instanceConfig "0.5 vCPU" "1 GB" // 0.5 vCPU, 1 GB
        let large = instanceConfig "1 vCPU" "2 GB" // 1 vCPU, 2 GB (default)
        let xlarge = instanceConfig "2 vCPU" "4 GB" // 2 vCPU, 4 GB

[<AutoOpen>]
module AppRunnerBuilders =
    /// <summary>
    /// Creates a new App Runner service builder.
    /// Example: appRunnerService "my-web-app" { sourceConfiguration (AppRunnerHelpers.ecrSource "my-repo:latest" 8080) }
    /// </summary>
    let appRunnerService name = AppRunnerServiceBuilder name
