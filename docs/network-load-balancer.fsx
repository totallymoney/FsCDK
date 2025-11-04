(**
---
title: Network Load Balancer
category: docs
index: 10
---

# Network Load Balancer (NLB)

Network Load Balancers provide ultra-high performance, low latency, and TLS offloading at scale.
They operate at Layer 4 (TCP/UDP) and are ideal for handling millions of requests per second.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK.AWS.EC2
(**
## Basic Internal NLB

By default, NLBs are created as internal (not internet-facing) for security.
*)

stack "InternalNLB" {
    // Create VPC
    let! myVpc = vpc "MyVpc" { () }

    // Create internal NLB (default)
    networkLoadBalancer "MyNLB" {
        vpc myVpc
        crossZoneEnabled true
    }
}

(**
## Internet-Facing NLB

For public-facing applications, explicitly set `internetFacing true`.
*)

stack "PublicNLB" {
    let! myVpc = vpc "MyVpc" { () }

    networkLoadBalancer "PublicNLB" {
        vpc myVpc
        internetFacing true
        vpcSubnets (SubnetSelection(SubnetType = SubnetType.PUBLIC))
    }
}

(**
## Production NLB with Deletion Protection

Enable deletion protection for production workloads.
*)

stack "ProductionNLB" {
    let! myVpc = vpc "MyVpc" { () }

    networkLoadBalancer "ProdNLB" {
        vpc myVpc
        internetFacing true
        deletionProtection true
        loadBalancerName "production-nlb"
        crossZoneEnabled true
    }
}

(**
## Multi-AZ High Availability Setup

*)

stack "HighAvailabilityNLB" {
    let! myVpc =
        vpc "MyVpc" {
            maxAzs 3
            natGateways 3
        }

    networkLoadBalancer "HANLB" {
        vpc myVpc
        internetFacing true
        crossZoneEnabled true // Distribute traffic evenly across all zones
        deletionProtection true
    }
}

(**
## Best Practices

### Security

- ✅ Use internal NLBs by default (not internet-facing)
- ✅ Place in private subnets when possible
- ✅ Use security groups to restrict access
- ✅ Enable access logs for auditing

### High Availability

- ✅ Enable cross-zone load balancing for even distribution
- ✅ Deploy across multiple AZs (minimum 2)
- ✅ Use health checks to detect unhealthy targets

### Cost Optimization

- ✅ Disable cross-zone load balancing if not needed (saves data transfer costs)
- ✅ Right-size target instances
- ✅ Use reserved capacity for predictable workloads

### Performance

- ✅ Use NLB for TCP/UDP workloads requiring extreme performance
- ✅ Enable connection draining
- ✅ Monitor CloudWatch metrics (ActiveFlowCount, ProcessedBytes)
*)
