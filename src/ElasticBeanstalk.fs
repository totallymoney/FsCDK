namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.ElasticBeanstalk
open System.Collections.Generic

/// <summary>
/// High-level Elastic Beanstalk Application builder.
///
/// **Rationale:**
/// Elastic Beanstalk simplifies application deployment by managing
/// infrastructure, auto-scaling, load balancing, and monitoring.
///
/// **Note:**
/// Elastic Beanstalk requires additional configuration via environments
/// and configuration options for production use.
/// </summary>
type ElasticBeanstalkApplicationConfig =
    { ApplicationName: string
      ConstructId: string option
      Description: string option }

type ElasticBeanstalkApplicationResource =
    {
        ApplicationName: string
        ConstructId: string
        /// The underlying CDK Application construct
        Application: CfnApplication
    }

type ElasticBeanstalkApplicationBuilder(name: string) =
    member _.Yield _ : ElasticBeanstalkApplicationConfig =
        { ApplicationName = name
          ConstructId = None
          Description = None }

    member _.Zero() : ElasticBeanstalkApplicationConfig =
        { ApplicationName = name
          ConstructId = None
          Description = None }

    member _.Combine
        (
            state1: ElasticBeanstalkApplicationConfig,
            state2: ElasticBeanstalkApplicationConfig
        ) : ElasticBeanstalkApplicationConfig =
        { ApplicationName = state2.ApplicationName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Description = state2.Description |> Option.orElse state1.Description }

    member inline x.For
        (
            config: ElasticBeanstalkApplicationConfig,
            [<InlineIfLambda>] f: unit -> ElasticBeanstalkApplicationConfig
        ) : ElasticBeanstalkApplicationConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ElasticBeanstalkApplicationConfig) : ElasticBeanstalkApplicationResource =
        let applicationName = config.ApplicationName
        let constructId = config.ConstructId |> Option.defaultValue applicationName

        let props = CfnApplicationProps()
        props.ApplicationName <- applicationName
        config.Description |> Option.iter (fun v -> props.Description <- v)

        { ApplicationName = applicationName
          ConstructId = constructId
          Application = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ElasticBeanstalkApplicationConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("description")>]
    member _.Description(config: ElasticBeanstalkApplicationConfig, description: string) =
        { config with
            Description = Some description }

/// <summary>
/// High-level Elastic Beanstalk Environment builder.
///
/// **Default Settings:**
/// - Tier = WebServer
/// - Solution stack = Latest (must be specified by user for actual deployment)
///
/// **Rationale:**
/// Environments represent deployments of your application.
/// Configuration is highly dependent on the platform and application type.
/// </summary>
type ElasticBeanstalkEnvironmentConfig =
    { EnvironmentName: string
      ConstructId: string option
      ApplicationName: string option
      SolutionStackName: string option
      Description: string option
      Tier: CfnEnvironment.ITierProperty option
      OptionSettings: CfnEnvironment.IOptionSettingProperty list }

type ElasticBeanstalkEnvironmentResource =
    {
        EnvironmentName: string
        ConstructId: string
        /// The underlying CDK Environment construct
        Environment: CfnEnvironment
    }

type ElasticBeanstalkEnvironmentBuilder(name: string) =
    member _.Yield _ : ElasticBeanstalkEnvironmentConfig =
        { EnvironmentName = name
          ConstructId = None
          ApplicationName = None
          SolutionStackName = None
          Description = None
          Tier = None
          OptionSettings = [] }

    member _.Zero() : ElasticBeanstalkEnvironmentConfig =
        { EnvironmentName = name
          ConstructId = None
          ApplicationName = None
          SolutionStackName = None
          Description = None
          Tier = None
          OptionSettings = [] }

    member _.Combine
        (
            state1: ElasticBeanstalkEnvironmentConfig,
            state2: ElasticBeanstalkEnvironmentConfig
        ) : ElasticBeanstalkEnvironmentConfig =
        { EnvironmentName = state2.EnvironmentName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          ApplicationName = state2.ApplicationName |> Option.orElse state1.ApplicationName
          SolutionStackName = state2.SolutionStackName |> Option.orElse state1.SolutionStackName
          Description = state2.Description |> Option.orElse state1.Description
          Tier = state2.Tier |> Option.orElse state1.Tier
          OptionSettings =
            if state2.OptionSettings.IsEmpty then
                state1.OptionSettings
            else
                state2.OptionSettings }

    member inline x.For
        (
            config: ElasticBeanstalkEnvironmentConfig,
            [<InlineIfLambda>] f: unit -> ElasticBeanstalkEnvironmentConfig
        ) : ElasticBeanstalkEnvironmentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: ElasticBeanstalkEnvironmentConfig) : ElasticBeanstalkEnvironmentResource =
        let environmentName = config.EnvironmentName
        let constructId = config.ConstructId |> Option.defaultValue environmentName

        let props = CfnEnvironmentProps()
        props.EnvironmentName <- environmentName
        config.ApplicationName |> Option.iter (fun v -> props.ApplicationName <- v)
        config.SolutionStackName |> Option.iter (fun v -> props.SolutionStackName <- v)
        config.Description |> Option.iter (fun v -> props.Description <- v)
        config.Tier |> Option.iter (fun v -> props.Tier <- v)

        if not config.OptionSettings.IsEmpty then
            props.OptionSettings <- Array.ofList config.OptionSettings

        { EnvironmentName = environmentName
          ConstructId = constructId
          Environment = null }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ElasticBeanstalkEnvironmentConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("applicationName")>]
    member _.ApplicationName(config: ElasticBeanstalkEnvironmentConfig, name: string) =
        { config with
            ApplicationName = Some name }

    [<CustomOperation("solutionStackName")>]
    member _.SolutionStackName(config: ElasticBeanstalkEnvironmentConfig, stackName: string) =
        { config with
            SolutionStackName = Some stackName }

    [<CustomOperation("description")>]
    member _.Description(config: ElasticBeanstalkEnvironmentConfig, description: string) =
        { config with
            Description = Some description }

    [<CustomOperation("tier")>]
    member _.Tier(config: ElasticBeanstalkEnvironmentConfig, tier: CfnEnvironment.ITierProperty) =
        { config with Tier = Some tier }

    [<CustomOperation("optionSettings")>]
    member _.OptionSettings
        (
            config: ElasticBeanstalkEnvironmentConfig,
            settings: CfnEnvironment.IOptionSettingProperty list
        ) =
        { config with
            OptionSettings = settings }

[<AutoOpen>]
module ElasticBeanstalkBuilders =
    /// <summary>
    /// Creates a new Elastic Beanstalk application builder.
    /// Example: ebApplication "my-app" { description "My web application" }
    /// </summary>
    let ebApplication name =
        ElasticBeanstalkApplicationBuilder name

    /// <summary>
    /// Creates a new Elastic Beanstalk environment builder.
    /// Example: ebEnvironment "my-env" { applicationName "my-app"; solutionStackName "64bit Amazon Linux 2 v5.8.0 running Node.js 18" }
    /// </summary>
    let ebEnvironment name =
        ElasticBeanstalkEnvironmentBuilder name
