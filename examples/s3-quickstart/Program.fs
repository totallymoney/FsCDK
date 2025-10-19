open Amazon.CDK
open FsCDK

[<EntryPoint>]
let main _ =
    let app = App()
    
    // Get environment configuration from environment variables
    let accountId = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
    let region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
    
    // Create stack props with environment
    let envProps = StackProps()
    if not (System.String.IsNullOrEmpty(accountId)) && not (System.String.IsNullOrEmpty(region)) then
        envProps.Env <- Amazon.CDK.Environment(Account = accountId, Region = region)
    envProps.Description <- "FsCDK S3 Quickstart Example - demonstrates S3 bucket with security defaults"
    
    // Create the stack
    let stack = Stack(app, "S3QuickstartStack", envProps)
    
    // Apply tags
    Tags.Of(stack).Add("Project", "FsCDK-Examples")
    Tags.Of(stack).Add("Example", "S3-Quickstart")
    Tags.Of(stack).Add("ManagedBy", "FsCDK")
    
    // Example 1: Basic bucket with all security defaults
    let basicBucket = s3Bucket "basic-secure-bucket" { () }
    // Uses defaults:
    // - BlockPublicAccess = BLOCK_ALL
    // - Encryption = KMS_MANAGED
    // - EnforceSSL = true
    // - Versioned = false
    
    // Example 2: Versioned bucket for data protection
    let versionedBucket = s3Bucket "versioned-bucket" {
        versioned true
    }
    
    // Example 3: Bucket with lifecycle rules for cost optimization
    let lifecycleBucket = s3Bucket "lifecycle-bucket" {
        versioned true
        
        // Expire old objects after 30 days
        LifecycleRuleHelpers.expireAfter 30 "expire-old-objects"
        
        // Transition to Glacier for archival after 90 days
        LifecycleRuleHelpers.transitionToGlacier 90 "archive-to-glacier"
        
        // Delete non-current versions after 180 days
        LifecycleRuleHelpers.deleteNonCurrentVersions 180 "cleanup-old-versions"
    }
    
    // Example 4: Bucket with custom removal policy for dev/test
    let tempBucket = s3Bucket "temporary-bucket" {
        removalPolicy RemovalPolicy.DESTROY
        autoDeleteObjects true
    }
    
    app.Synth() |> ignore
    0
