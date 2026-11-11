(**
---
title: CloudFront static sites and SPAs
category: Resources
categoryindex: 26
description: Serve a private S3 bucket through CloudFront with Origin Access Control, SPA routing and automated asset deployment
---

# ![CloudFront](img/icons/Arch_Amazon-CloudFront_48.png) CloudFront static sites and single-page apps

This page shows how to host a static site or single-page app (SPA) with FsCDK:

- a **private** S3 bucket that only CloudFront can read, through **Origin Access Control (OAC)**
- client-side routing with `spaRewrite` or `spaErrorResponses`
- uploading the built site with `bucketDeployment`, which also invalidates the CloudFront cache
- forwarding `/api/*` to an HTTP API on the same domain

The [serverless web sample](https://github.com/totallymoney/FsCDK/tree/main/samples/serverless-web) combines all of
these with Cognito, a .NET Lambda, DynamoDB and AWS WAF.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.CloudFront
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.S3.Deployment
open Amazon.CDK.AWS.Apigatewayv2

(*** hide ***)
// Placeholder folders stand in for the built site and the published Lambda so this page is evaluable.
let placeholderDir name file =
    let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name)
    System.IO.Directory.CreateDirectory(dir) |> ignore
    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, file), "placeholder")
    dir

let siteDist = placeholderDir "fscdk-docs-site" "index.html"
let apiPublishDir = placeholderDir "fscdk-docs-site-api" "Api.dll"

(**
## Private bucket behind Origin Access Control

`OriginAccessControlOrigin bucket` creates an OAC origin and lets CDK add the bucket policy that allows only this
distribution to read objects. The bucket stays private: no public access, no website endpoint.

> **Encryption:** FsCDK buckets default to SSE-KMS with the AWS managed `aws/s3` key. CloudFront cannot be granted
> `kms:Decrypt` on that key, so every object would return 403. FsCDK refuses that combination at synth time: use
> `encryption BucketEncryption.S3_MANAGED` as below, or a customer managed key with `encryptionKey`.

Bind the bucket and the distribution with `let!` to pass them to other builders.
*)

stack "StaticSite" {
    let! site = bucket "static-site-assets" { encryption BucketEncryption.S3_MANAGED }

    let! cdn =
        cloudFrontDistribution "StaticSiteCdn" {
            s3DefaultBehavior (OriginAccessControlOrigin site)
            defaultRootObject "index.html"

            errorResponse (
                ErrorResponse(HttpStatus = 404.0, ResponseHttpStatus = 404.0, ResponsePagePath = "/404.html")
            )
        }

    bucketDeployment "StaticSiteContent" {
        sourcePath siteDist
        destinationBucket site
        distribution cdn
    }

    cfnOutput "StaticSiteUrl" { value $"https://{cdn.DistributionDomainName}" }
}

(**
## Single-page app routing

An SPA has routes such as `/accounts/42` that exist only in the browser. S3 has no object with that key, so without
help CloudFront returns 403 (OAC hides missing keys as 403) and a page refresh breaks. FsCDK offers two fixes:

| Operation | How it works | Use when |
|-----------|--------------|----------|
| `spaRewrite "/index.html"` | A CloudFront Function on the **default behavior** rewrites paths whose last segment has no file extension to the page. | The distribution also serves an API or sits behind WAF (recommended). |
| `spaErrorResponses "/index.html"` | Custom error responses turn every 403/404 into a 200 with the page. | Static-only sites. Error responses apply to **every** behavior, so API 403/404s and WAF blocks would also become 200 + HTML. |
*)

stack "SpaSite" {
    let! site = bucket "spa-assets" { encryption BucketEncryption.S3_MANAGED }

    cloudFrontDistribution "SpaCdn" {
        s3DefaultBehavior (OriginAccessControlOrigin site)
        defaultRootObject "index.html"
        spaRewrite "/index.html"
    }
}

stack "StaticOnlySpa" {
    let! site = bucket "static-spa-assets" { encryption BucketEncryption.S3_MANAGED }

    cloudFrontDistribution "StaticSpaCdn" {
        s3DefaultBehavior (OriginAccessControlOrigin site)
        defaultRootObject "index.html"
        spaErrorResponses "/index.html"
    }
}

(**
## Deploying the site with `bucketDeployment`

`bucketDeployment` uploads a local folder (or zip) during `cdk deploy` and, with `distribution`, invalidates the
CloudFront cache afterwards (`/*` unless you set `distributionPaths`).

- `prune` (default `true`) deletes objects that are no longer in the source. Set it to `false` to keep previous
  fingerprinted assets, so browser tabs still running the old release can load them.
- `cacheControl` sets `Cache-Control` headers on every uploaded object.
- `source` adds any CDK `ISource`, for example `Source.JsonData` to write a runtime `config.json`.
- `exclude`, `destinationKeyPrefix`, `retainOnDelete`, `memoryLimit` and `logGroup` cover the rest.

For different caching per file type (immutable fingerprinted assets, never-cached `index.html`), build those files
into separate folders and use one `bucketDeployment` per folder with its own `cacheControl`.
*)

stack "CachedSite" {
    let! site = bucket "cached-site-assets" { encryption BucketEncryption.S3_MANAGED }

    let! cdn =
        cloudFrontDistribution "CachedSiteCdn" {
            s3DefaultBehavior (OriginAccessControlOrigin site)
            defaultRootObject "index.html"
            spaRewrite "/index.html"
        }

    bucketDeployment "SiteContent" {
        sourcePath siteDist
        source (Source.JsonData("config.json", dict [ "apiBase", box "/api" ]))
        destinationBucket site
        exclude [ "*.map" ] // Don't publish source maps
        cacheControl [ CacheControl.MaxAge(Duration.Minutes(5.0)) ]
        prune false
        distribution cdn
        distributionPaths [ "/index.html"; "/config.json" ]
    }
}

(**
## SPA and API on one domain

Serving the API under `/api/*` on the same CloudFront domain removes CORS entirely. Bind the HTTP API with `let!`,
take the host name from `ApiEndpoint`, and add it as an additional behavior. `spaRewrite` only touches the default
behavior, so API status codes reach the client unchanged.

The API cache policy below forwards `Authorization` (needed by JWT authorizers) and keeps TTLs at zero, so responses
are cached only when the API sends `Cache-Control`.
*)

stack "SpaWithApi" {
    let! itemsFn =
        lambda "SiteItemsApi" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Handler::handle"
            code apiPublishDir
        }

    let! api = httpApi "SiteApi" { route HttpMethod.GET "/api/items" itemsFn }

    let! site = bucket "spa-api-assets" { encryption BucketEncryption.S3_MANAGED }

    let apiCachePolicy =
        CachePolicy(
            Stack.Of(site),
            "SiteApiCachePolicy",
            CachePolicyProps(
                MinTtl = Duration.Seconds(0.0),
                DefaultTtl = Duration.Seconds(0.0),
                MaxTtl = Duration.Seconds(1.0),
                HeaderBehavior = CacheHeaderBehavior.AllowList("Authorization"),
                QueryStringBehavior = CacheQueryStringBehavior.All()
            )
        )

    let! cdn =
        cloudFrontDistribution "SpaApiCdn" {
            s3DefaultBehavior (OriginAccessControlOrigin site)
            defaultRootObject "index.html"
            spaRewrite "/index.html"

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

    bucketDeployment "SpaApiContent" {
        sourcePath siteDist
        destinationBucket site
        distribution cdn
    }

    cfnOutput "SpaApiUrl" { value $"https://{cdn.DistributionDomainName}" }
}

(**
## Adding AWS WAF

CloudFront web ACLs must be created in **us-east-1**, whatever region the site lives in, so the web ACL and the
site usually live in two stacks of the same app. For the site stack to use the ACL's ARN:

- both stacks need an **explicit** `env` (account and region): CDK cannot reference across regions from an
  env-agnostic stack;
- **both** stacks need `crossRegionReferences true`, otherwise the synth fails with
  "Cross stack references are only supported for stacks deployed to the same environment";
- the edge stack must come first, because `edgeAcl.Arn` is only available after it has been built.

When the stack has an explicit region, FsCDK also fails the synth if a CloudFront-scoped web ACL is placed outside
us-east-1.
*)

let wafApp = App()
let account = "123456789012"

let edgeAcl =
    webAcl "SiteEdgeAcl" {
        scope WebAclScope.CloudFront
        recommendedRules
        rateLimit 2000
    }

stack "SiteEdge" {
    scope wafApp
    env (Amazon.CDK.Environment(Account = account, Region = "us-east-1"))
    crossRegionReferences true
    edgeAcl
}

stack "Site" {
    scope wafApp
    env (Amazon.CDK.Environment(Account = account, Region = "eu-west-2"))
    crossRegionReferences true

    let! site = bucket "waf-site-assets" { encryption BucketEncryption.S3_MANAGED }

    cloudFrontDistribution "SiteCdn" {
        s3DefaultBehavior (OriginAccessControlOrigin site)
        defaultRootObject "index.html"
        spaRewrite "/index.html"
        webAclId edgeAcl.Arn
    }
}

(**
CDK implements the reference with an SSM parameter written in us-east-1 and a custom resource in the site stack that
reads it, so deploy the edge stack first (`cdk deploy --all` orders them for you).

## Checklist

| Area | What to do |
|------|------------|
| Security | Keep the bucket private with OAC; use S3-managed or customer managed KMS encryption. Add a CloudFront web ACL for public sites. |
| Routing | Prefer `spaRewrite` when the distribution serves anything besides the SPA. |
| Caching | Cache fingerprinted assets for a long time, never cache `index.html`, invalidate only what changed. |
| Deployment | Deploy from CI with a [GitHub Actions OIDC role](github-actions-oidc.html) instead of access keys. |

## Related documentation

- [API Gateway V2 (HTTP API)](api-gateway-v2.html) - the API behind `/api/*`
- [S3 Quickstart](s3-quickstart.html) - bucket defaults
- [Certificate Manager](certificate-manager.html) - certificates for custom domains (CloudFront needs us-east-1)
*)
