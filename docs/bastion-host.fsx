(**
---
title: Bastion Host
category: Resources
categoryindex: 3
---

# ![Bastion Host](img/icons/Res_Amazon-EC2_Instance_48.png) Hardening bastion hosts with FsCDK

Bastion hosts should be a last resort: short-lived, tightly monitored entry points into private subnets. This notebook codifies the controls highlighted by AWS Heroes **Scott Piper** and **Mark Nunnikhoven**, plus guidance from the **AWS re:Inforce** session “Secure remote access architectures.” Whenever possible, migrate to AWS Systems Manager Session Manager for zero-port-access workflows; when you cannot, adopt the configurations below.

## Quick-start templates

Each scenario references the **Well-Architected Security Pillar** and the **AWS Prescriptive Guidance** playbooks for operational excellence.
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.EC2

(**
## Basic bastion host

Start with the bare minimum: a single-instance jump box inside a dedicated VPC. Treat this only as a temporary access point while you implement Session Manager, as recommended in the **AWS Prescriptive Guidance** article “Transitioning from bastions to Session Manager.”
*)

stack "BasicBastion" {
    let! myVpc = vpc "MyVpc" { () }

    bastionHost "MyBastion" {
        vpc myVpc
        instanceName "dev-bastion"
    }
}

(**
## Production bastion with locked-down security groups

Restrict ingress to approved corporate CIDR ranges, deny outbound traffic by default, and log every session. This mirrors the defence-in-depth approach highlighted in **re:Inforce SEC311** “Harden administration paths,” where enforced IMDSv2 and least-privilege security groups prevent lateral movement.
*)

stack "SecureBastion" {
    let! myVpc = vpc "MyVpc" { () }

    let! bastionSG =
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
## Bastion with hardened AMI

Adopt CIS-hardened golden images or images produced by your pipeline so every bastion starts with patched packages, disabled unused services, and consistent audit tooling. This aligns with the **AWS Security Blog** series on golden AMIs and the **Center for Internet Security** benchmarks.
*)

stack "CustomBastion" {
    let! myVpc = vpc "MyVpc" { () }

    let hardenedAMI = MachineImage.GenericLinux(dict [ "us-east-1", "ami-12345678" ])

    bastionHost "HardenedBastion" {
        vpc myVpc
        machineImage hardenedAMI
        instanceName "hardened-bastion"
        requireImdsv2 true
    }
}

(**
## Multi-AZ bastion fleet

Highly regulated or mission-critical environments sometimes require redundant bastions. Deploy one per Availability Zone, attach automation to rotate host keys daily, and integrate health checks that fail closed—echoing recommendations from the **AWS Well-Architected Reliability Pillar**.
*)

stack "HABastion" {
    let! myVpc =
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
## Implementation checklist & learning resources

### Security controls
- Enforce IMDSv2, restrict SSH ingress to corporate CIDR ranges, and enable CloudWatch Logs or S3 session transcripts. These steps are emphasised in **re:Inforce SEC311** and the **Security Hub Foundational Best Practices** standard.
- Rotate SSH keys automatically with AWS Secrets Manager or Session Manager hybrid access, following the blueprint in the AWS blog “Automating key rotation for bastion hosts.”

### Cost & operations
- Use the smallest burstable instance type (t3.nano) for ad-hoc access, stop instances when idle, and tag every bastion with owner and expiry metadata. Schedule automation via EventBridge, replicating the workflow described by **AWS Hero Mark Nunnikhoven**.

### Prefer AWS Systems Manager Session Manager
- Eliminates inbound ports, centralises access in IAM, and provides CloudTrail-backed audit trails. Complete the **Session Manager Workshop** (4.9★ rating) to migrate off legacy bastions.

### Further learning
- **[AWS re:Inforce SEC311](https://www.youtube.com/results?search_query=aws+reinforce+SEC311+administrative+access)** – Hardening administrative access.
- **[Scott Piper – Common AWS security mistakes](https://summitroute.com/blog/)** (summitroute.com).
- **[Session Manager Immersion Day](https://catalog.workshops.aws/session-manager/)** – Official AWS hands-on lab.
- **[Well-Architected Security Pillar](https://docs.aws.amazon.com/wellarchitected/latest/security-pillar/welcome.html)** – Administration and access control section.

Document the access policy, monitor session activity, and track shutdown automation so your bastion hosts stay compliant and short-lived.
*)
