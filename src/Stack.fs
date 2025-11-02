namespace FsCDK

open System.Collections.Generic
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.Events
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.Route53
open Constructs
open Amazon.CDK.AWS.AppRunner
open Amazon.CDK.AWS.ElastiCache
open Amazon.CDK.AWS.DocDB
open Amazon.CDK.CustomResources
open Amazon.CDK.AWS.StepFunctions
open Amazon.CDK.AWS.XRay
open Amazon.CDK.AWS.AppSync
//open Amazon.CDK.AWS.CloudHSMV2

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
    | Route53RecordOp of Route53ARecordSpec
    | ALBOp of ALBSpec
    | SecretsManagerOp of SecretsManagerSpec
    | ElasticBeanstalkEnvironmentOp of ElasticBeanstalkEnvironmentSpec
    | DnsValidatedCertificateOp of DnsValidatedCertificateSpec
    | AppRunnerServiceOp of AppRunnerServiceResource
    | ElastiCacheRedisOp of ElastiCacheRedisResource
    | DocumentDBClusterOp of DocumentDBClusterResource
    | CustomResourceOp of CustomResourceResource
    | StepFunctionOp of StepFunctionResource
    | XRayGroupOp of XRayGroupResource
    | XRaySamplingRuleOp of XRaySamplingRuleResource
    | AppSyncApiOp of AppSyncApiResource
    | AppSyncDataSourceOp of AppSyncDataSourceResource

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
            // Yan Cui Production Best Practice: Auto-create DLQ if enabled
            // Check if DeadLetterQueueEnabled is set but no queue provided
            let propsToUse =
                if
                    lambdaSpec.Props.DeadLetterQueueEnabled.HasValue
                    && lambdaSpec.Props.DeadLetterQueueEnabled.Value
                    && lambdaSpec.Props.DeadLetterQueue = null
                then
                    // Auto-create SQS DLQ
                    let dlqName = $"{lambdaSpec.FunctionName}-dlq"
                    let dlqProps = QueueProps()
                    dlqProps.QueueName <- dlqName
                    dlqProps.RetentionPeriod <- Duration.Days(14.0) // Keep failed events for 14 days
                    let dlq = Queue(stack, $"{lambdaSpec.ConstructId}-DLQ", dlqProps)

                    // Update props with the DLQ
                    lambdaSpec.Props.DeadLetterQueue <- dlq
                    lambdaSpec.Props
                else
                    lambdaSpec.Props

            // Yan Cui Production Best Practice: Auto-add Powertools layer if ARN is provided
            match lambdaSpec.PowertoolsLayerArn with
            | Some arn ->
                let powertoolsLayer =
                    LayerVersion.FromLayerVersionArn(stack, $"{lambdaSpec.ConstructId}-PowertoolsLayer", arn)

                let existingLayers = if propsToUse.Layers = null then [||] else propsToUse.Layers
                propsToUse.Layers <- Array.append existingLayers [| powertoolsLayer |]
            | None -> ()

            let fn = AWS.Lambda.Function(stack, lambdaSpec.ConstructId, propsToUse)

            let _ =
                lambdaSpec.EventSources |> Seq.map (fun e -> fn.AddEventSource e) |> Seq.toList

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

            queueSpec.Encryption |> Option.iter (fun e -> props.Encryption <- e)

            queueSpec.EncryptionMasterKey |> Option.iter (fun k -> props.EncryptionMasterKey <- k)

            match queueSpec.DeadLetterQueueName, queueSpec.MaxReceiveCount with
            | Some dlqName, Some maxReceive ->
                try
                    let dlq = stack.Node.FindChild(dlqName) :?> Queue
                    let dlqSpec = DeadLetterQueue(Queue = dlq, MaxReceiveCount = maxReceive)
                    props.DeadLetterQueue <- dlqSpec
                with ex ->
                    printfn $"Warning: Could not configure DLQ for queue %s{queueSpec.QueueName}: %s{ex.Message}"
            | _ -> ()

            let queue = Queue(stack, queueSpec.ConstructId, props)
            queueSpec.Queue <- Some queue

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

        | RdsInstanceOp rdsSpec -> AWS.RDS.DatabaseInstance(stack, rdsSpec.ConstructId, rdsSpec.Props) |> ignore

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
                EventBus(stack, busSpec.ConstructId, EventBusProps(EventBusName = busSpec.EventBusName))

            busSpec.EventBus <- Some bus

        | BastionHostOp bastionSpec ->
            let bastion = BastionHostLinux(stack, bastionSpec.ConstructId, bastionSpec.Props)
            bastionSpec.BastionHost <- Some bastion

        | KMSKeyOp keySpec ->
            let key = Amazon.CDK.AWS.KMS.Key(stack, keySpec.ConstructId, keySpec.Props)
            keySpec.Key <- Some key

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
            let policy = ManagedPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- Some policy

        | CertificateOp certSpec ->
            let cert =
                Amazon.CDK.AWS.CertificateManager.Certificate(stack, certSpec.ConstructId, certSpec.Props)

            certSpec.Certificate <- Some cert

        | BucketPolicyOp policySpec ->
            let policy = BucketPolicy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- Some policy

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

        | KinesisStreamOp spec ->
            let stream = Stream(stack, spec.ConstructId, spec.Props)
            let _ = spec.GrantReads |> Seq.map stream.GrantRead |> Seq.toList
            let _ = spec.GrantWrites |> Seq.map stream.GrantWrite |> Seq.toList
            spec.Stream <- Some stream

        | HostedZoneOp spec ->
            let zone = HostedZone(stack, spec.ConstructId, spec.Props)
            spec.HostedZone <- Some zone

        | OriginAccessIdentityOp spec ->
            let oai = OriginAccessIdentity(stack, spec.ConstructId, spec.Props)
            spec.Identity <- Some oai

        | CloudWatchAlarmOp alarmSpec ->
            let ala = Alarm(stack, alarmSpec.ConstructId, alarmSpec.Props)
            alarmSpec.Alarm <- Some ala

        //| CloudHSMClusterOp hsmSpec ->
        //    CfnCluster(stack, hsmSpec.ConstructId, hsmSpec.Props) |> ignore

        | LambdaRoleOp roleSpec ->
            // Role is already created in the builder, just store reference if needed
            // The role is available in roleSpec.Role
            ()

        | EC2InstanceOp ec2Spec ->
            let instance = Instance_(stack, ec2Spec.ConstructId, ec2Spec.Props)
            ec2Spec.Instance <- instance

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
            certSpec.Certificate <- cert

        | AppRunnerServiceOp serviceSpec ->
            let props = CfnServiceProps()
            props.ServiceName <- serviceSpec.ServiceName

            serviceSpec.Config.SourceConfiguration
            |> Option.iter (fun v -> props.SourceConfiguration <- v)

            serviceSpec.Config.InstanceConfiguration
            |> Option.iter (fun v -> props.InstanceConfiguration <- v)

            serviceSpec.Config.HealthCheckConfiguration
            |> Option.iter (fun v -> props.HealthCheckConfiguration <- v)

            serviceSpec.Config.AutoScalingConfigurationArn
            |> Option.iter (fun v -> props.AutoScalingConfigurationArn <- v)

            if not serviceSpec.Config.Tags.IsEmpty then
                props.Tags <-
                    serviceSpec.Config.Tags
                    |> List.map (fun (k, v) -> CfnTag(Key = k, Value = v) :> ICfnTag)
                    |> Array.ofList

            let service = CfnService(stack, serviceSpec.ConstructId, props)
            serviceSpec.Service <- Some service

        | ElastiCacheRedisOp clusterSpec ->
            let cluster =
                CfnCacheCluster(
                    stack,
                    clusterSpec.ConstructId,
                    CfnCacheClusterProps(ClusterName = clusterSpec.ClusterName)
                )
            // Note: Full configuration handled in builder
            ()

        | DocumentDBClusterOp clusterSpec ->
            // Note: DocumentDB cluster creation requires VPC and credentials
            // Full implementation would use DatabaseCluster
            ()

        | CustomResourceOp resourceSpec ->
            // Note: Custom resource creation handled in builder
            ()

        | StepFunctionOp sfResource ->
            let sm = StateMachine(stack, sfResource.ConstructId, sfResource.Props)
            sfResource.StateMachine <- Some sm

        | XRayGroupOp xrayGroupResource ->
            let group = CfnGroup(stack, xrayGroupResource.ConstructId, xrayGroupResource.Props)
            xrayGroupResource.Group <- Some group

        | XRaySamplingRuleOp xraySamplingRuleResource ->
            let rule =
                CfnSamplingRule(stack, xraySamplingRuleResource.ConstructId, xraySamplingRuleResource.Props)

            xraySamplingRuleResource.SamplingRule <- Some rule

        | AppSyncApiOp appSyncApiResource ->
            let api =
                GraphqlApi(stack, appSyncApiResource.ConstructId, appSyncApiResource.Props)

            appSyncApiResource.GraphqlApi <- Some api

        | AppSyncDataSourceOp dsResource ->
            match dsResource.Config.Api with
            | None -> failwith "GraphQL API is required for AppSync Data Source"
            | Some api ->
                let ds: BaseDataSource =
                    match dsResource.Config.DynamoDBTable, dsResource.Config.LambdaFunction with
                    | Some table, None -> api.AddDynamoDbDataSource(dsResource.ConstructId, table) :> BaseDataSource
                    | None, Some func -> api.AddLambdaDataSource(dsResource.ConstructId, func) :> BaseDataSource
                    | Some _, Some _ ->
                        failwith "AppSync Data Source cannot have both DynamoDB table and Lambda function"
                    | None, None -> failwith "AppSync Data Source must have either DynamoDB table or Lambda function"

                dsResource.DataSource <- Some ds
// ============================================================================
// Stack and App Configuration DSL
// ============================================================================

type StackConfig =
    { Name: string
      Construct: Construct option
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
      Operations: Operation list }

type StackBuilder(name: string) =

    member _.Yield(_: unit) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [] }

    member _.Yield(env: IEnvironment) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [] }

    member _.Yield(tableSpec: TableSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ TableOp tableSpec ] }

    member _.Yield(tableSpec: EC2InstanceSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ EC2InstanceOp tableSpec ] }

    member _.Yield(tableSpec: Route53ARecordSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ Route53RecordOp tableSpec ] }


    member _.Yield(albSpec: ALBSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ ALBOp albSpec ] }

    member _.Yield(secretsSpec: SecretsManagerSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ SecretsManagerOp secretsSpec ] }

    member _.Yield(secretsSpec: ElasticBeanstalkEnvironmentSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ ElasticBeanstalkEnvironmentOp secretsSpec ] }

    member _.Yield(secretsSpec: DnsValidatedCertificateSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ DnsValidatedCertificateOp secretsSpec ] }

    member _.Yield(app: Construct) : StackConfig =
        { Name = name
          Construct = Some app
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
          Operations = [] }

    member _.Yield(funcSpec: FunctionSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ FunctionOp funcSpec ] }

    member _.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ DockerImageFunctionOp dockerSpec ] }

    member _.Yield(grantSpec: GrantSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ GrantOp grantSpec ] }

    member _.Yield(topicSpec: TopicSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ TopicOp topicSpec ] }

    member _.Yield(queueSpec: QueueSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ QueueOp queueSpec ] }

    member _.Yield(bucketSpec: BucketSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ BucketOp bucketSpec ] }

    member _.Yield(subSpec: SubscriptionSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ SubscriptionOp subSpec ] }

    member _.Yield(vpcSpec: VpcSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ VpcOp vpcSpec ] }

    member _.Yield(sgSpec: SecurityGroupSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ SecurityGroupOp sgSpec ] }

    member _.Yield(rdsSpec: DatabaseInstanceSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ RdsInstanceOp rdsSpec ] }

    member _.Yield(cfSpec: DistributionSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ CloudFrontDistributionOp cfSpec ] }

    member _.Yield(upSpec: UserPoolSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ UserPoolOp upSpec ] }

    member _.Yield(upcSpec: UserPoolClientSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ UserPoolClientOp upcSpec ] }

    member _.Yield(nlbSpec: NetworkLoadBalancerSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ NetworkLoadBalancerOp nlbSpec ] }

    member _.Yield(ruleSpec: EventBridgeRuleSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ EventBridgeRuleOp ruleSpec ] }

    member _.Yield(busSpec: EventBusSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ EventBusOp busSpec ] }

    member _.Yield(bastionSpec: BastionHostSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ BastionHostOp bastionSpec ] }

    member _.Yield(attachSpec: VPCGatewayAttachmentSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ VPCGatewayAttachmentOp attachSpec ] }

    member _.Yield(rtSpec: RouteTableSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ RouteTableOp rtSpec ] }

    member _.Yield(routeSpec: RouteSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ RouteOp routeSpec ] }

    member _.Yield(oidcSpec: OIDCProviderSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ OIDCProviderOp oidcSpec ] }

    member _.Yield(policySpec: ManagedPolicySpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ ManagedPolicyOp policySpec ] }

    member _.Yield(certSpec: CertificateSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ CertificateOp certSpec ] }

    member _.Yield(policySpec: BucketPolicySpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ BucketPolicyOp policySpec ] }

    member _.Yield(keySpec: KMSKeySpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ KMSKeyOp keySpec ] }

    member _.Yield(dashSpec: DashboardSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ CloudWatchDashboardOp dashSpec ] }

    member _.Yield(spec: EKSClusterSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ EKSClusterOp spec ] }

    member _.Yield(spec: KinesisStreamSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ KinesisStreamOp spec ] }

    member _.Yield(spec: Route53HostedZoneSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ HostedZoneOp spec ] }

    member _.Yield(spec: OriginAccessIdentitySpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ OriginAccessIdentityOp spec ] }

    //member _.Yield(hsmSpec: CloudHSMClusterSpec) : StackConfig =
    //    { Name = name
    //      Construct = None
    //      Props = None
    //      Operations = [ CloudHSMClusterOp hsmSpec ] }

    member _.Yield(roleSpec: IAM.LambdaRoleSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ LambdaRoleOp roleSpec ] }

    member _.Yield(alarmSpec: CloudWatchAlarmSpec) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ CloudWatchAlarmOp alarmSpec ] }

    member _.Yield(sfResource: StepFunctionResource) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ StepFunctionOp sfResource ] }

    member _.Yield(xrayGroupResource: XRayGroupResource) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ XRayGroupOp xrayGroupResource ] }

    member _.Yield(xraySamplingRuleResource: XRaySamplingRuleResource) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ XRaySamplingRuleOp xraySamplingRuleResource ] }

    member _.Yield(appSyncApiResource: AppSyncApiResource) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ AppSyncApiOp appSyncApiResource ] }

    member _.Yield(appSyncDataSourceResource: AppSyncDataSourceResource) : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [ AppSyncDataSourceOp appSyncDataSourceResource ] }

    member _.Zero() : StackConfig =
        { Name = name
          Construct = None
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
          Operations = [] }

    member _.Combine(state1: StackConfig, state2: StackConfig) : StackConfig =
        { Name = state1.Name
          Construct = state1.Construct
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
          Operations = state1.Operations @ state2.Operations }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackConfig) : StackConfig = f ()

    member inline x.For(config: StackConfig, [<InlineIfLambda>] f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

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

        let app = config.Construct |> Option.defaultWith (fun () -> App())
        let stack = Stack(app, name, props)

        for op in config.Operations do
            StackOperations.processOperation stack op

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
