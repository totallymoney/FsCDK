(**
---
title: FsCDK
category: Getting Started
categoryindex: 1
---

FsCDK lets you describe AWS infrastructure with a small, expressive F# DSL built on top of the AWS Cloud Development Kit (CDK). If you like computation expressions, immutability, and readable diffs, you’ll feel right at home.

This page gives you a quick, human-sized tour. No buzzwords, just a couple of realistic stacks you can read end-to-end.

What you’ll see below:
- Define per-environment settings once and reuse them.
- Declare DynamoDB tables, Lambdas, queues and topics with intent, not boilerplate.
- Wire resources together (grants and subscriptions) without hunting for ARNs.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda

// 1) Environments
let devEnv =
    environment {
        account "123456789012"
        region "us-east-1"
    }

let prodEnv =
    environment {
        account "123456789012"
        region "us-east-1"
    }

// 2) A Dev stack you can actually work with
stack "Dev" {
    devEnv
    description "Developer stack for feature work"
    tags [ "service", "users"; "env", "dev" ]

    // resources
    table "users" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        removalPolicy RemovalPolicy.DESTROY
    }

    lambda "users-api-dev" {
        handler "Users::Handler::FunctionHandler"
        runtime Runtime.DOTNET_8
        code "./examples/lambdas/users"
        memory 512
        timeout 10.0
        description "CRUD over the users table"
    }

    queue "users-dlq" {
        messageRetention (7.0 * 24.0 * 3600.0) // 7 days
    }

    queue "users-queue" {
        deadLetterQueue "users-dlq" 5
        visibilityTimeout 30.0
    }

    topic "user-events" { displayName "User events" }

    subscription {
        topic "user-events"
        queue "users-queue"
    }

    grant {
        table "users"
        lambda "users-api-dev"
        readWriteAccess
    }
}

stack "Prod" {
    prodEnv
    terminationProtection true
    tags [ "service", "users"; "env", "prod" ]

    table "users" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        removalPolicy RemovalPolicy.RETAIN
        pointInTimeRecovery true
    }

    lambda "users-api" {
        handler "Users::Handler::FunctionHandler"
        runtime Runtime.DOTNET_8
        code "./examples/lambdas/users"
        memory 1024
        timeout 15.0
        description "CRUD over the users table"
    }

    grant {
        table "users"
        lambda "users-api"
        readWriteAccess
    }
}

(**

## Why FsCDK?

![Why FsCDK](img/Why-FsCDK.png)

### FsCDK Architecture Overview

<div class="mermaid">
graph TB
    subgraph "Your F# Code"
        A[FsCDK Builders<br/>Computation Expressions]
    end
    
    subgraph "AWS CDK"
        B[AWS CDK Library<br/>Amazon.CDK.Lib]
    end
    
    subgraph "CloudFormation"
        C[CloudFormation Template<br/>JSON/YAML]
    end
    
    subgraph "AWS Cloud"
        D[Lambda Functions]
        E[DynamoDB Tables]
        F[S3 Buckets]
        G[API Gateway]
        H[Other AWS Resources]
    end
    
    A -->|Type-safe F# DSL| B
    B -->|Synthesize| C
    C -->|Deploy| D
    C -->|Deploy| E
    C -->|Deploy| F
    C -->|Deploy| G
    C -->|Deploy| H
    
    style A fill:#e1f5ff
    style B fill:#fff4e6
    style C fill:#f3e5f5
    style D fill:#e8f5e9
    style E fill:#e8f5e9
    style F fill:#e8f5e9
    style G fill:#e8f5e9
    style H fill:#e8f5e9
</div>

**Production-Safe Defaults**

- Implements [Yan Cui's serverless best practices](https://theburningmonk.com/) by default
- Auto-creates Dead Letter Queues (DLQs) for Lambda functions
- Enables X-Ray tracing and structured JSON logging
- Sets conservative concurrency limits to prevent runaway costs

**Security by Default**

- S3 buckets block public access and enforce SSL/TLS
- Lambda environment variables encrypted with KMS
- Security groups deny all outbound traffic by default (opt-in model)

**F# Developer Experience**

- Type-safe computation expressions for all AWS resources
- Immutable configuration with compile-time checks
- IntelliSense support for discovering available options
- Readable diffs in version control

## List of builders and their operations

(Most of them, this might not be complete)

<style>
table {
  border-collapse: collapse;
  width: 100%;
}
table th,
table td {
  border: 1px solid #ddd;
  padding: 8px;
  text-align: left;
  vertical-align: top;
}
table th {
  background-color: #f2f2f2;
  font-weight: bold;
}
</style>

| AWS Resource | Builder Name(s) | Parameters |
|-------------|------------------|---------------------------|
| **ALB** | `applicationLoadBalancer` | `constructId`, `deletionProtection`, `dropInvalidHeaderFields`, `http2Enabled`, `internetFacing`, `securityGroup`, `vpc`, `vpcSubnets` |
| **App Runner Service** | `appRunnerService` | `accessRole`, `autoScalingConfigurationArn`, `constructId`, `healthCheckConfiguration`, `instanceConfiguration`, `instanceRole`, `sourceConfiguration`, `tag`, `tags` |
| **AppSync GraphQL API** | `appSyncGraphqlApi` | `authorizationConfig`, `constructId`, `domainName`, `logConfig`, `name`, `schema`, `xrayEnabled` |
| **Bastion Host** | `bastionHost` | `constructId`, `instanceName`, `instanceType`, `machineImage`, `requireImdsv2`, `securityGroup`, `subnetSelection`, `vpc` |
| **Bucket** | `bucket`, `s3Bucket` | `autoDeleteObjects`, `blockPublicAccess`, `constructId`, `encryption`, `encryptionKey`, `enforceSSL`, `removalPolicy`, `serverAccessLogsBucket`, `serverAccessLogsPrefix`, `versioned`, `websiteErrorDocument`, `websiteIndexDocument` |
| **CDK App** | `app` | `context`, `stackTraces`, `synthesizer` |
| **CloudWatch Alarm** | `cloudwatchAlarm` | `comparisonOperator`, `constructId`, `description`, `dimensions`, `evaluationPeriods`, `metric`, `metricName`, `metricNamespace`, `period`, `statistic`, `threshold`, `treatMissingData` |
| **CloudWatch Log Group** | `logGroup` | `constructId`, `encryptionKey`, `logGroupClass`, `removalPolicy`, `retention` |
| **CloudWatch Metric Filter** | `metricFilter` | `constructId`, `defaultValue`, `filterPattern`, `logGroup`, `metricName`, `metricNamespace`, `metricValue`, `unit` |
| **CloudWatch Subscription Filter** | `subscriptionFilter` | `constructId`, `destination`, `filterPattern`, `logGroup` |
| **CloudHSM Cluster** | `cloudHSMCluster` | `constructId`, `hsmType`, `subnetIds`, `vpc` |
| **CloudTrail** | `cloudTrail` | `cloudWatchLogsRetention`, `constructId`, `enableFileValidation`, `includeGlobalServiceEvents`, `isMultiRegionTrail`, `isOrganizationTrail`, `managementEvents`, `s3Bucket`, `sendToCloudWatchLogs` |
| **Cors Rule** | `corsRule` | `allowedHeaders`, `allowedMethods`, `allowedOrigins`, `exposedHeaders`, `id`, `maxAgeSeconds` |
| **Custom Resource** | `customResource` | `constructId`, `installLatestAwsSdk`, `logRetention`, `onCreate`, `onDelete`, `onUpdate`, `policy`, `timeout` |
| **Database Instance** | `rdsInstance` | `allocatedStorage`, `autoMinorVersionUpgrade`, `backupRetentionDays`, `constructId`, `credentials`, `databaseName`, `deleteAutomatedBackups`, `deletionProtection`, `enablePerformanceInsights`, `engine`, `iamAuthentication`, `instanceType`, `masterUsername`, `monitoringInterval`, `multiAz`, `performanceInsightRetention`, `postgresEngine`, `preferredBackupWindow`, `preferredMaintenanceWindow`, `publiclyAccessible`, `removalPolicy`, `securityGroup`, `storageEncrypted`, `storageType`, `vpc`, `vpcSubnets` |
| **Distribution** | `cloudFrontDistribution` | `additionalBehavior`, `additionalHttpBehavior`, `additionalS3Behavior`, `certificate`, `comment`, `constructId`, `defaultBehavior`, `defaultRootObject`, `domainName`, `enableIpv6`, `enableLogging`, `enabled`, `httpDefaultBehavior`, `httpVersion`, `minimumProtocolVersion`, `priceClass`, `s3DefaultBehavior`, `webAclId` |
| **DocumentDB Cluster** | `documentDBCluster` | `backupRetentionDays`, `backupWindow`, `constructId`, `deletionProtection`, `instanceType`, `instances`, `maintenanceWindow`, `masterPassword`, `masterUsername`, `port`, `removalPolicy`, `securityGroup`, `storageEncrypted`, `tag`, `tags`, `vpc`, `vpcSubnets` |
| **ECR Lifecycle Rule** | (helpers) | `deleteUntaggedAfterDays`, `keepLastNImages`, `deleteTaggedAfterDays`, `standardDevLifecycleRules`, `standardProdLifecycleRules` |
| **ECR Repository** | `ecrRepository` | `constructId`, `emptyOnDelete`, `imageScanOnPush`, `imageTagMutability`, `lifecycleRule`, `removalPolicy`, `repositoryName` |
| **ECS Cluster** | `ecsCluster` | `constructId`, `containerInsights`, `enableFargateCapacityProviders`, `vpc` |
| **ECS Fargate Service** | `ecsFargateService` | `assignPublicIp`, `circuitBreaker`, `cluster`, `constructId`, `desiredCount`, `enableExecuteCommand`, `healthCheckGracePeriod`, `maxHealthyPercent`, `minHealthyPercent`, `securityGroups`, `serviceName`, `taskDefinition`, `vpcSubnets` |
| **EKS Cluster** | `eksCluster` | `addFargateProfile`, `addHelmChart`, `addNodegroupCapacity`, `addServiceAccount`, `constructId`, `coreDnsComputeType`, `defaultCapacity`, `defaultCapacityInstance`, `disableClusterLogging`, `enableAlbController`, `encryptionKey`, `endpointAccess`, `mastersRole`, `setClusterLogging`, `version`, `vpc`, `vpcSubnet` |
| **ElastiCache Redis** | `elastiCacheRedis` | `autoMinorVersionUpgrade`, `availabilityZone`, `cacheNodeType`, `constructId`, `engineVersion`, `maintenanceWindow`, `numCacheNodes`, `port`, `securityGroupId`, `securityGroupIds`, `snapshotRetentionLimit`, `snapshotWindow`, `subnetGroup`, `tag`, `tags` |
| **Elastic Beanstalk Application** | `elasticBeanstalkApplication` | `constructId`, `description` |
| **Elastic Beanstalk Environment** | `elasticBeanstalkEnvironment` | `applicationName`, `constructId`, `description`, `optionSettings`, `solutionStackName`, `tier` |
| **Event BridgeRule** | `eventBridgeRule` | `constructId`, `description`, `enabled`, `eventBus`, `eventPattern`, `ruleName`, `schedule`, `target` |
| **Event Bus** | `eventBus` | `constructId`, `customEventBusName`, `eventSourceName` |
| **Fargate Task Definition** | `fargateTaskDefinition` | `constructId`, `cpu`, `ephemeralStorageGiB`, `executionRole`, `family`, `memory`, `runtimePlatform`, `taskRole`, `volume`, `volumes` |
| **Function** | `lambda` | `architecture`, `code`, `constructId`, `deadLetterQueue`, `deadLetterQueueEnabled`, `description`, `dockerImageCode`, `environment`, `environmentEncryption`, `envVar`, `ephemeralStorageSize`, `handler`, `inlineCode`, `insightsVersion`, `layer`, `layers`, `loggingFormat`, `logGroup`, `maxEventAge`, `memory`, `reservedConcurrentExecutions`, `retryAttempts`, `role`, `runtime`, `securityGroups`, `timeout`, `tracing`, `xrayEnabled` |
| **Gateway VPC Endpoint** | `gatewayVpcEndpoint` | `constructId`, `service`, `subnets`, `vpc` |
| **Grant** | `grant` | `customAccess`, `lambda`, `readAccess`, `readWriteAccess`, `table`, `writeAccess` |
| **HTTP API (API Gateway V2)** | `httpApi` | `apiName`, `constructId`, `cors`, `createDefaultStage`, `defaultIntegration`, `description`, `disableExecuteApiEndpoint` |
| **IAM PolicyStatement** | `policyStatement` | (none - uses method chaining, not CustomOperations) |
| **Import Source** | `importSource` | `bucket`, `bucketOwner`, `compressionType`, `inputFormat`, `keyPrefix` |
| **Interface VPC Endpoint** | `interfaceVpcEndpoint` | `constructId`, `privateDnsEnabled`, `securityGroups`, `service`, `subnets`, `vpc` |
| **Kinesis Stream** | `kinesisStream` | `constructId`, `encryption`, `encryptionKey`, `grantRead`, `grantWrite`, `onDemand`, `retentionPeriod`, `shardCount`, `streamMode`, `streamName`, `unencrypted` |
| **KMS Key** | `kmsKey` | `admissionPrincipal`, `alias`, `constructId`, `description`, `disableKeyRotation`, `enableKeyRotation`, `enabled`, `keySpec`, `keyUsage`, `pendingWindow`, `policy`, `removalPolicy` |
| **LambdaRole** | `lambdaRole` | `assumeRolePrincipal`, `basicExecution`, `constructId`, `inlinePolicy`, `kmsDecrypt`, `managedPolicy`, `vpcExecution`, `xrayTracing` |
| **Origin Access Identity** | `originAccessIdentity` | `constructId`, `comment` |
| **Queue** | `queue` | `constructId`, `contentBasedDeduplication`, `deadLetterQueue`, `delaySeconds`, `fifo`, `messageRetention`, `visibilityTimeout` |
| **RDS Proxy** | `rdsProxy` | `constructId`, `debugLogging`, `iamAuth`, `idleClientTimeout`, `initQuery`, `maxConnectionsPercent`, `maxIdleConnectionsPercent`, `proxyTarget`, `requireTLS`, `secrets`, `sessionPinningFilters`, `vpc` |
| **REST API (API Gateway V1)** | `restApi` | `binaryMediaTypes`, `cloneFrom`, `constructId`, `defaultCorsPreflightOptions`, `defaultIntegration`, `defaultMethodOptions`, `deployOptions`, `description`, `disableExecuteApiEndpoint`, `endpointTypes`, `failOnWarnings`, `parameters`, `policy`, `restApiName`, `retainDeployments` |
| **Route53 ARecord** | `aRecord` | `comment`, `constructId`, `target`, `ttl`, `zone` |
| **Route53 HostedZone** | `hostedZone` | `comment`, `constructId`, `queryLogsLogGroupArn`, `vpc`, `vpcs` |
| **Route53 PrivateHostedZone** | `privateHostedZone` | `constructId`, `comment`, `vpc` |
| **Route Table** | `routeTable` | `constructId`, `tag`, `vpc` |
| **Route** | `route` | `constructId`, `destinationCidrBlock`, `destinationIpv6CidrBlock`, `gatewayId`, `natGatewayId`, `networkInterfaceId`, `routeTable`, `transitGatewayId`, `vpcPeeringConnectionId` |
| **Secrets Manager Secret** | `secretsManager` | `constructId`, `description`, `encryptionKey`, `generateSecretString`, `removalPolicy`, `replicaRegions`, `secretStringValue` |
| **Security Group** | `securityGroup` | `allowAllOutbound`, `constructId`, `description`, `disableInlineRules`, `vpc` |
| **SSM Parameter** | `ssmParameter` | `allowedPattern`, `constructId`, `description`, `stringValue`, `tier` |
| **SSM Document** | `ssmDocument` | `constructId`, `content`, `documentFormat`, `documentType`, `targetType` |
| **Stack** | `stack` | - |
| **Step Functions** | `stepFunction` | `comment`, `constructId`, `definition`, `logDestination`, `loggingLevel`, `logs`, `role`, `stateMachineName`, `stateMachineType`, `timeout`, `tracingEnabled` |
| **Subscription** | `subscription` | `email`, `filterPolicy`, `http`, `https`, `lambda`, `queue`, `sms`, `subscriptionDeadLetterQueue`, `topic` |
| **Table** | `table` | `billingMode`, `constructId`, `kinesisStream`, `partitionKey`, `pointInTimeRecovery`, `removalPolicy`, `sortKey`, `stream` |
| **Token Authorizer** | `tokenAuthorizer` | `assumeRole`, `constructId`, `handler`, `identitySource`, `resultsCacheTtl`, `validationRegex` |
| **Topic** | `topic` | `constructId`, `displayName`, `fifo`, `contentBasedDeduplication` |
| **User Pool** | `userPool` | `accountRecovery`, `autoVerify`, `constructId`, `customAttribute`, `emailSettings`, `lambdaTriggers`, `mfa`, `mfaSecondFactor`, `passwordPolicy`, `removalPolicy`, `selfSignUpEnabled`, `signInAliases`, `signInWithEmail`, `signInWithEmailAndUsername`, `smsRole`, `standardAttributes`, `userPoolName` |
| **User Pool Client** | `userPoolClient` | `authFlows`, `constructId`, `generateSecret`, `oAuth`, `preventUserExistenceErrors`, `supportedIdentityProvider`, `tokenValidities`, `userPool` |
| **Vpc** | `vpc` | `cidr`, `constructId`, `defaultInstanceTenancy`, `enableDnsHostnames`, `enableDnsSupport`, `ipAddresses`, `maxAzs`, `natGateways`, `subnet` |
| **VPC Link** | `vpcLink` | `constructId`, `description`, `targets`, `vpcLinkName` |
| **X-Ray Group** | `xrayGroup` | `constructId`, `filterExpression`, `insightsEnabled`, `tag`, `tags` |
| **X-Ray Sampling Rule** | `xraySamplingRule` | `constructId`, `fixedRate`, `host`, `httpMethod`, `priority`, `reservoirSize`, `resourceArn`, `serviceName`, `serviceType`, `tag`, `tags`, `urlPath` |



## The following AWS services are supported by FsCDK

| | Service | What it does |
|-|---------|--------------|
| [![ALB](img/icons/Arch_Elastic-Load-Balancing_48.png)](alb-secrets-route53.html) | [**ALB** (Application Load Balancer)](alb-secrets-route53.html) | Distributes incoming HTTP/HTTPS traffic across multiple targets 📚 *with curated learning resources* |
| [![API Gateway](img/icons/Arch_Amazon-API-Gateway_48.png)](api-gateway-v2.html) | [**API Gateway** (REST & HTTP API)](api-gateway-v2.html) | Creates REST and HTTP APIs to expose your backend services 📚 *with curated learning resources* |
| ![App Runner](img/icons/Arch_AWS-App-Runner_48.png) | **App Runner** | Fully managed container service for web apps and APIs |
| ![AppSync](img/icons/Arch_AWS-AppSync_48.png) | **AppSync** | Builds managed GraphQL APIs with real-time data synchronization |
| [![Bastion Host](img/icons/Res_Amazon-EC2_Instance_48.png)](bastion-host.html) | [**Bastion Host**](bastion-host.html) | Secure SSH access to instances in private subnets 📚 *with curated learning resources* |
| [![Certificate Manager](img/icons/Arch_AWS-Certificate-Manager_48.png)](certificate-manager.html) | [**Certificate Manager**](certificate-manager.html) | Manages SSL/TLS certificates for secure connections 📚 *with curated learning resources* |
| ![CloudFront](img/icons/Arch_Amazon-CloudFront_48.png) | **CloudFront** | Content delivery network (CDN) for fast global content distribution |
| ![CloudHSM](img/icons/Arch_AWS-CloudHSM_48.png) | **CloudHSM** | Hardware security modules for cryptographic key storage |
| [![CloudWatch](img/icons/Arch_Amazon-CloudWatch_48.png)](cloudwatch-dashboard.html) | [**CloudWatch**](cloudwatch-dashboard.html) | Monitors resources with alarms, log groups, metric filters, subscription filters, dashboards, and synthetic canaries |
| ![Cognito](img/icons/Arch_Amazon-Cognito_48.png) | **Cognito** | User authentication and authorization for web and mobile apps |
| ![DocumentDB](img/icons/Arch_Amazon-DocumentDB_48.png) | **DocumentDB** | MongoDB-compatible document database |
| [![DynamoDB](img/icons/Arch_Amazon-DynamoDB_48.png)](dynamodb.html) | [**DynamoDB**](dynamodb.html) | Fully managed NoSQL database for key-value and document data |
| [![EC2](img/icons/Arch_Amazon-EC2_48.png)](ec2-ecs.html) | [**EC2**](ec2-ecs.html) | Virtual servers in the cloud |
| [![ECR](img/icons/Arch_Amazon-Elastic-Container-Registry_48.png)](ecr-repository.html) | [**ECR** (Elastic Container Registry)](ecr-repository.html) | Stores and manages Docker container images |
| [![ECS](img/icons/Arch_Amazon-Elastic-Container-Service_48.png)](ec2-ecs.html) | [**ECS** (Elastic Container Service)](ec2-ecs.html) | Runs containerized applications using Docker and Fargate |
| ![EFS](img/icons/Arch_Amazon-EFS_48.png) | **EFS** (Elastic File System) | Scalable file storage for Lambda and EC2 |
| [![EKS](img/icons/Arch_Amazon-Elastic-Kubernetes-Service_48.png)](eks-kubernetes.html) | [**EKS** (Elastic Kubernetes Service)](eks-kubernetes.html) | Managed Kubernetes clusters for container orchestration |
| ![ElastiCache](img/icons/Arch_Amazon-ElastiCache_48.png) | **ElastiCache** | In-memory caching with Redis and Memcached |
| ![Elastic Beanstalk](img/icons/Arch_AWS-Elastic-Beanstalk_48.png) | **Elastic Beanstalk** | Platform-as-a-Service (PaaS) for deploying applications |
| ![Elastic IP](img/icons/Res_Amazon-EC2_Elastic-IP-Address_48.png) | **Elastic IP** | Static IPv4 addresses for dynamic cloud computing |
| [![EventBridge](img/icons/Arch_Amazon-EventBridge_48.png)](eventbridge.html) | [**EventBridge**](eventbridge.html) | Event bus for connecting applications with event-driven architecture |
| [![IAM](img/icons/Arch_AWS-Identity-and-Access-Management_48.png)](iam-best-practices.html) | [**IAM** (Identity & Access Management)](iam-best-practices.html) | Controls access to AWS resources with users, roles, and policies |
| [![Kinesis](img/icons/Arch_Amazon-Kinesis_48.png)](kinesis-streams.html) | [**Kinesis**](kinesis-streams.html) | Real-time data streaming for analytics and processing |
| [![KMS](img/icons/Arch_AWS-Key-Management-Service_48.png)](kms-encryption.html) | [**KMS** (Key Management Service)](kms-encryption.html) | Creates and manages encryption keys |
| [![Lambda](img/icons/Arch_AWS-Lambda_48.png)](lambda-quickstart.html) | [**Lambda**](lambda-quickstart.html) | Runs code without managing servers (serverless functions) with cost optimization controls |
| [![NLB](img/icons/Arch_Elastic-Load-Balancing_48.png)](network-load-balancer.html) | [**Network Load Balancer**](network-load-balancer.html) | High-performance TCP/UDP load balancer |
| ![OIDC Provider](img/icons/Arch_AWS-IAM-Identity-Center_48.png) | **OIDC Provider** | Federated identity using OpenID Connect |
| [![RDS](img/icons/Arch_Amazon-RDS_48.png)](rds-database.html) | [**RDS** (Relational Database Service)](rds-database.html) | Managed relational databases (PostgreSQL, MySQL, etc.) |
| ![Route53](img/icons/Arch_Amazon-Route-53_48.png) | **Route53** | DNS service and domain name management |
| [![S3](img/icons/Arch_Amazon-Simple-Storage-Service_48.png)](s3-quickstart.html) | [**S3** (Simple Storage Service)](s3-quickstart.html) | Object storage for files, backups, and static websites |
| [![Secrets Manager](img/icons/Arch_AWS-Secrets-Manager_48.png)](alb-secrets-route53.html) | [**Secrets Manager**](alb-secrets-route53.html) | Securely stores and rotates database credentials and API keys |
| [![SNS](img/icons/Arch_Amazon-Simple-Notification-Service_48.png)](sns-sqs-messaging.html) | [**SNS** (Simple Notification Service)](sns-sqs-messaging.html) | Pub/sub messaging for sending notifications |
| [![SQS](img/icons/Arch_Amazon-Simple-Queue-Service_48.png)](sns-sqs-messaging.html) | [**SQS** (Simple Queue Service)](sns-sqs-messaging.html) | Message queuing for decoupling and scaling applications |
| ![SSM](img/icons/Arch_AWS-Systems-Manager_48.png) | **SSM** (Systems Manager) | Manages parameters and documents for configuration |
| [![Step Functions](img/icons/Arch_AWS-Step-Functions_48.png)](step-functions.html) | [**Step Functions**](step-functions.html) | Coordinates multiple AWS services into serverless workflows |
| ![VPC](img/icons/Arch_Amazon-Virtual-Private-Cloud_48.png) | **VPC** (Virtual Private Cloud) | Isolated network environment for your AWS resources |
| ![X-Ray](img/icons/Arch_AWS-X-Ray_48.png) | **X-Ray** | Distributed tracing for debugging and analyzing microservices |

#### Additional Capabilities

- **Custom Resources** - Define custom CloudFormation resources
- **Lambda Powertools** - Production-ready observability for Lambda functions
- **Grants** - Simplified IAM permission management between resources
- **Tags** - Resource tagging across stacks
- **Production-Safe Defaults** - Security and reliability best practices built-in

*)
