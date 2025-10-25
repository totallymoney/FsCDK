(**
---
title: Load Balancer, Secrets Manager, and Route 53 Example
category: docs
index: 7
---

# Load Balancer, Secrets Manager, and Route 53 Example

This example demonstrates how to use Application Load Balancer, Secrets Manager, and Route 53 with FsCDK.

## Application Load Balancer (ALB)
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.ElasticLoadBalancingV2
open FsCDK

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "ALBStack" {
    app { context [ "environment", "production" ] }

    environment {
        account config.Account
        region config.Region
    }

    description "Application Load Balancer example"

    // Create VPC
    let myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    // Create internet-facing ALB
    applicationLoadBalancer "MyALB" {
        vpc myVpc
        internetFacing true
        http2Enabled true
        dropInvalidHeaderFields true // Security best practice
    }
}

(**
## Secrets Manager
*)

open Amazon.CDK.AWS.SecretsManager

stack "SecretsStack" {
    app { context [ "environment", "production" ] }

    environment {
        account config.Account
        region config.Region
    }

    description "Secrets Manager example"

    // Create a secret with auto-generated password
    secret "MyDatabasePassword" {
        description "Database admin password"
        generateSecretString (SecretsManagerHelpers.generatePassword 32 None)
    }

    // Create a secret for API credentials (JSON format)
    secret "MyApiCredentials" {
        description "External API credentials"
        generateSecretString (SecretsManagerHelpers.generateJsonSecret """{"username": "admin"}""" "password")
    }
}

(**
## Route 53 (DNS)
*)

open Amazon.CDK.AWS.Route53

stack "DNSStack" {
    app { context [ "environment", "production" ] }

    environment {
        account config.Account
        region config.Region
    }

    description "Route 53 DNS example"

    // Create VPC and ALB first
    let myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    let myAlb =
        applicationLoadBalancer "MyALB" {
            vpc myVpc
            internetFacing true
        }

    // Create hosted zone
    let myZone = hostedZone "example.com" { comment "Production domain" }

    // Create A record pointing to ALB
    aRecord "www" {
        zone myZone.HostedZone
        target (Route53Helpers.albTarget myAlb.LoadBalancer)
        ttl (Duration.Minutes(5.0))
    }
}

(**
## Elastic Beanstalk
*)

open Amazon.CDK.AWS.ElasticBeanstalk

stack "BeanstalkStack" {
    app { context [ "environment", "production" ] }

    environment {
        account config.Account
        region config.Region
    }

    description "Elastic Beanstalk example"

    // Create Elastic Beanstalk application
    let myApp = ebApplication "MyWebApp" { description "My web application" }

    // Create environment for the application
    // Note: Solution stack name depends on your platform
    ebEnvironment "MyWebAppEnv" {
        applicationName myApp.ApplicationName
        solutionStackName "64bit Amazon Linux 2 v5.8.0 running Node.js 18"
        description "Production environment"
    }
}

(**
## Key Features

### Application Load Balancer
- **Internal by Default**: Enhanced security by not exposing to internet unless explicitly configured
- **HTTP/2 Support**: Enabled by default for better performance
- **Security Headers**: Drops invalid header fields to prevent injection attacks
- **High Availability**: Distributes traffic across multiple targets

### Secrets Manager
- **KMS Encryption**: All secrets encrypted at rest with KMS
- **Automatic Rotation**: Support for automatic secret rotation (opt-in)
- **Retention on Delete**: Secrets retained when stack is deleted (prevents data loss)
- **Helper Functions**: Easy password and JSON secret generation

### Route 53
- **DNS Management**: Create and manage hosted zones and record sets
- **Alias Records**: Native support for AWS resources (ALB, CloudFront)
- **DNSSEC Support**: Optional DNSSEC signing (opt-in)
- **Query Logging**: Optional query logging to CloudWatch

### Elastic Beanstalk
- **Platform-as-a-Service**: Simplified application deployment
- **Multiple Platforms**: Support for various programming languages and platforms
- **Auto-Scaling**: Built-in auto-scaling and load balancing
- **Monitoring**: Integrated with CloudWatch for monitoring

## Security Best Practices

1. **Application Load Balancer**:
   - Internal by default to prevent accidental exposure
   - Drops invalid HTTP headers to prevent attacks
   - Supports HTTPS/TLS termination

2. **Secrets Manager**:
   - All secrets encrypted with KMS
   - Access controlled via IAM policies
   - Secrets retained on deletion to prevent data loss
   - Automatic rotation available

3. **Route 53**:
   - DNSSEC available for DNS security
   - Query logging for security auditing
   - IAM-based access control

## Deployment

```bash
# Synthesize CloudFormation template
cdk synth

# Deploy to AWS
cdk deploy

# Destroy resources when done
cdk destroy
```
*)
