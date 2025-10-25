namespace FsCDK

open Amazon.CDK.AWS.IAM

// ============================================================================
// OIDC Provider Configuration DSL
// ============================================================================

/// <summary>
/// High-level OIDC Provider builder for federated identity in IAM.
///
/// **Use Cases: **
/// - GitHub Actions authentication
/// - GitLab CI/CD authentication
/// - Other OIDC-based identity providers
///
/// **Security Best Practices: **
/// - Limit client IDs to known applications
/// - Use thumbprints to verify the identity provider's certificate
/// - Apply least-privilege IAM policies to federated roles
///
/// **Escape Hatch: **
/// Access the underlying CDK OpenIdConnectProvider via the `Provider` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type OIDCProviderConfig =
    { ProviderName: string
      ConstructId: string option
      Url: string option
      ClientIds: string list
      Thumbprints: string list }

type OIDCProviderSpec =
    { ProviderName: string
      ConstructId: string
      Props: OpenIdConnectProviderProps
      mutable Provider: IOpenIdConnectProvider option }

    /// Gets the underlying IOpenIdConnectProvider resource. Must be called after the stack is built.
    member this.Resource =
        match this.Provider with
        | Some provider -> provider
        | None ->
            failwith
                $"OIDCProvider '{this.ProviderName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

    /// Gets the provider ARN for use in IAM role trust policies
    member this.Arn =
        match this.Provider with
        | Some provider -> provider.OpenIdConnectProviderArn
        | None ->
            failwith
                $"OIDCProvider '{this.ProviderName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type OIDCProviderBuilder(name: string) =
    member _.Yield _ : OIDCProviderConfig =
        { ProviderName = name
          ConstructId = None
          Url = None
          ClientIds = []
          Thumbprints = [] }

    member _.Zero() : OIDCProviderConfig =
        { ProviderName = name
          ConstructId = None
          Url = None
          ClientIds = []
          Thumbprints = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> OIDCProviderConfig) : OIDCProviderConfig = f ()

    member inline x.For
        (
            config: OIDCProviderConfig,
            [<InlineIfLambda>] f: unit -> OIDCProviderConfig
        ) : OIDCProviderConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: OIDCProviderConfig, b: OIDCProviderConfig) : OIDCProviderConfig =
        { ProviderName = a.ProviderName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          Url =
            match a.Url with
            | Some _ -> a.Url
            | None -> b.Url
          ClientIds = a.ClientIds @ b.ClientIds
          Thumbprints = a.Thumbprints @ b.Thumbprints }

    member _.Run(config: OIDCProviderConfig) : OIDCProviderSpec =
        let props = OpenIdConnectProviderProps()
        let constructId = config.ConstructId |> Option.defaultValue config.ProviderName

        // URL is required
        props.Url <-
            match config.Url with
            | Some url -> url
            | None -> invalidArg "url" "OIDC provider URL is required"

        // Client IDs are optional but recommended
        if not (List.isEmpty config.ClientIds) then
            props.ClientIds <- config.ClientIds |> List.toArray

        // Thumbprints are optional (CDK can fetch automatically)
        if not (List.isEmpty config.Thumbprints) then
            props.Thumbprints <- config.Thumbprints |> List.toArray

        { ProviderName = config.ProviderName
          ConstructId = constructId
          Props = props
          Provider = None }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: OIDCProviderConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the OIDC provider URL.</summary>
    /// <param name="url">The provider URL (e.g., "https://token.actions.githubusercontent.com").</param>
    [<CustomOperation("url")>]
    member _.Url(config: OIDCProviderConfig, url: string) = { config with Url = Some url }

    /// <summary>Adds a client ID.</summary>
    [<CustomOperation("clientId")>]
    member _.ClientId(config: OIDCProviderConfig, clientId: string) =
        { config with
            ClientIds = clientId :: config.ClientIds }

    /// <summary>Adds a certificate thumbprint.</summary>
    [<CustomOperation("thumbprint")>]
    member _.Thumbprint(config: OIDCProviderConfig, thumbprint: string) =
        { config with
            Thumbprints = thumbprint :: config.Thumbprints }

// ============================================================================
// Helper module for common OIDC providers
// ============================================================================

module OIDCProviders =
    /// GitHub Actions OIDC provider URL
    let GitHubActionsUrl = "https://token.actions.githubusercontent.com"

    /// GitHub Actions default client ID (audience)
    let GitHubActionsClientId = "sts.amazonaws.com"

    /// GitLab OIDC provider URL
    let GitLabUrl = "https://gitlab.com"

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module OIDCProviderBuilders =
    /// <summary>Creates an OIDC Provider for federated identity.</summary>
    /// <param name="name">The provider name.</param>
    /// <code lang="fsharp">
    /// oidcProvider "GitHubActions" {
    ///     url "https://token.actions.githubusercontent.com"
    ///     clientId "sts.amazonaws.com"
    /// }
    /// </code>
    let oidcProvider (name: string) = OIDCProviderBuilder(name)
