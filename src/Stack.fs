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
            let t = Table(stack, tableSpec.ConstructId, tableSpec.Props)

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

                t.AddGlobalSecondaryIndex(gsiProps)

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

                t.AddLocalSecondaryIndex(lsiProps)

            tableSpec.Table <- Some t

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
          Operations = [ fun stack -> StackOperations.processOperation stack (TableOp tableSpec) ] }

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
          Operations = [ opToFunc (EC2InstanceOp tableSpec) ] }

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
          Operations = [ opToFunc (Route53RecordOp tableSpec) ] }


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
          Operations = [ opToFunc (ALBOp albSpec) ] }

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
          Operations = [ opToFunc (SecretsManagerOp secretsSpec) ] }

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
          Operations = [ opToFunc (ElasticBeanstalkEnvironmentOp secretsSpec) ] }

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
          Operations = [ opToFunc (DnsValidatedCertificateOp secretsSpec) ] }

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
          Operations = [ opToFunc (FunctionOp funcSpec) ] }

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
          Operations = [ opToFunc (DockerImageFunctionOp dockerSpec) ] }

    member _.Yield(grantSpec: GrantSpec) : StackConfig =
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
          Operations = [ opToFunc (GrantOp grantSpec) ] }

    member _.Yield(grantSpec: PolicySpec) : StackConfig =
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
          Operations = [ opToFunc (PolicyOp grantSpec) ] }

    member _.Yield(grantSpec: UserSpec) : StackConfig =
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
          Operations = [ opToFunc (UserOp grantSpec) ] }

    member _.Yield(grantSpec: AccessPointSpec) : StackConfig =
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
          Operations = [ opToFunc (AccessPointOp grantSpec) ] }

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
          Operations = [ opToFunc (TopicOp topicSpec) ] }

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
          Operations = [ opToFunc (QueueOp queueSpec) ] }

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
          Operations = [ opToFunc (BucketOp bucketSpec) ] }

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
          Operations = [ opToFunc (SubscriptionOp subSpec) ] }

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
          Operations = [ opToFunc (VpcOp vpcSpec) ] }

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
          Operations = [ opToFunc (SecurityGroupOp sgSpec) ] }

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
          Operations = [ opToFunc (RdsInstanceOp rdsSpec) ] }

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
          Operations = [ opToFunc (CloudFrontDistributionOp cfSpec) ] }

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
          Operations = [ opToFunc (UserPoolOp upSpec) ] }

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
          Operations = [ opToFunc (UserPoolClientOp upcSpec) ] }

    member _.Yield(uprsSpec: UserPoolResourceServerSpec) : StackConfig =
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
          Operations = [ opToFunc (UserPoolResourceServerOp uprsSpec) ] }

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
          Operations = [ opToFunc (NetworkLoadBalancerOp nlbSpec) ] }

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
          Operations = [ opToFunc (EventBridgeRuleOp ruleSpec) ] }

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
          Operations = [ opToFunc (EventBusOp busSpec) ] }

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
          Operations = [ opToFunc (BastionHostOp bastionSpec) ] }

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
          Operations = [ opToFunc (VPCGatewayAttachmentOp attachSpec) ] }

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
          Operations = [ opToFunc (RouteTableOp rtSpec) ] }

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
          Operations = [ opToFunc (RouteOp routeSpec) ] }

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
          Operations = [ opToFunc (OIDCProviderOp oidcSpec) ] }

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
          Operations = [ opToFunc (ManagedPolicyOp policySpec) ] }

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
          Operations = [ opToFunc (CertificateOp certSpec) ] }

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
          Operations = [ opToFunc (BucketPolicyOp policySpec) ] }

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
          Operations = [ opToFunc (KMSKeyOp keySpec) ] }

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
          Operations = [ opToFunc (CloudWatchDashboardOp dashSpec) ] }

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
          Operations = [ opToFunc (EKSClusterOp spec) ] }

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
          Operations = [ opToFunc (KinesisStreamOp spec) ] }

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
          Operations = [ opToFunc (HostedZoneOp spec) ] }

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
          Operations = [ opToFunc (OriginAccessIdentityOp spec) ] }

    //member _.Yield(hsmSpec: CloudHSMClusterSpec) : StackConfig =
    //    { Name = name
    //      Construct = None
    //      Props = None
    //      Operations = [ opToFunc (CloudHSMClusterOp hsmSpec) ] }

    member _.Yield(roleSpec: RoleSpec) : StackConfig =
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
          Operations = [ opToFunc (RoleOp roleSpec) ] }

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
          Operations = [ opToFunc (CloudWatchAlarmOp alarmSpec) ] }

    member _.Yield(customResourceResource: CustomResourceSpec) : StackConfig =
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
          Operations = [ opToFunc (CustomResourceOp customResourceResource) ] }

    member _.Yield(sfResource: StepFunctionSpec) : StackConfig =
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
          Operations = [ opToFunc (StepFunctionOp sfResource) ] }

    member _.Yield(xrayGroupResource: XRayGroupSpec) : StackConfig =
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
          Operations = [ opToFunc (XRayGroupOp xrayGroupResource) ] }

    member _.Yield(xraySamplingRuleResource: XRaySamplingRuleSpec) : StackConfig =
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
          Operations = [ opToFunc (XRaySamplingRuleOp xraySamplingRuleResource) ] }

    member _.Yield(appSyncApiResource: AppSyncApiSpec) : StackConfig =
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
          Operations = [ opToFunc (AppSyncApiOp appSyncApiResource) ] }

    member _.Yield(appSyncDataSourceResource: AppSyncDataSourceSpec) : StackConfig =
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
          Operations = [ opToFunc (AppSyncDataSourceOp appSyncDataSourceResource) ] }

    member _.Yield(restApiSpec: RestApiSpec) : StackConfig =
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
          Operations = [ opToFunc (RestApiOp restApiSpec) ] }

    member _.Yield(authorizerSpec: TokenAuthorizerSpec) : StackConfig =
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
          Operations = [ opToFunc (TokenAuthorizerOp authorizerSpec) ] }

    member _.Yield(vpcLinkSpec: VpcLinkSpec) : StackConfig =
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
          Operations = [ opToFunc (VpcLinkOp vpcLinkSpec) ] }

    member _.Yield(taskDefSpec: FargateTaskDefinitionSpec) : StackConfig =
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
          Operations = [ opToFunc (FargateTaskDefinitionOp taskDefSpec) ] }

    member _.Yield(clusterResource: ECSClusterSpec) : StackConfig =
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
          Operations = [ opToFunc (ECSClusterOp clusterResource) ] }

    member _.Yield(serviceResource: ECSFargateServiceSpec) : StackConfig =
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
          Operations = [ opToFunc (ECSFargateServiceOp serviceResource) ] }

    member _.Yield(endpointSpec: GatewayVpcEndpointSpec) : StackConfig =
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
          Operations = [ opToFunc (GatewayVpcEndpointOp endpointSpec) ] }

    member _.Yield(endpointSpec: InterfaceVpcEndpointSpec) : StackConfig =
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
          Operations = [ opToFunc (InterfaceVpcEndpointOp endpointSpec) ] }

    member _.Yield(proxySpec: DatabaseProxySpec) : StackConfig =
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
          Operations = [ opToFunc (DatabaseProxyOp proxySpec) ] }

    member _.Yield(logGroupResource: CloudWatchLogGroupSpec) : StackConfig =
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
          Operations = [ opToFunc (CloudWatchLogGroupOp logGroupResource) ] }

    member _.Yield(filterResource: CloudWatchMetricFilterSpec) : StackConfig =
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
          Operations = [ opToFunc (CloudWatchMetricFilterOp filterResource) ] }

    member _.Yield(subscriptionResource: CloudWatchSubscriptionFilterSpec) : StackConfig =
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
          Operations = [ opToFunc (CloudWatchSubscriptionFilterOp subscriptionResource) ] }

    member inline this.Bind(spec: VpcSpec, [<InlineIfLambda>] cont: IVpc -> StackConfig) : StackConfig =
        // Create the VPC first
        let createVpc =
            fun (stack: Stack) ->
                let vpc = Vpc(stack, spec.ConstructId, spec.Props)
                spec.Vpc <- Some vpc

        // Then execute the continuation with the created VPC
        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Vpc with
                | Some vpc ->
                    let contConfig = cont vpc
                    // Execute all operations from the continuation
                    for op in contConfig.Operations do
                        op stack
                | None -> failwith $"VPC '{spec.VpcName}' was not created. Make sure to create the VPC first."

        // Return the configuration with both operations
        let baseCfg = this.Yield(spec)

        { baseCfg with
            Operations = [ createVpc; executeContinuation ] }

    member inline this.Bind
        (
            spec: SecurityGroupSpec,
            [<InlineIfLambda>] cont: ISecurityGroup -> StackConfig
        ) : StackConfig =
        // Create the SecurityGroup first
        let createSecurityGroup =
            fun (stack: Stack) ->
                let sg = SecurityGroup(stack, spec.ConstructId, spec.Props)
                spec.SecurityGroup <- Some sg

        // Then execute the continuation with the created SecurityGroup
        let executeContinuation =
            fun (stack: Stack) ->
                match spec.SecurityGroup with
                | Some sg ->
                    let contConfig = cont sg
                    // Execute all operations from the continuation
                    for op in contConfig.Operations do
                        op stack
                | None ->
                    failwith
                        $"SecurityGroup '{spec.SecurityGroupName}' was not created. Make sure to create the SecurityGroup first."

        // Return the configuration with both operations
        let baseCfg = this.Yield(spec)

        { baseCfg with
            Operations = [ createSecurityGroup; executeContinuation ] }


    /// Bind for S3 Bucket
    member inline this.Bind(spec: BucketSpec, [<InlineIfLambda>] cont: IBucket -> StackConfig) : StackConfig =
        // Use existing Yield to enqueue Bucket creation, then run continuation
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Bucket with
                | Some bucket ->
                    let contCfg = cont (bucket :> IBucket)

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"Bucket '{spec.BucketName}' was not created. Make sure to create the Bucket first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Kinesis Stream
    member inline this.Bind(spec: KinesisStreamSpec, [<InlineIfLambda>] cont: IStream -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Stream with
                | Some stream ->
                    let contCfg = cont stream

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"Kinesis Stream '{spec.StreamName}' was not created. Make sure to create the Stream first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Secrets Manager Secret
    member inline this.Bind
        (
            spec: SecretsManagerSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.SecretsManager.ISecret -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                if isNull (box spec.Secret) then
                    failwith $"Secret '{spec.SecretName}' was not created. Make sure to create the Secret first."
                else
                    let contCfg = cont (spec.Secret :> Amazon.CDK.AWS.SecretsManager.ISecret)

                    for op in contCfg.Operations do
                        op stack

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Managed Policy
    member inline this.Bind
        (
            spec: ManagedPolicySpec,
            [<InlineIfLambda>] cont: IManagedPolicy -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Policy with
                | Some policy ->
                    let contCfg = cont policy

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"ManagedPolicy '{spec.PolicyName}' was not created. Make sure to create the Managed Policy first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for ACM Certificate
    member inline this.Bind(spec: CertificateSpec, [<InlineIfLambda>] cont: ICertificate -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Certificate with
                | Some cert ->
                    let contCfg = cont cert

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"Certificate '{spec.CertificateName}' was not created. Make sure to create the Certificate first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for EventBridge Rule
    member inline this.Bind(spec: EventBridgeRuleSpec, [<InlineIfLambda>] cont: IRule -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Rule with
                | Some rule ->
                    let contCfg = cont rule

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"EventBridge Rule '{spec.RuleName}' was not created. Make sure to create the Rule first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for CloudWatch Alarm
    member inline this.Bind(spec: CloudWatchAlarmSpec, [<InlineIfLambda>] cont: IAlarm -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Alarm with
                | Some alarm ->
                    let contCfg = cont alarm

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"CloudWatch Alarm '{spec.AlarmName}' was not created. Make sure to create the Alarm first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    member inline this.Bind
        (
            spec: FunctionSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.Lambda.IFunction -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Function with
                | Some func ->
                    let contCfg = cont func

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"Lambda Function '{spec.FunctionName}' was not created. Make sure to create the Function first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }




    /// Bind for EC2 Instance
    member inline this.Bind(spec: EC2InstanceSpec, [<InlineIfLambda>] cont: Instance_ -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                if isNull (box spec.Instance) then
                    failwith
                        $"EC2 Instance '{spec.InstanceName}' was not created. Make sure to create the Instance first."
                else
                    let contCfg = cont spec.Instance

                    for op in contCfg.Operations do
                        op stack

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Bastion Host
    member inline this.Bind
        (
            spec: BastionHostSpec,
            [<InlineIfLambda>] cont: BastionHostLinux -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.BastionHost with
                | Some bastion ->
                    let contCfg = cont bastion

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"BastionHost '{spec.BastionName}' was not created. Make sure to create the Bastion Host first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for API Gateway Token Authorizer
    member inline this.Bind
        (
            spec: TokenAuthorizerSpec,
            [<InlineIfLambda>] cont: IAuthorizer -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Authorizer with
                | Some auth ->
                    let contCfg = cont auth

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"TokenAuthorizer '{spec.AuthorizerName}' was not created. Make sure to create the Authorizer first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for API Gateway VpcLink
    member inline this.Bind(spec: VpcLinkSpec, [<InlineIfLambda>] cont: IVpcLink -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.VpcLink with
                | Some link ->
                    let contCfg = cont link

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"VpcLink '{spec.VpcLinkName}' was not created. Make sure to create the VpcLink first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for ECS Fargate Task Definition
    member inline this.Bind
        (
            spec: FargateTaskDefinitionSpec,
            [<InlineIfLambda>] cont: FargateTaskDefinition -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.TaskDefinition with
                | Some td ->
                    let contCfg = cont td

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"FargateTaskDefinition '{spec.TaskDefinitionName}' was not created. Make sure to create the Task Definition first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Step Functions State Machine
    member inline this.Bind
        (
            spec: StepFunctionSpec,
            [<InlineIfLambda>] cont: StateMachine -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.StateMachine with
                | Some sm ->
                    let contCfg = cont sm

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"StateMachine '{spec.StateMachineName}' was not created. Make sure to create the State Machine first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for SQS Queue
    member inline this.Bind(spec: QueueSpec, [<InlineIfLambda>] cont: IQueue -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Queue with
                | Some queue ->
                    let contCfg = cont queue

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"Queue '{spec.QueueName}' was not created. Make sure to create the Queue first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for KMS Key
    member inline this.Bind(spec: KMSKeySpec, [<InlineIfLambda>] cont: IKey -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Key with
                | Some key ->
                    let contCfg = cont key

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"KMS Key '{spec.KeyName}' was not created. Make sure to create the Key first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    member inline this.Bind
        (
            spec: AccessPointSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EFS.IAccessPoint -> StackConfig
        ) : StackConfig =

        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.AccessPoint with
                | Some ap ->
                    let contCfg = cont ap

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"Access Point '{spec.ConstructId}' was not created. Make sure to create the Access Point first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    member inline this.Bind
        (
            spec: EfsFileSystemSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EFS.IFileSystem -> StackConfig
        ) : StackConfig =

        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.FileSystem with
                | Some fs ->
                    let contCfg = cont fs

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"EFS File System '{spec.ConstructId}' was not created. Make sure to create the File System first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for CloudWatch Log Group
    member inline this.Bind
        (
            spec: CloudWatchLogGroupSpec,
            [<InlineIfLambda>] cont: ILogGroup -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.LogGroup with
                | Some lg ->
                    let contCfg = cont (lg :> ILogGroup)

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"LogGroup '{spec.LogGroupName}' was not created. Make sure to create the LogGroup first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for IAM Lambda Role
    member inline this.Bind(spec: RoleSpec, [<InlineIfLambda>] cont: IRole -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Role with
                | Some role ->
                    let contCfg = cont role

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"Role '{spec.RoleName}' was not created. Make sure to create the Role first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for API Gateway RestApi
    member inline this.Bind(spec: RestApiSpec, [<InlineIfLambda>] cont: IRestApi -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.RestApi with
                | Some api ->
                    let contCfg = cont api

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"RestApi '{spec.ApiName}' was not created. Make sure to create the RestApi first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Route53 Hosted Zone
    member inline this.Bind
        (
            spec: Route53HostedZoneSpec,
            [<InlineIfLambda>] cont: IHostedZone -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.HostedZone with
                | Some hz ->
                    let contCfg = cont hz

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"HostedZone '{spec.ZoneName}' was not created. Make sure to create the Hosted Zone first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Network Load Balancer
    member inline this.Bind
        (
            spec: NetworkLoadBalancerSpec,
            [<InlineIfLambda>] cont: INetworkLoadBalancer -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.LoadBalancer with
                | Some nlb ->
                    let contCfg = cont nlb

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith
                        $"NetworkLoadBalancer '{spec.LoadBalancerName}' was not created. Make sure to create the Load Balancer first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for Cognito User Pool
    member inline this.Bind(spec: UserPoolSpec, [<InlineIfLambda>] cont: IUserPool -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.UserPool with
                | Some up ->
                    let contCfg = cont up

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"UserPool '{spec.UserPoolName}' was not created. Make sure to create the User Pool first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    /// Bind for EKS Cluster
    member inline this.Bind
        (
            spec: EKSClusterSpec,
            [<InlineIfLambda>] cont: Amazon.CDK.AWS.EKS.ICluster -> StackConfig
        ) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Cluster with
                | Some cluster ->
                    let contCfg = cont cluster

                    for op in contCfg.Operations do
                        op stack
                | None ->
                    failwith $"EKS Cluster '{spec.ClusterName}' was not created. Make sure to create the Cluster first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    member inline this.Bind(spec: PolicySpec, [<InlineIfLambda>] cont: IPolicy -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.Policy with
                | Some policy ->
                    let contCfg = cont policy

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"Policy '{spec.PolicyName}' was not created. Make sure to create the Policy first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }


    member inline this.Bind(spec: UserSpec, [<InlineIfLambda>] cont: IUser -> StackConfig) : StackConfig =
        let baseCfg = this.Yield(spec)

        let executeContinuation =
            fun (stack: Stack) ->
                match spec.User with
                | Some policy ->
                    let contCfg = cont policy

                    for op in contCfg.Operations do
                        op stack
                | None -> failwith $"User '{spec.ConstructId}' was not created. Make sure to create the User first."

        { baseCfg with
            Operations = baseCfg.Operations @ [ executeContinuation ] }

    member _.Yield(trailSpec: CloudTrailSpec) : StackConfig =
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
          Operations = [ opToFunc (CloudTrailOp trailSpec) ] }

    member _.Yield(trailSpec: EfsFileSystemSpec) : StackConfig =
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
          Operations = [ opToFunc (EfsFileSystemOp trailSpec) ] }

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
