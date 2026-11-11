namespace FsCDK

open System.Collections.Generic
open System.Text.RegularExpressions
open Amazon.CDK
open Amazon.CDK.AWS.IAM

// ============================================================================
// GitHub Actions OIDC Deploy Role DSL
// ============================================================================

/// <summary>
/// IAM role that GitHub Actions workflows assume through OIDC (no long-lived access keys).
///
/// **Trust policy:**
/// - `token.actions.githubusercontent.com:aud` must equal the audience (default "sts.amazonaws.com")
/// - `token.actions.githubusercontent.com:sub` must match one of the allowed subjects built from
///   repository + branch / tag / environment / pull_request. With GitHub's default subject format a job
///   that uses an environment gets `repo:OWNER/REPO:environment:NAME` instead of the ref subject, so
///   branches and environments are alternatives (OR), not both required. Enforce both with environment
///   protection rules (deployment branches) in GitHub.
///
/// At least one branch, tag, environment or pullRequests is required: a role that any workflow
/// of the repository can assume is refused.
///
/// The OIDC provider defaults to the account's existing
/// `arn:aws:iam::ACCOUNT:oidc-provider/token.actions.githubusercontent.com`; an account has one provider per URL.
/// </summary>
type GitHubActionsRoleConfig =
    { RoleName: string
      ConstructId: string option
      ProviderArn: string option
      Repositories: string list
      Branches: string list
      Tags: string list
      Environments: string list
      PullRequests: bool
      Audience: string option
      Description: string option
      ManagedPolicies: IManagedPolicy list
      PolicyStatements: PolicyStatement list
      MaxSessionDuration: Duration option
      PermissionsBoundary: IManagedPolicy option }

module GitHubActionsOidc =
    /// The GitHub Actions token issuer host.
    let issuerHost = "token.actions.githubusercontent.com"

    /// The audience used by aws-actions/configure-aws-credentials.
    let defaultAudience = "sts.amazonaws.com"

    /// The account's GitHub OIDC provider ARN (tokens resolve to the deploying account and partition).
    let providerArnForCurrentAccount () =
        $"arn:{Aws.PARTITION}:iam::{Aws.ACCOUNT_ID}:oidc-provider/{issuerHost}"

    let private repositoryPattern = Regex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")

    /// The `sub` claims the role trusts.
    let subjects (config: GitHubActionsRoleConfig) =
        [ for repo in config.Repositories do
              if not (repositoryPattern.IsMatch repo) then
                  invalidArg "repository" $"GitHub repository must be 'owner/name' without wildcards (got '{repo}')."

              for b in config.Branches -> $"repo:{repo}:ref:refs/heads/{b}"
              for t in config.Tags -> $"repo:{repo}:ref:refs/tags/{t}"
              for e in config.Environments -> $"repo:{repo}:environment:{e}"

              if config.PullRequests then
                  yield $"repo:{repo}:pull_request" ]

    /// Builds the trust conditions: exact aud, and sub with StringEquals (or StringLike when a pattern has wildcards).
    let conditions (audience: string) (subjects: string list) : IDictionary<string, obj> =
        let subValue: obj =
            match subjects with
            | [ single ] -> single
            | many -> Array.ofList many

        let audKey = $"{issuerHost}:aud"
        let subKey = $"{issuerHost}:sub"

        let block (pairs: (string * obj) list) =
            let d = Dictionary<string, obj>()

            for k, v in pairs do
                d.[k] <- v

            d :> obj

        let result = Dictionary<string, obj>()

        if subjects |> List.exists (fun s -> s.Contains "*" || s.Contains "?") then
            result.["StringEquals"] <- block [ audKey, box audience ]
            result.["StringLike"] <- block [ subKey, subValue ]
        else
            result.["StringEquals"] <- block [ audKey, box audience; subKey, subValue ]

        result

type GitHubActionsRoleBuilder(name: string) =
    member _.Yield(_: unit) : GitHubActionsRoleConfig =
        { RoleName = name
          ConstructId = None
          ProviderArn = None
          Repositories = []
          Branches = []
          Tags = []
          Environments = []
          PullRequests = false
          Audience = None
          Description = None
          ManagedPolicies = []
          PolicyStatements = []
          MaxSessionDuration = None
          PermissionsBoundary = None }

    member x.Yield(statement: PolicyStatement) : GitHubActionsRoleConfig =
        { x.Yield(()) with
            PolicyStatements = [ statement ] }

    member x.Zero() : GitHubActionsRoleConfig = x.Yield(())

    member inline _.Delay([<InlineIfLambda>] f: unit -> GitHubActionsRoleConfig) : GitHubActionsRoleConfig = f ()

    member _.Combine(state1: GitHubActionsRoleConfig, state2: GitHubActionsRoleConfig) : GitHubActionsRoleConfig =
        { RoleName = state1.RoleName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          ProviderArn = state2.ProviderArn |> Option.orElse state1.ProviderArn
          Repositories = state1.Repositories @ state2.Repositories
          Branches = state1.Branches @ state2.Branches
          Tags = state1.Tags @ state2.Tags
          Environments = state1.Environments @ state2.Environments
          PullRequests = state1.PullRequests || state2.PullRequests
          Audience = state2.Audience |> Option.orElse state1.Audience
          Description = state2.Description |> Option.orElse state1.Description
          ManagedPolicies = state1.ManagedPolicies @ state2.ManagedPolicies
          PolicyStatements = state1.PolicyStatements @ state2.PolicyStatements
          MaxSessionDuration = state2.MaxSessionDuration |> Option.orElse state1.MaxSessionDuration
          PermissionsBoundary = state2.PermissionsBoundary |> Option.orElse state1.PermissionsBoundary }

    member inline x.For
        (
            config: GitHubActionsRoleConfig,
            [<InlineIfLambda>] f: unit -> GitHubActionsRoleConfig
        ) : GitHubActionsRoleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: GitHubActionsRoleConfig) : RoleSpec =
        if config.Repositories.IsEmpty then
            invalidArg "repository" $"GitHub Actions role '{config.RoleName}' needs a repository."

        let subjects = GitHubActionsOidc.subjects config

        if subjects.IsEmpty then
            invalidArg
                "branch"
                $"GitHub Actions role '{config.RoleName}' must be limited by branch, tag, environment or pullRequests."

        let providerArn =
            config.ProviderArn
            |> Option.defaultWith GitHubActionsOidc.providerArnForCurrentAccount

        let audience =
            config.Audience |> Option.defaultValue GitHubActionsOidc.defaultAudience

        let principal =
            FederatedPrincipal(
                providerArn,
                GitHubActionsOidc.conditions audience subjects,
                "sts:AssumeRoleWithWebIdentity"
            )

        let props = RoleProps()
        props.RoleName <- config.RoleName
        props.AssumedBy <- principal

        props.Description <-
            config.Description
            |> Option.defaultValue $"""GitHub Actions deploy role for {String.concat ", " config.Repositories}"""

        props.MaxSessionDuration <- config.MaxSessionDuration |> Option.defaultValue (Duration.Hours(1.0))

        if not config.ManagedPolicies.IsEmpty then
            props.ManagedPolicies <- Array.ofList config.ManagedPolicies

        config.PermissionsBoundary
        |> Option.iter (fun b -> props.PermissionsBoundary <- b)

        { RoleName = config.RoleName
          ConstructId = config.ConstructId |> Option.defaultValue config.RoleName
          Props = props
          PolicyStatements = config.PolicyStatements
          Role = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: GitHubActionsRoleConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Uses this OIDC provider (e.g. one created with oidcProvider and bound with let!).</summary>
    /// <param name="provider">The provider.</param>
    [<CustomOperation("provider")>]
    member _.Provider(config: GitHubActionsRoleConfig, provider: IOpenIdConnectProvider) =
        { config with
            ProviderArn = Some provider.OpenIdConnectProviderArn }

    /// <summary>Uses the OIDC provider with this ARN.</summary>
    /// <param name="arn">The provider ARN.</param>
    [<CustomOperation("providerArn")>]
    member _.ProviderArn(config: GitHubActionsRoleConfig, arn: string) = { config with ProviderArn = Some arn }

    /// <summary>Trusts workflows of this repository ("owner/name"). Can be used several times.</summary>
    /// <param name="repository">The repository.</param>
    /// <code lang="fsharp">
    /// githubActionsRole "Deploy" {
    ///     repository "mycorp/platform"
    ///     branch "main"
    /// }
    /// </code>
    [<CustomOperation("repository")>]
    member _.Repository(config: GitHubActionsRoleConfig, repository: string) =
        { config with
            Repositories = config.Repositories @ [ repository ] }

    /// <summary>Trusts workflow runs on this branch (wildcards like "release/*" allowed).</summary>
    /// <param name="branch">Branch name without refs/heads/.</param>
    [<CustomOperation("branch")>]
    member _.Branch(config: GitHubActionsRoleConfig, branch: string) =
        { config with
            Branches = config.Branches @ [ branch ] }

    /// <summary>Trusts workflow runs for this tag (wildcards like "v*" allowed).</summary>
    /// <param name="tag">Tag name without refs/tags/.</param>
    [<CustomOperation("tag")>]
    member _.Tag(config: GitHubActionsRoleConfig, tag: string) =
        { config with
            Tags = config.Tags @ [ tag ] }

    /// <summary>Trusts jobs that run in this GitHub environment.</summary>
    /// <param name="environment">The environment name, e.g. "production".</param>
    [<CustomOperation("environment")>]
    member _.Environment(config: GitHubActionsRoleConfig, environment: string) =
        { config with
            Environments = config.Environments @ [ environment ] }

    /// <summary>Trusts pull_request workflow runs (use only for read-only roles such as cdk diff).</summary>
    [<CustomOperation("pullRequests")>]
    member _.PullRequests(config: GitHubActionsRoleConfig) = { config with PullRequests = true }

    /// <summary>Sets the expected token audience (default: sts.amazonaws.com).</summary>
    [<CustomOperation("audience")>]
    member _.Audience(config: GitHubActionsRoleConfig, audience: string) =
        { config with Audience = Some audience }

    /// <summary>Sets the role description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: GitHubActionsRoleConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Attaches a managed policy.</summary>
    [<CustomOperation("managedPolicy")>]
    member _.ManagedPolicy(config: GitHubActionsRoleConfig, policy: IManagedPolicy) =
        { config with
            ManagedPolicies = config.ManagedPolicies @ [ policy ] }

    /// <summary>Adds an inline policy statement.</summary>
    [<CustomOperation("policyStatement")>]
    member _.PolicyStatement(config: GitHubActionsRoleConfig, statement: PolicyStatement) =
        { config with
            PolicyStatements = config.PolicyStatements @ [ statement ] }

    /// <summary>
    /// Allows assuming the CDK bootstrap roles (deploy, file/image publishing, lookup) of the deploying account,
    /// which is all `cdk deploy` needs. Uses the default bootstrap qualifier "hnb659fds".
    /// </summary>
    [<CustomOperation("cdkDeployAccess")>]
    member _.CdkDeployAccess(config: GitHubActionsRoleConfig) =
        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Actions = [| "sts:AssumeRole" |],
                    Resources = [| $"arn:{Aws.PARTITION}:iam::{Aws.ACCOUNT_ID}:role/cdk-hnb659fds-*" |]
                )
            )

        { config with
            PolicyStatements = config.PolicyStatements @ [ statement ] }

    /// <summary>Sets the maximum session duration (default: 1 hour).</summary>
    [<CustomOperation("maxSessionDuration")>]
    member _.MaxSessionDuration(config: GitHubActionsRoleConfig, duration: Duration) =
        { config with
            MaxSessionDuration = Some duration }

    /// <summary>Sets a permissions boundary.</summary>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: GitHubActionsRoleConfig, boundary: IManagedPolicy) =
        { config with
            PermissionsBoundary = Some boundary }

[<AutoOpen>]
module GitHubActionsBuilders =
    /// <summary>Creates an IAM role for GitHub Actions OIDC deployments. Yields a RoleSpec (bind with let! for IRole).</summary>
    /// <param name="name">The role name.</param>
    /// <code lang="fsharp">
    /// githubActionsRole "platform-deploy" {
    ///     repository "mycorp/platform"
    ///     branch "main"
    ///     environment "production"
    ///     cdkDeployAccess
    /// }
    /// </code>
    let githubActionsRole (name: string) = GitHubActionsRoleBuilder name
