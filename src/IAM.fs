namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.IAM

// ============================================================================
// Role Builder DSL
// ============================================================================

type RoleConfig =
    { RoleName: string
      ConstructId: string option
      AssumedBy: IPrincipal option
      Description: string option
      PolicyStatements: PolicyStatement list
      ExternalIds: string list option
      InlinePolicies: Map<string, PolicyDocument> option
      ManagedPolicies: IManagedPolicy list
      MaxSessionDuration: Duration option
      Path: string option
      PermissionsBoundary: IManagedPolicy option }

type RoleSpec =
    { RoleName: string
      ConstructId: string
      Props: RoleProps
      PolicyStatements: PolicyStatement list
      mutable Role: IRole option }

type RoleBuilder(name: string) =
    member _.Yield(_: unit) : RoleConfig =
        { RoleName = name
          ConstructId = None
          AssumedBy = None
          Description = None
          PolicyStatements = []
          ExternalIds = None
          InlinePolicies = None
          ManagedPolicies = []
          MaxSessionDuration = None
          Path = None
          PermissionsBoundary = None }

    member _.Yield(principal: IPrincipal) : RoleConfig =
        { RoleName = name
          ConstructId = None
          AssumedBy = Some principal
          Description = None
          PolicyStatements = []
          ExternalIds = None
          InlinePolicies = None
          ManagedPolicies = []
          MaxSessionDuration = None
          Path = None
          PermissionsBoundary = None }

    member _.Yield(policyStatement: PolicyStatement) : RoleConfig =
        { RoleName = name
          ConstructId = None
          AssumedBy = None
          Description = None
          PolicyStatements = [ policyStatement ]
          ExternalIds = None
          InlinePolicies = None
          ManagedPolicies = []
          MaxSessionDuration = None
          Path = None
          PermissionsBoundary = None }

    member _.Yield(managedPolicy: IManagedPolicy) : RoleConfig =
        { RoleName = name
          ConstructId = None
          AssumedBy = None
          Description = None
          PolicyStatements = []
          ExternalIds = None
          InlinePolicies = None
          ManagedPolicies = [ managedPolicy ]
          MaxSessionDuration = None
          Path = None
          PermissionsBoundary = None }

    member _.Zero() : RoleConfig =
        { RoleName = name
          ConstructId = None
          AssumedBy = None
          Description = None
          PolicyStatements = []
          ExternalIds = None
          InlinePolicies = None
          ManagedPolicies = []
          MaxSessionDuration = None
          Path = None
          PermissionsBoundary = None }

    member inline _.Delay([<InlineIfLambda>] f: unit -> RoleConfig) : RoleConfig = f ()

    member _.Combine(state1: RoleConfig, state2: RoleConfig) : RoleConfig =
        { RoleName = state1.RoleName
          ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
          AssumedBy = state2.AssumedBy |> Option.orElse state1.AssumedBy
          Description = state2.Description |> Option.orElse state1.Description
          PolicyStatements = List.append state1.PolicyStatements state2.PolicyStatements
          ExternalIds = state2.ExternalIds |> Option.orElse state1.ExternalIds
          InlinePolicies =
            match state1.InlinePolicies, state2.InlinePolicies with
            | Some p1, Some p2 -> Some(Map.fold (fun acc k v -> Map.add k v acc) p1 p2)
            | Some p, None -> Some p
            | None, Some p -> Some p
            | None, None -> None
          ManagedPolicies = List.append state1.ManagedPolicies state2.ManagedPolicies
          MaxSessionDuration = state2.MaxSessionDuration |> Option.orElse state1.MaxSessionDuration
          Path = state2.Path |> Option.orElse state1.Path
          PermissionsBoundary = state2.PermissionsBoundary |> Option.orElse state1.PermissionsBoundary }

    member inline x.For(config: RoleConfig, [<InlineIfLambda>] f: unit -> RoleConfig) : RoleConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Run(config: RoleConfig) : RoleSpec =

        let roleProps = RoleProps()
        roleProps.RoleName <- config.RoleName

        config.AssumedBy
        |> Option.iter (fun principal -> roleProps.AssumedBy <- principal)

        config.Description |> Option.iter (fun desc -> roleProps.Description <- desc)

        config.ExternalIds
        |> Option.iter (fun ids -> roleProps.ExternalIds <- ids |> Seq.toArray)

        config.InlinePolicies
        |> Option.iter (fun policies -> roleProps.InlinePolicies <- policies)

        roleProps.ManagedPolicies <- config.ManagedPolicies |> Seq.toArray

        config.MaxSessionDuration
        |> Option.iter (fun duration -> roleProps.MaxSessionDuration <- duration)

        config.Path |> Option.iter (fun path -> roleProps.Path <- path)

        config.PermissionsBoundary
        |> Option.iter (fun boundary -> roleProps.PermissionsBoundary <- boundary)

        { RoleName = config.RoleName
          ConstructId = config.RoleName
          PolicyStatements = config.PolicyStatements
          Props = roleProps
          Role = None }

    /// <summary>Sets the construct ID for the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="id">The construct ID.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     constructId "CustomLambdaRoleId"
    /// }
    /// </code>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: RoleConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the principal that can assume the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="principal">The IAM principal.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     assumedBy (ServicePrincipal("lambda.amazonaws.com"))
    /// }
    /// </code>
    [<CustomOperation("assumedBy")>]
    member _.AssumedBy(config: RoleConfig, principal: IPrincipal) =
        { config with
            AssumedBy = Some principal }

    /// <summary>Sets the description for the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="description">The role description.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     description "Role for my Lambda function"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: RoleConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Adds external IDs for the role trust policy.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="externalIds">The sequence of external IDs.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     externalIds [ "external-id-1"; "external-id-2" ]
    /// }
    /// </code>
    [<CustomOperation("externalIds")>]
    member _.ExternalIds(config: RoleConfig, externalIds: string list) =
        { config with
            ExternalIds = Some externalIds }

    /// <summary>Adds an inline policy to the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="policies">A tuple of policy name and policy document.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     inlinePolicies [ ("MyPolicy", myPolicyDocument) ]
    /// }
    /// </code>
    [<CustomOperation("inlinePolicies")>]
    member _.InlinePolicy(config: RoleConfig, policies: (string * PolicyDocument) list) =
        let policyMap =
            policies |> Seq.fold (fun acc (name, doc) -> Map.add name doc acc) Map.empty

        let updatedPolicies =
            match config.InlinePolicies with
            | Some existingPolicies -> Some(Map.fold (fun acc k v -> Map.add k v acc) existingPolicies policyMap)
            | None -> Some policyMap

        { config with
            InlinePolicies = updatedPolicies }


    /// <summary>Adds managed policies to the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="managedPolicies">The sequence of managed policies.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     managedPolicies [ myManagedPolicy1; myManagedPolicy2 ]
    /// }
    /// </code>
    [<CustomOperation("managedPolicies")>]
    member _.ManagedPolicies(config: RoleConfig, managedPolicies: IManagedPolicy list) =
        { config with
            ManagedPolicies = List.append config.ManagedPolicies managedPolicies }

    /// <summary>Adds managed policies to the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="managedPolicy">The managed policy.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     managedPolicy myManagedPolicy
    /// }
    /// </code>
    [<CustomOperation("managedPolicy")>]
    member _.ManagedPolicy(config: RoleConfig, managedPolicy: IManagedPolicy) =
        { config with
            ManagedPolicies = managedPolicy :: config.ManagedPolicies }


    /// <summary>Sets the maximum session duration for the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="duration">The maximum session duration.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     maxSessionDuration (Duration.hours 2)
    /// }
    /// </code>
    [<CustomOperation("maxSessionDuration")>]
    member _.MaxSessionDuration(config: RoleConfig, duration: Duration) =
        { config with
            MaxSessionDuration = Some duration }


    /// <summary>Sets the path for the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="path">The role path.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     path "/service/lambda/"
    /// }
    /// </code>
    [<CustomOperation("path")>]
    member _.Path(config: RoleConfig, path: string) = { config with Path = Some path }

    /// <summary>Adds statements to the role's policy.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="statements">The sequence of statements to add.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///  addToPolicies [ myPolicyStatement1; myPolicyStatement2 ]
    /// }
    /// </code>
    [<CustomOperation("addToPolicies")>]
    member _.AddToPolicies(config: RoleConfig, statements: PolicyStatement list) =
        { config with
            PolicyStatements = List.append config.PolicyStatements statements }

    /// <summary>Adds a statement to the role's policy.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="statement">The statement to add.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///  addToPolicy myPolicyStatement
    /// }
    /// </code>
    [<CustomOperation("addToPolicy")>]
    member _.AddToPolicy(config: RoleConfig, statement: PolicyStatement) =
        { config with
            PolicyStatements = statement :: config.PolicyStatements }

    /// <summary>Sets the permissions boundary for the role.</summary>
    /// <param name="config">The current role configuration.</param>
    /// <param name="boundary">The managed policy to use as the permissions boundary.</param>
    /// <code lang="fsharp">
    /// role "MyLambdaRole" {
    ///     permissionsBoundary myPermissionsBoundaryPolicy
    /// }
    /// </code>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: RoleConfig, boundary: IManagedPolicy) =
        { config with
            PermissionsBoundary = Some boundary }


// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module IAMBuilders =

    /// <summary>Creates an IAM Role using the RoleBuilder DSL.</summary>
    /// <param name="name">The name of the IAM Role.</param>
    /// <code lang="fsharp">
    /// let myRole =
    ///     role "MyLambdaRole" {
    ///         assumedBy (ServicePrincipal("lambda.amazonaws.com"))
    ///         description "Role for my Lambda function"
    ///         managedPolicies [ ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole") ]
    ///     }
    /// </code>
    let role name = RoleBuilder(name)
