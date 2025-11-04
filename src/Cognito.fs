namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.Lambda

// ============================================================================
// Cognito User Pool Configuration DSL
// ============================================================================

type PasswordPolicyConfig =
    { MinLength: float option
      PasswordHistorySize: float option
      RequireDigits: bool option
      RequireLowercase: bool option
      RequireUppercase: bool option
      RequireSymbols: bool option
      TempPasswordValidity: Duration option }

type PasswordPolicyBuilder() =
    member _.Yield _ : PasswordPolicyConfig =
        { MinLength = None
          PasswordHistorySize = None
          RequireDigits = None
          RequireLowercase = None
          RequireUppercase = None
          RequireSymbols = None
          TempPasswordValidity = None }

    member _.Zero() : PasswordPolicyConfig =
        { MinLength = None
          PasswordHistorySize = None
          RequireDigits = None
          RequireLowercase = None
          RequireUppercase = None
          RequireSymbols = None
          TempPasswordValidity = None }

    member _.Combine(a: PasswordPolicyConfig, b: PasswordPolicyConfig) : PasswordPolicyConfig =
        { MinLength = a.MinLength |> Option.orElse b.MinLength
          PasswordHistorySize = a.PasswordHistorySize |> Option.orElse b.PasswordHistorySize
          RequireDigits = a.RequireDigits |> Option.orElse b.RequireDigits
          RequireLowercase = a.RequireLowercase |> Option.orElse b.RequireLowercase
          RequireUppercase = a.RequireUppercase |> Option.orElse b.RequireUppercase
          RequireSymbols = a.RequireSymbols |> Option.orElse b.RequireSymbols
          TempPasswordValidity = a.TempPasswordValidity |> Option.orElse b.TempPasswordValidity }

    member inline this.For
        (
            config: PasswordPolicyConfig,
            [<InlineIfLambda>] f: unit -> PasswordPolicyConfig
        ) : PasswordPolicyConfig =
        let newConfig = f ()
        this.Combine(config, newConfig)

    member _.Run(config: PasswordPolicyConfig) : IPasswordPolicy =
        let policy = PasswordPolicy()
        config.MinLength |> Option.iter (fun v -> policy.MinLength <- v)

        config.PasswordHistorySize
        |> Option.iter (fun v -> policy.PasswordHistorySize <- v)

        config.RequireDigits |> Option.iter (fun v -> policy.RequireDigits <- v)
        config.RequireLowercase |> Option.iter (fun v -> policy.RequireLowercase <- v)
        config.RequireUppercase |> Option.iter (fun v -> policy.RequireUppercase <- v)
        config.RequireSymbols |> Option.iter (fun v -> policy.RequireSymbols <- v)

        config.TempPasswordValidity
        |> Option.iter (fun v -> policy.TempPasswordValidity <- v)

        policy

    [<CustomOperation("minLength")>]
    member _.MinLength(config: PasswordPolicyConfig, length: float) = { config with MinLength = Some length }

    [<CustomOperation("passwordHistorySize")>]
    member _.PasswordHistorySize(config: PasswordPolicyConfig, size: float) =
        { config with
            PasswordHistorySize = Some size }

    [<CustomOperation("requireDigits")>]
    member _.RequireDigits(config: PasswordPolicyConfig, require: bool) =
        { config with
            RequireDigits = Some require }

    [<CustomOperation("requireLowercase")>]
    member _.RequireLowercase(config: PasswordPolicyConfig, require: bool) =
        { config with
            RequireLowercase = Some require }

    [<CustomOperation("requireUppercase")>]
    member _.RequireUppercase(config: PasswordPolicyConfig, require: bool) =
        { config with
            RequireUppercase = Some require }

    [<CustomOperation("requireSymbols")>]
    member _.RequireSymbols(config: PasswordPolicyConfig, require: bool) =
        { config with
            RequireSymbols = Some require }

    [<CustomOperation("tempPasswordValidity")>]
    member _.TempPasswordValidity(config: PasswordPolicyConfig, duration: Duration) =
        { config with
            TempPasswordValidity = Some duration }

[<AutoOpen>]
module PasswordPolicyBuilders =
    /// <summary>Creates a Password Policy configuration.</summary>
    /// <code lang="fsharp">
    /// passwordPolicy {
    ///     minLength 10
    ///     requireDigits true
    /// }
    /// </code>
    let passwordPolicy = PasswordPolicyBuilder()

// ============================================================================
// Additional builders for implicit yields
// ============================================================================

// SignInAliases

type SignInAliasesConfig =
    { Email: bool option
      Phone: bool option
      PreferredUsername: bool option
      Username: bool option }

    static member Empty =
        { Email = None
          Phone = None
          PreferredUsername = None
          Username = None }

type SignInAliasesBuilder() =
    member _.Yield(_: unit) : SignInAliasesConfig = SignInAliasesConfig.Empty
    member _.Zero() : SignInAliasesConfig = SignInAliasesConfig.Empty

    member _.Combine(a: SignInAliasesConfig, b: SignInAliasesConfig) : SignInAliasesConfig =
        { Email = a.Email |> Option.orElse b.Email
          Phone = a.Phone |> Option.orElse b.Phone
          PreferredUsername = a.PreferredUsername |> Option.orElse b.PreferredUsername
          Username = a.Username |> Option.orElse b.Username }

    member inline this.For(cfg: SignInAliasesConfig, [<InlineIfLambda>] f: unit -> SignInAliasesConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: SignInAliasesConfig) : ISignInAliases =
        let s = SignInAliases()
        cfg.Email |> Option.iter (fun v -> s.Email <- v)
        cfg.Phone |> Option.iter (fun v -> s.Phone <- v)
        cfg.PreferredUsername |> Option.iter (fun v -> s.PreferredUsername <- v)
        cfg.Username |> Option.iter (fun v -> s.Username <- v)
        s

    [<CustomOperation("email")>]
    member _.Email(cfg: SignInAliasesConfig, v: bool) = { cfg with Email = Some v }

    [<CustomOperation("phone")>]
    member _.Phone(cfg: SignInAliasesConfig, v: bool) = { cfg with Phone = Some v }

    [<CustomOperation("preferredUsername")>]
    member _.PreferredUsername(cfg: SignInAliasesConfig, v: bool) = { cfg with PreferredUsername = Some v }

    [<CustomOperation("username")>]
    member _.Username(cfg: SignInAliasesConfig, v: bool) = { cfg with Username = Some v }

[<AutoOpen>]
module SignInAliasesBuilders =
    let signInAliases = SignInAliasesBuilder()

type AutoVerifiedAttrsConfig =
    { Email: bool option
      Phone: bool option }

    static member Empty = { Email = None; Phone = None }

type AutoVerifiedAttrsBuilder() =
    member _.Yield(_: unit) : AutoVerifiedAttrsConfig = AutoVerifiedAttrsConfig.Empty
    member _.Zero() : AutoVerifiedAttrsConfig = AutoVerifiedAttrsConfig.Empty

    member _.Combine(a: AutoVerifiedAttrsConfig, b: AutoVerifiedAttrsConfig) : AutoVerifiedAttrsConfig =
        { Email = a.Email |> Option.orElse b.Email
          Phone = a.Phone |> Option.orElse b.Phone }

    member inline this.For(cfg: AutoVerifiedAttrsConfig, [<InlineIfLambda>] f: unit -> AutoVerifiedAttrsConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: AutoVerifiedAttrsConfig) : IAutoVerifiedAttrs =
        let a = AutoVerifiedAttrs()
        cfg.Email |> Option.iter (fun v -> a.Email <- v)
        cfg.Phone |> Option.iter (fun v -> a.Phone <- v)
        a

    [<CustomOperation("email")>]
    member _.Email(cfg: AutoVerifiedAttrsConfig, v: bool) = { cfg with Email = Some v }

    [<CustomOperation("phone")>]
    member _.Phone(cfg: AutoVerifiedAttrsConfig, v: bool) = { cfg with Phone = Some v }

[<AutoOpen>]
module AutoVerifiedAttrsBuilders =
    let autoVerifiedAttrs = AutoVerifiedAttrsBuilder()

// MfaSecondFactor

type MfaSecondFactorConfig =
    { Otp: bool option
      Sms: bool option
      Email: bool option }

    static member Empty = { Otp = None; Sms = None; Email = None }

type MfaSecondFactorBuilder() =
    member _.Yield(_: unit) : MfaSecondFactorConfig = MfaSecondFactorConfig.Empty
    member _.Zero() : MfaSecondFactorConfig = MfaSecondFactorConfig.Empty

    member _.Combine(a: MfaSecondFactorConfig, b: MfaSecondFactorConfig) : MfaSecondFactorConfig =
        { Otp = a.Otp |> Option.orElse b.Otp
          Sms = a.Sms |> Option.orElse b.Sms
          Email = a.Email |> Option.orElse b.Email }

    member inline this.For(cfg: MfaSecondFactorConfig, [<InlineIfLambda>] f: unit -> MfaSecondFactorConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: MfaSecondFactorConfig) : IMfaSecondFactor =
        let m = MfaSecondFactor()
        cfg.Otp |> Option.iter (fun v -> m.Otp <- v)
        cfg.Sms |> Option.iter (fun v -> m.Sms <- v)
        cfg.Email |> Option.iter (fun v -> m.Email <- v)
        m

    [<CustomOperation("otp")>]
    member _.Otp(cfg: MfaSecondFactorConfig, v: bool) = { cfg with Otp = Some v }

    [<CustomOperation("sms")>]
    member _.Sms(cfg: MfaSecondFactorConfig, v: bool) = { cfg with Sms = Some v }

    [<CustomOperation("email")>]
    member _.Email(cfg: MfaSecondFactorConfig, v: bool) = { cfg with Email = Some v }

[<AutoOpen>]
module MfaSecondFactorBuilders =
    let mfaSecondFactor = MfaSecondFactorBuilder()

// StandardAttribute and StandardAttributes

type StandardAttrConfig =
    { Required: bool option
      Mutable: bool option }

    static member Empty = { Required = None; Mutable = None }

type StandardAttributeBuilder() =
    member _.Yield(_: unit) : StandardAttrConfig = StandardAttrConfig.Empty
    member _.Zero() : StandardAttrConfig = StandardAttrConfig.Empty

    member _.Combine(a: StandardAttrConfig, b: StandardAttrConfig) : StandardAttrConfig =
        { Required = a.Required |> Option.orElse b.Required
          Mutable = a.Mutable |> Option.orElse b.Mutable }

    member inline this.For(cfg: StandardAttrConfig, [<InlineIfLambda>] f: unit -> StandardAttrConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: StandardAttrConfig) : StandardAttribute =
        let sa = StandardAttribute()
        cfg.Required |> Option.iter (fun v -> sa.Required <- v)
        cfg.Mutable |> Option.iter (fun v -> sa.Mutable <- v)
        sa

    [<CustomOperation("required")>]
    member _.Required(cfg: StandardAttrConfig, v: bool) = { cfg with Required = Some v }

    [<CustomOperation("mutable'")>]
    member _.Mutable'(cfg: StandardAttrConfig, v: bool) = { cfg with Mutable = Some v }

[<AutoOpen>]
module StandardAttributeBuilders =
    let standardAttribute = StandardAttributeBuilder()

type StandardAttributesConfig =
    { Address: StandardAttribute option
      Birthdate: StandardAttribute option
      Email: StandardAttribute option
      FamilyName: StandardAttribute option
      Fullname: StandardAttribute option
      Gender: StandardAttribute option
      GivenName: StandardAttribute option
      LastUpdateTime: StandardAttribute option
      Locale: StandardAttribute option
      MiddleName: StandardAttribute option
      Nickname: StandardAttribute option
      PhoneNumber: StandardAttribute option
      PreferredUsername: StandardAttribute option
      ProfilePage: StandardAttribute option
      ProfilePicture: StandardAttribute option
      Timezone: StandardAttribute option
      Website: StandardAttribute option }

    static member Empty =
        { Address = None
          Birthdate = None
          Email = None
          FamilyName = None
          Fullname = None
          Gender = None
          GivenName = None
          LastUpdateTime = None
          Locale = None
          MiddleName = None
          Nickname = None
          PhoneNumber = None
          PreferredUsername = None
          ProfilePage = None
          ProfilePicture = None
          Timezone = None
          Website = None }

type StandardAttributesBuilder() =
    member _.Yield(_: unit) : StandardAttributesConfig = StandardAttributesConfig.Empty
    member _.Zero() : StandardAttributesConfig = StandardAttributesConfig.Empty

    member _.Combine(a: StandardAttributesConfig, b: StandardAttributesConfig) : StandardAttributesConfig =
        { Address = a.Address |> Option.orElse b.Address
          Birthdate = a.Birthdate |> Option.orElse b.Birthdate
          Email = a.Email |> Option.orElse b.Email
          FamilyName = a.FamilyName |> Option.orElse b.FamilyName
          Fullname = a.Fullname |> Option.orElse b.Fullname
          Gender = a.Gender |> Option.orElse b.Gender
          GivenName = a.GivenName |> Option.orElse b.GivenName
          LastUpdateTime = a.LastUpdateTime |> Option.orElse b.LastUpdateTime
          Locale = a.Locale |> Option.orElse b.Locale
          MiddleName = a.MiddleName |> Option.orElse b.MiddleName
          Nickname = a.Nickname |> Option.orElse b.Nickname
          PhoneNumber = a.PhoneNumber |> Option.orElse b.PhoneNumber
          PreferredUsername = a.PreferredUsername |> Option.orElse b.PreferredUsername
          ProfilePage = a.ProfilePage |> Option.orElse b.ProfilePage
          ProfilePicture = a.ProfilePicture |> Option.orElse b.ProfilePicture
          Timezone = a.Timezone |> Option.orElse b.Timezone
          Website = a.Website |> Option.orElse b.Website }

    member inline this.For(cfg: StandardAttributesConfig, [<InlineIfLambda>] f: unit -> StandardAttributesConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: StandardAttributesConfig) =
        let s = StandardAttributes()
        cfg.Address |> Option.iter (fun v -> s.Address <- v)
        cfg.Birthdate |> Option.iter (fun v -> s.Birthdate <- v)
        cfg.Email |> Option.iter (fun v -> s.Email <- v)
        cfg.FamilyName |> Option.iter (fun v -> s.FamilyName <- v)
        cfg.Fullname |> Option.iter (fun v -> s.Fullname <- v)
        cfg.Gender |> Option.iter (fun v -> s.Gender <- v)
        cfg.GivenName |> Option.iter (fun v -> s.GivenName <- v)
        cfg.LastUpdateTime |> Option.iter (fun v -> s.LastUpdateTime <- v)
        cfg.Locale |> Option.iter (fun v -> s.Locale <- v)
        cfg.MiddleName |> Option.iter (fun v -> s.MiddleName <- v)
        cfg.Nickname |> Option.iter (fun v -> s.Nickname <- v)
        cfg.PhoneNumber |> Option.iter (fun v -> s.PhoneNumber <- v)
        cfg.PreferredUsername |> Option.iter (fun v -> s.PreferredUsername <- v)
        cfg.ProfilePage |> Option.iter (fun v -> s.ProfilePage <- v)
        cfg.ProfilePicture |> Option.iter (fun v -> s.ProfilePicture <- v)
        cfg.Timezone |> Option.iter (fun v -> s.Timezone <- v)
        cfg.Website |> Option.iter (fun v -> s.Website <- v)
        s

    [<CustomOperation("address")>]
    member _.Address(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Address = Some v }

    [<CustomOperation("birthdate")>]
    member _.Birthdate(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Birthdate = Some v }

    [<CustomOperation("email")>]
    member _.Email(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Email = Some v }

    [<CustomOperation("familyName")>]
    member _.FamilyName(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with FamilyName = Some v }

    [<CustomOperation("fullname")>]
    member _.Fullname(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Fullname = Some v }

    [<CustomOperation("gender")>]
    member _.Gender(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Gender = Some v }

    [<CustomOperation("givenName")>]
    member _.GivenName(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with GivenName = Some v }

    [<CustomOperation("lastUpdateTime")>]
    member _.LastUpdateTime(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with LastUpdateTime = Some v }

    [<CustomOperation("locale")>]
    member _.Locale(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Locale = Some v }

    [<CustomOperation("middleName")>]
    member _.MiddleName(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with MiddleName = Some v }

    [<CustomOperation("nickname")>]
    member _.Nickname(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Nickname = Some v }

    [<CustomOperation("phoneNumber")>]
    member _.PhoneNumber(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with PhoneNumber = Some v }

    [<CustomOperation("preferredUsername")>]
    member _.PreferredUsername(cfg: StandardAttributesConfig, v: StandardAttribute) =
        { cfg with PreferredUsername = Some v }

    [<CustomOperation("profilePage")>]
    member _.ProfilePage(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with ProfilePage = Some v }

    [<CustomOperation("profilePicture")>]
    member _.ProfilePicture(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with ProfilePicture = Some v }

    [<CustomOperation("timezone")>]
    member _.Timezone(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Timezone = Some v }

    [<CustomOperation("website")>]
    member _.Website(cfg: StandardAttributesConfig, v: StandardAttribute) = { cfg with Website = Some v }

[<AutoOpen>]
module StandardAttributesBuilders =
    let standardAttributes = StandardAttributesBuilder()

// UserPoolTriggers

type UserPoolTriggersConfig =
    { CreateAuthChallenge: IFunction option
      CustomMessage: IFunction option
      DefineAuthChallenge: IFunction option
      PostAuthentication: IFunction option
      PostConfirmation: IFunction option
      PreAuthentication: IFunction option
      PreSignUp: IFunction option
      PreTokenGeneration: IFunction option
      UserMigration: IFunction option
      VerifyAuthChallengeResponse: IFunction option }

    static member Empty =
        { CreateAuthChallenge = None
          CustomMessage = None
          DefineAuthChallenge = None
          PostAuthentication = None
          PostConfirmation = None
          PreAuthentication = None
          PreSignUp = None
          PreTokenGeneration = None
          UserMigration = None
          VerifyAuthChallengeResponse = None }

type UserPoolTriggersBuilder() =
    member _.Yield(_: unit) : UserPoolTriggersConfig = UserPoolTriggersConfig.Empty
    member _.Zero() : UserPoolTriggersConfig = UserPoolTriggersConfig.Empty

    member _.Combine(a: UserPoolTriggersConfig, b: UserPoolTriggersConfig) : UserPoolTriggersConfig =
        { CreateAuthChallenge = a.CreateAuthChallenge |> Option.orElse b.CreateAuthChallenge
          CustomMessage = a.CustomMessage |> Option.orElse b.CustomMessage
          DefineAuthChallenge = a.DefineAuthChallenge |> Option.orElse b.DefineAuthChallenge
          PostAuthentication = a.PostAuthentication |> Option.orElse b.PostAuthentication
          PostConfirmation = a.PostConfirmation |> Option.orElse b.PostConfirmation
          PreAuthentication = a.PreAuthentication |> Option.orElse b.PreAuthentication
          PreSignUp = a.PreSignUp |> Option.orElse b.PreSignUp
          PreTokenGeneration = a.PreTokenGeneration |> Option.orElse b.PreTokenGeneration
          UserMigration = a.UserMigration |> Option.orElse b.UserMigration
          VerifyAuthChallengeResponse = a.VerifyAuthChallengeResponse |> Option.orElse b.VerifyAuthChallengeResponse }

    member inline this.For(cfg: UserPoolTriggersConfig, [<InlineIfLambda>] f: unit -> UserPoolTriggersConfig) =
        let n = f ()
        this.Combine(cfg, n)

    member _.Run(cfg: UserPoolTriggersConfig) : IUserPoolTriggers =
        let t = UserPoolTriggers()
        cfg.CreateAuthChallenge |> Option.iter (fun v -> t.CreateAuthChallenge <- v)
        cfg.CustomMessage |> Option.iter (fun v -> t.CustomMessage <- v)
        cfg.DefineAuthChallenge |> Option.iter (fun v -> t.DefineAuthChallenge <- v)
        cfg.PostAuthentication |> Option.iter (fun v -> t.PostAuthentication <- v)
        cfg.PostConfirmation |> Option.iter (fun v -> t.PostConfirmation <- v)
        cfg.PreAuthentication |> Option.iter (fun v -> t.PreAuthentication <- v)
        cfg.PreSignUp |> Option.iter (fun v -> t.PreSignUp <- v)
        cfg.PreTokenGeneration |> Option.iter (fun v -> t.PreTokenGeneration <- v)
        cfg.UserMigration |> Option.iter (fun v -> t.UserMigration <- v)

        cfg.VerifyAuthChallengeResponse
        |> Option.iter (fun v -> t.VerifyAuthChallengeResponse <- v)

        t

    [<CustomOperation("createAuthChallenge")>]
    member _.CreateAuthChallenge(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with
            CreateAuthChallenge = Some fn }

    [<CustomOperation("customMessage")>]
    member _.CustomMessage(cfg: UserPoolTriggersConfig, fn: IFunction) = { cfg with CustomMessage = Some fn }

    [<CustomOperation("defineAuthChallenge")>]
    member _.DefineAuthChallenge(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with
            DefineAuthChallenge = Some fn }

    [<CustomOperation("postAuthentication")>]
    member _.PostAuthentication(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with
            PostAuthentication = Some fn }

    [<CustomOperation("postConfirmation")>]
    member _.PostConfirmation(cfg: UserPoolTriggersConfig, fn: IFunction) = { cfg with PostConfirmation = Some fn }

    [<CustomOperation("preAuthentication")>]
    member _.PreAuthentication(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with PreAuthentication = Some fn }

    [<CustomOperation("preSignUp")>]
    member _.PreSignUp(cfg: UserPoolTriggersConfig, fn: IFunction) = { cfg with PreSignUp = Some fn }

    [<CustomOperation("preTokenGeneration")>]
    member _.PreTokenGeneration(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with
            PreTokenGeneration = Some fn }

    [<CustomOperation("userMigration")>]
    member _.UserMigration(cfg: UserPoolTriggersConfig, fn: IFunction) = { cfg with UserMigration = Some fn }

    [<CustomOperation("verifyAuthChallengeResponse")>]
    member _.VerifyAuthChallengeResponse(cfg: UserPoolTriggersConfig, fn: IFunction) =
        { cfg with
            VerifyAuthChallengeResponse = Some fn }

[<AutoOpen>]
module UserPoolTriggersBuilders =
    let userPoolTriggers = UserPoolTriggersBuilder()

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

    member _.Yield(_: unit) : UserPoolConfig =
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

    member _.Yield(passwordPolicy: IPasswordPolicy) : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = None
          StandardAttributes = None
          CustomAttributes = []
          PasswordPolicy = Some passwordPolicy
          MfaConfiguration = None
          MfaSecondFactor = None
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member _.Yield(signInAliases: ISignInAliases) : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = Some signInAliases
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

    member _.Yield(autoVerified: IAutoVerifiedAttrs) : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = Some autoVerified
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

    member _.Yield(standardAttrs: IStandardAttributes) : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = None
          StandardAttributes = Some standardAttrs
          CustomAttributes = []
          PasswordPolicy = None
          MfaConfiguration = None
          MfaSecondFactor = None
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member _.Yield(customAttr: ICustomAttribute) : UserPoolConfig =
        { UserPoolName = name
          ConstructId = None
          UserPoolName_ = None
          SelfSignUpEnabled = None
          SignInAliases = None
          AutoVerify = None
          StandardAttributes = None
          CustomAttributes = [ customAttr ]
          PasswordPolicy = None
          MfaConfiguration = None
          MfaSecondFactor = None
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member _.Yield(mfaSecondFactor: IMfaSecondFactor) : UserPoolConfig =
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
          MfaSecondFactor = Some mfaSecondFactor
          AccountRecovery = None
          EmailSettings = None
          SmsRole = None
          LambdaTriggers = None
          RemovalPolicy = None }

    member _.Yield(triggers: IUserPoolTriggers) : UserPoolConfig =
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
          LambdaTriggers = Some triggers
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

    /// <summary>Sets MFA configuration.</summary>
    [<CustomOperation("mfa")>]
    member _.Mfa(config: UserPoolConfig, mfa: Mfa) =
        { config with
            MfaConfiguration = Some mfa }

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
    member _.TokenValidities(config: UserPoolClientConfig, refresh_access_id: Duration * Duration * Duration) =
        let refreshToken, accessToken, idToken = refresh_access_id

        { config with
            RefreshTokenValidity = Some refreshToken
            AccessTokenValidity = Some accessToken
            IdTokenValidity = Some idToken }

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
