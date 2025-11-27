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
open Amazon.CDK.AWS.CertificateManager
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open Amazon.CDK.AWS.Events
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.Kinesis
open Amazon.CDK.AWS.KMS
open Amazon.CDK.AWS.Route53
open Constructs
open Amazon.CDK.AWS.ElastiCache
open Amazon.CDK.CustomResources
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.StepFunctions
open Amazon.CDK.AWS.XRay
open Amazon.CDK.AWS.AppSync
open Amazon.CDK.AWS.APIGateway
open Amazon.CDK.AWS.ECS
open Amazon.CDK.AWS.CloudTrail
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
    | UserPoolResourceServerOp of UserPoolResourceServerSpec
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
    | RoleOp of RoleSpec
    | CloudWatchAlarmOp of CloudWatchAlarmSpec
    | KMSKeyOp of KMSKeySpec
    | EC2InstanceOp of EC2InstanceSpec
    | Route53RecordOp of Route53ARecordSpec
    | ALBOp of ALBSpec
    | SecretsManagerOp of SecretsManagerSpec
    | ElasticBeanstalkEnvironmentOp of ElasticBeanstalkEnvironmentSpec
    | DnsValidatedCertificateOp of DnsValidatedCertificateSpec
    | AppRunnerServiceOp of AppRunnerServiceSpec
    | ElasticCacheRedisOp of ElasticCacheRedisSpec
    | DocumentDBClusterOp of DocumentDBClusterSpec
    | CustomResourceOp of CustomResourceSpec
    | StepFunctionOp of StepFunctionSpec
    | XRayGroupOp of XRayGroupSpec
    | XRaySamplingRuleOp of XRaySamplingRuleSpec
    | AppSyncApiOp of AppSyncApiSpec
    | AppSyncDataSourceOp of AppSyncDataSourceSpec
    | RestApiOp of RestApiSpec
    | TokenAuthorizerOp of TokenAuthorizerSpec
    | VpcLinkOp of VpcLinkSpec
    | FargateTaskDefinitionOp of FargateTaskDefinitionSpec
    | ECSClusterOp of ECSClusterSpec
    | ECSFargateServiceOp of ECSFargateServiceSpec
    | GatewayVpcEndpointOp of GatewayVpcEndpointSpec
    | InterfaceVpcEndpointOp of InterfaceVpcEndpointSpec
    | DatabaseProxyOp of DatabaseProxySpec
    | CloudWatchLogGroupOp of CloudWatchLogGroupSpec
    | CloudWatchMetricFilterOp of CloudWatchMetricFilterSpec
    | CloudWatchSubscriptionFilterOp of CloudWatchSubscriptionFilterSpec
    | CloudTrailOp of CloudTrailSpec
    | AccessPointOp of AccessPointSpec
    | EfsFileSystemOp of EfsFileSystemSpec
    | PolicyOp of PolicySpec
    | UserOp of UserSpec

// ============================================================================
// Helper Functions - Process Operations in Stack
// ============================================================================

module StackOperations =
    // Process a single operation on a stack
    let processOperation (stack: Stack) (operation: Operation) : unit =
        match operation with
        | TableOp tableSpec ->
            let table = Table(stack, tableSpec.ConstructId, tableSpec.Props)

            // Add Global Secondary Indexes
            for gsi in tableSpec.GlobalSecondaryIndexes do
                let gsiProps = GlobalSecondaryIndexProps()
                gsiProps.IndexName <- gsi.IndexName
                let pkName, pkType = gsi.PartitionKey
                gsiProps.PartitionKey <- Attribute(Name = pkName, Type = pkType)

                gsi.SortKey
                |> Option.iter (fun (skName, skType) -> gsiProps.SortKey <- Attribute(Name = skName, Type = skType))

                gsi.ProjectionType |> Option.iter (fun pt -> gsiProps.ProjectionType <- pt)

                gsi.NonKeyAttributes
                |> Option.iter (fun attrs ->
                    if not (List.isEmpty attrs) then
                        gsiProps.NonKeyAttributes <- Array.ofList attrs)

                gsi.ReadCapacity |> Option.iter (fun rc -> gsiProps.ReadCapacity <- rc)

                gsi.WriteCapacity |> Option.iter (fun wc -> gsiProps.WriteCapacity <- wc)

                table.AddGlobalSecondaryIndex(gsiProps)

            // Add Local Secondary Indexes
            for lsi in tableSpec.LocalSecondaryIndexes do
                let lsiProps = LocalSecondaryIndexProps()
                lsiProps.IndexName <- lsi.IndexName
                let skName, skType = lsi.SortKey
                lsiProps.SortKey <- Attribute(Name = skName, Type = skType)

                lsi.ProjectionType |> Option.iter (fun pt -> lsiProps.ProjectionType <- pt)

                lsi.NonKeyAttributes
                |> Option.iter (fun attrs ->
                    if not (List.isEmpty attrs) then
                        lsiProps.NonKeyAttributes <- Array.ofList attrs)

                table.AddLocalSecondaryIndex(lsiProps)

            tableSpec.Table <- Some table

            tableSpec.Grant
            |> Option.iter (fun grants ->
                match grants with
                | GrantReadData fn -> table.GrantReadData(fn) |> ignore
                | GrantFullAccess grantable -> table.GrantFullAccess(grantable) |> ignore
                | GrantReadWriteData grantable -> table.GrantReadWriteData(grantable) |> ignore
                | GrantWriteData grantable -> table.GrantWriteData(grantable) |> ignore
                | GrantStreamRead grantable -> table.GrantStreamRead(grantable) |> ignore
                | GrantStream(grantable, actions) -> table.GrantStream(grantable, List.toArray actions) |> ignore
                | GrantTableListStreams grantable -> table.GrantTableListStreams(grantable) |> ignore
                | Grant(grantable, actions) -> table.Grant(grantable, List.toArray actions) |> ignore)

        | FunctionOp lambdaSpec ->
            let fn = AWS.Lambda.Function(stack, lambdaSpec.ConstructId, lambdaSpec.Props)

            lambdaSpec.FunctionUrlOptions
            |> List.iter (fun url -> fn.AddFunctionUrl(url) |> ignore)

            lambdaSpec.EventSources |> List.iter fn.AddEventSource

            lambdaSpec.EventSourceMappings
            |> List.iter (fun (id, opts) -> fn.AddEventSourceMapping(id, opts) |> ignore)

            lambdaSpec.Permissions
            |> List.iter (fun perm -> fn.AddPermission(lambdaSpec.ConstructId, perm))

            lambdaSpec.RolePolicyStatements |> List.iter fn.AddToRolePolicy

            lambdaSpec.AsyncInvokeOptions |> List.iter fn.ConfigureAsyncInvoke

            lambdaSpec.Function <- Some fn

        | DockerImageFunctionOp imageLambdaSpec ->
            DockerImageFunction(stack, imageLambdaSpec.ConstructId, imageLambdaSpec.Props)
            |> ignore

        | GrantOp grantSpec -> Grants.processGrant stack grantSpec

        | TopicOp topicSpec -> Topic(stack, topicSpec.ConstructId, topicSpec.Props) |> ignore

        | QueueOp queueSpec ->
            let queue = Queue(stack, queueSpec.ConstructId, queueSpec.Props)

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

            // Security best practice: Enable VPC Flow Logs by default
            if vpcSpec.EnableFlowLogs then
                let flowLogGroupProps = LogGroupProps()

                flowLogGroupProps.Retention <- vpcSpec.FlowLogRetention |> Option.defaultValue RetentionDays.ONE_WEEK

                let flowLogGroup =
                    LogGroup(stack, $"{vpcSpec.ConstructId}-FlowLogs", flowLogGroupProps)

                let _ =
                    FlowLog(
                        stack,
                        $"{vpcSpec.ConstructId}-FlowLog",
                        FlowLogProps(
                            ResourceType = FlowLogResourceType.FromVpc(vpc),
                            Destination = FlowLogDestination.ToCloudWatchLogs(flowLogGroup)
                        )
                    )

                () // Return unit

        | SecurityGroupOp sgSpec ->
            let sg = SecurityGroup(stack, sgSpec.ConstructId, sgSpec.Props)
            sgSpec.SecurityGroup <- Some sg

        | RdsInstanceOp rdsSpec -> DatabaseInstance(stack, rdsSpec.ConstructId, rdsSpec.Props) |> ignore

        | CloudFrontDistributionOp cfSpec ->
            Amazon.CDK.AWS.CloudFront.Distribution(stack, cfSpec.ConstructId, cfSpec.Props)
            |> ignore

        | UserPoolOp upSpec ->
            let up = UserPool(stack, upSpec.ConstructId, upSpec.Props)
            upSpec.UserPool <- Some up

        | UserPoolClientOp upcSpec -> UserPoolClient(stack, upcSpec.ConstructId, upcSpec.Props) |> ignore

        | UserPoolResourceServerOp uprsSpec ->
            let rs = CfnUserPoolResourceServer(stack, uprsSpec.ConstructId, uprsSpec.Props)
            uprsSpec.ResourceServer <- Some rs

        | NetworkLoadBalancerOp nlbSpec ->
            let nlb = NetworkLoadBalancer(stack, nlbSpec.ConstructId, nlbSpec.Props)

            if isNull nlb.Vpc then
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
            let key = Key(stack, keySpec.ConstructId, keySpec.Props)
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
            let cert = Certificate(stack, certSpec.ConstructId, certSpec.Props)

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

        | RoleOp roleSpec ->
            let role = Role(stack, roleSpec.ConstructId, roleSpec.Props)

            for statement in roleSpec.PolicyStatements do
                role.AddToPolicy statement |> ignore

            roleSpec.Role <- Some role

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
            let cert = Certificate(stack, certSpec.ConstructId, certSpec.Props)

            certSpec.Certificate <- cert
            certSpec.Certificate <- cert

        | AppRunnerServiceOp serviceSpec ->
            let service =
                Amazon.CDK.AWS.AppRunner.CfnService(stack, serviceSpec.ConstructId, serviceSpec.Props)

            serviceSpec.Service <- Some service

        | ElasticCacheRedisOp clusterSpec ->
            let cfnCacheCluster =
                CfnCacheCluster(
                    stack,
                    clusterSpec.ConstructId,
                    CfnCacheClusterProps(ClusterName = clusterSpec.ClusterName)
                )

            clusterSpec.CacheCluster <- Some cfnCacheCluster

        | DocumentDBClusterOp clusterSpec ->
            let databaseCluster =
                AWS.DocDB.DatabaseCluster(stack, clusterSpec.ConstructId, clusterSpec.Props)

            clusterSpec.Cluster <- Some databaseCluster


        | CustomResourceOp resourceSpec ->
            let customResource =
                AwsCustomResource(stack, resourceSpec.ConstructId, resourceSpec.Props)

            resourceSpec.CustomResource <- Some customResource

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

        | RestApiOp restApiSpec ->
            let api = RestApi(stack, restApiSpec.ConstructId, restApiSpec.Props)
            restApiSpec.RestApi <- Some api

        | TokenAuthorizerOp authorizerSpec ->
            let authorizer =
                TokenAuthorizer(stack, authorizerSpec.ConstructId, authorizerSpec.Props)

            authorizerSpec.Authorizer <- Some authorizer

        | VpcLinkOp vpcLinkSpec ->
            let vpcLink = VpcLink(stack, vpcLinkSpec.ConstructId, vpcLinkSpec.Props)
            vpcLinkSpec.VpcLink <- Some vpcLink

        | FargateTaskDefinitionOp taskDefSpec ->
            let taskDef =
                FargateTaskDefinition(stack, taskDefSpec.ConstructId, taskDefSpec.Props)

            taskDefSpec.TaskDefinition <- Some taskDef

        | ECSClusterOp clusterSpec ->
            let cluster =
                Cluster(stack, clusterSpec.ConstructId, ClusterProps(ClusterName = clusterSpec.ClusterName))

            clusterSpec.Cluster <- Some cluster

        | ECSFargateServiceOp serviceResource ->
            let service =
                FargateService(
                    stack,
                    serviceResource.ConstructId,
                    FargateServiceProps(ServiceName = serviceResource.ServiceName)
                )

            serviceResource.Service <- Some service

        | GatewayVpcEndpointOp endpointSpec ->
            let endpoint =
                GatewayVpcEndpoint(stack, endpointSpec.ConstructId, endpointSpec.Props)

            endpointSpec.VpcEndpoint <- Some endpoint

        | InterfaceVpcEndpointOp endpointSpec ->
            let endpoint =
                InterfaceVpcEndpoint(stack, endpointSpec.ConstructId, endpointSpec.Props)

            endpointSpec.VpcEndpoint <- Some endpoint

        | DatabaseProxyOp proxySpec ->
            let proxy = DatabaseProxy(stack, proxySpec.ConstructId, proxySpec.Props)
            proxySpec.DatabaseProxy <- Some proxy

        | CloudWatchLogGroupOp logGroupResource ->
            let logGroup = LogGroup(stack, logGroupResource.ConstructId, logGroupResource.Props)
            logGroupResource.LogGroup <- Some logGroup

        | CloudWatchMetricFilterOp filterResource ->
            // Resolve LogGroup from either direct reference or CloudWatchLogGroupResource
            let logGroup =
                match filterResource.LogGroupToAttach, filterResource.LogGroupResource with
                | Some lg, _ -> lg
                | None, Some lgResource ->
                    match lgResource.LogGroup with
                    | Some lg -> lg
                    | None ->
                        failwith
                            $"LogGroup '{lgResource.LogGroupName}' must be yielded in the stack before the metric filter '{filterResource.FilterName}'"
                | None, None -> failwith $"LogGroup is required for metric filter '{filterResource.FilterName}'"

            let metricFilter =
                logGroup.AddMetricFilter(filterResource.ConstructId, filterResource.FilterOptions)

            filterResource.MetricFilter <- Some metricFilter

        | CloudWatchSubscriptionFilterOp subscriptionResource ->
            // Resolve LogGroup from either direct reference or CloudWatchLogGroupResource
            let logGroup =
                match subscriptionResource.LogGroupToAttach, subscriptionResource.LogGroupResource with
                | Some lg, _ -> lg
                | None, Some lgResource ->
                    match lgResource.LogGroup with
                    | Some lg -> lg
                    | None ->
                        failwith
                            $"LogGroup '{lgResource.LogGroupName}' must be yielded in the stack before the subscription filter '{subscriptionResource.FilterName}'"
                | None, None ->
                    failwith $"LogGroup is required for subscription filter '{subscriptionResource.FilterName}'"

            subscriptionResource.Props.LogGroup <- logGroup

            let subscriptionFilter =
                SubscriptionFilter(stack, subscriptionResource.ConstructId, subscriptionResource.Props)

            subscriptionResource.SubscriptionFilter <- Some subscriptionFilter

        | CloudTrailOp trailSpec ->
            // Create a CloudWatch Log Group if CloudWatch logging is enabled
            let trail =
                if trailSpec.SendToCloudWatchLogs then
                    let logGroupProps = LogGroupProps()

                    logGroupProps.Retention <-
                        trailSpec.CloudWatchLogsRetention |> Option.defaultValue RetentionDays.ONE_MONTH

                    let logGroup = LogGroup(stack, $"{trailSpec.ConstructId}-Logs", logGroupProps)

                    trailSpec.Props.CloudWatchLogGroup <- logGroup
                    Trail(stack, trailSpec.ConstructId, trailSpec.Props)
                else
                    Trail(stack, trailSpec.ConstructId, trailSpec.Props)

            trailSpec.Trail <- Some trail

        | AccessPointOp accessPointSpec ->
            let accessPoint =
                Amazon.CDK.AWS.EFS.AccessPoint(stack, accessPointSpec.ConstructId, accessPointSpec.Props)

            accessPointSpec.AccessPoint <- Some accessPoint

        | EfsFileSystemOp fileSystemSpec ->
            let fileSystem =
                Amazon.CDK.AWS.EFS.FileSystem(stack, fileSystemSpec.ConstructId, fileSystemSpec.Props)

            fileSystemSpec.FileSystem <- Some fileSystem
        | PolicyOp policySpec ->
            let policy = Policy(stack, policySpec.ConstructId, policySpec.Props)

            policySpec.Policy <- Some policy
        | UserOp userSpec ->
            let user = User(stack, userSpec.ConstructId, userSpec.Props)
            userSpec.User <- Some user


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
      NotificationArns: string list option
      PermissionsBoundary: Amazon.CDK.PermissionsBoundary option
      PropertyInjectors: IPropertyInjector list option
      Synthesizer: IStackSynthesizer option
      Operations: (Stack -> unit) list }

type StackBuilder(name: string) =

    // Helper to convert Operation to function
    let opToFunc op =
        fun stack -> StackOperations.processOperation stack op

    // Public helper to initialize a StackConfig from an Operation
    member this.Init(op: Operation) : StackConfig =
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
          Operations = [ opToFunc op ] }

    // Public helper used by per-type Bind overloads to reduce boilerplate
    member inline this.BindViaYield
        ([<InlineIfLambda>] toOp: 'Spec -> Operation)
        ([<InlineIfLambda>] tryGet: 'Spec -> 'Res option)
        (kind: string)
        ([<InlineIfLambda>] nameOf: 'Spec -> string)
        (spec: 'Spec)
        ([<InlineIfLambda>] cont: 'Res -> StackConfig)
        : StackConfig =

        let baseCfg = this.Init(toOp spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match tryGet spec with
                | Some res ->
                    let contCfg = cont res

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"{kind} '{nameOf spec}' was not created. Make sure to create the {kind} first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

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
          Operations = [] }

    member this.Yield(tableSpec: TableSpec) : StackConfig = this.Init(TableOp tableSpec)

    member this.Yield(tableSpec: EC2InstanceSpec) : StackConfig = this.Init(EC2InstanceOp tableSpec)

    member this.Yield(tableSpec: Route53ARecordSpec) : StackConfig = this.Init(Route53RecordOp tableSpec)


    member this.Yield(albSpec: ALBSpec) : StackConfig = this.Init(ALBOp albSpec)

    member this.Yield(secretsSpec: SecretsManagerSpec) : StackConfig = this.Init(SecretsManagerOp secretsSpec)

    member this.Yield(secretsSpec: ElasticBeanstalkEnvironmentSpec) : StackConfig =
        this.Init(ElasticBeanstalkEnvironmentOp secretsSpec)

    member this.Yield(secretsSpec: DnsValidatedCertificateSpec) : StackConfig =
        this.Init(DnsValidatedCertificateOp secretsSpec)

    member this.Yield(funcSpec: FunctionSpec) : StackConfig = this.Init(FunctionOp funcSpec)

    member this.Yield(dockerSpec: DockerImageFunctionSpec) : StackConfig =
        this.Init(DockerImageFunctionOp dockerSpec)

    member this.Yield(grantSpec: GrantSpec) : StackConfig = this.Init(GrantOp grantSpec)

    member this.Yield(grantSpec: PolicySpec) : StackConfig = this.Init(PolicyOp grantSpec)

    member this.Yield(grantSpec: UserSpec) : StackConfig = this.Init(UserOp grantSpec)

    member this.Yield(grantSpec: AccessPointSpec) : StackConfig = this.Init(AccessPointOp grantSpec)

    member this.Yield(topicSpec: TopicSpec) : StackConfig = this.Init(TopicOp topicSpec)

    member this.Yield(queueSpec: QueueSpec) : StackConfig = this.Init(QueueOp queueSpec)

    member this.Yield(bucketSpec: BucketSpec) : StackConfig = this.Init(BucketOp bucketSpec)

    member this.Yield(subSpec: SubscriptionSpec) : StackConfig = this.Init(SubscriptionOp subSpec)

    member this.Yield(vpcSpec: VpcSpec) : StackConfig = this.Init(VpcOp vpcSpec)

    member this.Yield(sgSpec: SecurityGroupSpec) : StackConfig = this.Init(SecurityGroupOp sgSpec)

    member this.Yield(rdsSpec: DatabaseInstanceSpec) : StackConfig = this.Init(RdsInstanceOp rdsSpec)

    member this.Yield(cfSpec: DistributionSpec) : StackConfig =
        this.Init(CloudFrontDistributionOp cfSpec)

    member this.Yield(upSpec: UserPoolSpec) : StackConfig = this.Init(UserPoolOp upSpec)

    member this.Yield(upcSpec: UserPoolClientSpec) : StackConfig = this.Init(UserPoolClientOp upcSpec)

    member this.Yield(uprsSpec: UserPoolResourceServerSpec) : StackConfig =
        this.Init(UserPoolResourceServerOp uprsSpec)

    member this.Yield(nlbSpec: NetworkLoadBalancerSpec) : StackConfig =
        this.Init(NetworkLoadBalancerOp nlbSpec)

    member this.Yield(ruleSpec: EventBridgeRuleSpec) : StackConfig = this.Init(EventBridgeRuleOp ruleSpec)

    member this.Yield(busSpec: EventBusSpec) : StackConfig = this.Init(EventBusOp busSpec)

    member this.Yield(bastionSpec: BastionHostSpec) : StackConfig = this.Init(BastionHostOp bastionSpec)

    member this.Yield(attachSpec: VPCGatewayAttachmentSpec) : StackConfig =
        this.Init(VPCGatewayAttachmentOp attachSpec)

    member this.Yield(rtSpec: RouteTableSpec) : StackConfig = this.Init(RouteTableOp rtSpec)

    member this.Yield(routeSpec: RouteSpec) : StackConfig = this.Init(RouteOp routeSpec)

    member this.Yield(oidcSpec: OIDCProviderSpec) : StackConfig = this.Init(OIDCProviderOp oidcSpec)

    member this.Yield(policySpec: ManagedPolicySpec) : StackConfig = this.Init(ManagedPolicyOp policySpec)

    member this.Yield(certSpec: CertificateSpec) : StackConfig = this.Init(CertificateOp certSpec)

    member this.Yield(policySpec: BucketPolicySpec) : StackConfig = this.Init(BucketPolicyOp policySpec)

    member this.Yield(keySpec: KMSKeySpec) : StackConfig = this.Init(KMSKeyOp keySpec)

    member this.Yield(dashSpec: DashboardSpec) : StackConfig =
        this.Init(CloudWatchDashboardOp dashSpec)

    member this.Yield(spec: EKSClusterSpec) : StackConfig = this.Init(EKSClusterOp spec)

    member this.Yield(spec: KinesisStreamSpec) : StackConfig = this.Init(KinesisStreamOp spec)

    member this.Yield(spec: Route53HostedZoneSpec) : StackConfig = this.Init(HostedZoneOp spec)

    member this.Yield(spec: OriginAccessIdentitySpec) : StackConfig = this.Init(OriginAccessIdentityOp spec)

    //member this.Yield(hsmSpec: CloudHSMClusterSpec) : StackConfig =
    //    { Name = name
    //      Construct = None
    //      Props = None
    //      Operations = [ opToFunc (CloudHSMClusterOp hsmSpec) ] }

    member this.Yield(roleSpec: RoleSpec) : StackConfig = this.Init(RoleOp roleSpec)

    member this.Yield(alarmSpec: CloudWatchAlarmSpec) : StackConfig = this.Init(CloudWatchAlarmOp alarmSpec)

    member this.Yield(customResourceResource: CustomResourceSpec) : StackConfig =
        this.Init(CustomResourceOp customResourceResource)

    member this.Yield(sfResource: StepFunctionSpec) : StackConfig = this.Init(StepFunctionOp sfResource)

    member this.Yield(xrayGroupResource: XRayGroupSpec) : StackConfig =
        this.Init(XRayGroupOp xrayGroupResource)

    member this.Yield(xraySamplingRuleResource: XRaySamplingRuleSpec) : StackConfig =
        this.Init(XRaySamplingRuleOp xraySamplingRuleResource)

    member this.Yield(appSyncApiResource: AppSyncApiSpec) : StackConfig =
        this.Init(AppSyncApiOp appSyncApiResource)

    member this.Yield(appSyncDataSourceResource: AppSyncDataSourceSpec) : StackConfig =
        this.Init(AppSyncDataSourceOp appSyncDataSourceResource)

    member this.Yield(restApiSpec: RestApiSpec) : StackConfig = this.Init(RestApiOp restApiSpec)

    member this.Yield(authorizerSpec: TokenAuthorizerSpec) : StackConfig =
        this.Init(TokenAuthorizerOp authorizerSpec)

    member this.Yield(vpcLinkSpec: VpcLinkSpec) : StackConfig = this.Init(VpcLinkOp vpcLinkSpec)

    member this.Yield(taskDefSpec: FargateTaskDefinitionSpec) : StackConfig =
        this.Init(FargateTaskDefinitionOp taskDefSpec)

    member this.Yield(clusterResource: ECSClusterSpec) : StackConfig = this.Init(ECSClusterOp clusterResource)

    member this.Yield(serviceResource: ECSFargateServiceSpec) : StackConfig =
        this.Init(ECSFargateServiceOp serviceResource)

    member this.Yield(endpointSpec: GatewayVpcEndpointSpec) : StackConfig =
        this.Init(GatewayVpcEndpointOp endpointSpec)

    member this.Yield(endpointSpec: InterfaceVpcEndpointSpec) : StackConfig =
        this.Init(InterfaceVpcEndpointOp endpointSpec)

    member this.Yield(proxySpec: DatabaseProxySpec) : StackConfig = this.Init(DatabaseProxyOp proxySpec)

    member this.Yield(logGroupResource: CloudWatchLogGroupSpec) : StackConfig =
        this.Init(CloudWatchLogGroupOp logGroupResource)

    member this.Yield(filterResource: CloudWatchMetricFilterSpec) : StackConfig =
        this.Init(CloudWatchMetricFilterOp filterResource)

    member this.Yield(subscriptionResource: CloudWatchSubscriptionFilterSpec) : StackConfig =
        this.Init(CloudWatchSubscriptionFilterOp subscriptionResource)

    member inline this.Bind(spec: VpcSpec, [<InlineIfLambda>] cont: IVpc -> StackConfig) : StackConfig =
        this.BindViaYield VpcOp (fun s -> s.Vpc) "VPC" (fun s -> s.VpcName) spec cont

    member inline this.Bind(spec: TableSpec, [<InlineIfLambda>] cont: ITable -> StackConfig) : StackConfig =
        this.BindViaYield TableOp (fun s -> s.Table) "Table" (fun s -> s.TableName) spec cont

    member inline this.Bind
        (
            spec: SecurityGroupSpec,
            [<InlineIfLambda>] cont: ISecurityGroup -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            SecurityGroupOp
            (fun s -> s.SecurityGroup)
            "SecurityGroup"
            (fun s -> s.SecurityGroupName)
            spec
            cont

    member inline this.Bind(spec: BucketSpec, [<InlineIfLambda>] cont: IBucket -> StackConfig) : StackConfig =
        this.BindViaYield BucketOp (fun s -> s.Bucket) "Bucket" (fun s -> s.BucketName) spec cont

    member inline this.Bind(spec: KinesisStreamSpec, [<InlineIfLambda>] cont: IStream -> StackConfig) : StackConfig =
        this.BindViaYield KinesisStreamOp (fun s -> s.Stream) "Kinesis Stream" (fun s -> s.StreamName) spec cont

    member inline this.Bind
        (
            spec: SecretsManagerSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.SecretsManager.ISecret -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            SecretsManagerOp
            (fun s -> if isNull (box s.Secret) then None else Some(s.Secret))
            "Secret"
            (fun s -> s.SecretName)
            spec
            cont

    member inline this.Bind
        (
            spec: ManagedPolicySpec,
            [<InlineIfLambda>] cont: IManagedPolicy -> StackConfig
        ) : StackConfig =
        this.BindViaYield ManagedPolicyOp (fun s -> s.Policy) "ManagedPolicy" (fun s -> s.PolicyName) spec cont

    member inline this.Bind(spec: CertificateSpec, [<InlineIfLambda>] cont: ICertificate -> StackConfig) : StackConfig =
        this.BindViaYield CertificateOp (fun s -> s.Certificate) "Certificate" (fun s -> s.CertificateName) spec cont

    member inline this.Bind(spec: EventBridgeRuleSpec, [<InlineIfLambda>] cont: IRule -> StackConfig) : StackConfig =
        this.BindViaYield EventBridgeRuleOp (fun s -> s.Rule) "EventBridge Rule" (fun s -> s.RuleName) spec cont

    member inline this.Bind(spec: CloudWatchAlarmSpec, [<InlineIfLambda>] cont: IAlarm -> StackConfig) : StackConfig =
        this.BindViaYield
            CloudWatchAlarmOp
            (fun s -> s.Alarm |> Option.map (fun a -> a))
            "CloudWatch Alarm"
            (fun s -> s.AlarmName)
            spec
            cont

    member inline this.Bind
        (
            spec: FunctionSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.Lambda.IFunction -> StackConfig
        ) : StackConfig =
        this.BindViaYield FunctionOp (fun s -> s.Function) "Lambda Function" (fun s -> s.FunctionName) spec cont

    member inline this.Bind(spec: EC2InstanceSpec, [<InlineIfLambda>] cont: Instance_ -> StackConfig) : StackConfig =
        this.BindViaYield
            EC2InstanceOp
            (fun s -> if isNull (box s.Instance) then None else Some s.Instance)
            "EC2 Instance"
            (fun s -> s.InstanceName)
            spec
            cont

    member inline this.Bind
        (
            spec: BastionHostSpec,
            [<InlineIfLambda>] cont: BastionHostLinux -> StackConfig
        ) : StackConfig =
        this.BindViaYield BastionHostOp (fun s -> s.BastionHost) "BastionHost" (fun s -> s.BastionName) spec cont

    member inline this.Bind
        (
            spec: TokenAuthorizerSpec,
            [<InlineIfLambda>] cont: IAuthorizer -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            TokenAuthorizerOp
            (fun s -> s.Authorizer)
            "TokenAuthorizer"
            (fun s -> s.AuthorizerName)
            spec
            cont

    member inline this.Bind(spec: VpcLinkSpec, [<InlineIfLambda>] cont: IVpcLink -> StackConfig) : StackConfig =
        this.BindViaYield VpcLinkOp (fun s -> s.VpcLink) "VpcLink" (fun s -> s.VpcLinkName) spec cont

    member inline this.Bind
        (
            spec: FargateTaskDefinitionSpec,
            [<InlineIfLambda>] cont: FargateTaskDefinition -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            FargateTaskDefinitionOp
            (fun s -> s.TaskDefinition)
            "FargateTaskDefinition"
            (fun s -> s.TaskDefinitionName)
            spec
            cont

    member inline this.Bind
        (
            spec: StepFunctionSpec,
            [<InlineIfLambda>] cont: StateMachine -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            StepFunctionOp
            (fun s -> s.StateMachine)
            "StateMachine"
            (fun s -> s.StateMachineName)
            spec
            cont

    member inline this.Bind(spec: QueueSpec, [<InlineIfLambda>] cont: IQueue -> StackConfig) : StackConfig =
        this.BindViaYield QueueOp (fun s -> s.Queue) "Queue" (fun s -> s.QueueName) spec cont

    member inline this.Bind(spec: KMSKeySpec, [<InlineIfLambda>] cont: IKey -> StackConfig) : StackConfig =
        this.BindViaYield KMSKeyOp (fun s -> s.Key) "KMS Key" (fun s -> s.KeyName) spec cont

    member inline this.Bind
        (
            spec: AccessPointSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EFS.IAccessPoint -> StackConfig
        ) : StackConfig =
        this.BindViaYield AccessPointOp (fun s -> s.AccessPoint) "Access Point" (fun s -> s.ConstructId) spec cont

    member inline this.Bind
        (
            spec: EfsFileSystemSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EFS.IFileSystem -> StackConfig
        ) : StackConfig =
        this.BindViaYield EfsFileSystemOp (fun s -> s.FileSystem) "EFS File System" (fun s -> s.ConstructId) spec cont

    member inline this.Bind
        (
            spec: CloudWatchLogGroupSpec,
            [<InlineIfLambda>] cont: ILogGroup -> StackConfig
        ) : StackConfig =
        this.BindViaYield CloudWatchLogGroupOp (fun s -> s.LogGroup) "LogGroup" (fun s -> s.LogGroupName) spec cont

    member inline this.Bind(spec: RoleSpec, [<InlineIfLambda>] cont: IRole -> StackConfig) : StackConfig =
        this.BindViaYield RoleOp (fun s -> s.Role) "Role" (fun s -> s.RoleName) spec cont

    member inline this.Bind(spec: RestApiSpec, [<InlineIfLambda>] cont: IRestApi -> StackConfig) : StackConfig =
        this.BindViaYield RestApiOp (fun s -> s.RestApi) "RestApi" (fun s -> s.ApiName) spec cont

    member inline this.Bind
        (
            spec: Route53HostedZoneSpec,
            [<InlineIfLambda>] cont: IHostedZone -> StackConfig
        ) : StackConfig =
        this.BindViaYield HostedZoneOp (fun s -> s.HostedZone) "HostedZone" (fun s -> s.ZoneName) spec cont

    member inline this.Bind
        (
            spec: NetworkLoadBalancerSpec,
            [<InlineIfLambda>] cont: INetworkLoadBalancer -> StackConfig
        ) : StackConfig =
        this.BindViaYield
            NetworkLoadBalancerOp
            (fun s -> s.LoadBalancer)
            "NetworkLoadBalancer"
            (fun s -> s.LoadBalancerName)
            spec
            cont

    member inline this.Bind(spec: UserPoolSpec, [<InlineIfLambda>] cont: IUserPool -> StackConfig) : StackConfig =
        this.BindViaYield UserPoolOp (fun s -> s.UserPool) "UserPool" (fun s -> s.UserPoolName) spec cont

    member inline this.Bind
        (
            spec: EKSClusterSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EKS.ICluster -> StackConfig
        ) : StackConfig =
        this.BindViaYield EKSClusterOp (fun s -> s.Cluster) "EKS Cluster" (fun s -> s.ClusterName) spec cont

    member inline this.Bind(spec: PolicySpec, [<InlineIfLambda>] cont: IPolicy -> StackConfig) : StackConfig =
        this.BindViaYield PolicyOp (fun s -> s.Policy) "Policy" (fun s -> s.PolicyName) spec cont

    member inline this.Bind(spec: UserSpec, [<InlineIfLambda>] cont: IUser -> StackConfig) : StackConfig =
        this.BindViaYield UserOp (fun s -> s.User) "User" (fun s -> s.ConstructId) spec cont

    member this.Yield(trailSpec: CloudTrailSpec) : StackConfig = this.Init(CloudTrailOp trailSpec)

    member this.Yield(trailSpec: EfsFileSystemSpec) : StackConfig = this.Init(EfsFileSystemOp trailSpec)

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
          Operations = [] }

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
          Operations = state1.Operations @ state2.Operations }

    member inline _.Delay([<InlineIfLambda>] f: unit -> StackConfig) : StackConfig = f ()

    member inline x.For(config: StackConfig, [<InlineIfLambda>] f: unit -> StackConfig) : StackConfig =
        let newConfig = f ()
        x.Combine(config, newConfig)

    member this.For(sequence: list<'T>, body: 'T -> StackConfig) =
        let mutable state = this.Zero()

        for item in sequence do
            state <- this.Combine(state, body item)

        state

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

        let app = config.Scope |> Option.defaultWith (fun () -> App(AppProps()))
        let stack = Stack(app, config.Name, props)

        // Execute all operations in order
        for op in config.Operations do
            op stack

    /// Run delayed config
    member this.Run(f: unit -> StackConfig) = this.Run(f ())

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
    member _.Tags(config: StackConfig, tags: (string * string) list) =
        { config with
            Tags = Some(tags |> Map.ofSeq) }

    [<CustomOperation("scope")>]
    member _.Scope(config: StackConfig, scope: Construct) = { config with Scope = Some scope }

    [<CustomOperation("env")>]
    member _.Env(config: StackConfig, env: IEnvironment) = { config with Env = Some env }

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
    member _.NotificationArns(config: StackConfig, arns: string list) =
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
