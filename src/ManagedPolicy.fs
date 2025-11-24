namespace FsCDK

open Amazon.CDK.AWS.IAM


// ============================================================================
// User Builder DSL
// ============================================================================

type UserConfig =
    { ConstructId: string option
      Groups: IGroup list
      ManagedPolicies: IManagedPolicy list
      Password: Amazon.CDK.SecretValue option
      PasswordResetRequired: bool option
      Path: string option
      PermissionsBoundary: IManagedPolicy option
      UserName: string option }

type UserSpec =
    { Props: UserProps
      ConstructId: string
      mutable User: User option }

type UserBuilder() =

    member _.Yield(_: unit) : UserConfig =
        { ConstructId = None
          Groups = []
          ManagedPolicies = []
          Password = None
          PasswordResetRequired = None
          Path = None
          PermissionsBoundary = None
          UserName = None }

    member _.Yield(permissionsBoundary: IManagedPolicy) : UserConfig =
        { ConstructId = None
          Groups = []
          ManagedPolicies = []
          Password = None
          PasswordResetRequired = None
          Path = None
          PermissionsBoundary = Some permissionsBoundary
          UserName = None }


    member _.Yield(group: IGroup) : UserConfig =
        { ConstructId = None
          Groups = [ group ]
          ManagedPolicies = []
          Password = None
          PasswordResetRequired = None
          Path = None
          PermissionsBoundary = None
          UserName = None }


    member _.Zero() : UserConfig =
        { ConstructId = None
          Groups = []
          ManagedPolicies = []
          Password = None
          PasswordResetRequired = None
          Path = None
          PermissionsBoundary = None
          UserName = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> UserConfig) : UserConfig = f ()

    member inline x.For(config: UserConfig, [<InlineIfLambda>] f: unit -> UserConfig) : UserConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(state1: UserConfig, state2: UserConfig) : UserConfig =
        { ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Groups = List.append state1.Groups state2.Groups
          ManagedPolicies = List.append state1.ManagedPolicies state2.ManagedPolicies
          Password = state2.Password |> Option.orElse state1.Password
          PasswordResetRequired = state2.PasswordResetRequired |> Option.orElse state1.PasswordResetRequired
          Path = state2.Path |> Option.orElse state1.Path
          PermissionsBoundary = state2.PermissionsBoundary |> Option.orElse state1.PermissionsBoundary
          UserName = state2.UserName |> Option.orElse state1.UserName }

    member _.Run(config: UserConfig) : UserSpec =
        let userProps = UserProps()
        let constructId = config.ConstructId |> Option.defaultValue "User"

        config.Groups
        |> Seq.iter (fun group -> userProps.Groups <- Array.append userProps.Groups [| group |])

        config.ManagedPolicies
        |> Seq.iter (fun policy -> userProps.ManagedPolicies <- Array.append userProps.ManagedPolicies [| policy |])

        config.Password |> Option.iter (fun pwd -> userProps.Password <- pwd)

        config.PasswordResetRequired
        |> Option.iter (fun req -> userProps.PasswordResetRequired <- req)

        config.Path |> Option.iter (fun p -> userProps.Path <- p)

        config.PermissionsBoundary
        |> Option.iter (fun pb -> userProps.PermissionsBoundary <- pb)

        config.UserName |> Option.iter (fun un -> userProps.UserName <- un)

        { Props = userProps
          ConstructId = constructId
          User = None }


    /// <summary>Adds the user to IAM groups.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="group">The IAM group to add the user to.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///     groups [
    ///        myGroup1
    ///        myGroup2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("groups")>]
    member _.Groups(config: UserConfig, group: IGroup list) =
        { config with
            Groups = List.append config.Groups group }


    /// <summary>Attaches managed policies to the user.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="policy">The managed policy to attach.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///     managedPolicies [
    ///        myPolicy1
    ///        myPolicy2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("managedPolicies")>]
    member _.ManagedPolicies(config: UserConfig, policy: IManagedPolicy list) =
        { config with
            ManagedPolicies = List.append config.ManagedPolicies policy }


    /// <summary>Sets the user's password.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="password">The user's password as a SecretValue.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///     password SecretValue.plainText "MySecurePassword"
    /// }
    /// </code>
    [<CustomOperation("password")>]
    member _.Password(config: UserConfig, password: Amazon.CDK.SecretValue) =
        { config with Password = Some password }


    /// <summary>Sets whether the user is required to reset their password on next sign-in.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="required">True if password reset is required, false otherwise.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///     passwordResetRequired true
    /// }
    /// </code>
    [<CustomOperation("passwordResetRequired")>]
    member _.PasswordResetRequired(config: UserConfig, required: bool) =
        { config with
            PasswordResetRequired = Some required }


    /// <summary>Sets the IAM path for the user.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="path">The path (e.g., "/division/team/").</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///    path "/division/team/"
    /// }
    /// </code>
    [<CustomOperation("path")>]
    member _.Path(config: UserConfig, path: string) = { config with Path = Some path }


    /// <summary>Sets the permissions boundary for the user.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="permissionsBoundary">The managed policy to use as the permissions boundary.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///   permissionsBoundary myPermissionsBoundaryPolicy
    /// }
    /// </code>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: UserConfig, permissionsBoundary: IManagedPolicy) =
        { config with
            PermissionsBoundary = Some permissionsBoundary }


    /// <summary>Sets the user name.</summary>
    /// <param name="config">The current user configuration.</param>
    /// <param name="userName">The user name.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///    userName "customUserName"
    /// }
    /// </code>
    [<CustomOperation("userName")>]
    member _.UserName(config: UserConfig, userName: string) =
        { config with UserName = Some userName }


[<AutoOpen>]
module UserBuilders =
    /// <summary>Creates an IAM User.</summary>
    /// <param name="name">The logical name of the user.</param>
    /// <code lang="fsharp">
    /// user "MyUser" {
    ///     groups [
    ///        myGroup1
    ///        myGroup2
    ///    ]
    ///     managedPolicies [
    ///        myPolicy1
    ///        myPolicy2
    ///    ]
    ///     password SecretValue.plainText "MySecurePassword"
    ///     passwordResetRequired true
    ///     path "/division/team/"
    ///     permissionsBoundary myPermissionsBoundaryPolicy
    ///     userName "customUserName"
    /// }
    /// </code>
    let user = UserBuilder()


// ============================================================================
// Policy Builder DSL
// ============================================================================

type PolicyConfig =
    { ConstructId: string option
      Document: PolicyDocument option
      Force: bool option
      Groups: IGroup list
      PolicyName: string option
      Roles: IRole list
      Statements: PolicyStatement list
      Users: IUser list }

type PolicySpec =
    { PolicyName: string
      ConstructId: string
      Props: PolicyProps
      mutable Policy: Policy option }

type PolicyBuilder(name: string) =

    member _.Yield(_: unit) : PolicyConfig =
        { ConstructId = None
          Document = None
          Force = None
          Groups = []
          PolicyName = None
          Roles = []
          Statements = []
          Users = [] }

    member _.Yield(document: PolicyDocument) : PolicyConfig =
        { ConstructId = None
          Document = Some document
          Force = None
          Groups = []
          PolicyName = None
          Roles = []
          Statements = []
          Users = [] }

    member _.Yield(statement: PolicyStatement) : PolicyConfig =
        { ConstructId = None
          Document = None
          Force = None
          Groups = []
          PolicyName = None
          Roles = []
          Statements = [ statement ]
          Users = [] }


    member _.Zero() : PolicyConfig =
        { ConstructId = None
          Document = None
          Force = None
          Groups = []
          PolicyName = None
          Roles = []
          Statements = []
          Users = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PolicyConfig) : PolicyConfig = f ()

    member _.Combine(state1: PolicyConfig, state2: PolicyConfig) : PolicyConfig =
        { ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          Document = state2.Document |> Option.orElse state1.Document
          Force = state2.Force |> Option.orElse state1.Force
          Groups = List.append state1.Groups state2.Groups
          PolicyName = state2.PolicyName |> Option.orElse state1.PolicyName
          Roles = List.append state1.Roles state2.Roles
          Statements = List.append state1.Statements state2.Statements
          Users = List.append state1.Users state2.Users }

    member inline x.For(config: PolicyConfig, [<InlineIfLambda>] f: unit -> PolicyConfig) : PolicyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: PolicyConfig) : PolicySpec =

        let policyProps = PolicyProps()

        let constructId = config.PolicyName |> Option.defaultValue name

        config.Document |> Option.iter (fun doc -> policyProps.Document <- doc)

        config.Force |> Option.iter (fun force -> policyProps.Force <- force)

        config.Groups
        |> Seq.iter (fun group -> policyProps.Groups <- Array.append policyProps.Groups [| group |])

        config.PolicyName |> Option.iter (fun name -> policyProps.PolicyName <- name)

        config.Roles
        |> Seq.iter (fun role -> policyProps.Roles <- Array.append policyProps.Roles [| role |])

        config.Statements
        |> Seq.iter (fun stmt ->
            if policyProps.Statements = null then
                policyProps.Statements <- [| stmt |]
            else
                policyProps.Statements <- Array.append policyProps.Statements [| stmt |])

        config.Users
        |> Seq.iter (fun user ->
            if policyProps.Users = null then
                policyProps.Users <- [| user |]
            else
                policyProps.Users <- Array.append policyProps.Users [| user |])

        { PolicyName = name
          ConstructId = constructId
          Props = policyProps
          Policy = None }

    /// <summary>Sets the construct ID for the managed policy.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     constructId "CustomPolicyId"
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: PolicyConfig, id: string) = { config with ConstructId = Some id }


    /// <summary>Sets whether to force the policy.</summary>
    /// <param name="config">The current policy configuration.</param>
    /// <param name="force">True to force the policy, false otherwise.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     force true
    /// }
    /// </code>
    [<CustomOperation("force")>]
    member _.Force(config: PolicyConfig, force: bool) = { config with Force = Some force }


    /// <summary>Sets the policy name.</summary>
    /// <param name="config">The current policy configuration.</param>
    /// <param name="name">The policy name.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     policyName "CustomPolicyName"
    /// }
    /// </code>
    [<CustomOperation("policyName")>]
    member _.PolicyName(config: PolicyConfig, name: string) = { config with PolicyName = Some name }


    /// <summary>Adds a policy statement.</summary>
    /// <param name="config">The current policy configuration.</param>
    /// <param name="statements">The policy statements to add.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     statements [
    ///         policyStatement {
    ///             effect Effect.ALLOW
    ///             actions [ "s3:GetObject" ]
    ///             resources [ "*" ]
    ///         }
    ///     ]
    /// }
    /// </code>
    [<CustomOperation("statements")>]
    member _.Statements(config: PolicyConfig, statements: PolicyStatement list) =
        { config with Statements = statements }

    [<CustomOperation("statement")>]
    member _.Statement(config: PolicyConfig, statement: PolicyStatement) =
        { config with
            Statements = statement :: config.Statements }

    /// <summary>Attaches the policy to a group.</summary>
    /// <param name="config">The current policy configuration.</param>
    /// <param name="group">The IAM group to attach the policy to.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     groups [
    ///        myGroup1
    ///        myGroup2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("groups")>]
    member _.Groups(config: PolicyConfig, group: IGroup list) =
        { config with
            Groups = List.append config.Groups group }

    /// <summary>Attaches the policy to a user.</summary>
    /// <param name="config">The current policy configuration.</param>
    /// <param name="user">The IAM user to attach the policy to.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     users [
    ///       myUser1
    ///       myUser2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("users")>]
    member _.Users(config: PolicyConfig, user: IUser list) =
        { config with
            Users = List.append config.Users user }


[<AutoOpen>]
module PolicyBuilders =
    /// <summary>Creates an IAM Policy.</summary>
    /// <param name="name">The policy name.</param>
    /// <code lang="fsharp">
    /// policy "MyPolicy" {
    ///     force true
    ///     policyName "CustomPolicyName"
    ///     statements [
    ///         policyStatement {
    ///             effect Effect.ALLOW
    ///             actions [ "s3:GetObject" ]
    ///             resources [ "*" ]
    ///        }
    ///    ]
    /// }
    /// </code>
    let policy (name: string) = PolicyBuilder(name)

type PolicyDocumentConfig =
    { AssignSids: bool option
      Minimize: bool option
      Statements: PolicyStatement list }

type PolicyDocumentBuilder() =

    member _.Yield _ : PolicyDocumentConfig =
        { AssignSids = None
          Minimize = None
          Statements = [] }

    member _.Yield(statement: PolicyStatement) : PolicyDocumentConfig =
        { AssignSids = None
          Minimize = None
          Statements = [ statement ] }

    member _.Zero() : PolicyDocumentConfig =
        { AssignSids = None
          Minimize = None
          Statements = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> PolicyDocumentConfig) : PolicyDocumentConfig = f ()

    member inline x.For
        (
            config: PolicyDocumentConfig,
            [<InlineIfLambda>] f: unit -> PolicyDocumentConfig
        ) : PolicyDocumentConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: PolicyDocumentConfig, b: PolicyDocumentConfig) : PolicyDocumentConfig =
        { AssignSids = a.AssignSids |> Option.orElse b.AssignSids
          Minimize = a.Minimize |> Option.orElse b.Minimize
          Statements = a.Statements |> List.append b.Statements }

    member _.Run(config: PolicyDocumentConfig) : PolicyDocument =
        let props = PolicyDocumentProps()

        config.AssignSids
        |> Option.iter (fun v -> props.AssignSids <- System.Nullable v)

        config.Minimize |> Option.iter (fun v -> props.Minimize <- System.Nullable v)

        if not (Seq.isEmpty config.Statements) then
            props.Statements <- config.Statements |> Seq.toArray

        PolicyDocument(props)


    /// <summary>Sets whether to assign SIDs to statements without one.</summary>
    /// <param name="config">The current policy document configuration.</param>
    /// <param name="assign">True to assign SIDs, false otherwise.</param>
    /// <code lang="fsharp">
    /// policyDocument {
    ///     assignSids true
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("assignSids")>]
    member _.AssignSids(config: PolicyDocumentConfig, assign: bool) =
        { config with AssignSids = Some assign }

    /// <summary>Sets whether to minimize the policy document.</summary>
    /// <param name="config">The current policy document configuration.</param>
    /// <param name="minimize">True to minimize, false otherwise.</param>
    /// <code lang="fsharp">
    /// policyDocument {
    ///     minimize false
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("minimize")>]
    member _.Minimize(config: PolicyDocumentConfig, minimize: bool) =
        { config with Minimize = Some minimize }

    /// <summary>Adds a policy statement to the document.</summary>
    /// <param name="config">The current policy document configuration.</param>
    /// <param name="statements">The policy statements to add.</param>
    /// <code lang="fsharp">
    /// policyDocument {
    ///     statements [
    ///         policyStatement {
    ///             effect Effect.ALLOW
    ///             actions [ "s3:GetObject" ]
    ///             resources [ "*" ]
    ///         }
    ///     ]
    /// }
    /// </code>
    [<CustomOperation("statements")>]
    member _.Statements(config: PolicyDocumentConfig, statements: PolicyStatement list) =
        { config with Statements = statements }

[<AutoOpen>]
module PolicyDocumentBuilders =
    /// <summary>Creates a Policy Document.</summary>
    /// <code lang="fsharp">
    /// policyDocument {
    ///     assignSids true
    ///     minimize false
    ///     policyStatement {
    ///         effect Effect.ALLOW
    ///         actions [ "s3:GetObject" ]
    ///         resources [ "*" ]
    ///     }
    /// }
    /// </code>
    let policyDocument = PolicyDocumentBuilder()


type ManagedPolicyConfig =
    { Description: string option
      Document: PolicyDocument option
      Groups: IGroup list
      PolicyName: string
      Path: string option
      Roles: IRole list
      Statements: PolicyStatement list
      ManagedPolicyName: string option
      Users: IUser list
      ConstructId: string option }

type ManagedPolicySpec =
    { PolicyName: string
      ConstructId: string
      Props: ManagedPolicyProps
      mutable Policy: IManagedPolicy option }

type ManagedPolicyBuilder(name: string) =
    member _.Yield(_: unit) : ManagedPolicyConfig =
        { Description = None
          Document = None
          Groups = []
          PolicyName = name
          Path = None
          Roles = []
          Statements = []
          ManagedPolicyName = None
          Users = []
          ConstructId = None }

    member _.Yield(statement: PolicyStatement) : ManagedPolicyConfig =
        { Description = None
          Document = None
          Groups = []
          PolicyName = name
          Path = None
          Roles = []
          Statements = [ statement ]
          ManagedPolicyName = None
          Users = []
          ConstructId = None }

    member _.Yield(statement: PolicyDocument) : ManagedPolicyConfig =
        { Description = None
          Document = Some statement
          Groups = []
          PolicyName = name
          Path = None
          Roles = []
          Statements = []
          ManagedPolicyName = None
          Users = []
          ConstructId = None }

    member _.Zero() : ManagedPolicyConfig =
        { Description = None
          Document = None
          Groups = []
          PolicyName = name
          Path = None
          Roles = []
          Statements = []
          ManagedPolicyName = None
          Users = []
          ConstructId = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> ManagedPolicyConfig) : ManagedPolicyConfig = f ()

    member inline x.For
        (
            config: ManagedPolicyConfig,
            [<InlineIfLambda>] f: unit -> ManagedPolicyConfig
        ) : ManagedPolicyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: ManagedPolicyConfig, b: ManagedPolicyConfig) : ManagedPolicyConfig =
        { Description = a.Description |> Option.orElse b.Description
          Document = a.Document |> Option.orElse b.Document
          Groups = List.append a.Groups b.Groups
          PolicyName = a.PolicyName
          Path = a.Path |> Option.orElse b.Path
          Roles = List.append a.Roles b.Roles
          Statements = List.append a.Statements b.Statements
          ManagedPolicyName = a.ManagedPolicyName |> Option.orElse b.ManagedPolicyName
          Users = List.append a.Users b.Users
          ConstructId = a.ConstructId |> Option.orElse b.ConstructId }

    member _.Run(config: ManagedPolicyConfig) : ManagedPolicySpec =
        let props = ManagedPolicyProps()

        let constructId = config.ConstructId |> Option.defaultValue config.PolicyName

        config.Description |> Option.iter (fun d -> props.Description <- d)

        match config.Document with
        | Some policyDoc ->
            // If a document is provided explicitly, use it
            if not (List.isEmpty config.Statements) then
                for statement in config.Statements |> List.rev do
                    policyDoc.AddStatements(statement)

            props.Document <- policyDoc
        | None when not (List.isEmpty config.Statements) ->
            // Otherwise, build a document from statements if present
            let policyDoc = PolicyDocument()

            for statement in config.Statements |> List.rev do
                policyDoc.AddStatements(statement)

            props.Document <- policyDoc
        | _ -> ()

        if not (Seq.isEmpty config.Groups) then
            props.Groups <- config.Groups |> Seq.toArray

        config.ManagedPolicyName |> Option.iter (fun n -> props.ManagedPolicyName <- n)
        config.Path |> Option.iter (fun p -> props.Path <- p)

        if not (Seq.isEmpty config.Roles) then
            props.Roles <- config.Roles |> Seq.toArray

        { PolicyName = config.PolicyName
          ConstructId = constructId
          Props = props
          Policy = None }

    /// <summary>Sets the construct ID for the managed policy.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     constructId "CustomPolicyId"
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ManagedPolicyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the managed policy name.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="name">The policy name as it appears in IAM.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     managedPolicyName "CustomPolicyName"
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("managedPolicyName")>]
    member _.ManagedPolicyName(config: ManagedPolicyConfig, name: string) =
        { config with
            ManagedPolicyName = Some name }

    /// <summary>Sets the policy description.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="description">The policy description.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     description "This is my custom managed policy"
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: ManagedPolicyConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Adds a policy statement.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="statement">The policy statement to add.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     statements [
    ///         policyStatement {
    ///             effect Effect.ALLOW
    ///             actions [ "s3:GetObject" ]
    ///             resources [ "*" ]
    ///         }
    ///     ]
    /// }
    /// </code>
    [<CustomOperation("statements")>]
    member _.Statements(config: ManagedPolicyConfig, statement: PolicyStatement list) =
        { config with
            Statements = List.append config.Statements statement }

    /// <summary>Sets the IAM path for the policy.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="path">The path (e.g., "/division/team/").</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     path "/division/team/"
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("path")>]
    member _.Path(config: ManagedPolicyConfig, path: string) = { config with Path = Some path }

    /// <summary>Attaches the policy to a group.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="group">The IAM group to attach the policy to.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     groups [
    ///        myGroup1
    ///        myGroup2
    ///    ]
    ///     ...
    /// }
    /// </code>
    [<CustomOperation("groups")>]
    member _.Groups(config: ManagedPolicyConfig, group: IGroup list) =
        { config with
            Groups = List.append config.Groups group }

    /// <summary>Attaches the policy to a user.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="users">The IAM users to attach the policy to.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     users [
    ///       myUser1
    ///       myUser2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("users")>]
    member _.Users(config: ManagedPolicyConfig, users: IUser list) =
        { config with
            Users = List.append config.Users users }

    /// <summary>Attaches the policy to a role.</summary>
    /// <param name="config">The current managed policy configuration.</param>
    /// <param name="roles">The IAM roles to attach the policy to.</param>
    /// <code lang="fsharp">
    /// managedPolicy "MyPolicy" {
    ///     roles [
    ///       myRole1
    ///       myRole2
    ///    ]
    /// }
    /// </code>
    [<CustomOperation("roles")>]
    member _.Roles(config: ManagedPolicyConfig, roles: IRole list) =
        { config with
            Roles = List.append config.Roles roles }

    /// <summary>Adds a statement allowing specific actions on specific resources.</summary>
    [<CustomOperation("allow")>]
    member _.Allow(config: ManagedPolicyConfig, actions: string list, resources: string list) =
        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Effect = System.Nullable Effect.ALLOW,
                    Actions = (actions |> List.toArray),
                    Resources = (resources |> List.toArray)
                )
            )

        { config with
            Statements = statement :: config.Statements }

    /// <summary>Adds a statement denying specific actions on specific resources.</summary>
    [<CustomOperation("deny")>]
    member _.Deny(config: ManagedPolicyConfig, actions: string list, resources: string list) =
        let statement =
            PolicyStatement(
                PolicyStatementProps(
                    Effect = System.Nullable Effect.DENY,
                    Actions = (actions |> List.toArray),
                    Resources = (resources |> List.toArray)
                )
            )

        { config with
            Statements = statement :: config.Statements }

    [<CustomOperation("document")>]
    member _.Document(config: ManagedPolicyConfig, doc: PolicyDocument) = { config with Document = Some doc }

[<RequireQualifiedAccess>]
module ManagedPolicyStatements =
    /// Creates a statement for S3 read-only access
    let s3ReadOnly (bucketArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "S3ReadOnly",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "s3:GetObject"; "s3:ListBucket" |],
                Resources = [| bucketArn; bucketArn + "/*" |]
            )
        )

    /// Creates a statement for S3 full access
    let s3FullAccess (bucketArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "S3FullAccess",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "s3:*" |],
                Resources = [| bucketArn; bucketArn + "/*" |]
            )
        )

    /// Creates a statement for DynamoDB read-only access
    let dynamoDBReadOnly (tableArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "DynamoDBReadOnly",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "dynamodb:GetItem"
                       "dynamodb:Query"
                       "dynamodb:Scan"
                       "dynamodb:BatchGetItem"
                       "dynamodb:DescribeTable" |],
                Resources = [| tableArn; tableArn + "/index/*" |]
            )
        )

    /// Creates a statement for DynamoDB full access
    let dynamoDBFullAccess (tableArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "DynamoDBFullAccess",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "dynamodb:*" |],
                Resources = [| tableArn; tableArn + "/index/*" |]
            )
        )

    /// Creates a statement for Lambda invoke permissions
    let lambdaInvoke (functionArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "LambdaInvoke",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "lambda:InvokeFunction" |],
                Resources = [| functionArn |]
            )
        )

    /// Creates a statement for CloudWatch Logs write access
    let cloudWatchLogsWrite (logGroupArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "CloudWatchLogsWrite",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "logs:CreateLogGroup"
                       "logs:CreateLogStream"
                       "logs:PutLogEvents"
                       "logs:DescribeLogStreams" |],
                Resources = [| logGroupArn; logGroupArn + ":*" |]
            )
        )

    /// Creates a statement for SQS full access
    let sqsFullAccess (queueArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "SQSFullAccess",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "sqs:SendMessage"
                       "sqs:ReceiveMessage"
                       "sqs:DeleteMessage"
                       "sqs:GetQueueAttributes"
                       "sqs:GetQueueUrl" |],
                Resources = [| queueArn |]
            )
        )

    /// Creates a statement for SNS publish permissions
    let snsPublish (topicArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "SNSPublish",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "sns:Publish" |],
                Resources = [| topicArn |]
            )
        )

    /// Creates a statement for Secrets Manager read access
    let secretsManagerRead (secretArn: string) =
        PolicyStatement(
            props =
                PolicyStatementProps(
                    Sid = "SecretsManagerRead",
                    Effect = System.Nullable Effect.ALLOW,
                    Actions = [| "secretsmanager:GetSecretValue"; "secretsmanager:DescribeSecret" |],
                    Resources = [| secretArn |]
                )
        )

    /// Creates a statement for KMS decrypt permissions
    let kmsDecrypt (keyArn: string) =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "KMSDecrypt",
                Effect = System.Nullable Effect.ALLOW,
                Actions = [| "kms:Decrypt"; "kms:DescribeKey" |],
                Resources = [| keyArn |]
            )
        )

    /// Creates a statement for EC2 describe permissions (read-only)
    let ec2Describe () =
        PolicyStatement(
            PolicyStatementProps(
                Sid = "EC2Describe",
                Effect = System.Nullable Effect.ALLOW,
                Actions =
                    [| "ec2:DescribeInstances"
                       "ec2:DescribeImages"
                       "ec2:DescribeKeyPairs"
                       "ec2:DescribeSecurityGroups"
                       "ec2:DescribeAvailabilityZones"
                       "ec2:DescribeSubnets"
                       "ec2:DescribeVpcs" |],
                Resources = [| "*" |]
            )
        )

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module ManagedPolicyBuilders =
    /// <summary>Creates a Managed Policy with AWS IAM best practices.</summary>
    /// <param name="name">The policy name.</param>
    /// <code lang="fsharp">
    /// managedPolicy "S3ReadPolicy" {
    ///     description "Read-only access to S3 buckets"
    ///     statements [
    ///         policyStatement {
    ///             effect Effect.ALLOW
    ///             actions [ "s3:GetObject" ]
    ///             resources [ "*" ]
    ///         }
    ///     ]
    ///     attachToRole myRole
    /// }
    /// </code>
    let managedPolicy (name: string) = ManagedPolicyBuilder(name)
