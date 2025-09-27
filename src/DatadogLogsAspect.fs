namespace FsCDK

open Amazon.CDK
open Constructs
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.Logs.Destinations
open Amazon.CDK.AWS.Lambda
open Amazon.JSII.Runtime.Deputy

// ============================================================================
// Datadog Log Subscription Configuration
// Two approaches are provided - both are JSII-compatible:
// 1. Aspect-based: Uses CDK Aspects to automatically find and subscribe all log groups
// 2. Module-based: Direct function calls to configure specific Lambda log subscriptions
// ============================================================================

/// An aspect that attaches a Datadog log subscription to every CloudWatch LogGroup in the scope
/// It forwards all events to the provided Datadog Forwarder Lambda (imported by ARN).
/// This implementation is JSII-compatible for use with CDK Aspects.
[<AllowNullLiteral>]
type DatadogLogSubscriptionAspect(stack: Stack, forwarderArn: string) =
    inherit DeputyBase()

    interface IAspect with
        member _.Visit(node: IConstruct) =
            match node with
            | :? LogGroup as lg ->
                // Import the Datadog forwarder Lambda by ARN
                let forwarder =
                    Function.FromFunctionArn(stack, $"{lg.Node.Id}-DdForwarder", forwarderArn)

                // Destination that also adds invoke permissions from CW Logs automatically
                let destination: ILogSubscriptionDestination =
                    LambdaDestination(forwarder, LambdaDestinationOptions(AddPermissions = true))
                    :> ILogSubscriptionDestination

                // Attach a subscription filter that forwards all log events
                SubscriptionFilter(
                    lg,
                    $"{lg.Node.Id}-DatadogSubscription",
                    SubscriptionFilterProps(
                        Destination = destination,
                        FilterPattern = FilterPattern.AllEvents(),
                        LogGroup = lg
                    )
                )
                |> ignore
            | _ -> ()

/// Alternative implementation using a simpler approach that's more JSII-friendly
/// This creates log subscriptions for Lambda functions directly
module DatadogLogSubscription =

    /// Configure log subscription for a specific Lambda function
    let configureLambdaLogSubscription (stack: Stack) (lambda: Function) (forwarderArn: string) =
        // Get or create the log group for the Lambda
        let logGroupName = $"/aws/lambda/{lambda.FunctionName}"

        // Import the log group
        let logGroup =
            LogGroup.FromLogGroupName(stack, $"{lambda.Node.Id}-LogGroup", logGroupName)

        // Import the Datadog forwarder Lambda
        let forwarder =
            Function.FromFunctionArn(stack, $"{lambda.Node.Id}-DdForwarder", forwarderArn)

        // Create the subscription
        let destination =
            LambdaDestination(forwarder, LambdaDestinationOptions(AddPermissions = true))

        SubscriptionFilter(
            stack,
            $"{lambda.Node.Id}-DatadogSubscription",
            SubscriptionFilterProps(
                Destination = destination,
                FilterPattern = FilterPattern.AllEvents(),
                LogGroup = logGroup
            )
        )
        |> ignore

    /// Configure log subscriptions for all Lambda functions in a stack
    let configureStackLogSubscriptions (stack: Stack) (forwarderArn: string) =
        // Find all Lambda functions in the stack
        for child in stack.Node.Children do
            match child with
            | :? Function as lambda -> configureLambdaLogSubscription stack lambda forwarderArn
            | _ -> ()
