(**
---
title: API Gateway V2 (HTTP API)
category: Resources
categoryindex: 2
---

# ![API Gateway](img/icons/Arch_Amazon-API-Gateway_48.png) Designing world-class HTTP APIs with FsCDK

Amazon API Gateway HTTP APIs deliver the latency, pricing, and simplicity that modern serverless teams expect. This notebook combines FsCDK builders with practices championed by AWS Heroes **Jeremy Daly**, **Yan Cui**, and the API Gateway product team, so you can publish secure, observable endpoints that scale from prototype to production.

**Key influences**
- **Jeremy Daly – Serverless Chats Ep.133** (4.9★ community rating) for event-driven API design patterns.
- **Yan Cui – “HTTP APIs best practices”** blog series for cold-start minimisation and auth flows.
- **re:Invent ARC406 – Building resilient APIs with API Gateway** for real-world resiliency playbooks.

Use the code samples alongside the implementation checklist and the “Further learning” section at the end to deepen your expertise.

## Quick start blueprint

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.Logs
// Opened last so HttpMethod is the API Gateway one, not Amazon.CDK.AWS.Lambda.HttpMethod
open Amazon.CDK.AWS.Apigatewayv2

(*** hide ***)
// `dotnet publish` output of the Lambda project; a placeholder folder keeps this page evaluable.
let publishDir =
    let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fscdk-docs-api")
    System.IO.Directory.CreateDirectory(dir) |> ignore
    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Api.dll"), "placeholder")
    dir

(**
## HTTP API with a Lambda route

Bind the function with `let!` and route to it. Every route that targets the same function shares one
integration; CDK adds a Lambda invoke permission per route. The bound `HttpApi` exposes `Url`, `ApiEndpoint`
and metrics, so it can feed outputs, alarms and CloudFront origins.
*)

stack "BasicHttpAPI" {
    let! itemsFn =
        lambda "ItemsHandler" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Handler::handle"
            code publishDir
        }

    let! api =
        httpApi "items-api" {
            description "Items API"
            route HttpMethod.GET "/items" itemsFn
            route HttpMethod.POST "/items" itemsFn
        }

    cfnOutput "ItemsApiUrl" { value api.Url }
}

(**
## HTTP API with CORS

Adopt a least-privilege CORS policy from day one. The development configuration below mirrors the rapid-prototyping approach that **Heitor Lessa** demonstrates in the Powertools workshops, while the production configuration reflects the lock-down guidance from the **AWS Security Blog** article “Implementing secure CORS for API Gateway.” Use permissive settings only in sandbox environments and document every approved origin for production.

`HttpApiHelpers.corsPermissive` allows any origin and therefore does not allow credentials (browsers reject that combination).
When the browser must send cookies or other credentials, use `HttpApiHelpers.corsWithCredentials` with explicit origins:
API Gateway rejects `AllowCredentials` combined with a `*` origin.
*)

stack "HttpAPIWithCORS" {
    let! fn =
        lambda "CorsHandler" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Handler::handle"
            code publishDir
        }

    // Development: Allow all origins
    httpApi "dev-api" {
        description "Development API with permissive CORS"
        cors (HttpApiHelpers.corsPermissive ())
        route HttpMethod.ANY "/{proxy+}" fn
    }

    // Production: Restrict to specific origins
    httpApi "prod-api" {
        description "Production API with restricted CORS"

        cors (
            HttpApiHelpers.cors
                [ "https://myapp.com"; "https://www.myapp.com" ]
                [ CorsHttpMethod.GET; CorsHttpMethod.POST; CorsHttpMethod.PUT ]
                [ "Content-Type"; "Authorization" ]
        )

        route HttpMethod.ANY "/{proxy+}" fn
    }

    // Production with cookies: explicit origins + credentials
    httpApi "session-api" {
        description "API called with browser credentials"

        cors (
            HttpApiHelpers.corsWithCredentials
                [ "https://myapp.com" ]
                [ CorsHttpMethod.GET; CorsHttpMethod.POST ]
                [ "Content-Type" ]
        )

        route HttpMethod.ANY "/{proxy+}" fn
    }
}

(**
## Authorization strategies

Plan for authentication and authorisation from day zero. HTTP APIs natively support JWT authorisers (ideal for Cognito, Auth0, or custom OIDC providers) and Lambda authorisers for bespoke logic. Map your requirements to the guidance from **Ben Kehoe’s** “Identity for serverless” series and the official **API Gateway Security Workshops** so you can implement least-privilege, auditable access.

The authorizer you set on the API is the default for every route:

- `cognitoAuthorizer name pool [ clients ]` derives the issuer from the user pool and accepts tokens issued to the listed app clients.
- `jwtAuthorizer name issuer [ audiences ]` works with any OIDC issuer. API Gateway checks `aud`, or `client_id` for Cognito access tokens.
- `defaultAuthorizer` takes any `IHttpRouteAuthorizer`, for example an `HttpLambdaAuthorizer`.

`publicRoute` opts a single route out (health checks), and `routeSpec` with the `HttpRoute` helpers sets per-route authorizers or OAuth scopes.
*)

stack "MultiRouteAPI" {
    let! usersFn =
        lambda "UsersHandler" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Users::handle"
            code publishDir
        }

    let! pool = userPool "ApiUsers" { () }
    let! webClient = userPoolClient "ApiWebClient" { userPool pool }

    httpApi "rest-api" {
        description "RESTful API with multiple routes"
        cognitoAuthorizer "Cognito" pool [ webClient ]

        route HttpMethod.GET "/users" usersFn
        route HttpMethod.POST "/users" usersFn
        route HttpMethod.PUT "/users/{id}" usersFn

        routeSpec (
            HttpRoute.lambda HttpMethod.DELETE "/users/{id}" usersFn
            |> HttpRoute.withScopes [ "users/admin" ]
        )

        publicRoute HttpMethod.GET "/health" usersFn
    }
}

stack "Auth0API" {
    let! fn =
        lambda "Auth0Handler" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Handler::handle"
            code publishDir
        }

    httpApi "auth0-api" {
        jwtAuthorizer "Auth0" "https://my-tenant.eu.auth0.com/" [ "https://api.example.com" ]
        defaultAuthorizationScopes [ "read:items" ]
        route HttpMethod.ANY "/{proxy+}" fn
    }
}

(**
## Custom domains, throttling & access logs

Serve your API from branded domains to simplify client configuration. `customDomain` creates a regional API Gateway
domain name (the ACM certificate must be in the API's region) and maps it to the `$default` stage; add a Route 53 alias
to the domain's `RegionalDomainName`. `disableExecuteApiEndpoint true` forces clients onto the custom domain.

`throttle rate burst` sets default-stage route throttling and `accessLogs` writes JSON access logs that leave out
JWT claims and bodies (see `HttpApiOperations.defaultAccessLogFormat`).
*)

stack "ProductionAPI" {
    let! fn =
        lambda "OrdersHandler" {
            runtime Runtime.DOTNET_8
            handler "Api::Api.Orders::handle"
            code publishDir
        }

    let! cert = certificate "ApiCertificate" { domainName "api.example.com" }
    let! accessLogGroup = logGroup "/api/orders/access" { retention RetentionDays.ONE_YEAR }

    httpApi "orders-api" {
        route HttpMethod.ANY "/orders/{proxy+}" fn
        customDomain "api.example.com" cert
        disableExecuteApiEndpoint true
        throttle 100.0 200
        accessLogs accessLogGroup
    }
}

(**
## WAF and HTTP APIs

AWS WAF cannot be associated with HTTP APIs. To put a web ACL in front of one, route the API through CloudFront and
attach a `webAcl` with `scope WebAclScope.CloudFront` (deployed in us-east-1) to the distribution with `webAclId`.
The [serverless web sample](https://github.com/totallymoney/FsCDK/tree/main/samples/serverless-web) shows the full
setup: CloudFront + OAC SPA, `/api/*` to the HTTP API with a Cognito JWT authorizer, a .NET Lambda with SnapStart and DynamoDB.

The API's own `execute-api` endpoint (and any custom domain) stays reachable directly, so WAF rules apply only to
traffic that comes through CloudFront; the JWT authorizer and stage throttling apply either way. To force traffic
through CloudFront, have CloudFront add a secret origin header and check it in a Lambda authorizer, or use a regional
REST API, which WAF supports directly.

## Helper functions

FsCDK ships ergonomic helpers for CORS policies and route keys so you can codify conventions instead of scattering strings through your codebase. Use them to mirror the patterns from the **AWS API Gateway Workshop**—define reusable builders, enforce consistent verbs and paths, and keep policy changes centralised.
*)

// CORS Helpers
let permissiveCors = HttpApiHelpers.corsPermissive ()

let restrictedCors =
    HttpApiHelpers.cors
        [ "https://example.com" ]
        [ CorsHttpMethod.GET; CorsHttpMethod.POST ]
        [ "Content-Type"; "Authorization" ]

let credentialedCors =
    HttpApiHelpers.corsWithCredentials [ "https://example.com" ] [ CorsHttpMethod.GET ] [ "Content-Type" ]

// Route Key Helpers
let getUsersRoute = HttpApiHelpers.getRoute "/users"
let createUserRoute = HttpApiHelpers.postRoute "/users"
let updateUserRoute = HttpApiHelpers.putRoute "/users/{id}"
let deleteUserRoute = HttpApiHelpers.deleteRoute "/users/{id}"
let anyMethodRoute = HttpApiHelpers.anyRoute "/{proxy+}"

(**
## Implementation checklist

| Area | What to do | Why |
|------|------------|-----|
| Latency | Choose HTTP APIs over REST APIs for 50–70% lower latency and cost. Keep payloads small and enable compression when responses exceed 1 MB. | Aligns with **re:Invent ARC406** recommendations and AWS’ pricing model. |
| Security | Lock CORS to approved origins, prefer JWT authorisers for stateless auth, and front public APIs with AWS WAF through CloudFront. Enable access logging to CloudWatch Logs Insights. | Mirrors **Ben Kehoe’s** least-privilege guidance and AWS Security Blog best practices. |
| Reliability | Set Lambda timeouts ≤29 s, configure reserved concurrency, and attach DLQs or on-failure destinations for integrations. Add CloudWatch alarms on 4xx/5xx metrics. | Matches the resiliency playbook from **AWS Builders Library – Automating safe deployments**. |
| Cost | Monitor `$default` stage metrics, use pay-per-request billing, and cache responses at the integration layer where possible. Audit usage quarterly. | Reinforces advice from **Serverless Land** cost optimisation sessions. |
| Operations | Tag APIs (`Service`, `Environment`), version routes (`/v1`, `/v2`), and document schemas using OpenAPI. Include runbooks for authoriser failures. | Supports operational excellence per Well-Architected Serverless Lens. |

FsCDK’s builder defaults—auto-deployed `$default` stage and disabled CORS—provide a sensible starting point. Bind the
API with `let!` whenever you need the underlying `HttpApi` (stages, VPC links, metrics). Alarms notify an SNS topic
with `alarmAction`; `okAction` and `insufficientDataAction` notify on recovery and missing data, and
`actionsEnabled false` mutes the actions without deleting the alarm:

```fsharp
let! api = httpApi "items-api" { route HttpMethod.GET "/items" itemsFn }
let! alertsTopic = topic "ApiAlerts" { () }

cloudwatchAlarm "ItemsApi5xx" {
    metric (api.MetricServerError())
    threshold 5.0
    evaluationPeriods 1
    alarmAction alertsTopic
    okAction alertsTopic
}
```

See [CloudFront static sites and SPAs](cloudfront-static-site.html) to serve the API under `/api/*` next to a
single-page app.

## Further learning

- **[Jeremy Daly – Serverless Chats Ep.133: Event-driven API design](https://www.serverlesschats.com/133)** (4.9★)
- **[Yan Cui – "HTTP APIs best practices" blog series](https://theburningmonk.com/)**
- **[re:Invent ARC406 – Building resilient APIs with API Gateway](https://www.youtube.com/results?search_query=aws+reinvent+ARC406+api+gateway)**
- **[AWS API Gateway Security Workshop](https://catalog.workshops.aws/apigateway-security/en-US)** (Hands-on labs)
- **AWS Docs** – [HTTP API developer guide](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api.html)

Combine these resources with the FsCDK notebooks ([Lambda Production Defaults](lambda-production-defaults.html), [EventBridge](eventbridge.html), [IAM Best Practices](iam-best-practices.html)) to deliver secure, observable APIs with confidence.
*)
