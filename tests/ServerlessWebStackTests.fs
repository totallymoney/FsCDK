module FsCDK.Tests.ServerlessWebStackTests

open Amazon.CDK
open Amazon.CDK.Assertions
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.S3.Deployment
open Amazon.CDK.AWS.Apigatewayv2
open Expecto
open FsCDK
open FsCdk.Tests.TestHelpers

let private json = Templates.json

[<Tests>]
let rdsBindTests =
    testList
        "RDS instance bind"
        [ test "let! exposes the instance for outputs and an RDS proxy" {
              let app = App()

              stack "RdsBind" {
                  scope app
                  let! network = vpc "Network" { maxAzs 2 }

                  let! db =
                      rdsInstance "Postgres" {
                          vpc network
                          postgresEngine
                          deletionProtection false
                      }

                  rdsProxy "PostgresProxy" {
                      vpc network
                      proxyTarget db
                      secret db.Secret
                  }

                  cfnOutput "DbEndpoint" { value db.DbInstanceEndpointAddress }
              }

              let template = Templates.ofStack app "RdsBind"
              template.ResourceCountIs("AWS::RDS::DBInstance", 1.0)
              template.ResourceCountIs("AWS::RDS::DBProxy", 1.0)

              template.HasResourceProperties(
                  "AWS::RDS::DBProxyTargetGroup",
                  Templates.obj [ "TargetGroupName", "default" ]
              )

              template.HasOutput("DbEndpoint", Match.AnyValue())
          } ]
    |> testSequenced

[<Tests>]
let cloudFrontOacTests =
    testList
        "CloudFront OAC, bind and bucket deployment"
        [ test "private bucket behind OAC with SPA error responses and invalidating deployment" {
              let app = App()

              stack "SpaStack" {
                  scope app
                  let! site = bucket "fscdk-test-site" { encryption Amazon.CDK.AWS.S3.BucketEncryption.S3_MANAGED }

                  let! cdn =
                      cloudFrontDistribution "SiteCdn" {
                          s3DefaultBehavior (OriginAccessControlOrigin site)
                          defaultRootObject "index.html"
                          spaErrorResponses "/index.html"
                      }

                  bucketDeployment "SiteAssets" {
                      source (Source.Data("index.html", "<html></html>"))
                      destinationBucket site
                      distribution cdn
                  }

                  cfnOutput "CdnDomain" { value cdn.DistributionDomainName }
              }

              let template = Templates.ofStack app "SpaStack"

              template.ResourceCountIs("AWS::CloudFront::OriginAccessControl", 1.0)
              template.ResourceCountIs("AWS::CloudFront::CloudFrontOriginAccessIdentity", 0.0)

              let distribution = Templates.single template "AWS::CloudFront::Distribution" |> json
              Expect.stringContains distribution "OriginAccessControlId" "origin uses OAC"
              Expect.stringContains distribution "\"ErrorCode\":403" "403 rewritten"
              Expect.stringContains distribution "\"ErrorCode\":404" "404 rewritten"
              Expect.stringContains distribution "/index.html" "rewritten to the app"

              let policy = Templates.single template "AWS::S3::BucketPolicy" |> json
              Expect.stringContains policy "cloudfront.amazonaws.com" "bucket trusts CloudFront"
              Expect.stringContains policy "AWS:SourceArn" "scoped to the distribution"

              template.HasResourceProperties(
                  "Custom::CDKBucketDeployment",
                  Templates.obj
                      [ "DistributionPaths", [| "/*" |]
                        "DistributionId", Match.AnyValue()
                        "Prune", true ]
              )

              template.HasOutput("CdnDomain", Match.AnyValue())
          }

          test "OAC refuses buckets encrypted with the AWS managed KMS key" {
              let app = App()

              Expect.throws
                  (fun () ->
                      stack "OacManagedKey" {
                          scope app
                          // FsCDK's default bucket encryption is KMS_MANAGED (aws/s3)
                          let! site = bucket "fscdk-test-managed-key" { () }

                          cloudFrontDistribution "Cdn" { s3DefaultBehavior (OriginAccessControlOrigin site) }
                      })
                  "aws/s3 objects cannot be read through OAC"
          }

          test "OAC accepts a customer managed KMS key and grants CloudFront decrypt" {
              let app = App()

              stack "OacCustomerKey" {
                  scope app

                  let! site = bucket "fscdk-test-customer-key" { encryption Amazon.CDK.AWS.S3.BucketEncryption.KMS }

                  cloudFrontDistribution "Cdn" { s3DefaultBehavior (OriginAccessControlOrigin site) }
              }

              let template = Templates.ofStack app "OacCustomerKey"
              let key = Templates.single template "AWS::KMS::Key" |> json
              Expect.stringContains key "cloudfront.amazonaws.com" "key policy lets CloudFront decrypt"
          }

          test "bucket deployment needs a destination" {
              Expect.throws
                  (fun () -> bucketDeployment "NoDest" { source (Source.Data("a", "b")) } |> ignore)
                  "destinationBucket is required"
          } ]
    |> testSequenced

[<Tests>]
let wafTests =
    testList
        "WAFv2 web ACL"
        [ test "regional ACL with managed groups, rate limit, association and redacted logging" {
              let app = App()

              stack "RegionalWaf" {
                  scope app
                  let! wafLogs = logGroup "aws-waf-logs-regional" { retention RetentionDays.ONE_YEAR }

                  webAcl "ApiAcl" {
                      recommendedRules
                      managedRuleGroup "AWSManagedRulesSQLiRuleSet" [ "SQLi_BODY" ]
                      rateLimit 1000
                      associate "arn:aws:elasticloadbalancing:eu-west-2:123456789012:loadbalancer/app/web/abc"
                      logTo wafLogs
                  }
              }

              let template = Templates.ofStack app "RegionalWaf"

              template.HasResourceProperties(
                  "AWS::WAFv2::WebACL",
                  Templates.obj
                      [ "Scope", "REGIONAL"
                        "DefaultAction", Templates.obj [ "Allow", Templates.obj [] ]
                        "Rules",
                        Match.ArrayWith(
                            [| Match.ObjectLike(
                                   Templates.obj
                                       [ "Name", "AWSManagedRulesCommonRuleSet"
                                         "Priority", 0
                                         "OverrideAction", Templates.obj [ "None", Templates.obj [] ] ]
                               )
                               Match.ObjectLike(
                                   Templates.obj
                                       [ "Name", "RateLimitPerIp"
                                         "Priority", 4
                                         "Action", Templates.obj [ "Block", Templates.obj [] ]
                                         "Statement",
                                         Templates.obj
                                             [ "RateBasedStatement",
                                               Templates.obj
                                                   [ "Limit", 1000
                                                     "AggregateKeyType", "IP"
                                                     "EvaluationWindowSec", 300 ] ] ]
                               ) |]
                        ) ]
              )

              let acl = Templates.single template "AWS::WAFv2::WebACL" |> json
              Expect.stringContains acl "\"RuleActionOverrides\"" "SQLi_BODY overridden"
              Expect.stringContains acl "SQLi_BODY" "SQLi_BODY counted"

              template.ResourceCountIs("AWS::WAFv2::WebACLAssociation", 1.0)

              let logging = Templates.single template "AWS::WAFv2::LoggingConfiguration" |> json
              Expect.stringContains logging "\"SingleHeader\":{\"Name\":\"authorization\"}" "authorization redacted"
              Expect.stringContains logging "\"SingleHeader\":{\"Name\":\"cookie\"}" "cookie redacted"
              Expect.stringContains logging ":log-group:\"" "log group ARN"
              Expect.isFalse (logging.Contains ":*") "log group ARN without :*"
          }

          test "CloudFront ACL attaches through the distribution webAclId" {
              let app = App()

              stack "EdgeWaf" {
                  scope app
                  env (Amazon.CDK.Environment(Account = "123456789012", Region = "us-east-1"))
                  let! site = bucket "fscdk-test-edge-site" { encryption Amazon.CDK.AWS.S3.BucketEncryption.S3_MANAGED }

                  let! acl =
                      webAcl "EdgeAcl" {
                          scope WebAclScope.CloudFront
                          recommendedRules
                          rateLimit "BurstLimit" 200 60
                          redactedField (WafRedactedField.Header "authorization")
                          redactedField WafRedactedField.QueryString
                      }

                  cloudFrontDistribution "EdgeCdn" {
                      s3DefaultBehavior (OriginAccessControlOrigin site)
                      webAclId acl.AttrArn
                  }
              }

              let template = Templates.ofStack app "EdgeWaf"
              template.HasResourceProperties("AWS::WAFv2::WebACL", Templates.obj [ "Scope", "CLOUDFRONT" ])

              let distribution = Templates.single template "AWS::CloudFront::Distribution" |> json
              Expect.stringContains distribution "\"WebACLId\":{\"Fn::GetAtt\":[\"EdgeAcl\",\"Arn\"]}" "ACL attached"
          }

          test "invalid configurations are refused" {
              Expect.throws
                  (fun () ->
                      webAcl "Bad" {
                          scope WebAclScope.CloudFront
                          associate "arn:aws:elasticloadbalancing:eu-west-2:1:loadbalancer/app/x/y"
                      }
                      |> ignore)
                  "CloudFront ACLs cannot use associate"

              Expect.throws (fun () -> webAcl "Bad" { rateLimit 5 } |> ignore) "rate limit below 10"

              Expect.throws (fun () -> webAcl "Bad" { rateLimit "R" 100 30 } |> ignore) "unsupported evaluation window"

              Expect.throws
                  (fun () ->
                      webAcl "Bad" {
                          managedRuleGroup "AWSManagedRulesCommonRuleSet"
                          recommendedRules
                      }
                      |> ignore)
                  "duplicate rule names"

              Expect.throws
                  (fun () -> webAcl "Bad" { rateLimit "Rate limit" 100 300 } |> ignore)
                  "rule name with a space"

              Expect.throws
                  (fun () ->
                      webAcl "Bad" { associate "arn:aws:apigateway:eu-west-2::/apis/abc123/stages/$default" }
                      |> ignore)
                  "HTTP API stages cannot be associated"

              Expect.throws
                  (fun () ->
                      stack "EdgeAclWrongRegion" {
                          scope (App())
                          env (Amazon.CDK.Environment(Account = "123456789012", Region = "eu-west-2"))
                          webAcl "Acl" { scope WebAclScope.CloudFront }
                      })
                  "CloudFront web ACLs only in us-east-1"

              let app = App()

              Expect.throws
                  (fun () ->
                      stack "BadWafLogs" {
                          scope app
                          let! lg = logGroup "/app/waf" { () }
                          webAcl "Acl" { logTo lg }
                      })
                  "log group name must start with aws-waf-logs-"
          } ]
    |> testSequenced

[<Tests>]
let alarmActionTests =
    testList
        "CloudWatch alarm actions"
        [ test "alarm and OK actions notify SNS" {
              let app = App()

              stack "AlarmActions" {
                  scope app
                  let! alerts = topic "Alerts" { () }

                  cloudwatchAlarm "Api5xx" {
                      metricNamespace "AWS/ApiGateway"
                      metricName "5xx"
                      threshold 5.0
                      alarmAction alerts
                      okAction alerts
                  }
              }

              let template = Templates.ofStack app "AlarmActions"
              let alarm = Templates.single template "AWS::CloudWatch::Alarm" |> json
              Expect.stringContains alarm "\"AlarmActions\":[{\"Ref\":\"Alerts" "alarm action is the topic"
              Expect.stringContains alarm "\"OKActions\":[{\"Ref\":\"Alerts" "ok action is the topic"
          } ]
    |> testSequenced

[<Tests>]
let gitHubActionsRoleTests =
    testList
        "GitHub Actions OIDC role"
        [ test "trusts branch and environment subjects with exact aud" {
              let app = App()

              stack "GitHubRole" {
                  scope app

                  githubActionsRole "platform-deploy" {
                      repository "mycorp/platform"
                      branch "main"
                      environment "production"
                      cdkDeployAccess
                  }
              }

              let template = Templates.ofStack app "GitHubRole"
              let role = Templates.single template "AWS::IAM::Role" |> json

              Expect.stringContains role "sts:AssumeRoleWithWebIdentity" "web identity"

              Expect.stringContains
                  role
                  "\"StringEquals\":{\"token.actions.githubusercontent.com:aud\":\"sts.amazonaws.com\",\"token.actions.githubusercontent.com:sub\":[\"repo:mycorp/platform:ref:refs/heads/main\",\"repo:mycorp/platform:environment:production\"]}"
                  "aud and sub pinned"

              Expect.isFalse (role.Contains "StringLike") "no wildcards"
              Expect.stringContains role "oidc-provider/token.actions.githubusercontent.com" "account provider"

              let policy = Templates.single template "AWS::IAM::Policy" |> json
              Expect.stringContains policy ":role/cdk-hnb659fds-*" "may assume CDK bootstrap roles"
          }

          test "wildcard tags use StringLike and a bound provider" {
              let app = App()

              stack "GitHubRoleTags" {
                  scope app

                  let! gh =
                      oidcProvider "GitHub" {
                          url OIDCProviders.GitHubActionsUrl
                          clientId OIDCProviders.GitHubActionsClientId
                      }

                  githubActionsRole "release" {
                      provider gh
                      repository "mycorp/platform"
                      tag "v*"
                  }
              }

              let template = Templates.ofStack app "GitHubRoleTags"
              let roles = template.FindResources("AWS::IAM::Role") |> json

              Expect.stringContains
                  roles
                  "\"StringLike\":{\"token.actions.githubusercontent.com:sub\":\"repo:mycorp/platform:ref:refs/tags/v*\"}"
                  "tag pattern"

              Expect.stringContains roles "\"Federated\":{\"Ref\":\"GitHub" "provider from let!"
          }

          test "unrestricted or malformed trust is refused" {
              Expect.throws
                  (fun () -> githubActionsRole "r" { repository "mycorp/platform" } |> ignore)
                  "needs branch/tag/environment"

              Expect.throws
                  (fun () ->
                      githubActionsRole "r" {
                          repository "mycorp/*"
                          branch "main"
                      }
                      |> ignore)
                  "no wildcard repositories"
          } ]
    |> testSequenced

/// The target stack: CloudFront + OAC SPA, HTTP API with Cognito JWT to a .NET Lambda, DynamoDB, WAF.
let serverlessWebStack (app: App) (name: string) =
    stack name {
        scope app
        env (Amazon.CDK.Environment(Account = "123456789012", Region = "us-east-1"))

        let! fn =
            lambda "ItemsApi" {
                runtime Runtime.DOTNET_8
                handler "Api::Api.Handler::handle"
                code (Lambda.dummyAssetDir ())
                snapStart SnapStartConf.ON_PUBLISHED_VERSIONS
                environment [ "TABLE_NAME", "items" ]
            }

        table "items" {
            partitionKey "pk" AttributeType.STRING
            billingMode BillingMode.PAY_PER_REQUEST
            grantReadWriteData fn
        }

        let! pool = userPool "Users" { () }
        let! client = userPoolClient "SpaClient" { userPool pool }

        let! api =
            httpApi "ItemsHttpApi" {
                cognitoAuthorizer "Cognito" pool [ client ]
                route HttpMethod.ANY "/api/{proxy+}" (fn :?> Function).CurrentVersion
                throttle 100.0 200
            }

        let! site = bucket "fscdk-test-spa" { encryption Amazon.CDK.AWS.S3.BucketEncryption.S3_MANAGED }

        let! acl =
            webAcl "EdgeAcl" {
                scope WebAclScope.CloudFront
                recommendedRules
                rateLimit 2000
            }

        let! cdn =
            cloudFrontDistribution "SpaCdn" {
                s3DefaultBehavior (OriginAccessControlOrigin site)
                defaultRootObject "index.html"
                spaRewrite "/index.html"
                webAclId acl.AttrArn

                // API Gateway rejects CloudFront's Host header, so forward everything except Host
                additionalBehavior
                    "/api/*"
                    (CloudFrontBehaviors.httpBehavior
                        (Fn.Select(2.0, Fn.Split("/", api.ApiEndpoint)))
                        None
                        None
                        (Some CachePolicy.CACHING_DISABLED)
                        (Some OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER)
                        None
                        (Some AllowedMethods.ALLOW_ALL)
                        None
                        None)
            }

        bucketDeployment "SpaAssets" {
            source (Source.Data("index.html", "<html><body>app</body></html>"))
            destinationBucket site
            distribution cdn
        }

        cfnOutput "SiteUrl" { value $"https://{cdn.DistributionDomainName}" }
        cfnOutput "UserPoolClientId" { value client.UserPoolClientId }
    }

[<Tests>]
let serverlessWebStackTests =
    testList
        "Serverless web stack"
        [ test "CloudFront+OAC SPA, HTTP API+JWT to .NET Lambda, DynamoDB and WAF synthesize together" {
              let app = App()
              serverlessWebStack app "ServerlessWeb"
              let template = Templates.ofStack app "ServerlessWeb"

              for resourceType in
                  [ "AWS::CloudFront::Distribution"
                    "AWS::CloudFront::OriginAccessControl"
                    "AWS::ApiGatewayV2::Api"
                    "AWS::ApiGatewayV2::Authorizer"
                    "AWS::Lambda::Version"
                    "AWS::DynamoDB::Table"
                    "AWS::WAFv2::WebACL"
                    "Custom::CDKBucketDeployment" ] do
                  Expect.isGreaterThanOrEqual
                      (template.FindResources(resourceType).Count)
                      1
                      $"{resourceType} is in the template"

              template.HasResourceProperties(
                  "AWS::Lambda::Function",
                  Templates.obj
                      [ "Runtime", "dotnet8"
                        "SnapStart", Templates.obj [ "ApplyOn", "PublishedVersions" ] ]
              )

              let integration = Templates.single template "AWS::ApiGatewayV2::Integration" |> json
              Expect.stringContains integration "Version" "integration targets the published version"

              let distribution = Templates.single template "AWS::CloudFront::Distribution" |> json
              Expect.stringContains distribution "\"PathPattern\":\"/api/*\"" "API behind CloudFront"

              // SPA routing must not rewrite API errors: no custom error responses, rewrite only on the default behavior
              Expect.isFalse (distribution.Contains "CustomErrorResponses") "no distribution-wide error rewrites"

              let rewrite = Templates.single template "AWS::CloudFront::Function" |> json
              Expect.stringContains rewrite "/index.html" "rewrites to the app"

              let config = template.FindResources("AWS::CloudFront::Distribution") |> Seq.head

              let distributionConfig =
                  (config.Value.["Properties"] :?> System.Collections.Generic.IDictionary<string, obj>).["DistributionConfig"]
                  :?> System.Collections.Generic.IDictionary<string, obj>

              Expect.stringContains
                  (json distributionConfig.["DefaultCacheBehavior"])
                  "\"EventType\":\"viewer-request\""
                  "default behavior runs the rewrite"

              Expect.isFalse
                  ((json distributionConfig.["CacheBehaviors"]).Contains "FunctionAssociations")
                  "the /api/* behavior keeps its status codes"

              let assembly = app.Synth()
              Expect.equal assembly.Stacks.Length 1 "one stack"
          } ]
    |> testSequenced
