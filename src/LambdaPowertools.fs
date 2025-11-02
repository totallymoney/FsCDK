namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.Lambda

/// <summary>
/// Lambda Powertools integration for production-grade observability.
///
/// **Features:**
/// - Structured logging with correlation IDs
/// - Custom metrics without CloudWatch overhead
/// - Distributed tracing integration
/// - Best practice environment variables
///
/// **Rationale:**
/// Yan Cui strongly recommends Lambda Powertools for production Lambda functions:
/// - Reduces boilerplate for logging, metrics, tracing
/// - Standardizes observability across functions
/// - Automatic correlation ID propagation
/// - Zero cold-start impact
///
/// **Use Cases:**
/// - Production Lambda functions
/// - Microservices architecture
/// - Event-driven applications
/// - Distributed tracing requirements
///
/// **Supported Runtimes:**
/// - Python 3.8+
/// - Node.js 14.x+
/// - Java 8+, 11, 17
/// - .NET 6+
/// </summary>
module LambdaPowertools =

    /// Lambda Powertools layer ARNs by region and runtime
    /// See: https://docs.powertools.aws.dev/lambda/python/latest/#lambda-layer
    module LayerVersionArns =

        /// Python Powertools layer ARNs
        module Python =
            let python38 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsPython:52"

            let python39 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsPython:52"

            let python310 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsPython:52"

            let python311 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsPython:52"

            let python312 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsPython:52"

        /// TypeScript/JavaScript Powertools layer ARNs
        module NodeJs =
            let node14 region =
                $"arn:aws:lambda:{region}:094274105915:layer:AWSLambdaPowertoolsTypeScript:25"

            let node16 region =
                $"arn:aws:lambda:{region}:094274105915:layer:AWSLambdaPowertoolsTypeScript:25"

            let node18 region =
                $"arn:aws:lambda:{region}:094274105915:layer:AWSLambdaPowertoolsTypeScript:25"

            let node20 region =
                $"arn:aws:lambda:{region}:094274105915:layer:AWSLambdaPowertoolsTypeScript:25"

        /// Java Powertools layer ARNs
        module Java =
            let java8 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsJava:15"

            let java11 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsJava:15"

            let java17 region =
                $"arn:aws:lambda:{region}:017000801446:layer:AWSLambdaPowertoolsJava:15"

        /// .NET Powertools (NuGet package - no layer needed)
        /// Use: dotnet add package AWS.Lambda.Powertools.Logging
        ///      dotnet add package AWS.Lambda.Powertools.Metrics
        ///      dotnet add package AWS.Lambda.Powertools.Tracing
        module DotNet =
            let nugetPackages =
                [ "AWS.Lambda.Powertools.Logging"
                  "AWS.Lambda.Powertools.Metrics"
                  "AWS.Lambda.Powertools.Tracing" ]

    /// Creates a Lambda layer from Powertools ARN
    let createPowertoolsLayer (scope: Constructs.Construct) (id: string) (layerVersionArn: string) =
        LayerVersion.FromLayerVersionArn(scope, id, layerVersionArn)

    /// Environment variables for Lambda Powertools configuration
    module EnvironmentVariables =

        /// Python Powertools environment variables
        module Python =
            let logLevel level = ("LOG_LEVEL", level) // DEBUG, INFO, WARNING, ERROR
            let serviceName name = ("POWERTOOLS_SERVICE_NAME", name)
            let metricsNamespace ns = ("POWERTOOLS_METRICS_NAMESPACE", ns)
            let tracingEnabled = ("POWERTOOLS_TRACE_DISABLED", "false")
            let tracingCaptureResponse = ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true")
            let tracingCaptureError = ("POWERTOOLS_TRACER_CAPTURE_ERROR", "true")

        /// TypeScript/JavaScript Powertools environment variables
        module NodeJs =
            let logLevel level = ("LOG_LEVEL", level)
            let serviceName name = ("POWERTOOLS_SERVICE_NAME", name)
            let metricsNamespace ns = ("POWERTOOLS_METRICS_NAMESPACE", ns)
            let tracingEnabled = ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true")

        /// Java Powertools environment variables
        module Java =
            let logLevel level = ("POWERTOOLS_LOG_LEVEL", level)
            let serviceName name = ("POWERTOOLS_SERVICE_NAME", name)
            let metricsNamespace ns = ("POWERTOOLS_METRICS_NAMESPACE", ns)
            let tracingEnabled = ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true")

        /// .NET Powertools environment variables
        module DotNet =
            let logLevel level = ("POWERTOOLS_LOG_LEVEL", level)
            let serviceName name = ("POWERTOOLS_SERVICE_NAME", name)
            let metricsNamespace ns = ("POWERTOOLS_METRICS_NAMESPACE", ns)
            let tracingEnabled = ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true")

    /// Configures a Lambda function with Powertools best practices
    let configurePowertools (func: IFunction) (serviceName: string) (logLevel: string) (metricsNamespace: string) =

        // Add common environment variables
        let envVars =
            [ EnvironmentVariables.Python.serviceName serviceName
              EnvironmentVariables.Python.logLevel logLevel
              EnvironmentVariables.Python.metricsNamespace metricsNamespace
              EnvironmentVariables.Python.tracingEnabled
              EnvironmentVariables.Python.tracingCaptureResponse
              EnvironmentVariables.Python.tracingCaptureError ]

        envVars

    /// Standard configurations for different environments
    module StandardConfigs =

        /// Development environment configuration
        let development serviceName =
            [ ("POWERTOOLS_SERVICE_NAME", serviceName)
              ("LOG_LEVEL", "DEBUG")
              ("POWERTOOLS_METRICS_NAMESPACE", $"{serviceName}/dev")
              ("POWERTOOLS_TRACE_DISABLED", "false") ]

        /// Production environment configuration
        let production serviceName =
            [ ("POWERTOOLS_SERVICE_NAME", serviceName)
              ("LOG_LEVEL", "INFO")
              ("POWERTOOLS_METRICS_NAMESPACE", $"{serviceName}/prod")
              ("POWERTOOLS_TRACE_DISABLED", "false")
              ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "true")
              ("POWERTOOLS_TRACER_CAPTURE_ERROR", "true") ]

        /// High-performance production configuration (minimal logging)
        let highPerformance serviceName =
            [ ("POWERTOOLS_SERVICE_NAME", serviceName)
              ("LOG_LEVEL", "WARN")
              ("POWERTOOLS_METRICS_NAMESPACE", $"{serviceName}/prod")
              ("POWERTOOLS_TRACE_DISABLED", "false")
              ("POWERTOOLS_TRACER_CAPTURE_RESPONSE", "false") ] // Reduce overhead

    /// Sample usage documentation
    module Examples =

        /// Python Lambda with Powertools
        let pythonExample =
            """
// In your F# CDK code:
lambda "MyFunction" {
    runtime Runtime.PYTHON_3_11
    handler "app.lambda_handler"
    code "./src"
    
    // Add Powertools layer
    layers [ 
        LayerVersion.FromLayerVersionArn(
            stack, 
            "PowertoolsLayer",
            "arn:aws:lambda:us-east-1:017000801446:layer:AWSLambdaPowertoolsPython:52"
        )
    ]
    
    // Add Powertools environment variables
    environment (LambdaPowertools.StandardConfigs.production "OrderService")
}

// In your Python Lambda code:
from aws_lambda_powertools import Logger, Tracer, Metrics
from aws_lambda_powertools.utilities.typing import LambdaContext

logger = Logger()
tracer = Tracer()
metrics = Metrics()

@logger.inject_lambda_context
@tracer.capture_lambda_handler
@metrics.log_metrics(capture_cold_start_metric=True)
def lambda_handler(event: dict, context: LambdaContext) -> dict:
    logger.info("Processing order", extra={"order_id": event.get("order_id")})
    metrics.add_metric(name="OrderProcessed", unit="Count", value=1)
    
    return {"statusCode": 200, "body": "Success"}
"""

        /// TypeScript Lambda with Powertools
        let typescriptExample =
            """
// In your F# CDK code:
lambda "MyFunction" {
    runtime Runtime.NODEJS_20_X
    handler "index.handler"
    code "./dist"
    
    layers [ 
        LayerVersion.FromLayerVersionArn(
            stack,
            "PowertoolsLayer", 
            "arn:aws:lambda:us-east-1:094274105915:layer:AWSLambdaPowertoolsTypeScript:25"
        )
    ]
    
    environment (LambdaPowertools.StandardConfigs.production "OrderService")
}

// In your TypeScript Lambda code:
import { Logger } from '@aws-lambda-powertools/logger';
import { Tracer } from '@aws-lambda-powertools/tracer';
import { Metrics } from '@aws-lambda-powertools/metrics';

const logger = new Logger();
const tracer = new Tracer();
const metrics = new Metrics();

export const handler = async (event: any) => {
    logger.info('Processing order', { orderId: event.orderId });
    metrics.addMetric('OrderProcessed', 'Count', 1);
    
    return { statusCode: 200, body: 'Success' };
};
"""

/// Helper functions for Lambda Powertools integration
module LambdaPowertoolsHelpers =

    /// Gets the appropriate Powertools layer ARN for a given runtime
    /// Returns None for unsupported runtimes (e.g., .NET which uses NuGet)
    let getPowertoolsLayerArn (runtime: Runtime) : string option =
        let region = "us-east-1" // Default region, will be replaced by actual region in stack

        // Match runtime family and return appropriate layer ARN
        match runtime.Name with
        | name when name.StartsWith("python3.8") -> Some(LambdaPowertools.LayerVersionArns.Python.python38 region)
        | name when name.StartsWith("python3.9") -> Some(LambdaPowertools.LayerVersionArns.Python.python39 region)
        | name when name.StartsWith("python3.10") -> Some(LambdaPowertools.LayerVersionArns.Python.python310 region)
        | name when name.StartsWith("python3.11") -> Some(LambdaPowertools.LayerVersionArns.Python.python311 region)
        | name when name.StartsWith("python3.12") -> Some(LambdaPowertools.LayerVersionArns.Python.python312 region)
        | name when name.StartsWith("nodejs14") -> Some(LambdaPowertools.LayerVersionArns.NodeJs.node14 region)
        | name when name.StartsWith("nodejs16") -> Some(LambdaPowertools.LayerVersionArns.NodeJs.node16 region)
        | name when name.StartsWith("nodejs18") -> Some(LambdaPowertools.LayerVersionArns.NodeJs.node18 region)
        | name when name.StartsWith("nodejs20") -> Some(LambdaPowertools.LayerVersionArns.NodeJs.node20 region)
        | name when name.StartsWith("java8") -> Some(LambdaPowertools.LayerVersionArns.Java.java8 region)
        | name when name.StartsWith("java11") -> Some(LambdaPowertools.LayerVersionArns.Java.java11 region)
        | name when name.StartsWith("java17") -> Some(LambdaPowertools.LayerVersionArns.Java.java17 region)
        | name when name.StartsWith("dotnet") -> None // .NET uses NuGet packages
        | _ -> None // Unsupported runtime

[<AutoOpen>]
module LambdaPowertoolsBuilders =
    // Helper to add Powertools to existing lambda configuration
    let withPowertools (serviceName: string) (environment: string) =
        match environment with
        | "development"
        | "dev" -> LambdaPowertools.StandardConfigs.development serviceName
        | "production"
        | "prod" -> LambdaPowertools.StandardConfigs.production serviceName
        | "performance" -> LambdaPowertools.StandardConfigs.highPerformance serviceName
        | _ -> LambdaPowertools.StandardConfigs.production serviceName
