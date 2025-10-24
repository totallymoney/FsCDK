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
    stackProps {
        devEnv
        description "Developer stack for feature work"
        tags [ "service", "users"; "env", "dev" ]
    }

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
    stackProps {
        prodEnv
        stackName "users-prod"
        terminationProtection true
        tags [ "service", "users"; "env", "prod" ]
    }

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
| **CDK App** | `app` | `context`, `stackTraces`, `synthesizer` |
| **Bucket** | `bucket`, `s3Bucket` | `constructId`, `blockPublicAccess`, `encryption`, `encryptionKey`, `enforceSSL`, `versioned`, `removalPolicy`, `serverAccessLogsBucket`, `serverAccessLogsPrefix`, `autoDeleteObjects`, `websiteIndexDocument`, `websiteErrorDocument` |
| **Cors Rule** | `corsRule` | `allowedMethods`, `allowedOrigins`, `allowedHeaders`, `exposedHeaders`, `id`, `maxAgeSeconds` |
| **LambdaRole** | `lambdaRole` | `constructId`, `assumeRolePrincipal`, `managedPolicy`, `inlinePolicy`, `basicExecution`, `vpcExecution`, `kmsDecrypt`, `xrayTracing` |
| **IAM PolicyStatement** | `policyStatement` | (none - uses method chaining, not CustomOperations) |
| **Topic** | `topic` | `constructId`, `displayName`, `fifo`, `contentBasedDeduplication` |
| **Subscription** | `subscription` | `topic`, `lambda`, `queue`, `email`, `sms`, `http`, `https`, `filterPolicy`, `subscriptionDeadLetterQueue` |
| **Database Instance** | `rdsInstance` | `constructId`, `engine`, `postgresEngine`, `instanceType`, `vpc`, `vpcSubnets`, `securityGroup`, `allocatedStorage`, `storageType`, `backupRetentionDays`, `deleteAutomatedBackups`, `removalPolicy`, `deletionProtection`, `multiAz`, `publiclyAccessible`, `databaseName`, `masterUsername`, `credentials`, `preferredBackupWindow`, `preferredMaintenanceWindow`, `storageEncrypted`, `monitoringInterval`, `enablePerformanceInsights`, `performanceInsightRetention`, `autoMinorVersionUpgrade`, `iamAuthentication` |
| **KMS Key** | `kmsKey` | `constructId`, `description`, `alias`, `enableKeyRotation`, `disableKeyRotation`, `removalPolicy`, `enabled`, `keySpec`, `keyUsage`, `pendingWindow`, `admissionPrincipal`, `policy` |
| **EKS Cluster** | `eksCluster` | `constructId`, `version`, `vpc`, `vpcSubnet`, `defaultCapacity`, `defaultCapacityInstance`, `mastersRole`, `endpointAccess`, `disableClusterLogging`, `setClusterLogging`, `enableAlbController`, `coreDnsComputeType`, `encryptionKey`, `addNodegroupCapacity`, `addServiceAccount`, `addHelmChart`, `addFargateProfile` |
| **Grant** | `grant` | `table`, `lambda`, `readAccess`, `writeAccess`, `readWriteAccess`, `customAccess` |
| **CloudWatch Alarm** | `cloudwatchAlarm` | `constructId`, `description`, `metricNamespace`, `metricName`, `metric`, `dimensions`, `statistic`, `period`, `threshold`, `evaluationPeriods`, `comparisonOperator`, `treatMissingData` |
| **Route53 HostedZone** | `hostedZone` | `constructId`, `comment`, `queryLogsLogGroupArn`, `vpcs`, `vpc` |
| **Route53 PrivateHostedZone** | `privateHostedZone` | `constructId`, `comment`, `vpc` |
| **Route53 ARecord** | `aRecord` | `constructId`, `zone`, `target`, `ttl`, `comment` |
| **Function** | `lambda` | `constructId`, `handler`, `runtime`, `code`, `dockerImageCode`, `inlineCode`, `environment`, `envVar`, `timeout`, `memory`, `description`, `reservedConcurrentExecutions`, `insightsVersion`, `layer`, `layers`, `architecture`, `tracing`, `securityGroups`, `deadLetterQueue`, `loggingFormat`, `maxEventAge`, `retryAttempts`, `deadLetterQueueEnabled`, `environmentEncryption`, `xrayEnabled`, `role` |
| **Table** | `table` | `constructId`, `partitionKey`, `sortKey`, `billingMode`, `removalPolicy`, `pointInTimeRecovery`, `stream`, `kinesisStream` |
| **Import Source** | `importSource` | `bucket`, `inputFormat`, `bucketOwner`, `compressionType`, `keyPrefix` |
| **Queue** | `queue` | `constructId`, `visibilityTimeout`, `messageRetention`, `fifo`, `contentBasedDeduplication`, `deadLetterQueue`, `delaySeconds` |
| **Vpc** | `vpc` | `constructId`, `maxAzs`, `natGateways`, `subnet`, `enableDnsHostnames`, `enableDnsSupport`, `defaultInstanceTenancy`, `ipAddresses`, `cidr` |
| **Security Group** | `securityGroup` | `constructId`, `vpc`, `description`, `allowAllOutbound`, `disableInlineRules` |
| **ALB** | `applicationLoadBalancer` | `constructId`, `vpc`, `internetFacing`, `vpcSubnets`, `securityGroup`, `deletionProtection`, `http2Enabled`, `dropInvalidHeaderFields` |
| **User Pool** | `userPool` | `constructId`, `userPoolName`, `selfSignUpEnabled`, `signInAliases`, `signInWithEmailAndUsername`, `signInWithEmail`, `autoVerify`, `standardAttributes`, `customAttribute`, `passwordPolicy`, `mfa`, `mfaSecondFactor`, `accountRecovery`, `emailSettings`, `smsRole`, `lambdaTriggers`, `removalPolicy` |
| **User Pool Client** | `userPoolClient` | `constructId`, `userPool`, `generateSecret`, `authFlows`, `oAuth`, `preventUserExistenceErrors`, `supportedIdentityProvider`, `tokenValidities` |
| **Distribution** | `cloudFrontDistribution` | `constructId`, `defaultBehavior`, `s3DefaultBehavior`, `httpDefaultBehavior`, `additionalBehavior`, `additionalS3Behavior`, `additionalHttpBehavior`, `domainName`, `certificate`, `defaultRootObject`, `comment`, `enabled`, `priceClass`, `httpVersion`, `minimumProtocolVersion`, `enableIpv6`, `enableLogging`, `webAclId` |
| **Origin Access Identity** | `originAccessIdentity` | `constructId`, `comment` |
| **Event BridgeRule** | `eventBridgeRule` | `constructId`, `ruleName`, `description`, `enabled`, `eventPattern`, `schedule`, `target`, `eventBus` |
| **Event Bus** | `eventBus` | `constructId`, `eventSourceName`, `customEventBusName` |
| **Kinesis Stream** | `kinesisStream` | `constructId`, `streamName`, `shardCount`, `retentionPeriod`, `streamMode`, `onDemand`, `unencrypted`, `encryptionKey`, `encryption`, `grantRead`, `grantWrite` |
| **Stack** | `stack` | - |

*)
