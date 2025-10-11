namespace FsCDK

open Amazon.CDK
open System.Collections.Generic

// ============================================================================
// Environment and StackProps Configuration DSL
// ============================================================================

type EnvironmentConfig =
    { Account: string option
      Region: string option }

type EnvironmentBuilder() =
    member _.Yield _ : EnvironmentConfig = { Account = None; Region = None }

    member _.Zero() : EnvironmentConfig = { Account = None; Region = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> EnvironmentConfig) : EnvironmentConfig = f ()

    member _.Combine(state1: EnvironmentConfig, state2: EnvironmentConfig) : EnvironmentConfig =
        { Account = state2.Account |> Option.orElse state1.Account
          Region = state2.Region |> Option.orElse state1.Region }

    member inline x.For
        (
            config: EnvironmentConfig,
            [<InlineIfLambda>] f: unit -> EnvironmentConfig
        ) : EnvironmentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: EnvironmentConfig) =
        let env = Environment()
        config.Account |> Option.iter (fun acc -> env.Account <- acc)
        config.Region |> Option.iter (fun reg -> env.Region <- reg)
        env

    /// <summary>Sets the AWS account ID for the environment.</summary>
    /// <param name="accountId">The AWS account ID.</param>
    /// <code lang="fsharp">
    /// environment {
    ///     account "123456789012"
    /// }
    /// </code>
    [<CustomOperation("account")>]
    member _.Account(config: EnvironmentConfig, accountId: string) =
        { config with Account = Some accountId }

    /// <summary>Sets the AWS region for the environment.</summary>
    /// <param name="regionName">The AWS region name.</param>
    /// <code lang="fsharp">
    /// environment {
    ///     region "us-west-2"
    /// }
    /// </code>
    [<CustomOperation("region")>]
    member _.Region(config: EnvironmentConfig, regionName: string) =
        { config with Region = Some regionName }

type StackPropsConfig =
    { Env: IEnvironment option
      Description: string option
      StackName: string option
      Tags: Map<string, string> option
      TerminationProtection: bool option
      AnalyticsReporting: bool option
      CrossRegionReferences: bool option
      SuppressTemplateIndentation: bool option
      NotificationArns: string list option
      PermissionsBoundary: PermissionsBoundary option
      PropertyInjectors: IPropertyInjector list option
      Synthesizer: IStackSynthesizer option }

type StackPropsBuilder() =
    member _.Yield _ : StackPropsConfig =
        { Env = None
          Description = None
          StackName = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None }

    member _.Yield(env: IEnvironment) : StackPropsConfig =
        { Env = Some env
          Description = None
          StackName = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackPropsConfig) : StackPropsConfig = f ()

    member _.Combine(state1: StackPropsConfig, state2: StackPropsConfig) : StackPropsConfig =
        { Env = state2.Env |> Option.orElse state1.Env
          Description = state2.Description |> Option.orElse state1.Description
          StackName = state2.StackName |> Option.orElse state1.StackName
          Tags = state2.Tags |> Option.orElse state1.Tags
          TerminationProtection = state2.TerminationProtection |> Option.orElse state1.TerminationProtection
          AnalyticsReporting = state2.AnalyticsReporting |> Option.orElse state1.AnalyticsReporting
          CrossRegionReferences = state2.CrossRegionReferences |> Option.orElse state1.CrossRegionReferences
          SuppressTemplateIndentation =
            state2.SuppressTemplateIndentation
            |> Option.orElse state1.SuppressTemplateIndentation
          NotificationArns = state2.NotificationArns |> Option.orElse state1.NotificationArns
          PermissionsBoundary = state2.PermissionsBoundary |> Option.orElse state1.PermissionsBoundary
          PropertyInjectors = state2.PropertyInjectors |> Option.orElse state1.PropertyInjectors
          Synthesizer = state2.Synthesizer |> Option.orElse state1.Synthesizer }

    member _.Zero() : StackPropsConfig =
        { Env = None
          Description = None
          StackName = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None }

    member _.Run(config: StackPropsConfig) =
        let props = StackProps()

        config.Env |> Option.iter (fun env -> props.Env <- env)
        config.Description |> Option.iter (fun desc -> props.Description <- desc)
        config.StackName |> Option.iter (fun name -> props.StackName <- name)

        config.Tags
        |> Option.iter (fun tags ->
            let tagDict = Dictionary<string, string>()
            tags |> Map.iter (fun k v -> tagDict.Add(k, v))
            props.Tags <- tagDict)

        config.TerminationProtection
        |> Option.iter (fun tp -> props.TerminationProtection <- System.Nullable<bool>(tp))

        config.AnalyticsReporting
        |> Option.iter (fun ar -> props.AnalyticsReporting <- System.Nullable<bool>(ar))

        config.CrossRegionReferences
        |> Option.iter (fun crr -> props.CrossRegionReferences <- System.Nullable<bool>(crr))

        config.SuppressTemplateIndentation
        |> Option.iter (fun sti -> props.SuppressTemplateIndentation <- System.Nullable<bool>(sti))

        config.NotificationArns
        |> Option.iter (fun arns -> props.NotificationArns <- (arns |> List.toArray))

        config.PermissionsBoundary
        |> Option.iter (fun pb -> props.PermissionsBoundary <- pb)

        config.PropertyInjectors
        |> Option.iter (fun injectors -> props.PropertyInjectors <- (injectors |> List.toArray))

        config.Synthesizer |> Option.iter (fun synth -> props.Synthesizer <- synth)

        props


    /// <summary>Sets the stack description.</summary>
    /// <param name="desc">A description of the stack.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     description "My application stack"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: StackPropsConfig, desc: string) = { config with Description = Some desc }

    /// <summary>Sets the stack name.</summary>
    /// <param name="name">The stack name.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     stackName "MyApplicationStack"
    /// }
    /// </code>
    [<CustomOperation("stackName")>]
    member _.StackName(config: StackPropsConfig, name: string) = { config with StackName = Some name }

    /// <summary>Adds tags to the stack.</summary>
    /// <param name="tags">A list of key-value pairs for tagging.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     tags [ "Environment", "Production"; "Team", "DevOps" ]
    /// }
    /// </code>
    [<CustomOperation("tags")>]
    member _.Tags(config: StackPropsConfig, tags: (string * string) list) =
        { config with
            Tags = Some(tags |> Map.ofList) }

    /// <summary>Enables or disables termination protection for the stack.</summary>
    /// <param name="enabled">Whether termination protection is enabled.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     terminationProtection true
    /// }
    /// </code>
    [<CustomOperation("terminationProtection")>]
    member _.TerminationProtection(config: StackPropsConfig, enabled: bool) =
        { config with
            TerminationProtection = Some enabled }

    /// <summary>Enables or disables analytics reporting.</summary>
    /// <param name="enabled">Whether analytics reporting is enabled.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     analyticsReporting false
    /// }
    /// </code>
    [<CustomOperation("analyticsReporting")>]
    member _.AnalyticsReporting(config: StackPropsConfig, enabled: bool) =
        { config with
            AnalyticsReporting = Some enabled }

    /// <summary>Enables or disables cross-region references.</summary>
    /// <param name="enabled">Whether cross-region references are enabled.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     crossRegionReferences true
    /// }
    /// </code>
    [<CustomOperation("crossRegionReferences")>]
    member _.CrossRegionReferences(config: StackPropsConfig, enabled: bool) =
        { config with
            CrossRegionReferences = Some enabled }

    /// <summary>Enables or disables CloudFormation template indentation suppression.</summary>
    /// <param name="enabled">Whether to suppress template indentation.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     suppressTemplateIndentation true
    /// }
    /// </code>
    [<CustomOperation("suppressTemplateIndentation")>]
    member _.SuppressTemplateIndentation(config: StackPropsConfig, enabled: bool) =
        { config with
            SuppressTemplateIndentation = Some enabled }

    /// <summary>Sets SNS topic ARNs for stack notifications.</summary>
    /// <param name="arns">List of SNS topic ARNs.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     notificationArns [ "arn:aws:sns:us-east-1:123456789:mytopic" ]
    /// }
    /// </code>
    [<CustomOperation("notificationArns")>]
    member _.NotificationArns(config: StackPropsConfig, arns: string list) =
        { config with
            NotificationArns = Some arns }

    /// <summary>Sets the permissions boundary for the stack.</summary>
    /// <param name="boundary">The permissions boundary.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     permissionsBoundary (PermissionsBoundary.fromName "MyBoundary")
    /// }
    /// </code>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: StackPropsConfig, boundary: PermissionsBoundary) =
        { config with
            PermissionsBoundary = Some boundary }

    /// <summary>Sets property injectors for the stack.</summary>
    /// <param name="injectors">List of property injectors.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     propertyInjectors [ myInjector ]
    /// }
    /// </code>
    [<CustomOperation("propertyInjectors")>]
    member _.PropertyInjectors(config: StackPropsConfig, injectors: IPropertyInjector list) =
        { config with
            PropertyInjectors = Some injectors }

    /// <summary>Sets the stack synthesizer.</summary>
    /// <param name="synthesizer">The stack synthesizer to use.</param>
    /// <code lang="fsharp">
    /// stackProps {
    ///     synthesizer (DefaultStackSynthesizer())
    /// }
    /// </code>
    [<CustomOperation("synthesizer")>]
    member _.Synthesizer(config: StackPropsConfig, synthesizer: IStackSynthesizer) =
        { config with
            Synthesizer = Some synthesizer }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module EnvironmentBuilders =
    /// <summary>Creates an AWS CDK Environment configuration.</summary>
    /// <code lang="fsharp">
    /// environment {
    ///     account "123456789012"
    ///     region "us-west-2"
    /// }
    /// </code>
    let environment = EnvironmentBuilder()

    /// <summary>Creates Stack properties configuration.</summary>
    /// <code lang="fsharp">
    /// stackProps {
    ///     description "My stack"
    ///     stackName "MyStack"
    ///     terminationProtection true
    /// }
    /// </code>
    let stackProps = StackPropsBuilder()
