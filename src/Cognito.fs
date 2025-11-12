namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Cognito


// ============================================================================
// Cognito User Pool Configuration DSL
// ============================================================================

type UserPoolConfig =
    { UserPoolName: string
      ConstructId: string option
      UserPoolName_: string option
      SelfSignUpEnabled: bool option
      SignInAliases: ISignInAliases option
      AutoVerify: IAutoVerifiedAttrs option
      StandardAttributes: IStandardAttributes option
      CustomAttributes: ICustomAttribute list
      PasswordPolicy: IPasswordPolicy option
      MfaConfiguration: Mfa option
      MfaSecondFactor: IMfaSecondFactor option
      AccountRecovery: AccountRecovery option
      EmailSettings: UserPoolEmail option
      SmsRole: Amazon.CDK.AWS.IAM.IRole option
      LambdaTriggers: IUserPoolTriggers option
      RemovalPolicy: RemovalPolicy option }

type UserPoolSpec =
    { UserPoolName: string
      ConstructId: string
      Props: UserPoolProps
      mutable UserPool: IUserPool option }

    /// Gets the underlying IUserPool resource. Must be called after the stack is built.
    member this.Resource =
        match this.UserPool with
        | Some vpc -> vpc
        | None ->
            failwith
                $"UserPool '{this.UserPoolName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

/// Represents a reference to a VPC that can be resolved later
type UserPoolRef =
    | UserPoolInterface of IUserPool
    | UserPoolSpecRef of UserPoolSpec

module UserPoolHelpers =
    /// Resolves a UserPool reference to an IUserPool
    let resolveUserPoolRef (ref: UserPoolRef) =
        match ref with
        | UserPoolInterface upi -> upi
        | UserPoolSpecRef spec ->
            match spec.UserPool with
            | Some vpc -> vpc
            | None ->
                failwith
                    $"UserPool '{spec.UserPoolName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type UserPoolBuilder(name: string) =

    member _.Yield _ : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = None
          StandardAttributes = None
          CustomAttributes = []
          PasswordPolicy = None
          MfaConfiguration = None
          MfaSecondFactor = None
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member _.Zero() : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = None
          StandardAttributes = None
          CustomAttributes = []
          PasswordPolicy = None
          MfaConfiguration = None
          MfaSecondFactor = None
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> UserPoolConfig) : UserPoolConfig = f ()

    member inline x.For(config: UserPoolConfig, [<InlineIfLambda>] f: unit -> UserPoolConfig) : UserPoolConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: UserPoolConfig, b: UserPoolConfig) : UserPoolConfig =
        { UserPoolName = a.UserPoolName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          UserPoolName_ =
            match a.UserPoolName_ with
            | Some _ -> a.UserPoolName_
            | None -> b.UserPoolName_
          SelfSignUpEnabled =
            match a.SelfSignUpEnabled with
            | Some _ -> a.SelfSignUpEnabled
            | None -> b.SelfSignUpEnabled
          SignInAliases =
            match a.SignInAliases with
            | Some _ -> a.SignInAliases
            | None -> b.SignInAliases
          AutoVerify =
            match a.AutoVerify with
            | Some _ -> a.AutoVerify
            | None -> b.AutoVerify
          StandardAttributes =
            match a.StandardAttributes with
            | Some _ -> a.StandardAttributes
            | None -> b.StandardAttributes
          CustomAttributes = a.CustomAttributes @ b.CustomAttributes
          PasswordPolicy =
            match a.PasswordPolicy with
            | Some _ -> a.PasswordPolicy
            | None -> b.PasswordPolicy
          MfaConfiguration =
            match a.MfaConfiguration with
            | Some _ -> a.MfaConfiguration
            | None -> b.MfaConfiguration
          MfaSecondFactor =
            match a.MfaSecondFactor with
            | Some _ -> a.MfaSecondFactor
            | None -> b.MfaSecondFactor
          AccountRecovery =
            match a.AccountRecovery with
            | Some _ -> a.AccountRecovery
            | None -> b.AccountRecovery
          EmailSettings =
            match a.EmailSettings with
            | Some _ -> a.EmailSettings
            | None -> b.EmailSettings
          SmsRole =
            match a.SmsRole with
            | Some _ -> a.SmsRole
            | None -> b.SmsRole
          LambdaTriggers =
            match a.LambdaTriggers with
            | Some _ -> a.LambdaTriggers
            | None -> b.LambdaTriggers
          RemovalPolicy =
            match a.RemovalPolicy with
            | Some _ -> a.RemovalPolicy
            | None -> b.RemovalPolicy }

    member _.Run(config: UserPoolConfig) : UserPoolSpec =
        let props = UserPoolProps()
        let constructId = config.ConstructId |> Option.defaultValue config.UserPoolName

        // AWS Best Practice: Enable email/username sign-in by default
        props.SignInAliases <-
            config.SignInAliases
            |> Option.defaultValue (SignInAliases(Email = true, Username = true))

        // AWS Best Practice: Auto-verify email by default
        props.AutoVerify <- config.AutoVerify |> Option.defaultValue (AutoVerifiedAttrs(Email = true))

        // AWS Best Practice: Strong password policy by default
        props.PasswordPolicy <-
            config.PasswordPolicy
            |> Option.defaultValue (
                PasswordPolicy(
                    MinLength = 8,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireDigits = true,
                    RequireSymbols = true
                )
            )

        // AWS Best Practice: Account recovery via email by default
        props.AccountRecovery <- config.AccountRecovery |> Option.defaultValue AccountRecovery.EMAIL_ONLY

        // AWS Best Practice: Disable self sign-up by default
        props.SelfSignUpEnabled <- config.SelfSignUpEnabled |> Option.defaultValue false

        config.UserPoolName_ |> Option.iter (fun n -> props.UserPoolName <- n)

        config.StandardAttributes
        |> Option.iter (fun a -> props.StandardAttributes <- a)

        if not (List.isEmpty config.CustomAttributes) then
            let attrDict = System.Collections.Generic.Dictionary<string, ICustomAttribute>()

            for attr in config.CustomAttributes do
                // Note: Using type name as key; for precise control, set the dictionary directly upstream if needed
                attrDict.Add(attr.GetType().Name, attr)

            props.CustomAttributes <- attrDict

        config.MfaConfiguration |> Option.iter (fun m -> props.Mfa <- m)
        config.MfaSecondFactor |> Option.iter (fun m -> props.MfaSecondFactor <- m)
        config.EmailSettings |> Option.iter (fun e -> props.Email <- e)
        config.SmsRole |> Option.iter (fun r -> props.SmsRole <- r)
        config.LambdaTriggers |> Option.iter (fun t -> props.LambdaTriggers <- t)
        config.RemovalPolicy |> Option.iter (fun r -> props.RemovalPolicy <- r)

        { UserPoolName = config.UserPoolName
          ConstructId = constructId
          Props = props
          UserPool = None }

    /// <summary>Sets the construct ID for the user pool.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: UserPoolConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the user pool name.</summary>
    [<CustomOperation("userPoolName")>]
    member _.UserPoolName(config: UserPoolConfig, name: string) =
        { config with
            UserPoolName_ = Some name }

    /// <summary>Enables or disables self sign-up.</summary>
    [<CustomOperation("selfSignUpEnabled")>]
    member _.SelfSignUpEnabled(config: UserPoolConfig, enabled: bool) =
        { config with
            SelfSignUpEnabled = Some enabled }

    /// <summary>Sets sign-in aliases.</summary>
    [<CustomOperation("signInAliases")>]
    member _.SignInAliases(config: UserPoolConfig, aliases: ISignInAliases) =
        { config with
            SignInAliases = Some aliases }

    /// <summary>Enables email and username as sign-in aliases.</summary>
    [<CustomOperation("signInWithEmailAndUsername")>]
    member _.SignInWithEmailAndUsername(config: UserPoolConfig) =
        { config with
            SignInAliases = Some(SignInAliases(Email = true, Username = true)) }

    /// <summary>Enables email only as sign-in alias.</summary>
    [<CustomOperation("signInWithEmail")>]
    member _.SignInWithEmail(config: UserPoolConfig) =
        { config with
            SignInAliases = Some(SignInAliases(Email = true)) }

    /// <summary>Sets auto-verification attributes.</summary>
    [<CustomOperation("autoVerify")>]
    member _.AutoVerify(config: UserPoolConfig, attrs: IAutoVerifiedAttrs) = { config with AutoVerify = Some attrs }

    /// <summary>Sets standard attributes.</summary>
    [<CustomOperation("standardAttributes")>]
    member _.StandardAttributes(config: UserPoolConfig, attrs: IStandardAttributes) =
        { config with
            StandardAttributes = Some attrs }

    /// <summary>Add a custom attribute (key derived from attribute type name).</summary>
    [<CustomOperation("customAttribute")>]
    member _.CustomAttribute(config: UserPoolConfig, attr: ICustomAttribute) =
        { config with
            CustomAttributes = attr :: config.CustomAttributes }

    /// <summary>Sets password policy.</summary>
    [<CustomOperation("passwordPolicy")>]
    member _.PasswordPolicy(config: UserPoolConfig, policy: IPasswordPolicy) =
        { config with
            PasswordPolicy = Some policy }

    /// <summary>Sets MFA configuration.</summary>
    [<CustomOperation("mfa")>]
    member _.Mfa(config: UserPoolConfig, mfa: Mfa) =
        { config with
            MfaConfiguration = Some mfa }

    /// <summary>Sets MFA second factor configuration.</summary>
    [<CustomOperation("mfaSecondFactor")>]
    member _.MfaSecondFactor(config: UserPoolConfig, secondFactor: IMfaSecondFactor) =
        { config with
            MfaSecondFactor = Some secondFactor }

    /// <summary>Sets account recovery method.</summary>
    [<CustomOperation("accountRecovery")>]
    member _.AccountRecovery(config: UserPoolConfig, recovery: AccountRecovery) =
        { config with
            AccountRecovery = Some recovery }

    /// <summary>Sets email settings.</summary>
    [<CustomOperation("emailSettings")>]
    member _.EmailSettings(config: UserPoolConfig, settings: UserPoolEmail) =
        { config with
            EmailSettings = Some settings }

    /// <summary>Sets the SMS role for the user pool.</summary>
    [<CustomOperation("smsRole")>]
    member _.SmsRole(config: UserPoolConfig, role: Amazon.CDK.AWS.IAM.IRole) = { config with SmsRole = Some role }

    /// <summary>Sets Lambda triggers for the user pool.</summary>
    [<CustomOperation("lambdaTriggers")>]
    member _.LambdaTriggers(config: UserPoolConfig, triggers: IUserPoolTriggers) =
        { config with
            LambdaTriggers = Some triggers }

    /// <summary>Sets the removal policy for the user pool.</summary>
    [<CustomOperation("removalPolicy")>]
    member _.RemovalPolicy(config: UserPoolConfig, policy: RemovalPolicy) =
        { config with
            RemovalPolicy = Some policy }

// ============================================================================
// Cognito User Pool Client Configuration DSL
// ============================================================================

type UserPoolClientConfig =
    { ClientName: string
      ConstructId: string option
      UserPool: UserPoolRef option
      GenerateSecret: bool option
      AuthFlows: IAuthFlow option
      OAuth: IOAuthSettings option
      PreventUserExistenceErrors: bool option
      SupportedIdentityProviders: UserPoolClientIdentityProvider list
      RefreshTokenValidity: Duration option
      AccessTokenValidity: Duration option
      IdTokenValidity: Duration option }

type UserPoolClientSpec =
    { ClientName: string
      ConstructId: string
      Props: UserPoolClientProps }

type UserPoolClientBuilder(name: string) =

    member _.Yield _ : UserPoolClientConfig =
        { ClientName = name
          ConstructId = None
          UserPool = None
          GenerateSecret = None
          AuthFlows = None
          OAuth = None
          PreventUserExistenceErrors = None
          SupportedIdentityProviders = []
          RefreshTokenValidity = None
          AccessTokenValidity = None
          IdTokenValidity = None }

    member _.Zero() : UserPoolClientConfig =
        { ClientName = name
          ConstructId = None
          UserPool = None
          GenerateSecret = None
          AuthFlows = None
          OAuth = None
          PreventUserExistenceErrors = None
          SupportedIdentityProviders = []
          RefreshTokenValidity = None
          AccessTokenValidity = None
          IdTokenValidity = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> UserPoolClientConfig) : UserPoolClientConfig = f ()

    member inline x.For
        (
            config: UserPoolClientConfig,
            [<InlineIfLambda>] f: unit -> UserPoolClientConfig
        ) : UserPoolClientConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: UserPoolClientConfig, b: UserPoolClientConfig) : UserPoolClientConfig =
        { ClientName = a.ClientName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          UserPool =
            match a.UserPool with
            | Some _ -> a.UserPool
            | None -> b.UserPool
          GenerateSecret =
            match a.GenerateSecret with
            | Some _ -> a.GenerateSecret
            | None -> b.GenerateSecret
          AuthFlows =
            match a.AuthFlows with
            | Some _ -> a.AuthFlows
            | None -> b.AuthFlows
          OAuth =
            match a.OAuth with
            | Some _ -> a.OAuth
            | None -> b.OAuth
          PreventUserExistenceErrors =
            match a.PreventUserExistenceErrors with
            | Some _ -> a.PreventUserExistenceErrors
            | None -> b.PreventUserExistenceErrors
          SupportedIdentityProviders = a.SupportedIdentityProviders @ b.SupportedIdentityProviders
          RefreshTokenValidity =
            match a.RefreshTokenValidity with
            | Some _ -> a.RefreshTokenValidity
            | None -> b.RefreshTokenValidity
          AccessTokenValidity =
            match a.AccessTokenValidity with
            | Some _ -> a.AccessTokenValidity
            | None -> b.AccessTokenValidity
          IdTokenValidity =
            match a.IdTokenValidity with
            | Some _ -> a.IdTokenValidity
            | None -> b.IdTokenValidity }

    member _.Run(config: UserPoolClientConfig) : UserPoolClientSpec =
        let props = UserPoolClientProps()
        let constructId = config.ConstructId |> Option.defaultValue config.ClientName

        // UserPool is required
        props.UserPool <-
            match config.UserPool with
            | Some pool -> UserPoolHelpers.resolveUserPoolRef pool
            | None -> invalidArg "userPool" "User Pool is required for User Pool Client"

        // AWS Best Practice: Prevent user existence errors for security
        props.PreventUserExistenceErrors <- config.PreventUserExistenceErrors |> Option.defaultValue true

        // AWS Best Practice: Don't generate secret for public clients (web/mobile)
        props.GenerateSecret <- config.GenerateSecret |> Option.defaultValue false

        // AWS Best Practice: Enable SRP and password auth flow by default
        props.AuthFlows <-
            config.AuthFlows
            |> Option.defaultValue (AuthFlow(UserSrp = true, UserPassword = true))

        config.OAuth |> Option.iter (fun o -> props.OAuth <- o)

        if not (List.isEmpty config.SupportedIdentityProviders) then
            props.SupportedIdentityProviders <- config.SupportedIdentityProviders |> List.toArray

        config.RefreshTokenValidity
        |> Option.iter (fun t -> props.RefreshTokenValidity <- t)

        config.AccessTokenValidity
        |> Option.iter (fun t -> props.AccessTokenValidity <- t)

        config.IdTokenValidity |> Option.iter (fun t -> props.IdTokenValidity <- t)

        { ClientName = config.ClientName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: UserPoolClientConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the user pool.</summary>
    [<CustomOperation("userPool")>]
    member _.UserPool(config: UserPoolClientConfig, pool: IUserPool) =
        { config with
            UserPool = Some(UserPoolRef.UserPoolInterface pool) }

    /// <summary>Sets the user pool.</summary>
    [<CustomOperation("userPool")>]
    member _.UserPool(config: UserPoolClientConfig, pool: UserPoolSpec) =
        { config with
            UserPool = Some(UserPoolRef.UserPoolSpecRef pool) }

    /// <summary>Enables or disables secret generation.</summary>
    [<CustomOperation("generateSecret")>]
    member _.GenerateSecret(config: UserPoolClientConfig, generate: bool) =
        { config with
            GenerateSecret = Some generate }

    /// <summary>Sets authentication flows.</summary>
    [<CustomOperation("authFlows")>]
    member _.AuthFlows(config: UserPoolClientConfig, flows: IAuthFlow) = { config with AuthFlows = Some flows }

    /// <summary>Sets OAuth settings.</summary>
    [<CustomOperation("oAuth")>]
    member _.OAuth(config: UserPoolClientConfig, settings: IOAuthSettings) = { config with OAuth = Some settings }

    /// <summary>Sets whether to prevent user existence errors.</summary>
    [<CustomOperation("preventUserExistenceErrors")>]
    member _.PreventUserExistenceErrors(config: UserPoolClientConfig, prevent: bool) =
        { config with
            PreventUserExistenceErrors = Some prevent }

    /// <summary>Add one supported identity provider.</summary>
    [<CustomOperation("supportedIdentityProvider")>]
    member _.SupportedIdentityProvider(config: UserPoolClientConfig, provider: UserPoolClientIdentityProvider) =
        { config with
            SupportedIdentityProviders = provider :: config.SupportedIdentityProviders }

    /// <summary>Sets token validities.</summary>
    [<CustomOperation("tokenValidities")>]
    member _.TokenValidities(config: UserPoolClientConfig, (refresh_access_id: Duration * Duration * Duration)) =
        let refreshToken, accessToken, idToken = refresh_access_id

        { config with
            RefreshTokenValidity = Some refreshToken
            AccessTokenValidity = Some accessToken
            IdTokenValidity = Some idToken }

// ============================================================================
// Cognito Resource Server Configuration DSL
// ============================================================================

type ResourceServerScope =
    { ScopeName: string
      ScopeDescription: string }

type UserPoolResourceServerConfig =
    { ResourceServerName: string
      ConstructId: string option
      UserPool: UserPoolRef option
      Identifier: string option
      Name: string option
      Scopes: ResourceServerScope list }

type UserPoolResourceServerSpec =
    { ResourceServerName: string
      ConstructId: string
      Props: CfnUserPoolResourceServerProps
      mutable ResourceServer: CfnUserPoolResourceServer option }

    member this.Resource =
        match this.ResourceServer with
        | Some rs -> rs
        | None ->
            failwith
                $"ResourceServer '{this.ResourceServerName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type UserPoolResourceServerBuilder(name: string) =

    member _.Yield _ : UserPoolResourceServerConfig =
        { ResourceServerName = name
          ConstructId = None
          UserPool = None
          Identifier = None
          Name = None
          Scopes = [] }

    member _.Zero() : UserPoolResourceServerConfig =
        { ResourceServerName = name
          ConstructId = None
          UserPool = None
          Identifier = None
          Name = None
          Scopes = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> UserPoolResourceServerConfig) : UserPoolResourceServerConfig =
        f ()

    member inline x.For
        (
            config: UserPoolResourceServerConfig,
            [<InlineIfLambda>] f: unit -> UserPoolResourceServerConfig
        ) : UserPoolResourceServerConfig =
        x.Combine(config, f ())

    member _.Combine(a: UserPoolResourceServerConfig, b: UserPoolResourceServerConfig) : UserPoolResourceServerConfig =
        { ResourceServerName = a.ResourceServerName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          UserPool =
            match a.UserPool with
            | Some _ -> a.UserPool
            | None -> b.UserPool
          Identifier =
            match a.Identifier with
            | Some _ -> a.Identifier
            | None -> b.Identifier
          Name =
            match a.Name with
            | Some _ -> a.Name
            | None -> b.Name
          Scopes = a.Scopes @ b.Scopes }

    member _.Run(config: UserPoolResourceServerConfig) : UserPoolResourceServerSpec =
        let constructId =
            config.ConstructId |> Option.defaultValue config.ResourceServerName

        let userPoolId =
            match config.UserPool with
            | Some pool -> (UserPoolHelpers.resolveUserPoolRef pool).UserPoolId
            | None -> invalidArg "userPool" "User Pool is required for Resource Server"

        let identifier = config.Identifier |> Option.defaultValue "api"
        let name = config.Name |> Option.defaultValue config.ResourceServerName

        let scopes =
            config.Scopes
            |> List.map (fun s ->
                CfnUserPoolResourceServer.ResourceServerScopeTypeProperty(
                    ScopeName = s.ScopeName,
                    ScopeDescription = s.ScopeDescription
                ))
            |> List.toArray

        let props =
            CfnUserPoolResourceServerProps(
                UserPoolId = userPoolId,
                Identifier = identifier,
                Name = name,
                Scopes = scopes
            )

        { ResourceServerName = config.ResourceServerName
          ConstructId = constructId
          Props = props
          ResourceServer = None }

    [<CustomOperation("constructId")>]
    member _.ConstructId(config: UserPoolResourceServerConfig, id: string) = { config with ConstructId = Some id }

    [<CustomOperation("userPool")>]
    member _.UserPool(config: UserPoolResourceServerConfig, pool: IUserPool) =
        { config with
            UserPool = Some(UserPoolRef.UserPoolInterface pool) }

    [<CustomOperation("userPool")>]
    member _.UserPool(config: UserPoolResourceServerConfig, pool: UserPoolSpec) =
        { config with
            UserPool = Some(UserPoolRef.UserPoolSpecRef pool) }

    [<CustomOperation("identifier")>]
    member _.Identifier(config: UserPoolResourceServerConfig, id: string) = { config with Identifier = Some id }

    [<CustomOperation("name")>]
    member _.Name(config: UserPoolResourceServerConfig, name: string) = { config with Name = Some name }

    [<CustomOperation("scope")>]
    member _.Scope(config: UserPoolResourceServerConfig, scopeName: string, description: string) =
        { config with
            Scopes =
                { ScopeName = scopeName
                  ScopeDescription = description }
                :: config.Scopes }

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module CognitoBuilders =
    /// <summary>Creates a Cognito User Pool with AWS best practices.</summary>
    /// <param name="name">The user pool name.</param>
    /// <code lang="fsharp">
    /// userPool "MyUserPool" {
    ///     signInWithEmail
    ///     selfSignUpEnabled true
    ///     mfa Mfa.OPTIONAL
    /// }
    /// </code>
    let userPool (name: string) = UserPoolBuilder name

    /// <summary>Creates a Cognito User Pool Client.</summary>
    /// <param name="name">The client name.</param>
    /// <code lang="fsharp">
    /// userPoolClient "MyAppClient" {
    ///     userPool myUserPool
    ///     generateSecret false
    /// }
    /// </code>
    let userPoolClient (name: string) = UserPoolClientBuilder name

    /// <summary>Creates a Cognito User Pool Resource Server for OAuth 2.0 scopes.</summary>
    /// <param name="name">The resource server name.</param>
    /// <code lang="fsharp">
    /// resourceServer "ApiResourceServer" {
    ///     userPool myUserPool
    ///     identifier "api"
    ///     scope "read" "Read access to resources"
    ///     scope "write" "Write access to resources"
    /// }
    /// </code>
    let resourceServer (name: string) = UserPoolResourceServerBuilder name
