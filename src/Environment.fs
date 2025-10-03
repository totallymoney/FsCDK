namespace FsCDK

open Amazon.CDK
open System.Collections.Generic

// ============================================================================
// Environment and StackProps Configuration DSL
// ============================================================================

// Environment configuration DSL
type EnvironmentConfig =
    { Account: string option
      Region: string option }

type EnvironmentBuilder() =
    member _.Yield _ : EnvironmentConfig = { Account = None; Region = None }

    member _.Zero() : EnvironmentConfig = { Account = None; Region = None }

    member _.Delay(f: unit -> EnvironmentConfig) : EnvironmentConfig = f ()

    member _.Combine(state1: EnvironmentConfig, state2: EnvironmentConfig) : EnvironmentConfig =
        { Account = state2.Account |> Option.orElse state1.Account
          Region = state2.Region |> Option.orElse state1.Region }

    member x.For(config: EnvironmentConfig, f: unit -> EnvironmentConfig) : EnvironmentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: EnvironmentConfig) =
        let env = Environment()
        config.Account |> Option.iter (fun acc -> env.Account <- acc)
        config.Region |> Option.iter (fun reg -> env.Region <- reg)
        env

    [<CustomOperation("account")>]
    member _.Account(config: EnvironmentConfig, accountId: string) =
        { config with Account = Some accountId }

    [<CustomOperation("region")>]
    member _.Region(config: EnvironmentConfig, regionName: string) =
        { config with Region = Some regionName }
// StackProps configuration DSL
type StackPropsConfig =
    { Env: Environment option
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

    [<CustomOperation("env")>]
    member _.Environment(config: StackPropsConfig, env: Environment) = { config with Env = Some env }

    [<CustomOperation("description")>]
    member _.Description(config: StackPropsConfig, desc: string) = { config with Description = Some desc }

    [<CustomOperation("stackName")>]
    member _.StackName(config: StackPropsConfig, name: string) = { config with StackName = Some name }

    [<CustomOperation("tags")>]
    member _.Tags(config: StackPropsConfig, tags: (string * string) list) =
        { config with
            Tags = Some(tags |> Map.ofList) }

    [<CustomOperation("terminationProtection")>]
    member _.TerminationProtection(config: StackPropsConfig, enabled: bool) =
        { config with
            TerminationProtection = Some enabled }

    [<CustomOperation("analyticsReporting")>]
    member _.AnalyticsReporting(config: StackPropsConfig, enabled: bool) =
        { config with
            AnalyticsReporting = Some enabled }

    [<CustomOperation("crossRegionReferences")>]
    member _.CrossRegionReferences(config: StackPropsConfig, enabled: bool) =
        { config with
            CrossRegionReferences = Some enabled }

    [<CustomOperation("suppressTemplateIndentation")>]
    member _.SuppressTemplateIndentation(config: StackPropsConfig, enabled: bool) =
        { config with
            SuppressTemplateIndentation = Some enabled }

    [<CustomOperation("notificationArns")>]
    member _.NotificationArns(config: StackPropsConfig, arns: string list) =
        { config with
            NotificationArns = Some arns }

    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: StackPropsConfig, boundary: PermissionsBoundary) =
        { config with
            PermissionsBoundary = Some boundary }

    [<CustomOperation("propertyInjectors")>]
    member _.PropertyInjectors(config: StackPropsConfig, injectors: IPropertyInjector list) =
        { config with
            PropertyInjectors = Some injectors }

    [<CustomOperation("synthesizer")>]
    member _.Synthesizer(config: StackPropsConfig, synthesizer: IStackSynthesizer) =
        { config with
            Synthesizer = Some synthesizer }
