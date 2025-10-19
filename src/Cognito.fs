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
      Props: UserPoolProps }

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

    member _.Combine(state1: UserPoolConfig, state2: UserPoolConfig) : UserPoolConfig =
        { UserPoolName = state1.UserPoolName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          UserPoolName_ =
            if state1.UserPoolName_.IsSome then
                state1.UserPoolName_
            else
                state2.UserPoolName_
          SelfSignUpEnabled =
            if state1.SelfSignUpEnabled.IsSome then
                state1.SelfSignUpEnabled
            else
                state2.SelfSignUpEnabled
          SignInAliases =
            if state1.SignInAliases.IsSome then
                state1.SignInAliases
            else
                state2.SignInAliases
          AutoVerify =
            if state1.AutoVerify.IsSome then
                state1.AutoVerify
            else
                state2.AutoVerify
          StandardAttributes =
            if state1.StandardAttributes.IsSome then
                state1.StandardAttributes
            else
                state2.StandardAttributes
          CustomAttributes = state1.CustomAttributes @ state2.CustomAttributes
          PasswordPolicy =
            if state1.PasswordPolicy.IsSome then
                state1.PasswordPolicy
            else
                state2.PasswordPolicy
          MfaConfiguration =
            if state1.MfaConfiguration.IsSome then
                state1.MfaConfiguration
            else
                state2.MfaConfiguration
          MfaSecondFactor =
            if state1.MfaSecondFactor.IsSome then
                state1.MfaSecondFactor
            else
                state2.MfaSecondFactor
          AccountRecovery =
            if state1.AccountRecovery.IsSome then
                state1.AccountRecovery
            else
                state2.AccountRecovery
          EmailSettings =
            if state1.EmailSettings.IsSome then
                state1.EmailSettings
            else
                state2.EmailSettings
          SmsRole =
            if state1.SmsRole.IsSome then
                state1.SmsRole
            else
                state2.SmsRole
          LambdaTriggers =
            if state1.LambdaTriggers.IsSome then
                state1.LambdaTriggers
            else
                state2.LambdaTriggers
          RemovalPolicy =
            if state1.RemovalPolicy.IsSome then
                state1.RemovalPolicy
            else
                state2.RemovalPolicy }

    member _.Run(config: UserPoolConfig) : UserPoolSpec =
        let props = UserPoolProps()
        let constructId = config.ConstructId |> Option.defaultValue config.UserPoolName

        // AWS Best Practice: Enable email/username sign-in by default
        props.SignInAliases <-
            config.SignInAliases
            |> Option.defaultValue (SignInAliases(Email = true, Username = true))

        // AWS Best Practice: Auto-verify email by default
        props.AutoVerify <-
            config.AutoVerify |> Option.defaultValue (AutoVerifiedAttrs(Email = true))

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
        props.AccountRecovery <-
            config.AccountRecovery |> Option.defaultValue AccountRecovery.EMAIL_ONLY

        // AWS Best Practice: Disable self sign-up by default for security
        // Applications should explicitly enable if needed
        props.SelfSignUpEnabled <- config.SelfSignUpEnabled |> Option.defaultValue false

        config.UserPoolName_
        |> Option.iter (fun n -> props.UserPoolName <- n)

        config.StandardAttributes
        |> Option.iter (fun a -> props.StandardAttributes <- a)

        if not (List.isEmpty config.CustomAttributes) then
            let attrDict = System.Collections.Generic.Dictionary<string, ICustomAttribute>()

            for attr in config.CustomAttributes do
                // Custom attributes need unique keys - using their type name as key
                attrDict.Add(attr.GetType().Name, attr)

            props.CustomAttributes <- attrDict

        config.MfaConfiguration
        |> Option.iter (fun m -> props.Mfa <- m)

        config.MfaSecondFactor
        |> Option.iter (fun m -> props.MfaSecondFactor <- m)

        config.EmailSettings
        |> Option.iter (fun e -> props.Email <- e)

        config.SmsRole |> Option.iter (fun r -> props.SmsRole <- r)

        config.LambdaTriggers
        |> Option.iter (fun t -> props.LambdaTriggers <- t)

        config.RemovalPolicy
        |> Option.iter (fun r -> props.RemovalPolicy <- r)

        { UserPoolName = config.UserPoolName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID for the user pool.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: UserPoolConfig, id: string) =
        { config with ConstructId = Some id }

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
    member _.AutoVerify(config: UserPoolConfig, attrs: IAutoVerifiedAttrs) =
        { config with AutoVerify = Some attrs }

    /// <summary>Sets standard attributes.</summary>
    [<CustomOperation("standardAttributes")>]
    member _.StandardAttributes(config: UserPoolConfig, attrs: IStandardAttributes) =
        { config with
            StandardAttributes = Some attrs }

    /// <summary>Sets password policy.</summary>
    [<CustomOperation("passwordPolicy")>]
    member _.PasswordPolicy(config: UserPoolConfig, policy: IPasswordPolicy) =
        { config with
            PasswordPolicy = Some policy }

    /// <summary>Sets MFA configuration.</summary>
    [<CustomOperation("mfa")>]
    member _.Mfa(config: UserPoolConfig, mfa: Mfa) = { config with MfaConfiguration = Some mfa }

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

// ============================================================================
// Cognito User Pool Client Configuration DSL
// ============================================================================

type UserPoolClientConfig =
    { ClientName: string
      ConstructId: string option
      UserPool: IUserPool option
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

    member inline x.For(config: UserPoolClientConfig, [<InlineIfLambda>] f: unit -> UserPoolClientConfig) : UserPoolClientConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: UserPoolClientConfig, state2: UserPoolClientConfig) : UserPoolClientConfig =
        { ClientName = state1.ClientName
          ConstructId =
            if state1.ConstructId.IsSome then
                state1.ConstructId
            else
                state2.ConstructId
          UserPool =
            if state1.UserPool.IsSome then
                state1.UserPool
            else
                state2.UserPool
          GenerateSecret =
            if state1.GenerateSecret.IsSome then
                state1.GenerateSecret
            else
                state2.GenerateSecret
          AuthFlows =
            if state1.AuthFlows.IsSome then
                state1.AuthFlows
            else
                state2.AuthFlows
          OAuth =
            if state1.OAuth.IsSome then
                state1.OAuth
            else
                state2.OAuth
          PreventUserExistenceErrors =
            if state1.PreventUserExistenceErrors.IsSome then
                state1.PreventUserExistenceErrors
            else
                state2.PreventUserExistenceErrors
          SupportedIdentityProviders = state1.SupportedIdentityProviders @ state2.SupportedIdentityProviders
          RefreshTokenValidity =
            if state1.RefreshTokenValidity.IsSome then
                state1.RefreshTokenValidity
            else
                state2.RefreshTokenValidity
          AccessTokenValidity =
            if state1.AccessTokenValidity.IsSome then
                state1.AccessTokenValidity
            else
                state2.AccessTokenValidity
          IdTokenValidity =
            if state1.IdTokenValidity.IsSome then
                state1.IdTokenValidity
            else
                state2.IdTokenValidity }

    member _.Run(config: UserPoolClientConfig) : UserPoolClientSpec =
        let props = UserPoolClientProps()
        let constructId = config.ConstructId |> Option.defaultValue config.ClientName

        // UserPool is required
        props.UserPool <-
            match config.UserPool with
            | Some pool -> pool
            | None -> failwith "User Pool is required for User Pool Client"

        // AWS Best Practice: Prevent user existence errors for security
        props.PreventUserExistenceErrors <-
            config.PreventUserExistenceErrors |> Option.defaultValue true

        // AWS Best Practice: Don't generate secret for public clients (web/mobile)
        props.GenerateSecret <- config.GenerateSecret |> Option.defaultValue false

        // AWS Best Practice: Enable SRP auth flow by default
        props.AuthFlows <-
            config.AuthFlows
            |> Option.defaultValue (AuthFlow(UserSrp = true, UserPassword = true))

        config.OAuth |> Option.iter (fun o -> props.OAuth <- o)

        if not (List.isEmpty config.SupportedIdentityProviders) then
            props.SupportedIdentityProviders <-
                config.SupportedIdentityProviders |> List.toArray

        config.RefreshTokenValidity
        |> Option.iter (fun t -> props.RefreshTokenValidity <- t)

        config.AccessTokenValidity
        |> Option.iter (fun t -> props.AccessTokenValidity <- t)

        config.IdTokenValidity
        |> Option.iter (fun t -> props.IdTokenValidity <- t)

        { ClientName = config.ClientName
          ConstructId = constructId
          Props = props }

    /// <summary>Sets the construct ID.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: UserPoolClientConfig, id: string) =
        { config with ConstructId = Some id }

    /// <summary>Sets the user pool.</summary>
    [<CustomOperation("userPool")>]
    member _.UserPool(config: UserPoolClientConfig, pool: IUserPool) =
        { config with UserPool = Some pool }

    /// <summary>Enables or disables secret generation.</summary>
    [<CustomOperation("generateSecret")>]
    member _.GenerateSecret(config: UserPoolClientConfig, generate: bool) =
        { config with
            GenerateSecret = Some generate }

    /// <summary>Sets authentication flows.</summary>
    [<CustomOperation("authFlows")>]
    member _.AuthFlows(config: UserPoolClientConfig, flows: IAuthFlow) =
        { config with AuthFlows = Some flows }

    /// <summary>Sets OAuth settings.</summary>
    [<CustomOperation("oAuth")>]
    member _.OAuth(config: UserPoolClientConfig, settings: IOAuthSettings) =
        { config with OAuth = Some settings }

    /// <summary>Sets token validities.</summary>
    [<CustomOperation("tokenValidities")>]
    member _.TokenValidities
        (
            config: UserPoolClientConfig,
            ?refreshToken: Duration,
            ?accessToken: Duration,
            ?idToken: Duration
        ) =
        { config with
            RefreshTokenValidity = refreshToken
            AccessTokenValidity = accessToken
            IdTokenValidity = idToken }

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
    let userPool (name: string) = UserPoolBuilder(name)

    /// <summary>Creates a Cognito User Pool Client.</summary>
    /// <param name="name">The client name.</param>
    /// <code lang="fsharp">
    /// userPoolClient "MyAppClient" {
    ///     userPool myUserPool
    ///     generateSecret false
    /// }
    /// </code>
    let userPoolClient (name: string) = UserPoolClientBuilder(name)
