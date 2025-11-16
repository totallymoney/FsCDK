(**
---
title: EC2 and ECS Example
category: docs
index: 8
---

# Amazon EC2 and ECS (Elastic Container Service) Example

This example demonstrates how to create Amazon EC2 (Elastic Compute Cloud) instances and ECS (Elastic Container Service) services using FsCDK.

## EC2 Instance (Virtual Machine)
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.EC2
open FsCDK

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "EC2Stack" {
    app { context [ "environment", "production" ] }

    environment {
        account config.Account
        region config.Region
    }

    description "EC2 instance example"

    // Create VPC first
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    // Create EC2 instance with secure defaults
    ec2Instance "MyWebServer" {
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        machineImage (MachineImage.LatestAmazonLinux2())
        vpc myVpc
        requireImdsv2 true // IMDSv2 for enhanced security
        detailedMonitoring false
    }

    ()
}

(**
## ECS Cluster and Fargate Service
*)

open Amazon.CDK.AWS.ECS

stack "ECSStack" {
    app { context [ "environment", "production" ] }

    description "ECS cluster with Fargate service"

    // Create VPC
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 1
            cidr "10.0.0.0/16"
        }

    // Create ECS cluster
    let myCluster =
        ecsCluster "MyCluster" {
            vpc myVpc
            containerInsights ContainerInsights.ENABLED
            enableFargateCapacityProviders true
        }

    ()
}

(**
## Key Features

### EC2 (Virtual Machines)
- **IMDSv2 Required**: Enhanced security for instance metadata by default
- **EBS Encryption**: Enabled by default for data-at-rest protection
- **Cost-Effective Defaults**: t3.micro instance type for dev/test workloads
- **Flexible Configuration**: Support for custom instance types, AMIs, and user data

### ECS (Container Orchestration)
- **Container Insights**: Enabled by default for monitoring and observability
- **Fargate Support**: Serverless container execution
- **Private by Default**: Services don't get public IPs unless explicitly configured
- **Best Practices**: Follows AWS Well-Architected Framework principles

## Security Best Practices

1. **EC2 Instances**:
   - IMDSv2 is required by default to prevent SSRF attacks
   - EBS volumes are encrypted by default
   - Detailed monitoring is opt-in to control costs

2. **ECS Services**:
   - Services are private by default (no public IP)
   - Container Insights enabled for security monitoring
   - Follows principle of least privilege

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
