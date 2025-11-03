namespace FsCDK

open Amazon.CDK.AWS.IAM
open Amazon.CDK

// ============================================================================
// Managed Policy Configuration DSL
// ============================================================================

/// <summary>
/// High-level Managed Policy builder following AWS IAM best practices.
///
/// **Default Security Settings:**
/// - No default statements (explicit policy definition required)
/// - No default principals (attach to specific roles/users/groups)
///
/// **Rationale:**
/// These defaults follow AWS Well-Architected Framework security pillar:
/// - Principle of least privilege requires explicit permissions
/// - Explicit statements prevent accidental broad access
/// - No default principals ensures intentional attachment
///
/// **Best Practices:**
/// - Use managed policies for common permission sets
/// - Attach to roles rather than users when possible
/// - Version your policies for safe rollback
/// - Use conditions to restrict access further
///
/// **Escape Hatch:**
/// Access the underlying CDK ManagedPolicy via the `Policy` property
/// for advanced scenarios not covered by this builder.
/// </summary>
type ManagedPolicyConfig =
    { PolicyName: string
      ConstructId: string option
      ManagedPolicyName: string option
      Description: string option
      Statements: PolicyStatement list
      Path: string option
      Groups: IGroup list
      Users: IUser list
      Roles: RoleRef list }

type ManagedPolicySpec =
    { PolicyName: string
      ConstructId: string
      Props: ManagedPolicyProps
      mutable Policy: IManagedPolicy option }

    /// Gets the underlying IManagedPolicy resource. Must be called after the stack is built.
    member this.Resource =
        match this.Policy with
        | Some policy -> policy
        | None ->
            failwith
                $"ManagedPolicy '{this.PolicyName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

    /// Gets the managed policy ARN
    member this.Arn =
        match this.Policy with
        | Some policy -> policy.ManagedPolicyArn
        | None ->
            failwith
                $"ManagedPolicy '{this.PolicyName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type ManagedPolicyRef =
    | ManagedPolicyInterface of IManagedPolicy
    | ManagedPolicySpecRef of ManagedPolicySpec

module ManagedPolicyHelpers =
    let resolveManagedPolicyRef (ref: ManagedPolicyRef) =
        match ref with
        | ManagedPolicyInterface policy -> policy
        | ManagedPolicySpecRef spec ->
            match spec.Policy with
            | Some policy -> policy
            | None ->
                failwith
                    $"ManagedPolicy '{spec.PolicyName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

type ManagedPolicyBuilder(name: string) =
    member _.Yield _ : ManagedPolicyConfig =
        { PolicyName = name
          ConstructId = None
          ManagedPolicyName = None
          Description = None
          Statements = []
          Path = None
          Groups = []
          Users = []
          Roles = [] }

    member _.Yield(statement: PolicyStatement) : ManagedPolicyConfig =
        { PolicyName = name
          ConstructId = None
          ManagedPolicyName = None
          Description = None
          Statements = [ statement ]
          Path = None
          Groups = []
          Users = []
          Roles = [] }

    member _.Zero() : ManagedPolicyConfig =
        { PolicyName = name
          ConstructId = None
          ManagedPolicyName = None
          Description = None
          Statements = []
          Path = None
          Groups = []
          Users = []
          Roles = [] }

    member inline _.Delay([<InlineIfLambda>] f: unit -> ManagedPolicyConfig) : ManagedPolicyConfig = f ()

    member inline x.For
        (
            config: ManagedPolicyConfig,
            [<InlineIfLambda>] f: unit -> ManagedPolicyConfig
        ) : ManagedPolicyConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member _.Combine(a: ManagedPolicyConfig, b: ManagedPolicyConfig) : ManagedPolicyConfig =
        { PolicyName = a.PolicyName
          ConstructId =
            match a.ConstructId with
            | Some _ -> a.ConstructId
            | None -> b.ConstructId
          ManagedPolicyName =
            match a.ManagedPolicyName with
            | Some _ -> a.ManagedPolicyName
            | None -> b.ManagedPolicyName
          Description =
            match a.Description with
            | Some _ -> a.Description
            | None -> b.Description
          Statements = a.Statements @ b.Statements
          Path =
            match a.Path with
            | Some _ -> a.Path
            | None -> b.Path
          Groups = a.Groups @ b.Groups
          Users = a.Users @ b.Users
          Roles = a.Roles @ b.Roles }

    member _.Run(config: ManagedPolicyConfig) : ManagedPolicySpec =
        let props = ManagedPolicyProps()
        let constructId = config.ConstructId |> Option.defaultValue config.PolicyName

        config.ManagedPolicyName |> Option.iter (fun n -> props.ManagedPolicyName <- n)

        config.Description |> Option.iter (fun d -> props.Description <- d)

        if not (List.isEmpty config.Statements) then
            // Create a PolicyDocument from statements
            let policyDoc = PolicyDocument()

            for statement in config.Statements |> List.rev do
                policyDoc.AddStatements(statement)

            props.Document <- policyDoc

        config.Path |> Option.iter (fun p -> props.Path <- p)

        if not (List.isEmpty config.Groups) then
            props.Groups <- config.Groups |> List.toArray

        if not (List.isEmpty config.Users) then
            props.Users <- config.Users |> List.toArray

        if not (List.isEmpty config.Roles) then
            props.Roles <-
                config.Roles
                |> List.map RoleHelpers.resolveRoleRef
                |> List.toArray

        { PolicyName = config.PolicyName
          ConstructId = constructId
          Props = props
          Policy = None }

    /// <summary>Sets the construct ID for the managed policy.</summary>
    [<CustomOperation("constructId")>]
    member _.ConstructId(config: ManagedPolicyConfig, id: string) = { config with ConstructId = Some id }

    /// <summary>Sets the managed policy name.</summary>
    /// <param name="name">The policy name as it appears in IAM.</param>
    [<CustomOperation("managedPolicyName")>]
    member _.ManagedPolicyName(config: ManagedPolicyConfig, name: string) =
        { config with
            ManagedPolicyName = Some name }

    /// <summary>Sets the policy description.</summary>
    [<CustomOperation("description")>]
    member _.Description(config: ManagedPolicyConfig, description: string) =
        { config with
            Description = Some description }

    /// <summary>Adds a policy statement.</summary>
    [<CustomOperation("statement")>]
    member _.Statement(config: ManagedPolicyConfig, statement: PolicyStatement) =
        { config with
            Statements = statement :: config.Statements }

    /// <summary>Sets the IAM path for the policy.</summary>
    /// <param name="path">The path (e.g., "/division/team/").</param>
    [<CustomOperation("path")>]
    member _.Path(config: ManagedPolicyConfig, path: string) = { config with Path = Some path }

    /// <summary>Attaches the policy to a group.</summary>
    [<CustomOperation("attachToGroup")>]
    member _.AttachToGroup(config: ManagedPolicyConfig, group: IGroup) =
        { config with
            Groups = group :: config.Groups }

    /// <summary>Attaches the policy to a user.</summary>
    [<CustomOperation("attachToUser")>]
    member _.AttachToUser(config: ManagedPolicyConfig, user: IUser) =
        { config with
            Users = user :: config.Users }

    /// <summary>Attaches the policy to a role.</summary>
    [<CustomOperation("attachToRole")>]
    member _.AttachToRole(config: ManagedPolicyConfig, role: IRole) =
        { config with
            Roles = (RoleRef.RoleInterface role) :: config.Roles }

    /// <summary>Attaches the policy to a role using a LambdaRoleSpec.</summary>
    [<CustomOperation("attachToRole")>]
    member _.AttachToRoleSpec(config: ManagedPolicyConfig, roleSpec: LambdaRoleSpec) =
        { config with
            Roles = (RoleRef.RoleSpecRef roleSpec) :: config.Roles }

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

// ============================================================================
// Helper module for common managed policies
// ============================================================================

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
    ///     statement (ManagedPolicyStatements.s3ReadOnly "arn:aws:s3:::my-bucket")
    ///     attachToRole myRole
    /// }
    /// </code>
    let managedPolicy (name: string) = ManagedPolicyBuilder name
