(**
---
title: FsCDK
category: docs
index: 0
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

### List of builders and their operations

(Most of them, this might not be complete)

| AWS Resource | Builder Name(s) | Parameters |
|-------------|------------------|---------------------------|
| **ALB** | `applicationLoadBalancer` | `constructId`, `vpc`, `internetFacing`, `vpcSubnets`, `securityGroup`, `deletionProtection`, `http2Enabled`, `dropInvalidHeaderFields` |
| **AppSync GraphQL API** | `appSyncGraphqlApi` | `constructId`, `name`, `schema`, `authorizationConfig`, `xrayEnabled`, `logConfig`, `domainName` |
| **Bucket** | `bucket`, `s3Bucket` | `constructId`, `blockPublicAccess`, `encryption`, `encryptionKey`, `enforceSSL`, `versioned`, `removalPolicy`, `serverAccessLogsBucket`, `serverAccessLogsPrefix`, `autoDeleteObjects`, `websiteIndexDocument`, `websiteErrorDocument` |
| **CDK App** | `app` | `context`, `stackTraces`, `synthesizer` |
| **CloudWatch Alarm** | `cloudwatchAlarm` | `constructId`, `description`, `metricNamespace`, `metricName`, `metric`, `dimensions`, `statistic`, `period`, `threshold`, `evaluationPeriods`, `comparisonOperator`, `treatMissingData` |
| **CloudWatch Log Group** | `logGroup` | `constructId`, `retention`, `removalPolicy`, `encryptionKey`, `logGroupClass` |
| **CloudWatch Metric Filter** | `metricFilter` | `constructId`, `logGroup`, `filterPattern`, `metricName`, `metricNamespace`, `metricValue`, `defaultValue`, `unit` |
| **CloudWatch Subscription Filter** | `subscriptionFilter` | `constructId`, `logGroup`, `destination`, `filterPattern` |
| **Cors Rule** | `corsRule` | `allowedMethods`, `allowedOrigins`, `allowedHeaders`, `exposedHeaders`, `id`, `maxAgeSeconds` |
| **Database Instance** | `rdsInstance` | `constructId`, `engine`, `postgresEngine`, `instanceType`, `vpc`, `vpcSubnets`, `securityGroup`, `allocatedStorage`, `storageType`, `backupRetentionDays`, `deleteAutomatedBackups`, `removalPolicy`, `deletionProtection`, `multiAz`, `publiclyAccessible`, `databaseName`, `masterUsername`, `credentials`, `preferredBackupWindow`, `preferredMaintenanceWindow`, `storageEncrypted`, `monitoringInterval`, `enablePerformanceInsights`, `performanceInsightRetention`, `autoMinorVersionUpgrade`, `iamAuthentication` |
| **Distribution** | `cloudFrontDistribution` | `constructId`, `defaultBehavior`, `s3DefaultBehavior`, `httpDefaultBehavior`, `additionalBehavior`, `additionalS3Behavior`, `additionalHttpBehavior`, `domainName`, `certificate`, `defaultRootObject`, `comment`, `enabled`, `priceClass`, `httpVersion`, `minimumProtocolVersion`, `enableIpv6`, `enableLogging`, `webAclId` |
| **ECR Lifecycle Rule** | (helpers) | `deleteUntaggedAfterDays`, `keepLastNImages`, `deleteTaggedAfterDays`, `standardDevLifecycleRules`, `standardProdLifecycleRules` |
| **ECR Repository** | `ecrRepository` | `constructId`, `repositoryName`, `imageScanOnPush`, `imageTagMutability`, `lifecycleRule`, `removalPolicy`, `emptyOnDelete` |
| **ECS Cluster** | `ecsCluster` | `constructId`, `vpc`, `containerInsights`, `enableFargateCapacityProviders` |
| **ECS Fargate Service** | `ecsFargateService` | `constructId`, `cluster`, `taskDefinition`, `desiredCount`, `serviceName`, `assignPublicIp`, `securityGroups`, `vpcSubnets`, `healthCheckGracePeriod`, `minHealthyPercent`, `maxHealthyPercent`, `enableExecuteCommand`, `circuitBreaker` |
| **EKS Cluster** | `eksCluster` | `constructId`, `version`, `vpc`, `vpcSubnet`, `defaultCapacity`, `defaultCapacityInstance`, `mastersRole`, `endpointAccess`, `disableClusterLogging`, `setClusterLogging`, `enableAlbController`, `coreDnsComputeType`, `encryptionKey`, `addNodegroupCapacity`, `addServiceAccount`, `addHelmChart`, `addFargateProfile` |
| **Event BridgeRule** | `eventBridgeRule` | `constructId`, `ruleName`, `description`, `enabled`, `eventPattern`, `schedule`, `target`, `eventBus` |
| **Event Bus** | `eventBus` | `constructId`, `eventSourceName`, `customEventBusName` |
| **Fargate Task Definition** | `fargateTaskDefinition` | `constructId`, `cpu`, `memory`, `taskRole`, `executionRole`, `family`, `runtimePlatform`, `ephemeralStorageGiB`, `volume`, `volumes` |
| **Function** | `lambda` | `constructId`, `handler`, `runtime`, `code`, `dockerImageCode`, `inlineCode`, `environment`, `envVar`, `timeout`, `memory`, `description`, `reservedConcurrentExecutions`, `insightsVersion`, `layer`, `layers`, `architecture`, `tracing`, `securityGroups`, `deadLetterQueue`, `loggingFormat`, `maxEventAge`, `retryAttempts`, `deadLetterQueueEnabled`, `environmentEncryption`, `xrayEnabled`, `role` |
| **Gateway VPC Endpoint** | `gatewayVpcEndpoint` | `constructId`, `vpc`, `service`, `subnets` |
| **Grant** | `grant` | `table`, `lambda`, `readAccess`, `writeAccess`, `readWriteAccess`, `customAccess` |
| **HTTP API (API Gateway V2)** | `httpApi` | `constructId`, `apiName`, `description`, `cors`, `defaultIntegration`, `createDefaultStage`, `disableExecuteApiEndpoint` |
| **IAM PolicyStatement** | `policyStatement` | (none - uses method chaining, not CustomOperations) |
| **Import Source** | `importSource` | `bucket`, `inputFormat`, `bucketOwner`, `compressionType`, `keyPrefix` |
| **Interface VPC Endpoint** | `interfaceVpcEndpoint` | `constructId`, `vpc`, `service`, `subnets`, `privateDnsEnabled`, `securityGroups` |
| **Kinesis Stream** | `kinesisStream` | `constructId`, `streamName`, `shardCount`, `retentionPeriod`, `streamMode`, `onDemand`, `unencrypted`, `encryptionKey`, `encryption`, `grantRead`, `grantWrite` |
| **KMS Key** | `kmsKey` | `constructId`, `description`, `alias`, `enableKeyRotation`, `disableKeyRotation`, `removalPolicy`, `enabled`, `keySpec`, `keyUsage`, `pendingWindow`, `admissionPrincipal`, `policy` |
| **LambdaRole** | `lambdaRole` | `constructId`, `assumeRolePrincipal`, `managedPolicy`, `inlinePolicy`, `basicExecution`, `vpcExecution`, `kmsDecrypt`, `xrayTracing` |
| **Origin Access Identity** | `originAccessIdentity` | `constructId`, `comment` |
| **Queue** | `queue` | `constructId`, `visibilityTimeout`, `messageRetention`, `fifo`, `contentBasedDeduplication`, `deadLetterQueue`, `delaySeconds` |
| **RDS Proxy** | `rdsProxy` | `constructId`, `proxyTarget`, `vpc`, `secrets`, `requireTLS`, `iamAuth`, `debugLogging`, `idleClientTimeout`, `maxConnectionsPercent`, `maxIdleConnectionsPercent`, `sessionPinningFilters`, `initQuery` |
| **REST API (API Gateway V1)** | `restApi` | `constructId`, `restApiName`, `description`, `deployOptions`, `endpointTypes`, `binaryMediaTypes`, `cloneFrom`, `policy`, `defaultCorsPreflightOptions`, `defaultIntegration`, `defaultMethodOptions`, `disableExecuteApiEndpoint`, `failOnWarnings`, `parameters`, `retainDeployments` |
| **Route53 ARecord** | `aRecord` | `constructId`, `zone`, `target`, `ttl`, `comment` |
| **Route53 HostedZone** | `hostedZone` | `constructId`, `comment`, `queryLogsLogGroupArn`, `vpcs`, `vpc` |
| **Route53 PrivateHostedZone** | `privateHostedZone` | `constructId`, `comment`, `vpc` |
| **Security Group** | `securityGroup` | `constructId`, `vpc`, `description`, `allowAllOutbound`, `disableInlineRules` |
| **Stack** | `stack` | - |
| **Step Functions** | `stepFunction` | `constructId`, `stateMachineName`, `stateMachineType`, `definition`, `role`, `timeout`, `comment`, `logs`, `loggingLevel`, `logDestination`, `tracingEnabled` |
| **Subscription** | `subscription` | `topic`, `lambda`, `queue`, `email`, `sms`, `http`, `https`, `filterPolicy`, `subscriptionDeadLetterQueue` |
| **Table** | `table` | `constructId`, `partitionKey`, `sortKey`, `billingMode`, `removalPolicy`, `pointInTimeRecovery`, `stream`, `kinesisStream` |
| **Token Authorizer** | `tokenAuthorizer` | `constructId`, `handler`, `identitySource`, `validationRegex`, `resultsCacheTtl`, `assumeRole` |
| **Topic** | `topic` | `constructId`, `displayName`, `fifo`, `contentBasedDeduplication` |
| **User Pool** | `userPool` | `constructId`, `userPoolName`, `selfSignUpEnabled`, `signInAliases`, `signInWithEmailAndUsername`, `signInWithEmail`, `autoVerify`, `standardAttributes`, `customAttribute`, `passwordPolicy`, `mfa`, `mfaSecondFactor`, `accountRecovery`, `emailSettings`, `smsRole`, `lambdaTriggers`, `removalPolicy` |
| **User Pool Client** | `userPoolClient` | `constructId`, `userPool`, `generateSecret`, `authFlows`, `oAuth`, `preventUserExistenceErrors`, `supportedIdentityProvider`, `tokenValidities` |
| **Vpc** | `vpc` | `constructId`, `maxAzs`, `natGateways`, `subnet`, `enableDnsHostnames`, `enableDnsSupport`, `defaultInstanceTenancy`, `ipAddresses`, `cidr` |
| **VPC Link** | `vpcLink` | `constructId`, `description`, `targets`, `vpcLinkName` |

The following AWS services are supported by FsCDK:

| Service | What it does |
|---------|--------------|
| **ALB** (Application Load Balancer) | Distributes incoming HTTP/HTTPS traffic across multiple targets |
| **API Gateway** (REST & HTTP API) | Creates REST and HTTP APIs to expose your backend services |
| **App Runner** | Fully managed container service for web apps and APIs |
| **AppSync** | Builds managed GraphQL APIs with real-time data synchronization |
| **Bastion Host** | Secure SSH access to instances in private subnets |
| **Certificate Manager** | Manages SSL/TLS certificates for secure connections |
| **CloudFront** | Content delivery network (CDN) for fast global content distribution |
| **CloudHSM** | Hardware security modules for cryptographic key storage |
| **CloudWatch** | Monitors resources with alarms, log groups, metric filters, subscription filters, dashboards, and synthetic canaries |
| **Cognito** | User authentication and authorization for web and mobile apps |
| **DocumentDB** | MongoDB-compatible document database |
| **DynamoDB** | Fully managed NoSQL database for key-value and document data |
| **EC2** | Virtual servers in the cloud |
| **ECR** (Elastic Container Registry) | Stores and manages Docker container images |
| **ECS** (Elastic Container Service) | Runs containerized applications using Docker and Fargate |
| **EFS** (Elastic File System) | Scalable file storage for Lambda and EC2 |
| **EKS** (Elastic Kubernetes Service) | Managed Kubernetes clusters for container orchestration |
| **ElastiCache** | In-memory caching with Redis and Memcached |
| **Elastic Beanstalk** | Platform-as-a-Service (PaaS) for deploying applications |
| **Elastic IP** | Static IPv4 addresses for dynamic cloud computing |
| **EventBridge** | Event bus for connecting applications with event-driven architecture |
| **IAM** (Identity & Access Management) | Controls access to AWS resources with users, roles, and policies |
| **Kinesis** | Real-time data streaming for analytics and processing |
| **KMS** (Key Management Service) | Creates and manages encryption keys |
| **Lambda** | Runs code without managing servers (serverless functions) |
| **Network Load Balancer** | High-performance TCP/UDP load balancer |
| **OIDC Provider** | Federated identity using OpenID Connect |
| **RDS** (Relational Database Service) | Managed relational databases (PostgreSQL, MySQL, etc.) |
| **Route53** | DNS service and domain name management |
| **S3** (Simple Storage Service) | Object storage for files, backups, and static websites |
| **Secrets Manager** | Securely stores and rotates database credentials and API keys |
| **SNS** (Simple Notification Service) | Pub/sub messaging for sending notifications |
| **SQS** (Simple Queue Service) | Message queuing for decoupling and scaling applications |
| **SSM** (Systems Manager) | Manages parameters and documents for configuration |
| **Step Functions** | Coordinates multiple AWS services into serverless workflows |
| **VPC** (Virtual Private Cloud) | Isolated network environment for your AWS resources |
| **X-Ray** | Distributed tracing for debugging and analyzing microservices |

#### Additional Capabilities

- **Custom Resources** - Define custom CloudFormation resources
- **Lambda Powertools** - Production-ready observability for Lambda functions
- **Grants** - Simplified IAM permission management between resources
- **Tags** - Resource tagging across stacks
- **Production-Safe Defaults** - Security and reliability best practices built-in

*)
