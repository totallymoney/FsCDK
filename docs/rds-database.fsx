(**
---
title: RDS Relational Databases
category: docs
index: 25
---

# Amazon RDS

Amazon RDS (Relational Database Service) makes it easy to set up, operate, and scale a relational database
in the cloud. It provides cost-efficient and resizable capacity while automating time-consuming
administration tasks such as hardware provisioning, database setup, patching, and backups.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.RDS
open Amazon.CDK.AWS.EC2

(**
## Basic PostgreSQL Database

Create a PostgreSQL database with secure defaults.
*)

stack "BasicRDS" {
    // Create VPC
    let appVpc = vpc "AppVPC" { maxAzs 2 }

    // Create database
    rdsInstance "AppDatabase" {
        vpc appVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        databaseName "appdb"
        backupRetentionDays 7.0
    }
}

(**
## Production Database

Production-ready configuration with Multi-AZ, encryption, and backups.
*)

stack "ProductionRDS" {
    let prodVpc = vpc "ProdVPC" { maxAzs 3 }

    rdsInstance "ProdDatabase" {
        vpc prodVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE))
        databaseName "production"

        // High availability
        multiAz true

        // Backups
        backupRetentionDays 30.0
        preferredBackupWindow "03:00-04:00"

        // Security
        storageEncrypted true
        deletionProtection true

        // Monitoring
        enablePerformanceInsights true
        monitoringInterval (Duration.Minutes(1.0))

        // Maintenance
        autoMinorVersionUpgrade true
        preferredMaintenanceWindow "sun:04:00-sun:05:00"

        // Lifecycle
        removalPolicy RemovalPolicy.RETAIN
    }
}

(**
## Development Database

Cost-optimized database for development/testing.
*)

stack "DevRDS" {
    let devVpc = vpc "DevVPC" { maxAzs 2 }

    rdsInstance "DevDatabase" {
        vpc devVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO))
        databaseName "devdb"

        // Single AZ for cost savings
        multiAz false

        // Shorter backup retention
        backupRetentionDays 1.0

        // No deletion protection
        deletionProtection false

        // Destroy on stack deletion
        removalPolicy RemovalPolicy.DESTROY
    }
}

(**
## Database with IAM Authentication

Enable IAM database authentication for enhanced security.
*)

stack "IAMAuthRDS" {
    let appVpc = vpc "AppVPC" { maxAzs 2 }

    rdsInstance "SecureDatabase" {
        vpc appVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        databaseName "securedb"
        iamAuthentication true
    }
}

(**
## Best Practices

### Performance

- Choose appropriate instance class (T3: burstable, M5: general, R5: memory-optimized)
- Enable Performance Insights for query analysis
- Monitor CloudWatch metrics (CPU, memory, IOPS)
- Use read replicas for read-heavy workloads
- Enable Enhanced Monitoring for OS-level metrics
- Optimize queries and add indexes

### Security

- Always enable storage encryption
- Use IAM authentication when possible
- Never make databases publicly accessible
- Use security groups to restrict access
- Store credentials in Secrets Manager
- Enable deletion protection for production
- Use SSL/TLS for connections
- Audit access with CloudTrail and database logs

### Cost Optimization

- Use t3 instances for development/test
- Right-size production instances based on metrics
- Use Aurora Serverless for variable workloads
- Delete unused snapshots
- Use reserved instances for production
- Enable auto minor version upgrades
- Set appropriate backup retention (don't over-retain)

### Reliability

- Enable Multi-AZ for production databases
- Set backup retention to 7-30 days
- Test restore procedures regularly
- Use automated backups + manual snapshots
- Monitor backup status in CloudWatch
- Set up alarms for critical metrics
- Plan for disaster recovery

### Operational Excellence

- Tag databases with project, environment, team
- Document database schemas and migrations
- Use descriptive database names
- Set up CloudWatch alarms (CPU, storage, connections)
- Schedule maintenance windows appropriately
- Enable auto minor version upgrades
- Monitor and optimize slow queries

## Database Engines

FsCDK supports all RDS database engines:

- **PostgreSQL**: Best open-source option, full-featured
- **MySQL**: Popular, wide community support
- **MariaDB**: MySQL fork with additional features
- **Oracle**: Enterprise features, licensing costs
- **SQL Server**: Microsoft SQL Server, licensing costs
- **Aurora**: AWS-optimized, best performance

## Instance Classes

### Burstable (T3)
- **Use**: Dev/test, small workloads
- **Cost**: Low
- **Performance**: Burstable CPU
- **Examples**: t3.micro, t3.small, t3.medium

### General Purpose (M5)
- **Use**: Production, balanced workloads
- **Cost**: Medium
- **Performance**: Consistent CPU
- **Examples**: m5.large, m5.xlarge, m5.2xlarge

### Memory Optimized (R5)
- **Use**: Large datasets, high concurrency
- **Cost**: High
- **Performance**: High memory, consistent CPU
- **Examples**: r5.large, r5.xlarge, r5.2xlarge

## Multi-AZ vs Read Replicas

| Feature | Multi-AZ | Read Replica |
|---------|----------|--------------|
| **Purpose** | High availability | Read scaling |
| **Synchronous** | Yes | No (async) |
| **Automatic Failover** | Yes | No |
| **Cost** | 2x instance cost | Per replica |
| **Use Case** | Production | Read-heavy apps |

## Backup and Recovery

### Automated Backups
- Daily full snapshot
- Transaction logs every 5 minutes
- Retention: 1-35 days
- Point-in-time recovery within retention
- No performance impact

### Manual Snapshots
- User-initiated
- Retained until manually deleted
- Can copy across regions
- Can share with other accounts

## Default Settings

The RDS instance builder applies these best practices:

- **Instance Type**: t3.micro (optimize for cost in dev)
- **Backup Retention**: 7 days
- **Delete Automated Backups**: true
- **Multi-AZ**: false (enable explicitly for production)
- **Publicly Accessible**: false (secure by default)
- **Storage Encrypted**: true
- **Deletion Protection**: false (enable for production)
- **Auto Minor Version Upgrade**: true

## Resources

- [Amazon RDS Documentation](https://docs.aws.amazon.com/rds/)
- [RDS Best Practices](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_BestPractices.html)
- [RDS Security](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.html)
- [Performance Insights](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_PerfInsights.html)
*)
