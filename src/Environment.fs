namespace FsCDK

open Amazon.CDK

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
