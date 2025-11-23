namespace FsCDK

open System.Collections.Generic
open Amazon.CDK.AWS.IAM

// ============================================================================
// IAM PolicyStatement Builder DSL
// ============================================================================

type PolicyStatementConfig =
    { Actions: string list
      Resources: string list
      Effect: Effect option
      Principals: IPrincipal list
      Conditions: (string * obj) list
      Sid: string option }

type PolicyStatementBuilder() =
    member _.Yield(_: unit) : PolicyStatementConfig =
        { Actions = []
          Resources = []
          Effect = None
          Principals = []
          Conditions = []
          Sid = None }

    member _.Zero() : PolicyStatementConfig =
        { Actions = []
          Resources = []
          Effect = None
          Principals = []
          Conditions = []
          Sid = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PolicyStatementConfig) : PolicyStatementConfig = f ()

    member inline x.For
        (
            config: PolicyStatementConfig,
            [<InlineIfLambda>] f: unit -> PolicyStatementConfig
        ) : PolicyStatementConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: PolicyStatementConfig, state2: PolicyStatementConfig) : PolicyStatementConfig =
        { Actions = state1.Actions @ state2.Actions
          Resources = state1.Resources @ state2.Resources
          Effect =
            if state1.Effect.IsSome then
                state1.Effect
            else
                state2.Effect
          Principals = state1.Principals @ state2.Principals
          Conditions = state1.Conditions @ state2.Conditions
          Sid = if state1.Sid.IsSome then state1.Sid else state2.Sid }

    member _.Run(config: PolicyStatementConfig) : PolicyStatement =
        // Security validation: Check for dangerous wildcard patterns
        let hasWildcardActions = config.Actions |> List.exists (fun a -> a = "*")
        let hasWildcardResources = config.Resources |> List.exists (fun r -> r = "*")

        // CRITICAL: Both wildcards together is a security violation
        if hasWildcardActions && hasWildcardResources then
            failwith
                """
SECURITY ERROR: PolicyStatement has wildcard actions ('*') AND resources ('*').

This grants unrestricted access to ALL AWS services and violates the
principle of least privilege recommended in AWS Well-Architected Framework.

Please specify:
1. Specific actions (e.g., ["s3:GetObject"; "s3:PutObject"])
2. Specific resources (e.g., ["arn:aws:s3:::my-bucket/*"])

If you truly need admin access, use a managed policy instead:
  managedPolicy (ManagedPolicy.FromAwsManagedPolicyName("AdministratorAccess"))

See docs/iam-best-practices.fsx for examples.
            """

        // WARNING: Individual wildcards should be reviewed
        if hasWildcardActions then
            eprintfn "⚠️  WARNING: Using wildcard actions ('*') in PolicyStatement."
            eprintfn "    This may grant more permissions than intended. Consider using specific actions."
            eprintfn "    Example: [\"s3:GetObject\"; \"s3:PutObject\"] instead of [\"s3:*\"]"
            eprintfn ""

        if hasWildcardResources then
            eprintfn "⚠️  WARNING: Using wildcard resources ('*') in PolicyStatement."
            eprintfn "    This applies permissions to ALL resources of the specified type."
            eprintfn "    Consider scoping to specific ARNs."
            eprintfn "    Example: [\"arn:aws:s3:::my-bucket/*\"] instead of [\"*\"]"
            eprintfn ""

        let props = PolicyStatementProps()

        if config.Actions.Length > 0 then
            props.Actions <- config.Actions |> List.toArray

        if config.Resources.Length > 0 then
            props.Resources <- config.Resources |> List.toArray

        config.Effect |> Option.iter (fun e -> props.Effect <- e)

        if config.Principals.Length > 0 then
            props.Principals <- config.Principals |> List.toArray

        config.Sid |> Option.iter (fun sid -> props.Sid <- sid)

        props.Conditions <- config.Conditions |> Seq.toList |> Map.ofList |> Dictionary

        PolicyStatement(props)

    [<CustomOperation("actions")>]
    member _.Actions(config: PolicyStatementConfig, actions: string list) = { config with Actions = actions }

    [<CustomOperation("resources")>]
    member _.Resources(config: PolicyStatementConfig, resources: string list) = { config with Resources = resources }

    [<CustomOperation("effect")>]
    member _.Effect(config: PolicyStatementConfig, effect: Effect) = { config with Effect = Some effect }

    [<CustomOperation("principals")>]
    member _.Principals(config: PolicyStatementConfig, principals: IPrincipal list) =
        { config with Principals = principals }

    [<CustomOperation("sid")>]
    member _.Sid(config: PolicyStatementConfig, sid: string) = { config with Sid = Some sid }

    [<CustomOperation("conditions")>]
    member _.Condition(config: PolicyStatementConfig, conditions: (string * obj) list) =
        { config with Conditions = conditions }

    [<CustomOperation("condition")>]
    member _.Condition(config: PolicyStatementConfig, key: string, value: string) =
        { config with
            Conditions = (key, value) :: config.Conditions }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module PolicyStatementBuilders =
    let policyStatement = PolicyStatementBuilder()
