(**
---
title: Cognito OAuth 2.0 Machine-to-Machine (M2M)
category: 3. Resources
categoryindex: 7
---

# OAuth 2.0 Machine-to-Machine Authentication with Cognito

Implement secure service-to-service authentication using Cognito User Pools, OAuth 2.0 scopes, and Lambda authorizers.

## OAuth 2.0 M2M Flow

<pre class="mermaid">
sequenceDiagram
    participant Service as Service A<br/>(Client)
    participant Cognito as Cognito<br/>User Pool
    participant API as API Gateway<br/>+ Lambda Authorizer
    participant Backend as Service B<br/>(Protected Resource)
    
    Service->>Cognito: 1. POST /oauth2/token<br/>grant_type=client_credentials<br/>client_id & client_secret
    
    Cognito->>Cognito: 2. Validate credentials<br/>Check scopes
    
    Cognito->>Service: 3. Return Access Token<br/>JWT with scopes
    
    Service->>API: 4. Request with<br/>Authorization: Bearer {token}
    
    API->>API: 5. Lambda Authorizer<br/>Validate JWT signature<br/>Check scopes & claims
    
    alt Token Valid
        API->>Backend: 6. Forward Request<br/>with user context
        Backend->>Backend: 7. Process Request
        Backend->>API: 8. Response
        API->>Service: 9. Return Response
    else Token Invalid
        API->>Service: 401 Unauthorized
    end
    
    Note over Service,Cognito: OAuth 2.0 Client Credentials Flow
    Note over API,Backend: Scope-based authorization
</div>

## Architecture Overview

**Traditional User Auth:** User → Cognito → JWT → API Gateway → Lambda
**M2M Auth:** Service → OAuth Client Credentials → Access Token → API Gateway → Lambda

## OAuth Resource Server with Custom Scopes

Define business-specific scopes for your API:

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.Cognito
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.IAM

(**
## Basic M2M Setup

Create a Cognito User Pool with OAuth resource server for machine-to-machine authentication:
*)

stack "M2MAuth" {
    // User Pool for both user and M2M authentication
    let myUserPool =
        userPool "AppUserPool" {
            signInWithEmail
            selfSignUpEnabled false // M2M clients created administratively
        }

    // Define OAuth resource server with custom scopes
    resourceServer "ApiResourceServer" {
        userPool myUserPool
        identifier "api"
        name "API Resource Server"
        scope "read" "Read access to resources"
        scope "write" "Write access to resources"
        scope "admin" "Administrative operations"
    }

    // Create M2M client
    userPoolClient "ServiceClient" {
        userPool myUserPool
        generateSecret true // Required for client credentials flow

        authFlows (
            AuthFlow(
                UserSrp = false,
                UserPassword = false,
                AdminUserPassword = false
            // Custom = true // For client_credentials grant
            )
        )

        // Short-lived tokens for services
        tokenValidities (
            (Duration.Days 30.0), // refreshToken (not used in client_credentials)
            (Duration.Hours 1.0), // accessToken
            (Duration.Hours 1.0) // idToken
        )
    }
}

(**
## Resource Server with Multiple Scopes

Add more scopes to support different permission levels:
*)

stack "CompleteM2MOAuth" {
    let myUserPool =
        userPool "AppUserPool" {
            signInWithEmail
            selfSignUpEnabled false
        }

    // Resource server with granular scopes
    resourceServer "ApiResourceServer" {
        userPool myUserPool
        identifier "api"
        scope "read" "Read access to resources"
        scope "write" "Write access to resources"
        scope "delete" "Delete access to resources"
        scope "admin" "Administrative operations"
        scope "execute" "Execute business transactions"
    }
}

(**

## Lambda Authorizer for Dual Authentication

Support both user JWT tokens and M2M access tokens:
*)

stack "DualAuthAPI" {
    // Lambda authorizer supporting both token types
    lambda "ApiAuthorizer" {
        runtime Runtime.PYTHON_3_11
        handler "authorizer.handler"
        code "./authorizer"
        timeout 30.0

        environment [ "USER_POOL_ID", "us-east-1_XXXXX"; "REGION", "us-east-1" ]

        // IAM permissions for Cognito
        policyStatement {
            effect Effect.ALLOW
            actions [ "cognito-idp:DescribeUserPool" ]
            resources [ "arn:aws:cognito-idp:us-east-1:123456789012:userpool/*" ]
        }
    }
}

(**
## Authorizer Implementation Pattern

Lambda authorizer implementation to handle both user JWT and M2M access tokens.

**Note:** Python example shown below as Lambda authorizers commonly use Python/Node.js for JWT validation. The FsCDK builder above shows how to deploy this Lambda function.

```python
import jwt
import requests
from typing import Dict, Any

def lambda_handler(event: Dict[str, Any], context) -> Dict[str, Any]:
    token = extract_token(event)
    
    # Decode without verification to check token type
    unverified = jwt.decode(token, options={"verify_signature": False})
    
    if unverified.get('token_use') == 'access':
        # M2M access token flow
        claims = validate_access_token(token)
        context = extract_m2m_context(claims)
    elif unverified.get('token_use') == 'id':
        # User JWT token flow
        claims = validate_id_token(token)
        context = extract_user_context(claims)
    else:
        raise Exception('Unknown token type')
    
    return generate_policy(context)

def extract_m2m_context(claims):
    """Extract tenant and permissions from M2M token"""
    client_id = claims['client_id']
    scopes = claims.get('scope', '').split()
    
    # Map scopes to application roles
    roles = []
    if 'api/admin' in scopes:
        roles.append('admin')
    if 'api/write' in scopes:
        roles.append('write')
    if 'api/read' in scopes:
        roles.append('read')
    
    return {
        'clientId': client_id,
        'roles': ','.join(roles),
        'authType': 'm2m'
    }
```

## Obtaining M2M Access Tokens

Services request tokens using client credentials:

```bash
# Token endpoint
curl -X POST https://mydomain.auth.us-east-1.amazoncognito.com/oauth2/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=YOUR_CLIENT_ID" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=api/read api/write"

# Response
{
  "access_token": "eyJraWQiOiJ...",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

## Using M2M Tokens

```bash
# Call API with M2M token
curl https://api.example.com/resources \
  -H "Authorization: Bearer eyJraWQiOiJ..."
```

## Scope-to-Role Mapping Strategy

Map OAuth scopes to application-specific business roles:

| OAuth Scope | Application Role | Permissions |
|-------------|------------------|-------------|
| `api/read` | `viewer` | GET operations only |
| `api/write` | `editor` | GET, POST, PUT operations |
| `api/admin` | `administrator` | All operations including DELETE |
| `api/execute` | `executor` | Execute business transactions |

## Token Validation Best Practices

```python
def validate_access_token(token):
    """Validate M2M access token with Cognito"""
    # Download Cognito JWKS
    jwks_url = f'https://cognito-idp.{REGION}.amazonaws.com/{USER_POOL_ID}/.well-known/jwks.json'
    jwks = requests.get(jwks_url).json()
    
    # Validate signature, issuer, expiration
    claims = jwt.decode(
        token,
        jwks,
        algorithms=['RS256'],
        issuer=f'https://cognito-idp.{REGION}.amazonaws.com/{USER_POOL_ID}'
    )
    
    # Verify token_use
    if claims.get('token_use') != 'access':
        raise Exception('Invalid token type')
    
    # Verify not expired
    if claims['exp'] < time.time():
        raise Exception('Token expired')
    
    return claims
```

## Security Considerations

### M2M Client Management

- Store client secrets in AWS Secrets Manager (never in code)
- Rotate client secrets regularly (90-day rotation)
- Use separate clients per service/environment
- Implement client credential monitoring and alerts

### Token Security

- **Short-lived tokens** - 1 hour max for access tokens
- **Scope principle of least privilege** - Only grant needed scopes
- **Token monitoring** - Log all token requests and usage
- **Revocation strategy** - Ability to revoke compromised clients

### API Gateway Integration

```fsharp
// API Gateway method with authorizer
// (Configuration varies by gateway type - REST vs HTTP)
```

## Cost Considerations

Cognito M2M pricing:

- **User Pool** - Free tier: 50,000 MAU
- **M2M clients** - $0.05 per MAU (Monthly Active User)
- **Token operations** - Included in pricing
- **Typical cost** - ~$6/month per service client

## Monitoring M2M Authentication

CloudWatch metrics to track:

- Token request rate per client
- Failed authentication attempts
- Token validation errors
- Scope usage patterns

## Complete M2M Example

```fsharp
// 1. Create User Pool with resource server (see above)
// 2. Create M2M client with client_credentials flow
// 3. Deploy Lambda authorizer
// 4. Configure API Gateway to use authorizer
// 5. Services request tokens and call API
```

## Migration from API Keys

If migrating from API key authentication:

| API Keys | OAuth M2M |
|----------|-----------|
| Manual rotation | Automatic expiration |
| Broad access | Granular scopes |
| Simple but risky | Secure but complex |
| No usage attribution | Client-level tracking |

**Migration strategy:**
1. Implement M2M alongside API keys
2. Migrate services one-by-one
3. Monitor for 30 days
4. Deprecate API keys

## Resources

- [Cognito OAuth 2.0](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-user-pools-app-integration.html)
- [OAuth 2.0 Client Credentials](https://datatracker.ietf.org/doc/html/rfc6749#section-4.4)
- [API Gateway Lambda Authorizers](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-use-lambda-authorizer.html)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
*)
