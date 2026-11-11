# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- AWS Bedrock builders (Agent, Knowledge Base, Data Source, Guardrail)
- `elasticIp` associates a network interface via a separate `AWS::EC2::EIPAssociation` resource
- `transitEncryptionEnabled` operation on the ElastiCache Redis builder
- `corsWithCredentials` HTTP API helper (explicit origins; API Gateway rejects credentials with a wildcard origin)
- Regression tests asserting that builders materialize resources in the synthesized template
- `httpApi` creates a real HTTP API: Lambda/integration routes, `publicRoute`, `routeSpec`, Cognito and JWT authorizers, custom domain, default-stage throttling, JSON access logs, `let!` bind to `HttpApi`
- `webAcl` WAFv2 builder: managed rule groups, rate limits, REGIONAL/CLOUDFRONT scope, associations, logging with redacted fields
- `bucketDeployment` builder (asset upload with CloudFront invalidation) and `cfnOutput` builder
- CloudFront `OriginAccessControlOrigin` origin (refuses `aws/s3`-key buckets OAC cannot read), `errorResponse`, `spaErrorResponses`, `spaRewrite` (CloudFront Function on the default behavior); `let!` bind to `IDistribution`
- `githubActionsRole` builder for GitHub Actions OIDC deploy roles (repository + branch/tag/environment `sub` conditions, `cdkDeployAccess`)
- `cloudwatchAlarm` `alarmAction`, `okAction`, `insufficientDataAction` (SNS) and `actionsEnabled`
- `let!` binds for `rdsInstance` (`DatabaseInstance`), `rdsProxy`, `oidcProvider`, `userPoolResourceServer`, `bucketDeployment`, `webAcl`
- `samples/serverless-web`: CloudFront + OAC SPA, HTTP API with Cognito JWT to a .NET Lambda (SnapStart), DynamoDB, WAF

### Fixed
- `table`: a yielded global or local secondary index or import source reset earlier `pointInTimeRecovery` and `encryption` to their defaults
- `userPoolClient` defaults to SRP only (`USER_PASSWORD_AUTH` was on by default) and disables OAuth unless `oAuth` is set (CDK's default enabled the implicit grant with a `https://example.com` callback)
- `logGroup` no longer defaults to `RemovalPolicy.DESTROY`; CDK's RETAIN applies unless `removalPolicy` is set
- Docs claimed default DLQs, concurrency limits, KMS-encrypted Lambda environments, 90-day log retention and PAY_PER_REQUEST billing that the code does not apply
- `bucket`/`s3Bucket`: a yielded lifecycle rule, CORS rule or metrics reset earlier `versioned`, `encryption`, `enforceSSL` and `blockPublicAccess` to their defaults
- `eksCluster` failed every synth: CDK requires a kubectl layer and no operation set one. `kubectlLayer` is new and required, and so is `version` (the 1.28 default is gone)
- CloudFront HTTP-origin behaviors (`httpDefaultBehavior`, `additionalHttpBehavior`, `CloudFrontBehaviors.httpBehavior`) cached with `CACHING_OPTIMIZED` while `ALL_VIEWER` forwarded Authorization, cookies and query strings outside the cache key, so one viewer's response could reach another. The default is now `CACHING_DISABLED`; pass a cache policy to cache
- Thirteen builders previously produced no CloudFormation resource at all — their props never
  reached a construct: `httpApi`, `ecrRepository`, `canary`, `ebApplication`, `albTargetGroup`,
  `albListener`, `recordSet` (Route53), `healthCheck` (Route53), `privateHostedZone`, `elasticIp`,
  `redisCluster`, DocumentDB cluster and App Runner service (missing stack Yield). All are now
  created when yielded into a `stack { }`.
- `ecsCluster` / `ecsFargateService`: all configuration (vpc, container insights, cluster, task
  definition, desired count, security groups) was discarded at stack synthesis; every
  `ecsFargateService` failed at synth
- ElastiCache Redis: builder configuration (engine, node type, snapshots, security groups) was
  discarded at stack synthesis, producing an invalid template
- `dockerImageFunction`: the image path and timeout never reached the construct (synth failure)
- `bucketPolicy`: statements (including `denyInsecureTransport` and IP rules) were silently
  dropped, deploying an empty policy; helper statements now resolve the bucket ARN regardless of
  operation order and deny statements also cover bucket-level actions
- `lambda`: `retryAttempts`/`maxEventAge` were silently ignored; event sources yielded into the
  CE were dropped; `autoCreateDLQ` and `autoAddPowertools` were documented but not implemented
  (now wired as opt-in: DLQ via `deadLetterQueueEnabled`, Powertools layer resolved for the
  stack's region and shared per runtime); the `addEventSources`/`addEventSourceMappings`/
  `addPermissions` operations replaced the accumulated list instead of appending; multiple
  permissions collided on the same construct id (now the first-added permission keeps the legacy
  id and later ones get stable `-{i}` suffixes); duplicate environment variable keys threw
  instead of last-wins
- Removed hidden default of 10 reserved concurrent executions on every lambda (an invisible
  concurrency cap that also consumed account reserved-concurrency quota)
- `app { }` no longer forces `AutoSynth=false`, `Outdir="cdk.out"` and `TreeMetadata=false`;
  CDK CLI defaults apply again so `cdk synth` works without a manual `Synth()` call
- CloudTrail: `SendToCloudWatchLogs` is now actually enabled when CloudWatch logging is on
  (previously the log group was created but the trail never streamed to it)
- Grants: failures (typo'd construct id, wrong resource type) now fail the synth instead of being
  silently swallowed, which deployed lambdas without their IAM permissions
- `dnsValidatedCertificate` silently produced an email-validated, same-region certificate; it now
  applies DNS validation and honors the requested region
- DocumentDB: `instanceType` was hardcoded to db.t3.medium regardless of the requested value;
  `backupWindow` and `tag`/`tags` were silently dropped
- RDS: `masterUsername` was silently ignored (now generates a secret via
  `Credentials.FromGeneratedSecret` when no explicit credentials are set)
- KMS: `admissionPrincipal` was silently dropped (now maps to key admins)
- IAM: `constructId` on role and policy builders had no effect; `user`/`policy` builders threw
  `ArgumentNullException` when `groups`/`roles`/`managedPolicies` were set; `managedPolicy` with
  `users` attached the policy to nobody
- `stage { }`: `outdir`, `permissionsBoundary`, `policyValidationBeta1` and `propertyInjectors`
  were silently dropped
- EKS: `addNodegroupCapacity`, `addServiceAccount`, `addHelmChart` and `addFargateProfile` were
  hardcoded to empty lists
- App Runner: `instanceRole` and `accessRole` were silently dropped;
  `ecrSourceWithAutoDeploy` ignored its access role parameter
- EventBridge: `eventSourceName` on the event bus was silently dropped (partner event buses);
  when it is set, `EventBusName` is now omitted since CDK rejects providing both
- EC2: `keyPairName` was silently dropped (instance deployed with no SSH key pair)
- EFS access point: `path` was silently dropped (mounted at `/`)
- Kinesis: `onDemand` always threw at synth (shard count was set for ON_DEMAND streams)
- Cognito: custom attributes were keyed by .NET type name (deployed as `custom:StringAttribute`,
  duplicates threw); `customAttribute` now takes the attribute name
- `policyStatement`: Deny-all guardrail statements are no longer rejected by the wildcard check;
  the `condition` operation now produces valid IAM condition JSON
  (`condition "StringEquals" "aws:PrincipalOrgID" "o-123"`) and same-operator conditions merge
- AppSync: `schemaFromString` treated the SDL text as a file path and could never work
- ElastiCache docs falsely claimed encryption at rest/in transit were enabled by default
- Lambda Powertools: `tracingEnabled` env var fixed for Node.js and .NET; layer ARN resolution
  no longer hardcodes us-east-1
- `rdsInstance` and `cloudFrontDistribution` constructs are stored on the spec instead of discarded
- `userPoolClient` stores its construct, so `let!` on it works
- `rdsProxy` `proxyTarget` accepts database instances (the overload always resolved to clusters)
- `HttpApiHelpers.corsPermissive` no longer combines `*` origins with credentials

### Changed
- NuGet package license metadata corrected from Apache-2.0 to MIT (matches the LICENSE file)
- `global.json` allows newer .NET SDKs via `rollForward: latestMajor`
- `dnsValidatedCertificate` keeps its DNS-validation promise: same-region certs use a plain
  `Certificate` with DNS validation, cross-region requests use the (deprecated but functional)
  `DnsValidatedCertificate` construct; the spec's `Certificate` field is now `ICertificate`

### Upgrade notes (read before deploying existing stacks)

Several operations that previously did nothing are now honored. If you had set them, review
your `cdk diff` carefully before deploying:

- **RDS `masterUsername`**: changing `MasterUsername` on `AWS::RDS::DBInstance` requires
  **replacement** — CloudFormation provisions a fresh, empty database. If you had set
  `masterUsername` while it was a no-op, remove it (or set `credentials` explicitly to the
  current values) before upgrading, or you risk data loss.
- **IAM `constructId` on `role`/`policy`**: previously ignored, now honored. If you had set it,
  the CloudFormation logical id changes, causing role/policy replacement (and possible
  `EntityAlreadyExists` failures for named roles). Remove the `constructId` to keep the old
  logical id.
- **Lambda reserved concurrency**: the hidden default of 10 reserved concurrent executions is
  removed. Functions that relied on it as an implicit throttle are now uncapped — set
  `reservedConcurrentExecutions` explicitly if you need the cap.
- **`dnsValidatedCertificate`**: validation changes from CDK's default (email) to DNS, which
  replaces the certificate resource on the next deploy.
- **`userPoolClient` auth flows**: a client that signs users in with `USER_PASSWORD_AUTH`, or uses
  the hosted UI without `oAuth` settings, loses that on the next deploy. Set
  `authFlows (AuthFlow(UserSrp = true, UserPassword = true))` or `oAuth` explicitly to keep it.
- **CloudFront HTTP origins**: responses are no longer cached unless a cache policy is passed.
- **`eksCluster`**: `version` and `kubectlLayer` are required.
- **`logGroup`**: the deletion policy changes from Delete to Retain.
- **EC2 `keyPairName`**: previously ignored, now applied — adding a key pair to a deployed
  instance requires replacement.
- Source-breaking signature changes: `customAttribute` now takes the attribute name
  (`customAttribute "name" attr`), `condition` now takes operator/key/value
  (`condition "StringEquals" "aws:PrincipalOrgID" "o-123"`), and
  `LambdaPowertoolsHelpers.getPowertoolsLayerArn` takes the region as its first argument.

## [0.2.0] - 2025-12-10

### Added
- SNS subscription builders
- DynamoDB Global Secondary Index (GSI) and Local Secondary Index (LSI) builders
- CloudWatch Dashboard builder

### Fixed
- DeadLetterQueue CE: fixed strict evaluation bug in Option.defaultValue
- SNS and SQS builder improvements

## [0.1.0] - 2025-11-25

### Added
- Lambda functions
- S3 buckets
- DynamoDB tables
- SNS topics
- SQS queues
- VPC with public
- Security Groups
- RDS PostgreSQL
- CloudFront

[unreleased]: https://github.com/totallymoney/FsCDK/compare/0.2.0...HEAD
[0.2.0]: https://github.com/totallymoney/FsCDK/compare/0.1.0...0.2.0
[0.1.0]: https://github.com/totallymoney/FsCDK/releases/tag/0.1.0
