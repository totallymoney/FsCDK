(**
---
title: Backup and Disaster Recovery on AWS
category: docs
index: 35
---

# Backup and Disaster Recovery on AWS

This guide covers backup strategies, disaster recovery patterns, and business continuity planning for AWS infrastructure deployed with FsCDK. Based on the AWS Well-Architected Reliability Pillar and battle-tested patterns from AWS Solutions Architects.

## Understanding Recovery Objectives

Before implementing any backup strategy, define your Recovery Point Objective (RPO) and Recovery Time Objective (RTO). These metrics determine your architecture and costs.

**Recovery Point Objective (RPO)**: Maximum acceptable data loss measured in time. If your RPO is 1 hour, you can lose at most 1 hour of data.

**Recovery Time Objective (RTO)**: Maximum acceptable downtime. If your RTO is 4 hours, you must restore operations within 4 hours of an incident.

### Common RPO/RTO Requirements by Industry

| Industry | Typical RPO | Typical RTO | Compliance Drivers |
|----------|-------------|-------------|-------------------|
| Financial Services | < 1 hour | < 4 hours | SOX, PCI DSS |
| Healthcare | < 4 hours | < 8 hours | HIPAA |
| E-commerce | < 15 minutes | < 1 hour | Revenue impact |
| SaaS Applications | < 1 hour | < 4 hours | SLA commitments |
| Media/Content | < 24 hours | < 24 hours | Business tolerance |

Reference: AWS Disaster Recovery Whitepaper (https://docs.aws.amazon.com/whitepapers/latest/disaster-recovery-workloads-on-aws/disaster-recovery-options-in-the-cloud.html)

## RDS Automated Backups

RDS provides automated backups with point-in-time recovery. This is the foundation for database disaster recovery.

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.CloudWatch
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.Lambda

(**

### Production Database with Automated Backups

RDS automatically takes continuous backups, enabling restoration to any point within the backup retention window (1-35 days).

*)

stack "ProductionBackupStrategy" {
    environment { region "us-east-1" }

    description "Production database with automated backups"

    // VPC for database
    let prodVpc =
        vpc "ProductionVpc" {
            maxAzs 2
            natGateways 1
        }

    // Production database with maximum backup retention
    rdsInstance "ProductionDB" {
        vpc prodVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE))
        allocatedStorage 100

        // Backup configuration
        backupRetentionDays 35.0 // Maximum retention for PITR
        preferredBackupWindow "03:00-04:00" // 3-4 AM UTC

        // High availability
        multiAz true

        // Security
        deletionProtection true
        storageEncrypted true

        // Monitoring
        enablePerformanceInsights true
    }
}

(**

### Compliance-Driven Retention Policies

Different compliance frameworks mandate specific retention periods. Configure your backup plans accordingly.

**PCI DSS Requirements:**
- Retain backups for at least 3 months
- Quarterly backups for 1 year
- Reference: PCI DSS Requirement 3.1

**HIPAA Requirements:**
- Retain backups for 6 years minimum
- Ensure encryption at rest and in transit
- Reference: 45 CFR §164.308(a)(7)(ii)(A)

**SOX Requirements:**
- Retain financial data backups for 7 years
- Ensure immutability and audit trails
- Reference: Sarbanes-Oxley Section 802

For long-term compliance retention beyond 35 days, use RDS snapshots exported to S3 with lifecycle policies.

*)

stack "ComplianceBackups" {
    environment { region "us-east-1" }

    // Compliance database with automated snapshots
    let compVpc = vpc "ComplianceVpc" { maxAzs 2 }

    rdsInstance "ComplianceDB" {
        vpc compVpc
        postgresEngine
        backupRetentionDays 35.0
        deletionProtection true
        storageEncrypted true
    }

    // S3 bucket for long-term snapshot storage
    bucket "long-term-backup-storage" {
        versioned true
        encryption BucketEncryption.KMS_MANAGED

        // Lifecycle policy for cost optimization
        lifecycleRule {
            enabled true

            // Transition to Glacier after 90 days
            transitions
                [ transition {
                      storageClass StorageClass.GLACIER
                      transitionAfter (Duration.Days 90.0)
                  } ]

            // Delete after 7 years (SOX compliance)
            expiration (Duration.Days 2555.0)
        }
    }
}

(**

Reference: Export RDS snapshots to S3 (https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_ExportSnapshot.html)

## Point-in-Time Recovery for Databases

### RDS Point-in-Time Recovery

RDS PITR allows restoration to any second within the retention period. This is critical for recovering from data corruption or accidental deletions.

*)

stack "PITRDatabase" {
    let pitrVpc = vpc "PITRVpc" { maxAzs 2 }

    rdsInstance "PITRDatabase" {
        vpc pitrVpc
        postgresEngine
        backupRetentionDays 35.0 // Maximum PITR retention
        multiAz true
        deletionProtection true
        enablePerformanceInsights true
    }
}

(**

### DynamoDB Point-in-Time Recovery

DynamoDB PITR provides continuous backups for 35 days. This feature is enabled by default in FsCDK production defaults.

*)

stack "DynamoDBBackup" {
    table "TransactionalData" {
        partitionKey "id" AttributeType.STRING
        sortKey "timestamp" AttributeType.NUMBER
        billingMode BillingMode.PAY_PER_REQUEST

        // PITR enabled by default in FsCDK
        pointInTimeRecovery true

        // Enable streams for replication
        stream StreamViewType.NEW_AND_OLD_IMAGES
    }
}

(**

Reference: DynamoDB Point-in-Time Recovery (https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/PointInTimeRecovery.html)

## Cross-Region Replication for Disaster Recovery

For mission-critical workloads, implement cross-region replication to survive regional failures.

### RDS Cross-Region Read Replicas

Create read replicas in secondary regions that can be promoted during a disaster. According to AWS, cross-region replicas typically have 1-5 second replication lag.

*)

stack "MultiRegionDatabase" {
    environment { region "us-east-1" }

    description "Primary database with cross-region DR"

    let primaryVpc =
        vpc "PrimaryVpc" {
            maxAzs 3
            natGateways 2
        }

    // Primary database
    rdsInstance "PrimaryDB" {
        vpc primaryVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.XLARGE))
        multiAz true
        backupRetentionDays 30.0
        enablePerformanceInsights true
        deletionProtection true
    }
}

(**

For the DR region, deploy a separate stack:

```fsharp
stack "DRDatabase" {
    environment {
        region "us-west-2"  // DR region
    }

    let drVpc = vpc "DRVpc" { maxAzs 3 }

    // Create read replica from primary (via AWS Console or CLI)
    // aws rds create-db-instance-read-replica \
    //   --db-instance-identifier dr-replica \
    //   --source-db-instance-identifier arn:aws:rds:us-east-1:account:db:primary-db \
    //   --region us-west-2
}
```

Reference: AWS RDS Best Practices (https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_BestPractices.html)

### DynamoDB Global Tables

DynamoDB Global Tables provide automatic multi-region replication with typical latency under 1 second.

*)

stack "GlobalDynamoDB" {
    environment { region "us-east-1" }

    table "GlobalUserData" {
        partitionKey "userId" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST

        // Enable streams for global table replication
        stream StreamViewType.NEW_AND_OLD_IMAGES

        pointInTimeRecovery true
    }
}

(**

After deploying the table, enable global replication via AWS CLI:

```bash
aws dynamodb create-global-table \
    --global-table-name GlobalUserData \
    --replication-group RegionName=us-east-1 \
    --replication-group RegionName=us-west-2 \
    --replication-group RegionName=eu-west-1
```

Reference: DynamoDB Global Tables (https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GlobalTables.html)

### S3 Cross-Region Replication

Replicate S3 buckets across regions for disaster recovery. S3 CRR typically replicates objects within 15 minutes.

*)

stack "S3Replication" {
    // Source bucket with versioning (required for replication)
    bucket "source-assets" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
    }

    // Destination bucket in DR region (deploy separately)
    bucket "dr-assets-replica" {
        versioned true
        encryption BucketEncryption.S3_MANAGED
    }
}

(**

Configure replication using AWS CLI after deployment:

```bash
# Create replication role
aws iam create-role --role-name s3-replication-role \
    --assume-role-policy-document file://trust-policy.json

# Attach replication policy
aws iam put-role-policy --role-name s3-replication-role \
    --policy-name replication-policy \
    --policy-document file://replication-policy.json

# Enable replication
aws s3api put-bucket-replication --bucket source-assets \
    --replication-configuration file://replication-config.json
```

Reference: S3 Replication (https://docs.aws.amazon.com/AmazonS3/latest/userguide/replication.html)

## Disaster Recovery Patterns

AWS defines four disaster recovery strategies, each with different costs and complexity.

### Pattern 1: Backup and Restore (RPO: hours, RTO: 24+ hours)

Lowest cost option. Take periodic backups and restore when needed.

**Cost**: Low (backup storage only, ~$0.05/GB/month)  
**Complexity**: Low  
**Best for**: Development, non-critical workloads

Implementation: Use RDS automated backups and DynamoDB on-demand backups.

Reference: Werner Vogels (AWS CTO) - "Building Resilient Applications" (https://www.allthingsdistributed.com/2020/11/building-resilient-applications.html)

### Pattern 2: Pilot Light (RPO: minutes, RTO: hours)

Maintain minimal version of environment running in DR region. Core infrastructure always on, but scaled down.

**Cost**: Medium (minimal compute running continuously)  
**Complexity**: Medium  
**Best for**: Standard production workloads

*)

stack "PilotLight" {
    environment { region "us-east-1" }

    description "Pilot light infrastructure - core services minimal"

    let pilotVpc = vpc "PilotVpc" { maxAzs 2 }

    // Minimal database that can be scaled up
    rdsInstance "PilotDB" {
        vpc pilotVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL)) // Minimal size
        allocatedStorage 20
        maxAllocatedStorage 1000 // Can auto-scale
        multiAz false // Single AZ to save cost
        backupRetentionDays 7.0
    }
}

(**

During disaster, scale up the instance class to production size.

### Pattern 3: Warm Standby (RPO: seconds, RTO: minutes)

Scaled-down but fully functional version runs in DR region.

**Cost**: Medium-High (continuous smaller environment)  
**Complexity**: Medium-High  
**Best for**: Business-critical workloads

*)

stack "WarmStandby" {
    environment {
        region "us-west-2" // DR region
    }

    description "Warm standby - scaled down production environment"

    let warmVpc = vpc "WarmStandbyVpc" { maxAzs 2 }

    // Scaled down but fully functional
    rdsInstance "WarmStandbyDB" {
        vpc warmVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE)) // 50% of prod
        multiAz true
        backupRetentionDays 30.0
    }

    // Minimal Lambda capacity
    lambda "WarmStandbyAPI" {
        runtime Runtime.DOTNET_8
        handler "Handler::process"
        code "./publish"
        memory 512
        reservedConcurrentExecutions 5 // Minimal capacity
    }
}

(**

### Pattern 4: Hot Standby/Active-Active (RPO: near-zero, RTO: automatic)

Full environment runs in multiple regions simultaneously.

**Cost**: High (full duplicate infrastructure)  
**Complexity**: High  
**Best for**: Mission-critical, 99.99%+ SLA requirements

Used by Netflix, Airbnb, and other companies requiring five-nines availability.

*)

stack "HotStandbyPrimary" {
    environment { region "us-east-1" }

    description "Active-Active primary region"

    let primaryVpc = vpc "PrimaryVpc" { maxAzs 3 }

    rdsInstance "PrimaryDB" {
        vpc primaryVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.XLARGE))
        multiAz true
        backupRetentionDays 30.0
    }

    lambda "PrimaryAPI" {
        runtime Runtime.DOTNET_8
        handler "Handler::process"
        code "./publish"
        memory 1024
        reservedConcurrentExecutions 100
    }
}

stack "HotStandbySecondary" {
    environment { region "us-west-2" }

    description "Active-Active secondary region"

    let secondaryVpc = vpc "SecondaryVpc" { maxAzs 3 }

    rdsInstance "SecondaryDB" {
        vpc secondaryVpc
        postgresEngine
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.XLARGE))
        multiAz true
        backupRetentionDays 30.0
    }

    lambda "SecondaryAPI" {
        runtime Runtime.DOTNET_8
        handler "Handler::process"
        code "./publish"
        memory 1024
        reservedConcurrentExecutions 100
    }
}

(**

Reference: Adrian Cockcroft (Netflix) - "Migrating to Microservices" (https://www.nginx.com/blog/microservices-at-netflix-architectural-best-practices/)

## Monitoring and Alerting

Set up CloudWatch alarms for backup and replication failures.

*)

stack "BackupMonitoring" {
    // Alarm for RDS backup failures
    cloudwatchAlarm "RDSBackupFailure" {
        metricName "BackupRetentionPeriodStorageUsed"
        metricNamespace "AWS/RDS"
        threshold 0.0
        evaluationPeriods 1
        statistic "Average"
        comparisonOperator ComparisonOperator.LESS_THAN_OR_EQUAL_TO_THRESHOLD
        treatMissingData TreatMissingData.BREACHING
    }

    // Alarm for DynamoDB replication lag (for Global Tables)
    cloudwatchAlarm "DynamoDBReplicationLag" {
        metricName "ReplicationLatency"
        metricNamespace "AWS/DynamoDB"
        threshold 60000.0 // 60 seconds
        evaluationPeriods 2
        statistic "Average"
        comparisonOperator ComparisonOperator.GREATER_THAN_THRESHOLD
        treatMissingData TreatMissingData.NOT_BREACHING
    }
}

(**

## Testing Disaster Recovery

AWS recommends testing DR procedures quarterly. According to the AWS Reliability Pillar, untested DR plans fail 30-40% of the time during actual disasters.

### DR Testing Checklist

1. **Backup Verification**: Restore from backup to test environment monthly
2. **Failover Testing**: Switch to DR region quarterly
3. **Data Integrity**: Verify restored data matches production
4. **RTO Measurement**: Time the complete recovery process
5. **Documentation**: Update runbooks based on test results

Reference: AWS Well-Architected Reliability Pillar - REL13 (https://docs.aws.amazon.com/wellarchitected/latest/reliability-pillar/test-reliability.html)

### Automated DR Testing with AWS Fault Injection Simulator

AWS FIS allows testing failover scenarios without impacting production:

- Test RDS Multi-AZ failover
- Test application behavior during region degradation
- Validate monitoring and alerting
- Practice runbook procedures

## Cost Considerations

Backup and DR costs vary significantly based on strategy:

| Strategy | Monthly Cost (Example) | Use Case |
|----------|----------------------|----------|
| Backup Only | $50-200 | Development, non-critical |
| Pilot Light | $200-1000 | Standard production |
| Warm Standby | $1000-5000 | Business-critical |
| Hot Standby | $5000-20000+ | Mission-critical, 99.99%+ SLA |

**Cost Optimization Tips:**

1. Use S3 Glacier for long-term archive (90+ days) - 80% cheaper than standard storage
2. Enable lifecycle policies to transition old backups automatically
3. Use cross-region replication only for critical data
4. Test restore times before committing to expensive active-active
5. Use Aurora Global Database instead of RDS for faster replication

Reference: AWS Cost Optimization Pillar (https://docs.aws.amazon.com/wellarchitected/latest/cost-optimization-pillar/welcome.html)

## Compliance Mapping

Common compliance frameworks and their DR requirements:

**PCI DSS**
- Requirement 3.1: Retain backups with cardholder data
- Requirement 9.5: Physically secure backup media
- Requirement 10.5: Protect audit trail backups

**HIPAA**
- 164.308(a)(7)(i): Contingency plan required
- 164.308(a)(7)(ii)(A): Data backup plan
- 164.308(a)(7)(ii)(B): Disaster recovery plan
- 164.308(a)(7)(ii)(C): Emergency mode operation plan

**SOX**
- Section 404: Financial data retention (7 years)
- Immutable backups for audit trail integrity

**ISO 27001**
- A.12.3.1: Information backup procedures
- A.17.1.2: Business continuity procedures
- A.17.1.3: Verify, review, evaluate continuity

## Real-World Case Study: AWS US-EAST-1 Outage

**US-EAST-1 Outage (December 2021)**
- Caused by network device failure
- Affected major services for 7+ hours
- Companies with multi-region setups remained operational
- Single-region customers experienced complete outage

**Key Takeaway**: Do not rely on single region for production workloads. US-EAST-1 is the largest region but has had several multi-hour outages.

Reference: AWS Post-Event Summaries (https://aws.amazon.com/message/12721/)

## Additional Resources

**AWS Official Documentation:**
- AWS Backup Developer Guide: https://docs.aws.amazon.com/aws-backup/
- RDS Backup and Restore: https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_CommonTasks.BackupRestore.html
- DynamoDB Backup and Restore: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/BackupRestore.html

**AWS Whitepapers:**
- Disaster Recovery on AWS: https://docs.aws.amazon.com/whitepapers/latest/disaster-recovery-workloads-on-aws/
- AWS Well-Architected Framework - Reliability Pillar: https://docs.aws.amazon.com/wellarchitected/latest/reliability-pillar/

**Community Resources:**
- AWS re:Invent 2023 - Disaster Recovery Best Practices (DOP326)
- Corey Quinn, "The AWS Morning Brief" podcast episodes on DR
- Adrian Hornsby (AWS Principal Evangelist) "Chaos Engineering" blog series

**Books:**
- "AWS System Administration" by Michael Wittig and Andreas Wittig (O'Reilly)
- "Implementing AWS Disaster Recovery" (Packt Publishing)

---

This guide reflects AWS best practices as of 2025. Always refer to the latest AWS documentation and your organization's compliance requirements when implementing disaster recovery strategies.

*)
