module FsCDK.Tests.WiredBuildersTests

open Expecto
open FsCDK
open Amazon.CDK
open System.IO

/// <summary>
/// Regression tests for builders that previously assembled CDK props in Run
/// but never created a construct in the stack: the configuration silently
/// vanished and no resource reached the CloudFormation template. Each test
/// synthesizes a stack and asserts the resource type is present in the
/// generated template.
/// </summary>

let private templateText (app: App) (stackName: string) =
    let assembly = app.Synth()
    let artifact = assembly.GetStackByName(stackName)
    File.ReadAllText(artifact.TemplateFullPath)

[<Tests>]
let wired_builders_tests =
    // Sequenced: parallel App() construction races in jsii's one-time
    // resource extraction (EBUSY on the shared tarball)
    testSequenced
    <| testList
        "Previously inert builders create resources"
        [ test "httpApi, ecrRepository, ebApplication and elasticIp materialize" {
              let app = App()

              stack "WiredMisc" {
                  scope app

                  httpApi "my-api" { description "test api" }

                  ecrRepository "my-repo" { () }

                  ebApplication "my-app" { description "test app" }

                  elasticIp "my-eip" { () }

                  eventBus "partner-bus" { eventSourceName "aws.partner/example.com/123/test" }
              }

              let template = templateText app "WiredMisc"

              Expect.stringContains template "AWS::ApiGatewayV2::Api" "httpApi should create an API"
              Expect.stringContains template "AWS::ECR::Repository" "ecrRepository should create a repository"

              Expect.stringContains
                  template
                  "AWS::ElasticBeanstalk::Application"
                  "ebApplication should create an application"

              Expect.stringContains template "AWS::EC2::EIP" "elasticIp should create an EIP"
              // Partner buses must synth: CDK rejects EventBusName + EventSourceName together
              Expect.stringContains template "aws.partner/example.com/123/test" "partner event bus should synth"
          }

          test "route53 recordSet and healthCheck materialize" {
              let app = App()

              stack "WiredRoute53" {
                  scope app

                  recordSet "my-record" {
                      hostedZoneId "Z00000000000000000000"
                      recordName "test.example.com."
                      resourceRecords [ "192.0.2.1" ]
                  }

                  healthCheck "my-health-check" { domainName "example.com" }
              }

              let template = templateText app "WiredRoute53"

              Expect.stringContains template "AWS::Route53::RecordSet" "recordSet should create a record set"
              Expect.stringContains template "AWS::Route53::HealthCheck" "healthCheck should create a health check"
          }

          test "privateHostedZone and albTargetGroup materialize" {
              let app = App()

              stack "WiredVpcScoped" {
                  scope app

                  let! network = vpc "net" { () }

                  privateHostedZone "internal.example.com" { vpc network }

                  albTargetGroup "my-tg" { vpc network }
              }

              let template = templateText app "WiredVpcScoped"

              Expect.stringContains template "AWS::Route53::HostedZone" "privateHostedZone should create a hosted zone"

              Expect.stringContains
                  template
                  "AWS::ElasticLoadBalancingV2::TargetGroup"
                  "albTargetGroup should create a target group"
          }

          test "ecsCluster keeps vpc and elastiCache keeps engine configuration" {
              let app = App()

              stack "WiredEcsCache" {
                  scope app

                  let! network = vpc "cluster-net" { () }

                  ecsCluster "my-cluster" { vpc network }

                  redisCluster "my-cache" {
                      cacheNodeType "cache.t3.small"
                      transitEncryptionEnabled true
                  }
              }

              let template = templateText app "WiredEcsCache"

              Expect.stringContains template "AWS::ECS::Cluster" "ecsCluster should create a cluster"
              Expect.stringContains template "AWS::ElastiCache::CacheCluster" "redisCluster should create a cache"
              // Previously Stack.fs rebuilt the props with only the name, dropping all configuration
              Expect.stringContains template "cache.t3.small" "cache node type should reach the template"
              Expect.stringContains template "TransitEncryptionEnabled" "transit encryption should reach the template"
          }

          test "bucketPolicy statements reach the template" {
              let app = App()

              stack "WiredBucketPolicy" {
                  scope app

                  let! b = bucket "policy-bucket" { () }

                  bucketPolicy "my-policy" {
                      bucket b
                      denyInsecureTransport
                  }
              }

              let template = templateText app "WiredBucketPolicy"

              Expect.stringContains template "AWS::S3::BucketPolicy" "bucketPolicy should create a policy"
              // Previously the statements were dropped and the policy synthesized empty
              Expect.stringContains template "DenyInsecureTransport" "the deny statement should reach the template"
          } ]
