(**
---
title: AWS Cost Optimization Strategies
category: Best Practices
categoryindex: 1
---

# AWS Cost Optimization Strategies

![AWS Cost Optimization](img/AWS-cost-optimization.png)

Price estimates based on November 2025.
Practical approaches to reduce infrastructure costs while maintaining security and functionality.

## Environment-Specific Cost Tiers

Different environments have different cost/security trade-offs:

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.RDS

(**
## Cost-Optimized Development Environment

Minimize costs in development without sacrificing functionality:
*)

stack "DevEnvironment" {
    // Minimal VPC - single NAT or no NAT
    let devVpc =
        vpc "DevVPC" {
            maxAzs 2
            natGateways 0 // Save ~$32/month - use public subnets
            cidr "10.0.0.0/16"
        }

    // Smallest viable database
    rdsInstance "DevDatabase" {
        vpc devVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))
        databaseName "devdb"

        // Cost optimizations
        multiAz false // Save ~50% on RDS
        backupRetentionDays 1.0 // Minimal backups
        deletionProtection false // Easy to tear down

        // Security maintained
        storageEncrypted true
        publiclyAccessible false

        removalPolicy RemovalPolicy.DESTROY
    }
}

(**
**Dev Environment Cost: ~$20-40/month**

| Service | Cost Saving |
|---------|-------------|
| NAT Gateway removal | -$32/month |
| Single-AZ RDS | -$25/month |
| No VPC Endpoints | -$7-15/month |
| Smaller instances | -$20-50/month |

## Production Environment with Security

Full security and high availability:
*)

stack "ProductionEnvironment" {
    // Production VPC with HA
    let prodVpc =
        vpc "ProdVPC" {
            maxAzs 3
            natGateways 2 // HA: one per AZ
            cidr "10.0.0.0/16"
        }

    // Production-grade database
    rdsInstance "ProdDatabase" {
        vpc prodVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE))
        databaseName "proddb"

        // High availability
        multiAz true
        backupRetentionDays 30.0

        // Production security
        deletionProtection true
        storageEncrypted true
        enablePerformanceInsights true

        removalPolicy RemovalPolicy.RETAIN
    }

    // VPC Endpoints for private AWS service access
    gatewayVpcEndpoint "S3Endpoint" {
        vpc prodVpc
        service GatewayVpcEndpointAwsService.S3
    }

    gatewayVpcEndpoint "DynamoDBEndpoint" {
        vpc prodVpc
        service GatewayVpcEndpointAwsService.DYNAMODB
    }
}

(**
**Production Environment Cost: ~$150-300/month**

## NAT Gateway Cost Analysis

NAT Gateways are often the largest single cost in VPC architecture:

| Configuration | Monthly Cost | Use Case |
|---------------|--------------|----------|
| **No NAT** | $0 | Dev with public subnets |
| **1 NAT Gateway** | ~$32 + data | Dev/staging |
| **2 NAT Gateways** | ~$64 + data | Production (HA) |
| **3+ NAT Gateways** | ~$96+ | Enterprise (multi-AZ HA) |

### Alternatives to NAT Gateways

1. **Public Subnets** - Lambda/ECS in public subnets (dev only)
2. **VPC Endpoints** - Private AWS service access without NAT
3. **AWS PrivateLink** - Direct service connections
4. **Proxy Instances** - Single t4g.nano as NAT (~$3/month)

## VPC Endpoint Cost Trade-offs

Interface VPC Endpoints cost ~$7-15/month each but save NAT data transfer costs:

| Scenario | NAT Cost | VPC Endpoint | Recommendation |
|----------|----------|--------------|----------------|
| **Low traffic** | $32 + $5/mo data | $7/mo fixed | Use NAT |
| **Medium traffic** | $32 + $20/mo data | $7/mo fixed | Use VPC Endpoint |
| **High traffic** | $32 + $50+/mo data | $7/mo fixed | Use VPC Endpoint |

Gateway endpoints (S3, DynamoDB) are **free** - always use them!

## Database Cost Optimization

### Instance Class Selection

| Workload | Dev | Staging | Production |
|----------|-----|---------|------------|
| **Low traffic** | t3.micro ($13) | t3.small ($25) | t3.medium ($50) |
| **Medium traffic** | t3.small ($25) | t3.medium ($50) | m5.large ($140) |
| **High traffic** | t3.medium ($50) | m5.large ($140) | r5.xlarge ($370) |

### Multi-AZ Cost Impact

- Single-AZ: Base cost
- Multi-AZ: **~2x base cost** (full instance standby)
- Use Multi-AZ only in production or staging

### Storage Optimization
*)
// Define VPC first
let myVpc =
    vpc "MyVpc" {
        maxAzs 2
        natGateways 1
        cidr "10.0.0.0/16"
    }

rdsInstance "OptimizedDB" {
    vpc myVpc
    postgresEngine

    // Auto-scaling storage - pay only for what you use
    allocatedStorage 20 // Start small (20GB minimum)
    maxAllocatedStorage 100 // Auto-scale up to 100GB as needed

    storageType StorageType.GP3 // 20% cheaper than gp2, better performance
}

(**
## Lambda Cost Optimization

Lambda with FsCDK production defaults is already optimized:

- **Reserved concurrency (10)** - Prevents runaway costs
- **Memory/CPU ratio** - Use Lambda Power Tuning to find optimal memory
- **Ephemeral storage** - 512 MB default (free tier)

### Lambda Cost Formula

**Cost = (Duration × Memory × Price) + Requests**

- Duration: Optimize cold starts and execution time
- Memory: More memory = more CPU = potentially faster (cheaper)
- Use Lambda Power Tuning to find sweet spot

## Load Balancer Cost Analysis

| Load Balancer | Monthly Cost | Use Case |
|---------------|--------------|----------|
| **Application LB** | ~$16 + data | HTTP/HTTPS traffic |
| **Network LB** | ~$16 + data | TCP/UDP traffic |
| **Classic LB** | ~$18 + data | Legacy (avoid) |

**Anti-pattern:** Using both ALB + NLB together (~$32/month)
**Better:** Use ALB only for HTTP/HTTPS APIs

## CloudWatch Costs

Reduce monitoring costs without losing visibility:

### Log Retention Strategy

| Environment | Retention | Cost Impact |
|-------------|-----------|-------------|
| **Dev** | 3-7 days | Minimal |
| **Staging** | 14 days | Low |
| **Production** | 30-90 days | Moderate |

### Custom Metrics

- CloudWatch custom metrics: **$0.30/metric/month**
- Reduce cardinality: Use dimensions wisely
- Consider metric sampling for high-volume metrics

```fsharp
// ❌ BAD: Too many unique metric combinations
CloudWatch.putMetric("RequestCount", dimensions = [
    ("UserId", userId),        // High cardinality!
    ("Endpoint", endpoint),
    ("Method", method)
])

// ✅ GOOD: Aggregate high-cardinality dimensions
CloudWatch.putMetric("RequestCount", dimensions = [
    ("Endpoint", endpoint),    // Low cardinality
    ("Method", method)
])
```

## S3 Cost Optimization

### Storage Classes

| Class | Cost/GB | Retrieval | Use Case |
|-------|---------|-----------|----------|
| **Standard** | $0.023 | Free | Active data |
| **IA** | $0.0125 | $0.01/GB | >30 days old |
| **Glacier** | $0.004 | Hours | >90 days old |

### Lifecycle Policies

*)
bucket "CostOptimizedStorage" {
    versioned true

    yield
        lifecycleRule {
            enabled true

            transitions
                [ transition {
                      storageClass Amazon.CDK.AWS.S3.StorageClass.INFREQUENT_ACCESS
                      transitionAfter (Duration.Days 30.0)
                  }
                  transition {
                      storageClass Amazon.CDK.AWS.S3.StorageClass.GLACIER
                      transitionAfter (Duration.Days 90.0)
                  } ]

            // Delete old versions
            noncurrentVersionExpiration (Duration.Days 90.0)
        }
}
(**

## Environment Cost Comparison

| Component | Dev | Staging | Production |
|-----------|-----|---------|------------|
| **VPC (NAT)** | $0 | $32 | $64 |
| **RDS** | $13 | $50 | $100+ |
| **Lambda** | $5 | $20 | $50+ |
| **ALB** | $16 | $16 | $16 |
| **S3** | $2 | $5 | $20+ |
| **CloudWatch** | $2 | $5 | $20+ |
| **Total** | **~$40** | **~$130** | **~$270+** |

## Cost Monitoring and Alerts

Set up budget alerts for each environment:

```fsharp
// Use AWS Budgets to alert on cost thresholds
// Dev: Alert at $50
// Staging: Alert at $150  
// Production: Alert at $300
```

## Quick Wins Checklist

- [ ] Remove NAT Gateways in dev environments
- [ ] Use t3.micro RDS instances in dev
- [ ] Single-AZ RDS in non-production
- [ ] S3 lifecycle policies on all buckets
- [ ] Gateway VPC Endpoints for S3/DynamoDB
- [ ] 7-day log retention in dev
- [ ] Reserved concurrency on all Lambdas
- [ ] Delete unused resources regularly

## Common Cost Anti-Patterns

### ❌ Don't Do This

1. **Multi-AZ in dev** - Wastes 50% on RDS
2. **NAT Gateways everywhere** - $32/month per gateway
3. **Interface VPC Endpoints without traffic analysis** - $7-15/month each
4. **Oversized RDS instances** - Start small, scale up
5. **Infinite log retention** - Use 7-30 days max
6. **No resource tagging** - Can't track costs per project
7. **Unused load balancers** - $16/month even with no traffic

### ✅ Do This Instead

1. Single-AZ in dev/staging
2. Public subnets in dev (no NAT)
3. Gateway endpoints (free) first, interface endpoints only if high traffic
4. Right-size instances based on CloudWatch metrics
5. 7-day retention in dev, 30 days in prod
6. Tag everything: `Environment`, `Project`, `Owner`
7. Delete unused resources in automated cleanup

## Monthly Cost Targets

Set realistic cost targets per environment:

- **Sandbox/POC:** $10-30/month
- **Development:** $40-80/month
- **Staging:** $100-150/month
- **Production (small):** $200-500/month
- **Production (medium):** $500-2000/month

## Resources

- [AWS Cost Optimization](https://aws.amazon.com/pricing/cost-optimization/)
- [AWS Well-Architected Cost Optimization Pillar](https://docs.aws.amazon.com/wellarchitected/latest/cost-optimization-pillar/welcome.html)
- [AWS Cost Explorer](https://aws.amazon.com/aws-cost-management/aws-cost-explorer/)
- [AWS Trusted Advisor](https://aws.amazon.com/premiumsupport/technology/trusted-advisor/)
*)
