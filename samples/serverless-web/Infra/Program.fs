module Infra.Program

open System
open Amazon.CDK
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
open Amazon.CDK.AWS.Apigatewayv2
open FsCDK

/// Serverless web app: CloudFront (WAF) serves a private S3 SPA through OAC and forwards /api/* to an
/// HTTP API with a Cognito JWT authorizer, which invokes a .NET Lambda (SnapStart) backed by DynamoDB.
///
/// WAF cannot be associated with HTTP APIs, so the API is only protected by WAF when called through
/// CloudFront. CloudFront web ACLs must live in us-east-1, hence the separate edge stack.
[<EntryPoint>]
let main _ =
    let app = App()

    let account =
        Environment.GetEnvironmentVariable "CDK_DEFAULT_ACCOUNT"
        |> Option.ofObj
        |> Option.defaultValue "123456789012"

    let region = "eu-west-2"
    let apiCode = "../Api/bin/Release/net8.0/publish"
    let siteAssets = "../web/dist"

    // --- us-east-1: CloudFront web ACL -------------------------------------------------------------

    let wafLogs =
        logGroup "aws-waf-logs-serverless-web" { retention RetentionDays.ONE_YEAR }

    let edgeAcl =
        webAcl "ServerlessWebEdgeAcl" {
            scope WebAclScope.CloudFront
            recommendedRules
            rateLimit 2000
            logTo wafLogs
        }

    stack "ServerlessWebEdge" {
        scope app
        env (Amazon.CDK.Environment(Account = account, Region = "us-east-1"))
        crossRegionReferences true
        wafLogs
        edgeAcl
    }

    // --- eu-west-2: application --------------------------------------------------------------------

    stack "ServerlessWeb" {
        scope app
        env (Amazon.CDK.Environment(Account = account, Region = region))
        crossRegionReferences true

        let! itemsFn =
            lambda "ItemsApi" {
                runtime Runtime.DOTNET_8
                handler "Api::Api.Handler::handle"
                code apiCode
                memorySize 1024
                timeout 10.0
                snapStart SnapStartConf.ON_PUBLISHED_VERSIONS
                environment [ "TABLE_NAME", "serverless-web-items" ]
            }

        table "serverless-web-items" {
            partitionKey "pk" AttributeType.STRING
            sortKey "sk" AttributeType.STRING
            billingMode BillingMode.PAY_PER_REQUEST
            grantReadWriteData itemsFn
        }

        let! users = userPool "ServerlessWebUsers" { () }
        let! spaClient = userPoolClient "ServerlessWebSpa" { userPool users }
        let! apiAccessLogs = logGroup "/serverless-web/api-access" { retention RetentionDays.ONE_YEAR }

        let! api =
            httpApi "ServerlessWebApi" {
                cognitoAuthorizer "Cognito" users [ spaClient ]
                // SnapStart applies to published versions, so route to the current version
                route HttpMethod.GET "/api/items" (itemsFn :?> Function).CurrentVersion
                throttle 50.0 100
                accessLogs apiAccessLogs
            }

        // SSE-S3: CloudFront OAC cannot decrypt objects under the AWS managed aws/s3 KMS key (FsCDK default)
        let! site = bucket $"serverless-web-site-{account}" { encryption Amazon.CDK.AWS.S3.BucketEncryption.S3_MANAGED }

        // The JWT must reach API Gateway: put Authorization in the cache key (forwarded by CloudFront)
        // and keep TTLs at zero so responses are only cached when the API sends Cache-Control.
        let apiCachePolicy =
            CachePolicy(
                Stack.Of(site),
                "ApiCachePolicy",
                CachePolicyProps(
                    MinTtl = Duration.Seconds(0.0),
                    DefaultTtl = Duration.Seconds(0.0),
                    MaxTtl = Duration.Seconds(1.0),
                    HeaderBehavior = CacheHeaderBehavior.AllowList("Authorization"),
                    QueryStringBehavior = CacheQueryStringBehavior.All()
                )
            )

        let! cdn =
            cloudFrontDistribution "ServerlessWebCdn" {
                s3DefaultBehavior (OriginAccessControlOrigin site)
                defaultRootObject "index.html"
                // Rewrite only the default behavior so /api/* 403/404 responses reach the client
                spaRewrite "/index.html"
                webAclId edgeAcl.Arn

                additionalBehavior
                    "/api/*"
                    (CloudFrontBehaviors.httpBehavior
                        (Fn.Select(2.0, Fn.Split("/", api.ApiEndpoint)))
                        None
                        None
                        (Some apiCachePolicy)
                        (Some OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER)
                        None
                        (Some AllowedMethods.ALLOW_ALL)
                        None
                        None)
            }

        bucketDeployment "ServerlessWebSite" {
            sourcePath siteAssets
            destinationBucket site
            distribution cdn
        }

        let! alerts = topic "ServerlessWebAlerts" { () }

        cloudwatchAlarm "ServerlessWebApi5xx" {
            metric (api.MetricServerError())
            threshold 5.0
            evaluationPeriods 1
            alarmAction alerts
        }

        githubActionsRole "serverless-web-deploy" {
            repository "mycorp/serverless-web"
            branch "main"
            environment "production"
            cdkDeployAccess
        }

        cfnOutput "SiteUrl" { value $"https://{cdn.DistributionDomainName}" }
        cfnOutput "UserPoolId" { value users.UserPoolId }
        cfnOutput "UserPoolClientId" { value spaClient.UserPoolClientId }
    }

    app.Synth() |> ignore
    0
