namespace FsCDK

open Amazon.CDK
open Constructs

// ============================================================================
// Stage onfiguration DSL
// ============================================================================

type StageConfig =
    { Env: IEnvironment option
      Outdir: string option
      PermissionsBoundary: PermissionsBoundary option
      PolicyValidationBeta1: IPolicyValidationPluginBeta1 seq option
      PropertyInjectors: IPropertyInjector seq option
      Name: string
      Construct: Construct option }

type StageBuilder(name: string) =

    member _.Yield(_: unit) : StageConfig =
        { Env = None
          Name = name
          Outdir = None
          PermissionsBoundary = None
          PolicyValidationBeta1 = None
          PropertyInjectors = None
          Construct = None }

    member _.Yield(env: IEnvironment) : StageConfig =
        { Env = Some env
          Name = name
          Outdir = None
          PermissionsBoundary = None
          PolicyValidationBeta1 = None
          PropertyInjectors = None
          Construct = None }

    member _.Yield(construct: Construct) : StageConfig =
        { Env = None
          Name = name
          Outdir = None
          PermissionsBoundary = None
          PolicyValidationBeta1 = None
          PropertyInjectors = None
          Construct = Some construct }

    member _.Zero() : StageConfig =
        { Env = None
          Name = name
          Outdir = None
          PermissionsBoundary = None
          PolicyValidationBeta1 = None
          PropertyInjectors = None
          Construct = None }

    member _.Combine(state1: StageConfig, state2: StageConfig) : StageConfig =
        { Env = state2.Env |> Option.orElse state1.Env
          Name = state1.Name
          Outdir = state2.Outdir |> Option.orElse state1.Outdir
          PermissionsBoundary = state2.PermissionsBoundary |> Option.orElse state1.PermissionsBoundary
          PolicyValidationBeta1 = state2.PolicyValidationBeta1 |> Option.orElse state1.PolicyValidationBeta1
          PropertyInjectors = state2.PropertyInjectors |> Option.orElse state1.PropertyInjectors
          Construct = state2.Construct |> Option.orElse state1.Construct }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StageConfig) : StageConfig = f ()

    member inline x.For(config: StageConfig, [<InlineIfLambda>] f: unit -> StageConfig) : StageConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.Run(config: StageConfig) =
        let props = StageProps()
        props.StageName <- config.Name
        config.Env |> Option.iter (fun v -> props.Env <- v)
        let construct = config.Construct |> Option.defaultWith (fun () -> App())
        Stage(construct, config.Name, props)

    /// <summary>Sets the permissions boundary for the stage.</summary>
    /// <param name="config">The current stage configuration.</param>
    /// <param name="pb">The permissions boundary to apply.</param>
    /// <code lang="fsharp">
    /// stage "MyStage" {
    ///     permissionsBoundary myPermissionsBoundary
    /// }
    /// </code>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: StageConfig, pb: PermissionsBoundary) =
        { config with
            PermissionsBoundary = Some pb }

    /// <summary>Sets the output directory for the stage.</summary>
    /// <param name="config">The current stage configuration.</param>
    /// <param name="outdir">The output directory path.</param>
    /// <code lang="fsharp">
    /// stage "MyStage" {
    ///     outdir "cdk.out/mystage"
    /// }
    /// </code>
    [<CustomOperation("outdir")>]
    member _.Outdir(config: StageConfig, outdir: string) = { config with Outdir = Some outdir }

    /// <summary>Adds policy validation plugins to the stage.</summary>
    /// <param name="config">The current stage configuration.</param>
    /// <param name="plugins">A sequence of policy validation plugins.</param>
    /// <code lang="fsharp">
    /// stage "MyStage" {
    ///     policyValidationBeta1 [ myPolicyPlugin1; myPolicyPlugin2 ]
    /// }
    /// </code>
    [<CustomOperation("policyValidationBeta1")>]
    member _.PolicyValidationBeta1(config: StageConfig, plugins: IPolicyValidationPluginBeta1 seq) =
        { config with
            PolicyValidationBeta1 = Some plugins }

    /// <summary>Adds property injectors to the stage.</summary>
    /// <param name="config">The current stage configuration.</param>
    /// <param name="injectors">A sequence of property injectors.</param>
    /// <code lang="fsharp">
    /// stage "MyStage" {
    ///     propertyInjectors [ myInjector1; myInjector2 ]
    /// }
    /// </code>
    [<CustomOperation("propertyInjectors")>]
    member _.PropertyInjectors(config: StageConfig, injectors: IPropertyInjector seq) =
        { config with
            PropertyInjectors = Some injectors }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module StageBuilders =
    /// <summary>Creates an AWS CDK Stage construct.</summary>
    /// <param name="name">The name of the stage.</param>
    /// <code lang="fsharp">
    /// stage "MyStage" {
    ///     outdir "cdk.out/mystage"
    ///    permissionsBoundary myPermissionsBoundary
    /// }
    /// </code>
    let stage name = StageBuilder(name)
