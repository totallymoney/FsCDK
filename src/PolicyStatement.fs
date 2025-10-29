namespace FsCDK

open System
open Amazon.CDK.AWS.IAM
// ============================================================================
// IAM PolicyStatement Builder DSL
// ============================================================================

type PolicyStatementConfig =
    { Props: PolicyStatementProps option
      Actions: string seq option
      Resources: string seq option
      Effect: Effect option
      Principals: IPrincipal seq option
      Conditions: Map<string, obj> option
      Sid: string option }

    static member inline Empty =
        { Props = None
          Actions = None
          Resources = None
          Effect = None
          Principals = None
          Conditions = None
          Sid = None }

type PolicyStatementBuilder() =
    member _.Yield(_: unit) : PolicyStatementConfig = PolicyStatementConfig.Empty

    member _.Zero() : PolicyStatementConfig = PolicyStatementConfig.Empty

    member inline _.Delay([<InlineIfLambda>] f: unit -> PolicyStatementConfig) : PolicyStatementConfig = f ()

    member inline x.For
        (
            config: PolicyStatementConfig,
            [<InlineIfLambda>] f: unit -> PolicyStatementConfig
        ) : PolicyStatementConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: PolicyStatementConfig, state2: PolicyStatementConfig) : PolicyStatementConfig =
        { Props = state1.Props |> Option.orElse state2.Props
          Actions = state1.Actions |> Option.orElse state2.Actions
          Resources = state1.Resources |> Option.orElse state2.Resources
          Effect = state1.Effect |> Option.orElse state2.Effect
          Principals = state1.Principals |> Option.orElse state2.Principals
          Conditions = state1.Conditions |> Option.orElse state2.Conditions
          Sid = state1.Sid |> Option.orElse state2.Sid }

    member _.Run(config: PolicyStatementConfig) : PolicyStatement =
        let props = PolicyStatementProps()
        config.Actions |> Option.iter (fun a -> props.Actions <- (a |> Seq.toArray))
        config.Resources |> Option.iter (fun r -> props.Resources <- (r |> Seq.toArray))
        config.Effect |> Option.iter (fun e -> props.Effect <- Nullable(e))

        config.Principals
        |> Option.iter (fun pr -> props.Principals <- (pr |> Seq.toArray))

        config.Conditions |> Option.iter (fun c -> props.Conditions <- c)

        config.Sid |> Option.iter (fun sid -> props.Sid <- sid)
        PolicyStatement(props)

    /// <summary>Sets the actions for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="actions">The list of actions.</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     actions [ "s3:GetObject"; "s3:ListBucket" ]
    /// }
    /// </code>
    [<CustomOperation("actions")>]
    member _.Actions(config: PolicyStatementConfig, actions: string list) = { config with Actions = Some actions }

    /// <summary>Sets the resources for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="resources">The list of resources.</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     resources [ "arn:aws:s3:::my-bucket"; "arn:aws:s3:::my-bucket/*" ]
    /// }
    /// </code>
    [<CustomOperation("resources")>]
    member _.Resources(config: PolicyStatementConfig, resources: string list) =
        { config with
            Resources = Some resources }

    /// <summary>Sets the effect for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="effect">The effect (Allow or Deny).</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     effect Effect.ALLOW
    /// }
    /// </code>
    [<CustomOperation("effect")>]
    member _.Effect(config: PolicyStatementConfig, effect: Effect) = { config with Effect = Some effect }

    /// <summary>Sets the principals for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="principals">The list of principals.</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     principals [ ArnPrincipal("arn:aws:iam::123456789012:role/AdminRole") :> IPrincipal ]
    /// }
    /// </code>
    [<CustomOperation("principals")>]
    member _.Principals(config: PolicyStatementConfig, principals: IPrincipal list) =
        { config with
            Principals = Some principals }


    /// <summary>Sets the conditions for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="conditions">The map of conditions.</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     conditions [ ("Bool", dict [ "aws:SecureTransport", box "false" ]) ]
    /// }    /// </code>
    [<CustomOperation("conditions")>]
    member _.Conditions(config: PolicyStatementConfig, conditions: (string * obj) seq) =
        let conditionMap = conditions |> Map.ofSeq

        { config with
            Conditions = Some conditionMap }

    /// <summary>Sets the SID for the policy statement.</summary>
    /// <param name="config">The current policy statement configuration.</param>
    /// <param name="sid">The statement identifier (SID).</param>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     sid "MyStatementID"
    /// }
    /// </code>
    [<CustomOperation("sid")>]
    member _.Sid(config: PolicyStatementConfig, sid: string) = { config with Sid = Some sid }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module PolicyStatementBuilders =
    /// <summary>Creates an AWS CDK IAM PolicyStatement.</summary>
    /// <code lang="fsharp">
    /// policyStatement {
    ///     actions [ "s3: GetObject"; "s3:ListBucket" ]
    ///     resources [ "arn:aws:s3:::my-bucket"; "arn:aws:s3:::my-bucket/*" ]
    ///     effect Effect.ALLOW
    /// }
    /// </code>
    let policyStatement = PolicyStatementBuilder()
