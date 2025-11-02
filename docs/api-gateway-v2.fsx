(**
---
title: API Gateway V2 (HTTP API)
category: docs
index: 20
---

# API Gateway V2 (HTTP API)

Amazon API Gateway V2 HTTP APIs provide a low-latency, cost-effective way to build RESTful APIs.
HTTP APIs are up to 70% cheaper than REST APIs and are optimized for serverless workloads.

## Quick Start

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

Enable CORS for cross-origin requests from web applications.
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

Connect Lambda functions to HTTP API routes.
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
## Multiple Routes

Define multiple routes for different HTTP methods and paths.
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
## Custom Domain

Use a custom domain name for your API.

Note: Custom domains require a Certificate Manager certificate and Route53 hosted zone.
These must be created separately using the CDK API directly.
*)

(**
## API with Authorization

Add JWT or Lambda authorizers for API security.

Note: Authorizers must be added using the CDK HttpApi directly.
*)

(**
## Helper Functions

FsCDK provides helper functions for common HTTP API operations.
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
## Best Practices

### Performance

- Use HTTP APIs instead of REST APIs for lower latency (50% latency reduction)
- Enable payload compression for large responses
- Use regional endpoints for single-region deployments
- Consider edge-optimized endpoints for global APIs

### Security

- Enable CORS only for trusted origins in production
- Use JWT authorizers for token-based authentication
- Use Lambda authorizers for custom authentication logic
- Enable AWS WAF for DDoS protection and rate limiting
- Use API keys for partner integrations
- Enable CloudWatch logging for security monitoring

### Cost Optimization

- HTTP APIs are 70% cheaper than REST APIs
- Use pay-per-request billing (no minimum fees)
- Cache responses at the integration level when possible
- Monitor invocation metrics to optimize cold starts

### Reliability

- Configure Lambda reserved concurrency to prevent throttling
- Set appropriate Lambda timeouts (max 30s for HTTP APIs)
- Use DLQ for failed invocations
- Enable CloudWatch alarms for 4xx and 5xx errors
- Implement circuit breaker patterns for downstream dependencies

### Operational Excellence

- Use descriptive API names and descriptions
- Document routes and request/response schemas
- Tag APIs with project and environment
- Enable access logging to CloudWatch
- Monitor API Gateway metrics (requests, latency, errors)
- Version your APIs using paths (/v1/, /v2/)

## HTTP API vs REST API

| Feature | HTTP API | REST API |
|---------|----------|----------|
| **Cost** | 70% cheaper | More expensive |
| **Latency** | ~50% lower | Higher |
| **Native CORS** | Yes | No (requires OPTIONS method) |
| **WebSocket** | Yes | No |
| **Request Validation** | No | Yes |
| **API Keys** | No | Yes |
| **Usage Plans** | No | Yes |
| **Best For** | Serverless, microservices | Complex APIs with validation |

**Recommendation**: Use HTTP APIs for most serverless workloads. Use REST APIs only if you need request validation, API keys, or usage plans.

## Default Settings

The HTTP API builder applies these defaults:

- **CORS**: Disabled by default (opt-in for specific origins)
- **Auto-deploy**: Enabled (automatic stage deployment)
- **Stage**: $default stage created automatically
- **Throttling**: AWS defaults (10,000 requests/second burst, 5,000 steady state)

## Escape Hatch

For advanced scenarios, access the underlying CDK HttpApi:

`fsharp
let apiResource = httpApi "my-api" { description "My API" }

// Access the CDK HttpApi for advanced configuration
let cdkApi = apiResource.Api

// Add routes with integrations
let integration = HttpLambdaIntegration("Integration", lambdaFunc)
cdkApi.AddRoutes(HttpRouteOptions(
    Path = "/users",
    Methods = [| HttpMethod.GET |],
    Integration = integration
))

// Add authorizer
let authorizer = HttpJwtAuthorizer("JwtAuthorizer", "https://cognito-idp.region.amazonaws.com/poolId",
    JwtAudience = [| "client-id" |]
)
`

## Resources

- [API Gateway V2 HTTP API Documentation](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api.html)
- [HTTP API vs REST API](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-vs-rest.html)
- [CORS Configuration](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-cors.html)
- [JWT Authorizers](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-jwt-authorizer.html)
*)
