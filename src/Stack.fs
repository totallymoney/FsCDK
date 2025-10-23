namespace FsCDK

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.Events
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.ECS
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.Route53

// ============================================================================
// Operation Types - Unified Discriminated Union
// ============================================================================

type Operation =
    | TableOp of TableSpec
    | FunctionOp of FunctionSpec
    | DockerImageFunctionOp of DockerImageFunctionSpec
    | GrantOp of GrantSpec
    | TopicOp of TopicSpec
    | QueueOp of QueueSpec
    | BucketOp of BucketSpec
    | SubscriptionOp of SubscriptionSpec
    | VpcOp of VpcSpec
    | SecurityGroupOp of SecurityGroupSpec
    | RdsInstanceOp of DatabaseInstanceSpec
    | CloudFrontDistributionOp of DistributionSpec
    | UserPoolOp of UserPoolSpec
    | UserPoolClientOp of UserPoolClientSpec
    // New operations
    | NetworkLoadBalancerOp of NetworkLoadBalancerSpec
    | EventBridgeRuleOp of EventBridgeRuleSpec
    | EventBusOp of EventBusSpec
    | BastionHostOp of BastionHostSpec
    | VPCGatewayAttachmentOp of VPCGatewayAttachmentSpec
    | RouteTableOp of RouteTableSpec
    | RouteOp of RouteSpec
    | OIDCProviderOp of OIDCProviderSpec
    | ManagedPolicyOp of ManagedPolicySpec
    | CertificateOp of CertificateSpec
    | BucketPolicyOp of BucketPolicySpec
    | CloudWatchDashboardOp of DashboardSpec
    | EKSClusterOp of EKSClusterSpec
    | KinesisStreamOp of KinesisStreamSpec
    | HostedZoneOp of Route53HostedZoneSpec
    | OriginAccessIdentityOp of OriginAccessIdentitySpec

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (operation: Operation) : unit =
        match operation with
        | TableOp tableSpec ->
            let t = Table(stack, tableSpec.ConstructId, tableSpec.Props)
            tableSpec.Table <- Some t

        | FunctionOp lambdaSpec ->
            let fn = AWS.Lambda.Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)
            lambdaSpec.Function <- Some fn

            for action in lambdaSpec.Actions do
                action fn

        | DockerImageFunctionOp imageLambdaSpec ->
            AWS.Lambda.DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)
            |> ignore

        | GrantOp grantSpec -> Grants.processGrant stack grantSpec

        | TopicOp topicSpec -> Topic(stack, topicSpec.ConstructId, topicSpec.Props) |> ignore

        | QueueOp queueSpec ->
            // Build QueueProps from spec (convert primitives to Duration etc.)
            let props = QueueProps()
            props.QueueName <- queueSpec.QueueName

            queueSpec.VisibilityTimeout
            |> Option.iter (fun v -> props.VisibilityTimeout <- Duration.Seconds(v))

            queueSpec.MessageRetention
            |> Option.iter (fun r -> props.RetentionPeriod <- Duration.Seconds(r))

            queueSpec.FifoQueue |> Option.iter (fun f -> props.Fifo <- f)

            queueSpec.ContentBasedDeduplication
            |> Option.iter (fun c -> props.ContentBasedDeduplication <- c)

            queueSpec.DelaySeconds
            |> Option.iter (fun d -> props.DeliveryDelay <- Duration.Seconds(float d))

            match queueSpec.DeadLetterQueueName, queueSpec.MaxReceiveCount with
            | Some dlqName, Some maxReceive ->
                try
                    let dlq = stack.Node.FindChild(dlqName) :?> Queue
                    let dlqSpec = DeadLetterQueue(Queue = dlq, MaxReceiveCount = maxReceive)
                    props.DeadLetterQueue <- dlqSpec
                with ex ->
                    printfn $"Warning: Could not configure DLQ for queue %s{queueSpec.QueueName}: %s{ex.Message}"
            | _ -> ()

            Queue(stack, queueSpec.ConstructId, props) |> ignore

        | BucketOp bucketSpec ->
            match bucketSpec.Bucket with
            | None ->
                let bucket = Bucket(stack, bucketSpec.ConstructId, bucketSpec.Props)
                bucketSpec.Bucket <- Some bucket
            | Some b ->
                if b.Stack <> stack || b.BucketName <> bucketSpec.BucketName then
                    printfn
                        $"Warning: Bucket %s{b.BucketName} was already created to stack {b.Stack.StackName} when constructing same but %s{b.BucketName} to %s{stack.StackName}."

                ()

        | SubscriptionOp subscriptionSpec -> SNS.processSubscription stack subscriptionSpec

        | VpcOp vpcSpec ->
            let vpc = Vpc(stack, vpcSpec.ConstructId, vpcSpec.Props)
            vpcSpec.Vpc <- Some vpc

        | SecurityGroupOp sgSpec ->
            let sg = SecurityGroup(stack, sgSpec.ConstructId, sgSpec.Props)
            sgSpec.SecurityGroup <- Some sg

        | RdsInstanceOp rdsSpec -> DatabaseInstance(stack, rdsSpec.ConstructId, rdsSpec.Props) |> ignore

        | CloudFrontDistributionOp cfSpec -> Distribution(stack, cfSpec.ConstructId, cfSpec.Props) |> ignore

        | UserPoolOp upSpec ->
            let up = UserPool(stack, upSpec.ConstructId, upSpec.Props)
            upSpec.UserPool <- Some up

        | UserPoolClientOp upcSpec -> UserPoolClient(stack, upcSpec.ConstructId, upcSpec.Props) |> ignore

        // New operations
        | NetworkLoadBalancerOp nlbSpec ->
            let nlb = NetworkLoadBalancer(stack, nlbSpec.ConstructId, nlbSpec.Props)

            if nlb.Vpc = null then
                failwith "VPC is required for Network Load Balancer"

            nlbSpec.LoadBalancer <- Some nlb


        | EventBridgeRuleOp ruleSpec ->
            let rule = Rule(stack, ruleSpec.ConstructId, ruleSpec.Props)
            ruleSpec.Rule <- Some rule

        | EventBusOp busSpec ->
            let bus =
                Amazon.CDK.AWS.Events.EventBus(
                    stack,
                    busSpec.ConstructId,
                    EventBusProps(EventBusName = busSpec.EventBusName)
                )

            busSpec.EventBus <- Some bus

        | BastionHostOp bastionSpec ->
            let bastion = BastionHostLinux(stack, bastionSpec.ConstructId, bastionSpec.Props)
            bastionSpec.BastionHost <- Some bastion

        | VPCGatewayAttachmentOp attachSpec ->
            let props = CfnVPCGatewayAttachmentProps()
            props.VpcId <- attachSpec.VpcId

            attachSpec.InternetGatewayId
            |> Option.iter (fun id -> props.InternetGatewayId <- id)

            attachSpec.VpnGatewayId |> Option.iter (fun id -> props.VpnGatewayId <- id)
            let attachment = CfnVPCGatewayAttachment(stack, attachSpec.ConstructId, props)
            attachSpec.Attachment <- Some attachment

        | RouteTableOp rtSpec ->
            let rt = CfnRouteTable(stack, rtSpec.ConstructId, rtSpec.Props)
            rtSpec.RouteTable <- Some rt

        | RouteOp routeSpec -> CfnRoute(stack, routeSpec.ConstructId, routeSpec.Props) |> ignore

        | OIDCProviderOp oidcSpec ->
            let provider = OpenIdConnectProvider(stack, oidcSpec.ConstructId, oidcSpec.Props)
            oidcSpec.Provider <- Some provider

        | ManagedPolicyOp policySpec ->
            let policy =
                Amazon.CDK.AWS.IAM.ManagedPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- Some policy

        | CertificateOp certSpec ->
            let cert =
                Amazon.CDK.AWS.CertificateManager.Certificate(stack, certSpec.ConstructId, certSpec.Props)

            certSpec.Certificate <- Some cert

        | BucketPolicyOp policySpec ->
            let policy =
                Amazon.CDK.AWS.S3.BucketPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- Some policy

        | CloudWatchDashboardOp dashSpec ->
            let dashboard = Dashboard(stack, dashSpec.ConstructId, dashSpec.Props)
            dashSpec.Dashboard <- Some dashboard

        | EKSClusterOp spec ->
            let cluster = AWS.EKS.Cluster(stack, spec.ConstructId, spec.Props)
            spec.Cluster <- Some cluster

        | KinesisStreamOp spec ->
            let stream = Stream(stack, spec.ConstructId, spec.Props)
            spec.Stream <- Some stream

        | HostedZoneOp spec ->
            let zone = HostedZone(stack, spec.ConstructId, spec.Props)
            spec.HostedZone <- Some zone

        | OriginAccessIdentityOp spec ->
            let oai = OriginAccessIdentity(stack, spec.ConstructId, spec.Props)
            spec.Identity <- Some oai

// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

type StackConfig =
    { Name: string
      App: App option
      Props: StackProps option
      Operations: Operation list }

type StackBuilder(name: string) =

    member _.Yield _ : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [] }

    member _.Yield(tableSpec: TableSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ TableOp tableSpec ] }

    member _.Yield(app: App) : StackConfig =
        { Name = name
          App = Some app
          Props = None
          Operations = [] }

    member _.Yield(funcSpec: FunctionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ FunctionOp funcSpec ] }

    member _.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ DockerImageFunctionOp dockerSpec ] }

    member _.Yield(grantSpec: GrantSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ GrantOp grantSpec ] }

    member _.Yield(topicSpec: TopicSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ TopicOp topicSpec ] }

    member _.Yield(queueSpec: QueueSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ QueueOp queueSpec ] }

    member _.Yield(bucketSpec: BucketSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ BucketOp bucketSpec ] }

    member _.Yield(subSpec: SubscriptionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ SubscriptionOp subSpec ] }

    member _.Yield(vpcSpec: VpcSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ VpcOp vpcSpec ] }

    member _.Yield(sgSpec: SecurityGroupSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ SecurityGroupOp sgSpec ] }

    member _.Yield(rdsSpec: DatabaseInstanceSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ RdsInstanceOp rdsSpec ] }

    member _.Yield(cfSpec: DistributionSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ CloudFrontDistributionOp cfSpec ] }

    member _.Yield(upSpec: UserPoolSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ UserPoolOp upSpec ] }

    member _.Yield(upcSpec: UserPoolClientSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ UserPoolClientOp upcSpec ] }

    member _.Yield(props: StackProps) : StackConfig =
        { Name = name
          App = None
          Props = Some props
          Operations = [] }

    // New Yield overloads
    member _.Yield(nlbSpec: NetworkLoadBalancerSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ NetworkLoadBalancerOp nlbSpec ] }

    member _.Yield(ruleSpec: EventBridgeRuleSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ EventBridgeRuleOp ruleSpec ] }

    member _.Yield(busSpec: EventBusSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ EventBusOp busSpec ] }

    member _.Yield(bastionSpec: BastionHostSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ BastionHostOp bastionSpec ] }

    member _.Yield(attachSpec: VPCGatewayAttachmentSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ VPCGatewayAttachmentOp attachSpec ] }

    member _.Yield(rtSpec: RouteTableSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ RouteTableOp rtSpec ] }

    member _.Yield(routeSpec: RouteSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ RouteOp routeSpec ] }

    member _.Yield(oidcSpec: OIDCProviderSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ OIDCProviderOp oidcSpec ] }

    member _.Yield(policySpec: ManagedPolicySpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ ManagedPolicyOp policySpec ] }

    member _.Yield(certSpec: CertificateSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ CertificateOp certSpec ] }

    member _.Yield(policySpec: BucketPolicySpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ BucketPolicyOp policySpec ] }

    member _.Yield(dashSpec: DashboardSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ CloudWatchDashboardOp dashSpec ] }

    member _.Yield(spec: EKSClusterSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ EKSClusterOp spec ] }

    member _.Yield(spec: KinesisStreamSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ KinesisStreamOp spec ] }

    member _.Yield(spec: Route53HostedZoneSpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ HostedZoneOp spec ] }

    member _.Yield(spec: OriginAccessIdentitySpec) : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [ OriginAccessIdentityOp spec ] }

    member _.Zero() : StackConfig =
        { Name = name
          App = None
          Props = None
          Operations = [] }

    member _.Combine(state1: StackConfig, state2: StackConfig) : StackConfig =
        { Name = state1.Name
          App = state1.App
          Props = if state1.Props.IsSome then state1.Props else state2.Props
          Operations = state1.Operations @ state2.Operations }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackConfig) : StackConfig = f ()

    member inline x.For(config: StackConfig, [<InlineIfLambda>] f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.Run(config: StackConfig) =
        let app = config.App |> Option.defaultWith (fun () -> App())

        let stack =
            match config.Props with
            | Some props -> Stack(app, name, props)
            | None -> Stack(app, name)

        for op in config.Operations do
            StackOperations.processOperation stack op

// ============================================================================
// Builders
// ============================================================================

[<AutoOpen>]
module StackBuilders =
    /// <summary>Creates an AWS CDK Stack construct.</summary>
    /// <param name="name">The name of the stack.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     lambda myFunction
    ///     bucket myBucket
    /// }
    /// </code>
    let stack name = StackBuilder(name)
