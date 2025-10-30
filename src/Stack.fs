namespace FsCDK

open System.Collections.Generic
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
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
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.Route53
open Constructs
//open Amazon.CDK.AWS.CloudHSMV2

// ============================================================================
// Operation Types - Unified Discriminated Union
// ============================================================================

type Operation =
    | TableOp of TableSpec
    | FunctionOp of FunctionSpec
    | DockerImageFunctionOp of DockerImageFunctionSpec
    // | GrantOp of GrantSpec
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
    //| CloudHSMClusterOp of CloudHSMClusterSpec
    | LambdaRoleOp of IAM.LambdaRoleSpec
    | CloudWatchAlarmOp of CloudWatchAlarmSpec
    | KMSKeyOp of KMSKeySpec
    | EC2InstanceOp of EC2InstanceSpec
    | ECSFargateServiceOp of ECSFargateServiceSpec
    | Route53RecordOp of Route53ARecordSpec
    | ALBOp of ALBSpec
    | SecretsManagerOp of SecretsManagerSpec
    | ElasticBeanstalkEnvironmentOp of ElasticBeanstalkEnvironmentSpec
    | DnsValidatedCertificateOp of DnsValidatedCertificateSpec
    | ECSClusterOp of ECSClusterSpec
    | DatabaseInstanceOp of DatabaseInstanceSpec

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (operation: Operation) : unit =
        match operation with
        | TableOp tableSpec ->
            let table = Table(stack, tableSpec.ConstructId, tableSpec.Props)
            tableSpec.Table <- table

            tableSpec.Grants
            |> Option.iter (fun grants ->
                match grants with
                | GrantReadData fn -> table.GrantReadData(fn) |> ignore
                | GrantFullAccess grantable -> table.GrantFullAccess(grantable) |> ignore
                | GrantReadWriteData grantable -> table.GrantReadWriteData(grantable) |> ignore
                | GrantWriteData grantable -> table.GrantWriteData(grantable) |> ignore
                | GrantStreamRead grantable -> table.GrantStreamRead(grantable) |> ignore
                | GrantStream(grantable, actions) -> table.GrantStream(grantable, Seq.toArray actions) |> ignore
                | GrantTableListStreams grantable -> table.GrantTableListStreams(grantable) |> ignore
                | Grant(grantable, actions) -> table.Grant(grantable, Seq.toArray actions) |> ignore)

        | FunctionOp lambdaSpec ->
            let fn = AWS.Lambda.Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)

            lambdaSpec.EventSources
            |> Seq.map (fun e -> fn.AddEventSource e)
            |> Seq.toList
            |> ignore

            lambdaSpec.Function <- fn

            for action in lambdaSpec.Actions do
                action fn

        | DockerImageFunctionOp imageLambdaSpec ->
            let fn =
                AWS.Lambda.DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)

            imageLambdaSpec.Function <- fn

        // | GrantOp grantSpec -> Grants.processGrant stack grantSpec

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
            let bucket = Bucket(stack, bucketSpec.ConstructId, bucketSpec.Props)

            bucketSpec.Grant
            |> Option.iter (fun grants ->
                match grants with
                | GrantRead fn -> bucket.GrantRead(fn) |> ignore
                | GrantDelete grantable -> bucket.GrantDelete(grantable) |> ignore
                | GrantPublicAccess grantable -> bucket.GrantPublicAccess(grantable) |> ignore
                | GrantPut grantable -> bucket.GrantPut(grantable) |> ignore
                | GrantPutAcl grantable -> bucket.GrantPutAcl(grantable) |> ignore
                | GrantReplicationPermission(grantable, props) ->
                    bucket.GrantReplicationPermission(grantable, props) |> ignore
                | GrantReadWrite(grantable) -> bucket.GrantReadWrite(grantable) |> ignore)

            bucketSpec.Bucket <- bucket

        | SubscriptionOp subscriptionSpec -> SNS.processSubscription stack subscriptionSpec

        | VpcOp vpcSpec ->
            let vpc = Vpc(stack, vpcSpec.ConstructId, vpcSpec.Props)
            vpcSpec.Vpc <- vpc

        | SecurityGroupOp sgSpec ->
            let sg = SecurityGroup(stack, sgSpec.ConstructId, sgSpec.Props)
            sgSpec.SecurityGroup <- sg

        | RdsInstanceOp rdsSpec -> DatabaseInstance(stack, rdsSpec.ConstructId, rdsSpec.Props) |> ignore

        | CloudFrontDistributionOp cfSpec -> Distribution(stack, cfSpec.ConstructId, cfSpec.Props) |> ignore

        | UserPoolOp upSpec ->
            let up = UserPool(stack, upSpec.ConstructId, upSpec.Props)
            upSpec.UserPool <- Some up

        | UserPoolClientOp upcSpec -> UserPoolClient(stack, upcSpec.ConstructId, upcSpec.Props) |> ignore

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
                EventBus(stack, busSpec.ConstructId, EventBusProps(EventBusName = busSpec.EventBusName))

            busSpec.EventBus <- Some bus

        | BastionHostOp bastionSpec ->
            let bastion = BastionHostLinux(stack, bastionSpec.ConstructId, bastionSpec.Props)
            bastionSpec.BastionHost <- bastion

        | KMSKeyOp keySpec ->
            let key = Amazon.CDK.AWS.KMS.Key(stack, keySpec.ConstructId, keySpec.Props)
            keySpec.Key <- key

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
            rtSpec.RouteTable <- rt

        | RouteOp routeSpec -> CfnRoute(stack, routeSpec.ConstructId, routeSpec.Props) |> ignore

        | OIDCProviderOp oidcSpec ->
            let provider = OpenIdConnectProvider(stack, oidcSpec.ConstructId, oidcSpec.Props)
            oidcSpec.Provider <- provider

        | ManagedPolicyOp policySpec ->
            let policy = ManagedPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- policy

        | CertificateOp certSpec ->
            let cert =
                Amazon.CDK.AWS.CertificateManager.Certificate(stack, certSpec.ConstructId, certSpec.Props)

            certSpec.Certificate <- cert

        | BucketPolicyOp policySpec ->
            let policy = BucketPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- policy

        | CloudWatchDashboardOp dashSpec ->
            let dashboard = Dashboard(stack, dashSpec.ConstructId, dashSpec.Props)
            dashSpec.Dashboard <- Some dashboard

        | EKSClusterOp spec ->
            let cluster = AWS.EKS.Cluster(stack, spec.ConstructId, spec.Props)

            let _ =
                spec.AddNodegroupCapacity
                |> List.map (fun (name, opts) -> cluster.AddNodegroupCapacity(name, opts))

            let _ =
                spec.AddHelmChart
                |> List.map (fun (name, opts) -> cluster.AddHelmChart(name, opts))

            let _ =
                spec.AddServiceAccount
                |> List.map (fun (name, opts) -> cluster.AddServiceAccount(name, opts))

            let _ =
                spec.AddFargateProfile
                |> List.map (fun (name, opts) -> cluster.AddFargateProfile(name, opts))

            spec.Cluster <- Some cluster

        | ECSFargateServiceOp spec ->
            let service = AWS.ECS.FargateService(stack, spec.ConstructId, spec.Props)
            spec.Service <- service

        | KinesisStreamOp spec ->
            let stream = Stream(stack, spec.ConstructId, spec.Props)
            spec.Stream <- stream

        | HostedZoneOp spec ->
            let zone = HostedZone(stack, spec.ConstructId, spec.Props)
            spec.HostedZone <- zone

        | OriginAccessIdentityOp spec ->
            let oai = OriginAccessIdentity(stack, spec.ConstructId, spec.Props)
            spec.Identity <- oai

        | CloudWatchAlarmOp alarmSpec ->
            let ala = Alarm(stack, alarmSpec.ConstructId, alarmSpec.Props)
            alarmSpec.Alarm <- ala

        //| CloudHSMClusterOp hsmSpec ->
        //    CfnCluster(stack, hsmSpec.ConstructId, hsmSpec.Props) |> ignore

        | LambdaRoleOp roleSpec ->
            // Role is already created in the builder, just store reference if needed
            // The role is available in roleSpec.Role
            ()

        | EC2InstanceOp ec2Spec ->
            let instance = Instance_(stack, ec2Spec.ConstructId, ec2Spec.Props)
            ec2Spec.Instance <- instance

        | ECSClusterOp ecsSpec ->
            let cluster = AWS.ECS.Cluster(stack, ecsSpec.ConstructId, ecsSpec.Props)
            ecsSpec.Cluster <- cluster

        | Route53RecordOp recordSpec ->
            let record = ARecord(stack, recordSpec.ConstructId, recordSpec.Props)
            recordSpec.ARecord <- record

        | ALBOp albSpec ->
            let alb = ApplicationLoadBalancer(stack, albSpec.ConstructId, albSpec.Props)
            albSpec.LoadBalancer <- alb

        | SecretsManagerOp secretsSpec ->
            let secret =
                Amazon.CDK.AWS.SecretsManager.Secret(stack, secretsSpec.ConstructId, secretsSpec.Props)

            secretsSpec.Secret <- secret

        | ElasticBeanstalkEnvironmentOp envSpec ->
            let env =
                Amazon.CDK.AWS.ElasticBeanstalk.CfnEnvironment(stack, envSpec.ConstructId, envSpec.Props)

            envSpec.Environment <- env

        | DnsValidatedCertificateOp certSpec ->
            let cert =
                Amazon.CDK.AWS.CertificateManager.Certificate(stack, certSpec.ConstructId, certSpec.Props)

            certSpec.Certificate <- cert

        | DatabaseInstanceOp dbSpec ->
            let dbInstance = DatabaseInstance(stack, dbSpec.ConstructId, dbSpec.Props)
            dbSpec.Instance <- dbInstance

// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

type StackConfig =
    { Name: string
      Scope: Construct option
      Env: IEnvironment option
      Description: string option
      Tags: Map<string, string> option
      TerminationProtection: bool option
      AnalyticsReporting: bool option
      CrossRegionReferences: bool option
      SuppressTemplateIndentation: bool option
      NotificationArns: string seq option
      PermissionsBoundary: Amazon.CDK.PermissionsBoundary option
      PropertyInjectors: IPropertyInjector list option
      Synthesizer: IStackSynthesizer option
      Operations: Operation list
      Deferred: (Stack -> StackConfig) list }

type StackBuilder(name: string) =

    member _.Yield(_: unit) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = []
          Deferred = [] }

    member _.Yield(env: IEnvironment) : StackConfig =
        { Name = name
          Scope = None
          Env = Some env
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = []
          Deferred = [] }

    member _.Yield(tableSpec: TableSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ TableOp tableSpec ]
          Deferred = [] }

    member _.Yield(tableSpec: EC2InstanceSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ EC2InstanceOp tableSpec ]
          Deferred = [] }

    member _.Yield(tableSpec: Route53ARecordSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ Route53RecordOp tableSpec ]
          Deferred = [] }

    member _.Yield(albSpec: ALBSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ ALBOp albSpec ]
          Deferred = [] }

    member _.Yield(secretsSpec: SecretsManagerSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ SecretsManagerOp secretsSpec ]
          Deferred = [] }

    member _.Yield(secretsSpec: ElasticBeanstalkEnvironmentSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ ElasticBeanstalkEnvironmentOp secretsSpec ]
          Deferred = [] }

    member _.Yield(secretsSpec: DnsValidatedCertificateSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ DnsValidatedCertificateOp secretsSpec ]
          Deferred = [] }

    member _.Yield(scope: Construct) : StackConfig =
        { Name = name
          Scope = Some scope
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = []
          Deferred = [] }

    member _.Yield(funcSpec: FunctionSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ FunctionOp funcSpec ]
          Deferred = [] }

    member _.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ DockerImageFunctionOp dockerSpec ]
          Deferred = [] }

    // member _.Yield(grantSpec: GrantSpec) : StackConfig =
    //     { Name = name
    //       Scope = None
    //       Env = None
    //       Description = None
    //       Tags = None
    //       TerminationProtection = None
    //       AnalyticsReporting = None
    //       CrossRegionReferences = None
    //       SuppressTemplateIndentation = None
    //       NotificationArns = None
    //       PermissionsBoundary = None
    //       PropertyInjectors = None
    //       Synthesizer = None
    //       Operations = [ GrantOp grantSpec ]
    //       Deferred = [] }

    member _.Yield(topicSpec: TopicSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ TopicOp topicSpec ]
          Deferred = [] }

    member _.Yield(queueSpec: QueueSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ QueueOp queueSpec ]
          Deferred = [] }

    member _.Yield(bucketSpec: BucketSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ BucketOp bucketSpec ]
          Deferred = [] }

    member _.Yield(subSpec: SubscriptionSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ SubscriptionOp subSpec ]
          Deferred = [] }

    member _.Yield(vpcSpec: VpcSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ VpcOp vpcSpec ]
          Deferred = [] }

    member _.Yield(sgSpec: SecurityGroupSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ SecurityGroupOp sgSpec ]
          Deferred = [] }

    member _.Yield(rdsSpec: DatabaseInstanceSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ RdsInstanceOp rdsSpec ]
          Deferred = [] }

    member _.Yield(cfSpec: DistributionSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ CloudFrontDistributionOp cfSpec ]
          Deferred = [] }

    member _.Yield(upSpec: UserPoolSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ UserPoolOp upSpec ]
          Deferred = [] }

    member _.Yield(upcSpec: UserPoolClientSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ UserPoolClientOp upcSpec ]
          Deferred = [] }

    member _.Yield(nlbSpec: NetworkLoadBalancerSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ NetworkLoadBalancerOp nlbSpec ]
          Deferred = [] }

    member _.Yield(ruleSpec: EventBridgeRuleSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ EventBridgeRuleOp ruleSpec ]
          Deferred = [] }

    member _.Yield(busSpec: EventBusSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ EventBusOp busSpec ]
          Deferred = [] }

    member _.Yield(bastionSpec: BastionHostSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ BastionHostOp bastionSpec ]
          Deferred = [] }

    member _.Yield(attachSpec: VPCGatewayAttachmentSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ VPCGatewayAttachmentOp attachSpec ]
          Deferred = [] }

    member _.Yield(rtSpec: RouteTableSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ RouteTableOp rtSpec ]
          Deferred = [] }

    member _.Yield(routeSpec: RouteSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ RouteOp routeSpec ]
          Deferred = [] }

    member _.Yield(oidcSpec: OIDCProviderSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ OIDCProviderOp oidcSpec ]
          Deferred = [] }

    member _.Yield(policySpec: ManagedPolicySpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ ManagedPolicyOp policySpec ]
          Deferred = [] }

    member _.Yield(certSpec: CertificateSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ CertificateOp certSpec ]
          Deferred = [] }

    member _.Yield(policySpec: BucketPolicySpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ BucketPolicyOp policySpec ]
          Deferred = [] }

    member _.Yield(keySpec: KMSKeySpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ KMSKeyOp keySpec ]
          Deferred = [] }

    member _.Yield(dashSpec: DashboardSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ CloudWatchDashboardOp dashSpec ]
          Deferred = [] }

    member _.Yield(spec: EKSClusterSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ EKSClusterOp spec ]
          Deferred = [] }

    member _.Yield(spec: KinesisStreamSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ KinesisStreamOp spec ]
          Deferred = [] }

    member _.Yield(spec: Route53HostedZoneSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ HostedZoneOp spec ]
          Deferred = [] }

    member _.Yield(spec: OriginAccessIdentitySpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ OriginAccessIdentityOp spec ]
          Deferred = [] }

    //member _.Yield(hsmSpec: CloudHSMClusterSpec) : StackConfig =
    //    { Name = name
    //      Construct = None
    //      Props = None
    //      Operations = [ CloudHSMClusterOp hsmSpec ] }

    member _.Yield(roleSpec: IAM.LambdaRoleSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ LambdaRoleOp roleSpec ]
          Deferred = [] }

    member _.Yield(alarmSpec: CloudWatchAlarmSpec) : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = [ CloudWatchAlarmOp alarmSpec ]
          Deferred = [] }

    member _.Zero() : StackConfig =
        { Name = name
          Scope = None
          Env = None
          Description = None
          Tags = None
          TerminationProtection = None
          AnalyticsReporting = None
          CrossRegionReferences = None
          SuppressTemplateIndentation = None
          NotificationArns = None
          PermissionsBoundary = None
          PropertyInjectors = None
          Synthesizer = None
          Operations = []
          Deferred = [] }

    member _.Combine(state1: StackConfig, state2: StackConfig) : StackConfig =
        { Name = state1.Name
          Scope = state1.Scope
          Env = if state2.Env.IsSome then state2.Env else state1.Env
          Description = state1.Description |> Option.orElse state2.Description
          Tags =
            match state1.Tags, state2.Tags with
            | Some tags1, Some tags2 -> Some(Map.fold (fun acc k v -> Map.add k v acc) tags1 tags2)
            | Some tags, None -> Some tags
            | None, Some tags -> Some tags
            | None, None -> None
          TerminationProtection = state1.TerminationProtection |> Option.orElse state2.TerminationProtection

          AnalyticsReporting = state1.AnalyticsReporting |> Option.orElse state2.AnalyticsReporting
          CrossRegionReferences = state1.CrossRegionReferences |> Option.orElse state2.CrossRegionReferences
          SuppressTemplateIndentation =
            state1.SuppressTemplateIndentation
            |> Option.orElse state2.SuppressTemplateIndentation
          NotificationArns =
            match state1.NotificationArns, state2.NotificationArns with
            | Some n1, Some n2 -> Some(Seq.toList n1 @ Seq.toList n2)
            | Some n, None -> Some n
            | None, Some n -> Some n
            | None, None -> None
          PermissionsBoundary =
            if state2.PermissionsBoundary.IsSome then
                state2.PermissionsBoundary
            else
                state1.PermissionsBoundary
          PropertyInjectors =
            match state1.PropertyInjectors, state2.PropertyInjectors with
            | Some p1, Some p2 -> Some(p1 @ p2)
            | Some p, None -> Some p
            | None, Some p -> Some p
            | None, None -> None
          Synthesizer = state1.Synthesizer |> Option.orElse state2.Synthesizer
          Operations = state1.Operations @ state2.Operations
          Deferred = state1.Deferred @ state2.Deferred }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackConfig) : StackConfig = f ()

    member inline x.For(config: StackConfig, [<InlineIfLambda>] f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.Bind(spec: TableSpec, cont: ITable -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Table
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind
        (
            spec: FunctionSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.Lambda.IFunction -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Function
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind
        (
            spec: DockerImageFunctionSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.Lambda.IFunction -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Function
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }


    // member this.Bind
    //     (
    //         spec: GrantSpec,
    //         [<InlineIfLambda>] cont: IGrantable -> StackConfig
    //     ) : StackConfig =
    //     let baseCfg = this.Yield(spec)
    //
    //     let deferred (_: Stack) =
    //         let fn = spec.Grantable
    //         cont fn
    //
    //     { baseCfg with
    //         Deferred = baseCfg.Deferred @ [ deferred ] }


    member inline this.Bind(spec: BucketSpec, [<InlineIfLambda>] cont: IBucket -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Bucket
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind(spec: EC2InstanceSpec, [<InlineIfLambda>] cont: Instance_ -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Instance
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind(spec: VpcSpec, [<InlineIfLambda>] cont: IVpc -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.Vpc
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind
        (
            spec: SecurityGroupSpec,
            [<InlineIfLambda>] cont: ISecurityGroup -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.SecurityGroup
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member inline this.Bind(spec: Route53ARecordSpec, [<InlineIfLambda>] cont: ARecord -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let deferred (_: Stack) =
            let fn = spec.ARecord
            cont fn

        { baseCfg with
            Deferred = baseCfg.Deferred @ [ deferred ] }

    member this.Run(config: StackConfig) =
        let props = StackProps()
        props.StackName <- config.Name
        config.Env |> Option.iter (fun v -> props.Env <- v)
        config.Description |> Option.iter (fun v -> props.Description <- v)

        config.Tags
        |> Option.iter (fun tags ->
            let tagDict = Dictionary<string, string>()
            tags |> Map.iter (fun k v -> tagDict.Add(k, v))
            props.Tags <- tagDict)

        config.TerminationProtection
        |> Option.iter (fun v -> props.TerminationProtection <- v)

        config.AnalyticsReporting
        |> Option.iter (fun v -> props.AnalyticsReporting <- v)

        config.CrossRegionReferences
        |> Option.iter (fun v -> props.CrossRegionReferences <- v)

        config.SuppressTemplateIndentation
        |> Option.iter (fun v -> props.SuppressTemplateIndentation <- v)

        config.NotificationArns
        |> Option.iter (fun v -> props.NotificationArns <- Seq.toArray v)

        config.PermissionsBoundary
        |> Option.iter (fun v -> props.PermissionsBoundary <- v)

        config.PropertyInjectors
        |> Option.iter (fun v -> props.PropertyInjectors <- Seq.toArray v)

        config.Synthesizer |> Option.iter (fun v -> props.Synthesizer <- v)

        let app = config.Scope |> Option.defaultWith (fun () -> App())
        let stack = Stack(app, name, props)

        // 1) Process the initial Operations
        for op in config.Operations do
            StackOperations.processOperation stack op

        // 2) Process deferred continuations (each may add more operations and more deferred work)
        let rec runDeferred (deferred: (Stack -> StackConfig) list) =
            for d in deferred do
                let nextCfg = d stack
                // run operations produced by this continuation
                for op in nextCfg.Operations do
                    StackOperations.processOperation stack op
                // and recurse into any nested deferred continuations it produced
                runDeferred nextCfg.Deferred

        runDeferred config.Deferred

    /// <summary>Sets the stack description.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="desc">A description of the stack.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     description "My application stack"
    /// }
    /// </code>
    [<CustomOperation("description")>]
    member _.Description(config: StackConfig, desc: string) = { config with Description = Some desc }

    /// <summary>Adds tags to the stack.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="tags">A list of key-value pairs for tagging.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     tags [ "Environment", "Production"; "Team", "DevOps" ]
    /// }
    /// </code>
    [<CustomOperation("tags")>]
    member _.Tags(config: StackConfig, tags: (string * string) seq) =
        { config with
            Tags = Some(tags |> Map.ofSeq) }

    /// <summary>Enables or disables termination protection for the stack.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="enabled">Whether termination protection is enabled.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     terminationProtection true
    /// }
    /// </code>
    [<CustomOperation("terminationProtection")>]
    member _.TerminationProtection(config: StackConfig, enabled: bool) =
        { config with
            TerminationProtection = Some enabled }

    /// <summary>Enables or disables analytics reporting.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="enabled">Whether analytics reporting is enabled.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     analyticsReporting false
    /// }
    /// </code>
    [<CustomOperation("analyticsReporting")>]
    member _.AnalyticsReporting(config: StackConfig, enabled: bool) =
        { config with
            AnalyticsReporting = Some enabled }

    /// <summary>Enables or disables cross-region references.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="enabled">Whether cross-region references are enabled.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     crossRegionReferences true
    /// }
    /// </code>
    [<CustomOperation("crossRegionReferences")>]
    member _.CrossRegionReferences(config: StackConfig, enabled: bool) =
        { config with
            CrossRegionReferences = Some enabled }

    /// <summary>Enables or disables CloudFormation template indentation suppression.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="enabled">Whether to suppress template indentation.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     suppressTemplateIndentation true
    /// }
    /// </code>
    [<CustomOperation("suppressTemplateIndentation")>]
    member _.SuppressTemplateIndentation(config: StackConfig, enabled: bool) =
        { config with
            SuppressTemplateIndentation = Some enabled }

    /// <summary>Sets SNS topic ARNs for stack notifications.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="arns">List of SNS topic ARNs.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     notificationArns [ "arn:aws:sns:us-east-1:123456789:mytopic" ]
    /// }
    /// </code>
    [<CustomOperation("notificationArns")>]
    member _.NotificationArns(config: StackConfig, arns: string seq) =
        { config with
            NotificationArns = Some arns }

    /// <summary>Sets the permissions boundary for the stack.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="boundary">The permissions boundary.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     permissionsBoundary (PermissionsBoundary.fromName "MyBoundary")
    /// }
    /// </code>
    [<CustomOperation("permissionsBoundary")>]
    member _.PermissionsBoundary(config: StackConfig, boundary: Amazon.CDK.PermissionsBoundary) =
        { config with
            PermissionsBoundary = Some boundary }

    /// <summary>Sets property injectors for the stack.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="injectors">List of property injectors.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     propertyInjectors [ myInjector ]
    /// }
    /// </code>
    [<CustomOperation("propertyInjectors")>]
    member _.PropertyInjectors(config: StackConfig, injectors: IPropertyInjector list) =
        { config with
            PropertyInjectors = Some injectors }

    /// <summary>Sets the stack synthesizer.</summary>
    /// <param name="config">The current stack configuration.</param>
    /// <param name="synthesizer">The stack synthesizer to use.</param>
    /// <code lang="fsharp">
    /// stack "MyStack" {
    ///     synthesizer (DefaultStackSynthesizer())
    /// }
    /// </code>
    [<CustomOperation("synthesizer")>]
    member _.Synthesizer(config: StackConfig, synthesizer: IStackSynthesizer) =
        { config with
            Synthesizer = Some synthesizer }

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
