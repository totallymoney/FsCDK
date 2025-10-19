namespace FsCDK.Security

open Amazon.CDK.AWS.IAM
open System.Text.RegularExpressions

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
    
    /// <summary>
    /// Sanitizes a name for IAM resource naming (removes invalid characters)
    /// IAM names can contain alphanumeric characters, hyphens, and underscores
    /// </summary>
    let sanitizeName (name: string) =
        Regex.Replace(name, "[^a-zA-Z0-9-_]", "-")
    
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
        let roleProps = createRole "lambda.amazonaws.com" (sprintf "%s-execution-role" functionName)
        
        // Attach basic execution role for CloudWatch Logs
        let role = Role(null, sanitizeName (sprintf "%s-Role" functionName), roleProps)
        attachManagedPolicy role "service-role/AWSLambdaBasicExecutionRole"
        
        // Optionally add KMS decrypt for environment variables
        if includeKmsDecrypt then
            let kmsStmt = allow [ "kms:Decrypt" ] [ "arn:aws:kms:*:*:key/*" ]
            role.AddToPolicy(kmsStmt) |> ignore
        
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
        role.AddToPolicy(stmt) |> ignore
        
        role
    
    /// <summary>
    /// Creates a role for DynamoDB table access
    /// </summary>
    let createDynamoDBAccessRole (roleName: string) (tableArn: string) (readOnly: bool) =
        let roleProps = createRole "lambda.amazonaws.com" roleName
        let role = Role(null, sanitizeName roleName, roleProps)
        
        let actions = 
            if readOnly then
                [ "dynamodb:GetItem"; "dynamodb:Query"; "dynamodb:Scan"; "dynamodb:BatchGetItem" ]
            else
                [ "dynamodb:GetItem"; "dynamodb:PutItem"; "dynamodb:UpdateItem"; "dynamodb:DeleteItem"
                  "dynamodb:Query"; "dynamodb:Scan"; "dynamodb:BatchGetItem"; "dynamodb:BatchWriteItem" ]
        
        let stmt = allow actions [ tableArn; sprintf "%s/*" tableArn ]
        role.AddToPolicy(stmt) |> ignore
        
        role

/// <summary>
/// Policy statement builder for creating inline IAM policies
/// </summary>
type PolicyStatementBuilder() =
    let mutable actions: string list = []
    let mutable resources: string list = []
    let mutable effect: Effect = Effect.ALLOW
    
    member _.Actions(acts: string list) =
        actions <- acts
        ()
    
    member _.Resources(res: string list) =
        resources <- res
        ()
    
    member _.Effect(eff: Effect) =
        effect <- eff
        ()
    
    member _.Build() =
        IAM.createPolicyStatement actions resources effect
