(**
---
title: API Gateway V2 (HTTP API)
category: 3. Resources
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
open Amazon.CDK.AWS.Apigatewayv2
//open Amazon.CDK.AWS.Apigatewayv2Integrations
open Amazon.CDK.AWS.Lambda

(**
## Basic HTTP API

Create a simple HTTP API with auto-deployment enabled by default.
*)

stack "BasicHttpAPI" {
    // Create the HTTP API
    let api = httpApi "my-api" { description "My HTTP API" }
    ()
}

(**
## HTTP API with CORS

Adopt a least-privilege CORS policy from day one. The development configuration below mirrors the rapid-prototyping approach that **Heitor Lessa** demonstrates in the Powertools workshops, while the production configuration reflects the lock-down guidance from the **AWS Security Blog** article “Implementing secure CORS for API Gateway.” Use permissive settings only in sandbox environments and document every approved origin for production.
*)

stack "HttpAPIWithCORS" {
    // Development: Allow all origins
    let devApi =
        httpApi "dev-api" {
            description "Development API with permissive CORS"
            cors (HttpApiHelpers.corsPermissive ())
            ()
        }

    // Production: Restrict to specific origins
    let corsOptions =
        HttpApiHelpers.cors
            [ "https://myapp.com"; "https://www.myapp.com" ]
            [ CorsHttpMethod.GET; CorsHttpMethod.POST; CorsHttpMethod.PUT ]
            [ "Content-Type"; "Authorization" ]

    let prodApi =
        httpApi "prod-api" {
            description "Production API with restricted CORS"
            cors corsOptions
        }

    ()
}

(**
## HTTP API with Lambda Integration

FsCDK keeps the Lambda integration lightweight so you can focus on business logic. Follow the playbook shared in **re:Invent SVS402 – Deep dive on serverless APIs**: separate route definitions from function code, enable structured logging via Powertools, and surface integration metrics in CloudWatch. The snippet below establishes the foundation; pair it with reserved concurrency and DLQs as shown in [Lambda Production Defaults](lambda-production-defaults.html) to hit production-readiness quickly.
*)

stack "HttpAPIWithLambda" {
    // Create Lambda function
    let apiFunction =
        lambda "ApiHandler" {
            runtime Runtime.DOTNET_8
            handler "Api::Handler"
            code "./lambda"
            environment [ "API_VERSION", "v1" ]
            ()
        }

    // Create HTTP API
    let api =
        httpApi "users-api" {
            description "Users API with Lambda integration"
            cors (HttpApiHelpers.corsPermissive ())
        }

    // Note: Route integrations must be added using the CDK HttpApi directly
    // Example:
    //   let integration = HttpLambdaIntegration("GetUsersIntegration", apiFunction.Function.Value)
    //   api.Api.AddRoutes(HttpRouteOptions(
    //       Path = "/users",
    //       Methods = [| HttpMethod.GET |],
    //       Integration = integration
    //   ))

    ()
}

(**
## Multiple routes & harmonised contracts

Model your REST surface explicitly so consumers always know which verbs and payloads are supported. This mirrors the contract-first workflow advocated by **Jeremy Daly** in his “EventBridge and API Gateway integration” talks—each route should map cleanly to a Lambda function or integration with clear telemetry. The code below introduces per-verb handlers; augment it with JSON schema validators or Lambda Powertools’ middleware for request/response shaping.
*)

stack "MultiRouteAPI" {
    // Create Lambda functions for different operations
    let getUsersFunc =
        lambda "GetUsers" {
            runtime Runtime.DOTNET_8
            handler "Api::GetUsers"
            code "./lambda"
            ()
        }

    let createUserFunc =
        lambda "CreateUser" {
            runtime Runtime.DOTNET_8
            handler "Api::CreateUser"
            code "./lambda"
        }

    let updateUserFunc =
        lambda "UpdateUser" {
            runtime Runtime.DOTNET_8
            handler "Api::UpdateUser"
            code "./lambda"
        }

    let deleteUserFunc =
        lambda "DeleteUser" {
            runtime Runtime.DOTNET_8
            handler "Api::DeleteUser"
            code "./lambda"
        }

    // Create HTTP API
    let api =
        httpApi "rest-api" {
            description "RESTful API with multiple routes"
            cors (HttpApiHelpers.corsPermissive ())
        }

    // Routes are added using CDK directly:
    // GET /users - List users
    // POST /users - Create user
    // PUT /users/{id} - Update user
    // DELETE /users/{id} - Delete user`
    ()
}

(**
## Custom domains & TLS

Serve your API from branded domains to simplify client configuration and enforce HSTS. Provision ACM certificates in the target region (us-east-1 for edge-optimised) and map them via Route 53 alias records, following the detailed steps in the **AWS Networking Blog** post “End-to-end TLS with API Gateway.”
*)

(**
## Authorization strategies

Plan for authentication and authorisation from day zero. HTTP APIs natively support JWT authorisers (ideal for Cognito, Auth0, or custom OIDC providers) and Lambda authorisers for bespoke logic. Map your requirements to the guidance from **Ben Kehoe’s** “Identity for serverless” series and the official **API Gateway Security Workshops** so you can implement least-privilege, auditable access.
*)

(**
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
| Latency | Choose HTTP APIs over REST APIs for 50–70% lower latency and cost. Keep payloads small and enable compression when responses exceed 1 MB. | Aligns with **re:Invent ARC406** recommendations and AWS’ pricing model. |
| Security | Lock CORS to approved origins, prefer JWT authorisers for stateless auth, and front public APIs with AWS WAF. Enable access logging to CloudWatch Logs Insights. | Mirrors **Ben Kehoe’s** least-privilege guidance and AWS Security Blog best practices. |
| Reliability | Set Lambda timeouts ≤29 s, configure reserved concurrency, and attach DLQs or on-failure destinations for integrations. Add CloudWatch alarms on 4xx/5xx metrics. | Matches the resiliency playbook from **AWS Builders Library – Automating safe deployments**. |
| Cost | Monitor `$default` stage metrics, use pay-per-request billing, and cache responses at the integration layer where possible. Audit usage quarterly. | Reinforces advice from **Serverless Land** cost optimisation sessions. |
| Operations | Tag APIs (`Service`, `Environment`), version routes (`/v1`, `/v2`), and document schemas using OpenAPI. Include runbooks for authoriser failures. | Supports operational excellence per Well-Architected Serverless Lens. |

FsCDK’s builder defaults—auto-deployed `$default` stage, throttling, and disabled CORS—provide a sensible starting point. Use the escape hatch (`api.Api`) whenever you need to customise integrations, authorisers, or stages.

```fsharp
// Example escape hatch usage (schema validation, authorisers, etc.)
// let integration = HttpLambdaIntegration("UsersIntegration", handler.Function.Value)
// api.Api.AddRoutes(HttpRouteOptions(Path = "/users", Methods = [| HttpMethod.GET |], Integration = integration))
```

## Further learning

- **[Jeremy Daly – Serverless Chats Ep.133: Event-driven API design](https://www.serverlesschats.com/133)** (4.9★)
- **[Yan Cui – "HTTP APIs best practices" blog series](https://theburningmonk.com/)**
- **[re:Invent ARC406 – Building resilient APIs with API Gateway](https://www.youtube.com/results?search_query=aws+reinvent+ARC406+api+gateway)**
- **[AWS API Gateway Security Workshop](https://catalog.workshops.aws/apigateway-security/en-US)** (Hands-on labs)
- **AWS Docs** – [HTTP API developer guide](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api.html)

Combine these resources with the FsCDK notebooks ([Lambda Production Defaults](lambda-production-defaults.html), [EventBridge](eventbridge.html), [IAM Best Practices](iam-best-practices.html)) to deliver secure, observable APIs with confidence.
*)
