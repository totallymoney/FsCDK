namespace FsCDK

open Amazon.CDK.AWS.IAM
open System.Text.RegularExpressions

// Forward declaration - LambdaRoleSpec is defined below
type LambdaRoleSpec =
    { RoleName: string
      ConstructId: string
      mutable Role: IRole option }

    /// Gets the underlying Role resource. Must be called after the stack is built.
    member this.Resource =
        match this.Role with
        | Some role -> role
        | None ->
            failwith
                $"Role '{this.RoleName}' has not been created yet. Ensure it's yielded in the stack before referencing it."

/// <summary>
/// IAM helpers for creating roles and policies following least-privilege principles.
///
/// **Rationale:**
/// - Least-privilege access reduces blast radius of compromised credentials
/// - AWS managed policies provide maintained, secure defaults
/// - Role-based access control simplifies permission management
/// - Service-specific roles limit cross-service access
///
/// **Best Practices:**
/// - Always start with minimum required permissions
/// - Use AWS managed policies when appropriate
/// - Create custom policies for specific application needs
/// - Avoid wildcards (*) in production
/// - Review and audit IAM policies regularly
/// </summary>
module IAM =

    let private regName = Regex "[^a-zA-Z0-9-_]"
    /// <summary>
    /// Sanitizes a name for IAM resource naming (removes invalid characters)
    /// IAM names can contain alphanumeric characters, hyphens, and underscores
    /// </summary>
    let sanitizeName (name: string) = regName.Replace(name, "-")

    /// <summary>
    /// Creates a basic IAM role with a trust policy for the specified service principal
    /// </summary>
    let createRole (servicePrincipal: string) (roleName: string) =
        let props = RoleProps()
        props.RoleName <- sanitizeName roleName
        props.AssumedBy <- ServicePrincipal(servicePrincipal)
        props

    /// <summary>
    /// Creates an IAM policy statement with specified actions and resources
    /// </summary>
    let createPolicyStatement (actions: string list) (resources: string list) (effect: Effect) =
        let props = PolicyStatementProps()
        props.Effect <- effect
        props.Actions <- (actions |> List.toArray)
        props.Resources <- (resources |> List.toArray)
        PolicyStatement(props)

    /// <summary>
    /// Creates a policy statement allowing specified actions on resources
    /// </summary>
    let allow (actions: string list) (resources: string list) =
        createPolicyStatement actions resources Effect.ALLOW

    /// <summary>
    /// Creates a policy statement denying specified actions on resources
    /// </summary>
    let deny (actions: string list) (resources: string list) =
        createPolicyStatement actions resources Effect.DENY

    /// <summary>
    /// Attaches an AWS managed policy to a role by its name
    /// Common managed policies:
    /// - AWSLambdaBasicExecutionRole: CloudWatch Logs write
    /// - AWSLambdaVPCAccessExecutionRole: VPC network interfaces
    /// - AmazonS3ReadOnlyAccess: Read S3 objects
    /// - AmazonDynamoDBReadOnlyAccess: Read DynamoDB tables
    /// </summary>
    let attachManagedPolicy (role: IRole) (managedPolicyName: string) =
        role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName(managedPolicyName))

    /// <summary>
    /// Creates a Lambda execution role with minimal permissions:
    /// - CloudWatch Logs write (basic execution)
    /// - Optional: KMS decrypt for encrypted environment variables
    /// </summary>
    let createLambdaExecutionRole (functionName: string) (includeKmsDecrypt: bool) =
        let roleProps =
            createRole "lambda.amazonaws.com" (sprintf "%s-execution-role" functionName)

        // Attach basic execution role for CloudWatch Logs
        let role = Role(null, sanitizeName (sprintf "%s-Role" functionName), roleProps)
        attachManagedPolicy role "service-role/AWSLambdaBasicExecutionRole"

        // Optionally add KMS decrypt for environment variables
        if includeKmsDecrypt then
            let kmsStmt = allow [ "kms:Decrypt" ] [ "arn:aws:kms:*:*:key/*" ]
            role.AddToPolicy kmsStmt |> ignore

        role

    /// <summary>
    /// Creates a role for S3 bucket access with specific permissions
    /// </summary>
    let createS3AccessRole (roleName: string) (bucketArn: string) (readOnly: bool) =
        let roleProps = createRole "lambda.amazonaws.com" roleName
        let role = Role(null, sanitizeName roleName, roleProps)

        let actions =
            if readOnly then
                [ "s3:GetObject"; "s3:ListBucket" ]
            else
                [ "s3:GetObject"; "s3:PutObject"; "s3:DeleteObject"; "s3:ListBucket" ]

        let stmt = allow actions [ bucketArn; sprintf "%s/*" bucketArn ]
        role.AddToPolicy stmt |> ignore

        role

    /// <summary>
    /// Creates a role for DynamoDB table access
    /// </summary>
    let createDynamoDBAccessRole (roleName: string) (tableArn: string) (readOnly: bool) =
        let roleProps = createRole "lambda.amazonaws.com" roleName
        let role = Role(null, sanitizeName roleName, roleProps)

        let actions =
            if readOnly then
                [ "dynamodb:GetItem"
                  "dynamodb:Query"
                  "dynamodb:Scan"
                  "dynamodb:BatchGetItem" ]
            else
                [ "dynamodb:GetItem"
                  "dynamodb:PutItem"
                  "dynamodb:UpdateItem"
                  "dynamodb:DeleteItem"
                  "dynamodb:Query"
                  "dynamodb:Scan"
                  "dynamodb:BatchGetItem"
                  "dynamodb:BatchWriteItem" ]

        let stmt = allow actions [ tableArn; sprintf "%s/*" tableArn ]
        role.AddToPolicy(stmt) |> ignore

        role

    // ============================================================================
    // Lambda Role Builder DSL
    // ============================================================================

    /// <summary>
    /// High-level Lambda Role builder with common permission patterns.
    /// Follows least-privilege principle with opt-in permissions.
    /// </summary>
    type LambdaRoleConfig =
        { RoleName: string
          ConstructId: string option
          AssumeRolePrincipal: string option
          ManagedPolicies: string list
          InlinePolicies: PolicyStatement list
          IncludeBasicExecution: bool option
          IncludeVpcExecution: bool option
          IncludeKmsDecrypt: bool option
          IncludeXRay: bool option }

    type LambdaRoleBuilder(name: string) =
        member _.Yield(_: unit) : LambdaRoleConfig =
            { RoleName = name
              ConstructId = None
              AssumeRolePrincipal = Some "lambda.amazonaws.com"
              ManagedPolicies = []
              InlinePolicies = []
              IncludeBasicExecution = Some true
              IncludeVpcExecution = None
              IncludeKmsDecrypt = None
              IncludeXRay = None }

        member _.Zero() : LambdaRoleConfig =
            { RoleName = name
              ConstructId = None
              AssumeRolePrincipal = Some "lambda.amazonaws.com"
              ManagedPolicies = []
              InlinePolicies = []
              IncludeBasicExecution = Some true
              IncludeVpcExecution = None
              IncludeKmsDecrypt = None
              IncludeXRay = None }

        member inline _.Delay([<InlineIfLambda>] f: unit -> LambdaRoleConfig) : LambdaRoleConfig = f ()

        member _.Combine(state1: LambdaRoleConfig, state2: LambdaRoleConfig) : LambdaRoleConfig =
            { RoleName = state1.RoleName
              ConstructId = state2.ConstructId |> Option.orElse state1.ConstructId
              AssumeRolePrincipal = state2.AssumeRolePrincipal |> Option.orElse state1.AssumeRolePrincipal
              ManagedPolicies = state1.ManagedPolicies @ state2.ManagedPolicies
              InlinePolicies = state1.InlinePolicies @ state2.InlinePolicies
              IncludeBasicExecution = state2.IncludeBasicExecution |> Option.orElse state1.IncludeBasicExecution
              IncludeVpcExecution = state2.IncludeVpcExecution |> Option.orElse state1.IncludeVpcExecution
              IncludeKmsDecrypt = state2.IncludeKmsDecrypt |> Option.orElse state1.IncludeKmsDecrypt
              IncludeXRay = state2.IncludeXRay |> Option.orElse state1.IncludeXRay }

        member inline x.For
            (
                config: LambdaRoleConfig,
                [<InlineIfLambda>] f: unit -> LambdaRoleConfig
            ) : LambdaRoleConfig =
            let newConfig = f ()
            x.Combine(config, newConfig)

        member _.Run(config: LambdaRoleConfig) : LambdaRoleSpec =
            let constructId = config.ConstructId |> Option.defaultValue config.RoleName

            let principal =
                config.AssumeRolePrincipal |> Option.defaultValue "lambda.amazonaws.com"

            let roleProps = RoleProps()
            roleProps.RoleName <- sanitizeName config.RoleName
            roleProps.AssumedBy <- ServicePrincipal(principal)

            let role = Role(null, sanitizeName constructId, roleProps)

            // Add basic execution role (CloudWatch Logs)
            if config.IncludeBasicExecution |> Option.defaultValue true then
                attachManagedPolicy role "service-role/AWSLambdaBasicExecutionRole"

            // Add VPC execution role (ENI management)
            if config.IncludeVpcExecution |> Option.defaultValue false then
                attachManagedPolicy role "service-role/AWSLambdaVPCAccessExecutionRole"

            // Add KMS decrypt for environment variables
            if config.IncludeKmsDecrypt |> Option.defaultValue false then
                let kmsStmt = allow [ "kms:Decrypt" ] [ "arn:aws:kms:*:*:key/*" ]
                role.AddToPolicy kmsStmt |> ignore

            // Add X-Ray tracing
            if config.IncludeXRay |> Option.defaultValue false then
                attachManagedPolicy role "AWSXRayDaemonWriteAccess"

            // Add managed policies
            for policy in config.ManagedPolicies do
                attachManagedPolicy role policy

            // Add inline policies
            for statement in config.InlinePolicies do
                role.AddToPolicy statement |> ignore

            { RoleName = config.RoleName
              ConstructId = constructId
              Role = Some role }

        /// <summary>Sets the construct ID for the role.</summary>
        [<CustomOperation("constructId")>]
        member _.ConstructId(config: LambdaRoleConfig, id: string) = { config with ConstructId = Some id }

        /// <summary>Sets the service principal (default: lambda.amazonaws.com).</summary>
        [<CustomOperation("assumeRolePrincipal")>]
        member _.AssumeRolePrincipal(config: LambdaRoleConfig, principal: string) =
            { config with
                AssumeRolePrincipal = Some principal }

        /// <summary>Adds an AWS managed policy by name.</summary>
        [<CustomOperation("managedPolicy")>]
        member _.ManagedPolicy(config: LambdaRoleConfig, policyName: string) =
            { config with
                ManagedPolicies = config.ManagedPolicies @ [ policyName ] }

        /// <summary>Adds an inline policy statement.</summary>
        [<CustomOperation("inlinePolicy")>]
        member _.InlinePolicy(config: LambdaRoleConfig, statement: PolicyStatement) =
            { config with
                InlinePolicies = config.InlinePolicies @ [ statement ] }

        /// <summary>Includes basic execution role for CloudWatch Logs (default: true).</summary>
        [<CustomOperation("basicExecution")>]
        member _.BasicExecution(config: LambdaRoleConfig) =
            { config with
                IncludeBasicExecution = Some true }

        /// <summary>Includes VPC execution role for ENI management.</summary>
        [<CustomOperation("vpcExecution")>]
        member _.VpcExecution(config: LambdaRoleConfig) =
            { config with
                IncludeVpcExecution = Some true }

        /// <summary>Includes KMS decrypt permission for encrypted environment variables.</summary>
        [<CustomOperation("kmsDecrypt")>]
        member _.KmsDecrypt(config: LambdaRoleConfig) =
            { config with
                IncludeKmsDecrypt = Some true }

        /// <summary>Includes X-Ray tracing permissions.</summary>
        [<CustomOperation("xrayTracing")>]
        member _.XrayTracing(config: LambdaRoleConfig) = { config with IncludeXRay = Some true }

/// <summary>
/// Policy statement builder for creating inline IAM policies (high-level API)
/// </summary>
type IAMPolicyStatementBuilderState =
    { Actions: string list
      Resources: string list
      Effect: Effect }

/// <summary>
/// Policy statement builder for creating inline IAM policies (high-level API) using immutable state
/// </summary>
type IAMPolicyStatementBuilder(state: IAMPolicyStatementBuilderState) =
    new() =
        IAMPolicyStatementBuilder(
            { Actions = []
              Resources = []
              Effect = Effect.ALLOW }
        )

    member _.Actions(acts: string list) =
        IAMPolicyStatementBuilder({ state with Actions = acts })

    member _.Resources(res: string list) =
        IAMPolicyStatementBuilder({ state with Resources = res })

    member _.Effect(eff: Effect) =
        IAMPolicyStatementBuilder({ state with Effect = eff })

    member _.Build() =
        IAM.createPolicyStatement state.Actions state.Resources state.Effect

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module IAMBuilders =
    /// <summary>
    /// Lambda role builder for creating inline IAM policies (high-level API) using immutable state
    /// </summary>
    let lambdaRole = IAM.LambdaRoleBuilder

    /// <summary>
    /// Policy statement builder for creating inline IAM policies (high-level API) using immutable state
    /// </summary>
    let policyStatement = IAMPolicyStatementBuilder
