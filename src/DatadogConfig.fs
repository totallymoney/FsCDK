namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.SSM
open Amazon.CDK.AWS.Lambda
open System.Collections.Generic

module DatadogConfig =

    type DatadogSettings =
        { Site: string
          ApiKeyParameterName: string
          Service: string
          Environment: string
          Version: string option
          Tags: string option }

    let getDefaultSettings (env: string) (version: string) =
        { Site = "datadoghq.eu"
          ApiKeyParameterName = "/datadog/api-key"
          Service = "Highlights"
          Environment = env
          Version = Some version
          Tags = Some "language:fsharp,team:platform" }

    // Datadog Lambda Extension layer ARNs for .NET (EU regions only)
    // Version 58 have good SSM support and are publicly accessible
    // See: https://docs.datadoghq.com/serverless/libraries_integrations/extension/
    let getDatadogExtensionLayerArn (region: string) =
        match region with
        | "eu-west-1" -> "arn:aws:lambda:eu-west-1:464622532012:layer:Datadog-Extension:58"
        | "eu-west-2" -> "arn:aws:lambda:eu-west-2:464622532012:layer:Datadog-Extension:58"
        | _ ->
            failwithf $"Datadog Extension layer only configured for eu-west-1 and eu-west-2. Current region: %s{region}"

    // .NET tracer layer ARNs (EU regions only)
    // Version 15 is a stable, publicly accessible version
    let getDatadogDotNetTracerLayerArn (region: string) =
        match region with
        | "eu-west-1" -> "arn:aws:lambda:eu-west-1:464622532012:layer:dd-trace-dotnet:15"
        | "eu-west-2" -> "arn:aws:lambda:eu-west-2:464622532012:layer:dd-trace-dotnet:15"
        | _ ->
            failwithf
                $"Datadog .NET Tracer layer only configured for eu-west-1 and eu-west-2. Current region: %s{region}"

    let configureDatadogForLambda (stack: Stack) (lambda: Function) (settings: DatadogSettings) =
        // Use CDK Fn.Sub to dynamically construct the layer ARN based on the region at deployment time
        // This handles the fact that stack.Region is a token during synthesis

        // Datadog Extension layer ARN (version 58 - SSM compatible)
        let extensionLayerArn =
            Fn.Sub("arn:aws:lambda:${AWS::Region}:464622532012:layer:Datadog-Extension:58", null)

        // .NET tracer layer ARN (version 15 - stable public version)
        let tracerLayerArn =
            Fn.Sub("arn:aws:lambda:${AWS::Region}:464622532012:layer:dd-trace-dotnet:15", null)

        // Add Datadog Extension layer
        let extensionLayer =
            LayerVersion.FromLayerVersionArn(stack, $"{lambda.Node.Id}-DatadogExtension", extensionLayerArn)

        // Add .NET tracer layer
        let tracerLayer =
            LayerVersion.FromLayerVersionArn(stack, $"{lambda.Node.Id}-DatadogDotNetTracer", tracerLayerArn)

        // Add layers to Lambda
        lambda.AddLayers(extensionLayer, tracerLayer)

        // Add Datadog environment variables
        let envVars = Dictionary<string, string>()

        // Core Datadog configuration
        // Resolves plain String parameter via CDK helper (renders a CFN dynamic reference)
        let ddApiKey =
            StringParameter.ValueForStringParameter(stack, settings.ApiKeyParameterName)

        envVars.Add("DD_API_KEY", ddApiKey)

        envVars.Add("DD_SITE", settings.Site)
        envVars.Add("DD_ENV", settings.Environment)
        envVars.Add("DD_SERVICE", settings.Service)

        // Enable various Datadog features
        envVars.Add("DD_TRACE_ENABLED", "true")
        envVars.Add("DD_LOGS_INJECTION", "true")
        envVars.Add("DD_MERGE_XRAY_TRACES", "true")
        envVars.Add("DD_SERVERLESS_LOGS_ENABLED", "true")
        envVars.Add("DD_CAPTURE_LAMBDA_PAYLOAD", "false")
        envVars.Add("DD_ENHANCED_METRICS", "true")

        // .NET-specific settings
        envVars.Add("CORECLR_ENABLE_PROFILING", "1")
        envVars.Add("CORECLR_PROFILER", "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}")
        envVars.Add("CORECLR_PROFILER_PATH", "/opt/datadog/Datadog.Trace.ClrProfiler.Native.so")
        envVars.Add("DD_DOTNET_TRACER_HOME", "/opt/datadog")

        // Add a version if provided
        match settings.Version with
        | Some version -> envVars.Add("DD_VERSION", version)
        | None -> ()

        // Add tags if provided
        match settings.Tags with
        | Some tags -> envVars.Add("DD_TAGS", tags)
        | None -> ()

        // Apply environment variables to Lambda
        envVars
        |> Seq.iter (fun kvp -> lambda.AddEnvironment(kvp.Key, kvp.Value) |> ignore)
