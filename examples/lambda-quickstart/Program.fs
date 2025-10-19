open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open FsCDK.Compute

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
    envProps.Description <- "FsCDK Lambda Quickstart Example - demonstrates Lambda functions with security defaults"
    
    // Create the stack
    let stack = Stack(app, "LambdaQuickstartStack", envProps)
    
    // Apply tags
    Tags.Of(stack).Add("Project", "FsCDK-Examples")
    Tags.Of(stack).Add("Example", "Lambda-Quickstart")
    Tags.Of(stack).Add("ManagedBy", "FsCDK")
    
    // Example 1: Basic function with all defaults
    // Note: In a real scenario, provide actual code path
    let basicFunc = lambdaFunction "basic-function" {
        handler "index.handler"
        runtime Runtime.NODEJS_18_X
        codePath "./dummy-code"
        description "Basic Lambda function with secure defaults"
        // Uses defaults:
        // - memorySize = 512 MB
        // - timeout = 30 seconds
        // - logRetention = 90 days
        // - environment encryption = KMS
    }
    
    // Example 2: Function with custom memory and timeout
    let computeFunc = lambdaFunction "compute-intensive-function" {
        handler "process.handler"
        runtime Runtime.PYTHON_3_11
        codePath "./dummy-code"
        memorySize 2048
        timeout 300.0
        description "Compute-intensive function with higher memory and timeout"
    }
    
    // Example 3: Function with environment variables (encrypted by default)
    let apiFunc = lambdaFunction "api-handler-function" {
        handler "api.handler"
        runtime Runtime.NODEJS_20_X
        codePath "./dummy-code"
        environment [
            "LOG_LEVEL", "INFO"
            "API_VERSION", "v1"
            "REGION", (if System.String.IsNullOrEmpty(region) then "us-east-1" else region)
        ]
        description "API handler with encrypted environment variables"
    }
    
    // Example 4: Function with X-Ray tracing enabled
    let tracedFunc = lambdaFunction "traced-function" {
        handler "traced.handler"
        runtime Runtime.PYTHON_3_11
        codePath "./dummy-code"
        xrayEnabled
        description "Function with X-Ray tracing for debugging"
    }
    
    // Example 5: Function with custom log retention
    let devFunc = lambdaFunction "dev-function" {
        handler "dev.handler"
        runtime Runtime.DOTNET_8
        codePath "./dummy-code"
        logRetention RetentionDays.ONE_WEEK
        timeout 60.0
        description "Development function with shorter log retention"
    }
    
    // Example 6: Function with reserved concurrency
    let rateLimitedFunc = lambdaFunction "rate-limited-function" {
        handler "ratelimited.handler"
        runtime Runtime.NODEJS_18_X
        codePath "./dummy-code"
        reservedConcurrentExecutions 10
        description "Function with reserved concurrent executions for rate limiting"
    }
    
    app.Synth() |> ignore
    0
