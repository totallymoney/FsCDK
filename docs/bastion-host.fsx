(**
---
title: Bastion Host
category: docs
index: 12
---

# Bastion Host

A bastion host is a server used to provide access to a private network from an external network.
FsCDK provides secure bastion host configuration following AWS best practices.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.EC2

(**
## Basic Bastion Host

Create a minimal bastion host in a VPC.
*)

stack "BasicBastion" {
    let myVpc = vpc "MyVpc" { () }

    bastionHost "MyBastion" {
        vpc myVpc
        instanceName "dev-bastion"
    }
}

(**
## Production Bastion with Security Group

For production, use a security group to restrict SSH access.
*)

stack "SecureBastion" {
    let myVpc = vpc "MyVpc" { () }

    let bastionSG =
        securityGroup "BastionSG" {
            vpc myVpc
            description "Security group for bastion host"
            allowAllOutbound false
        }

    bastionHost "ProductionBastion" {
        vpc myVpc
        securityGroup bastionSG
        instanceName "production-bastion"
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))
    }
}

(**
## Bastion with Custom AMI

Use a hardened custom AMI for enhanced security.
*)

stack "CustomBastion" {
    let myVpc = vpc "MyVpc" { () }

    let hardenedAMI = MachineImage.GenericLinux(dict [ "us-east-1", "ami-12345678" ])

    bastionHost "HardenedBastion" {
        vpc myVpc
        machineImage hardenedAMI
        instanceName "hardened-bastion"
        requireImdsv2 true
    }
}

(**
## Multi-AZ Bastion Setup

Deploy bastions in multiple availability zones for high availability.
*)

stack "HABastion" {
    let myVpc =
        vpc "MyVpc" {
            maxAzs 2
            natGateways 2
        }

    // Bastion in first AZ
    bastionHost "Bastion1" {
        vpc myVpc
        instanceName "bastion-az1"
        subnetSelection (SubnetSelection(SubnetType = SubnetType.PUBLIC, AvailabilityZones = [| "us-east-1a" |]))
    }

    // Bastion in second AZ
    bastionHost "Bastion2" {
        vpc myVpc
        instanceName "bastion-az2"
        subnetSelection (SubnetSelection(SubnetType = SubnetType.PUBLIC, AvailabilityZones = [| "us-east-1b" |]))
    }
}

(**
## Best Practices

### Security

- ✅ Use IMDSv2 (enabled by default)
- ✅ Restrict SSH access to specific IP ranges
- ✅ Use SSH key pairs (never passwords)
- ✅ Enable CloudWatch logging
- ✅ Use AWS Systems Manager Session Manager instead when possible

### Cost Optimization

- ✅ Use t3.nano for minimal workloads (default)
- ✅ Stop bastions when not in use
- ✅ Use spot instances for dev/test environments

### Operational Excellence

- ✅ Tag bastions with owner and purpose
- ✅ Set up CloudWatch alarms for unusual activity
- ✅ Regularly update and patch the OS
- ✅ Use infrastructure as code (this!)

### Alternative: AWS Systems Manager Session Manager

Consider using Session Manager instead of bastion hosts:
- No open inbound ports required
- Centralized access control via IAM
- Audit trail in CloudTrail
- No need to manage SSH keys
*)
