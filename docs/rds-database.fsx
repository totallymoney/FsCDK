(**
---
title: RDS Relational Databases
category: Resources
categoryindex: 21
---

# ![Amazon RDS](img/icons/Arch_Amazon-RDS_48.png) Amazon RDS

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
    let! appVpc = vpc "AppVPC" { maxAzs 2 }

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
    let! prodVpc = vpc "ProdVPC" { maxAzs 3 }

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
    let! devVpc = vpc "DevVPC" { maxAzs 2 }

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
    let! appVpc = vpc "AppVPC" { maxAzs 2 }

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

- ✅ **Storage encryption enabled by default** (no action needed)
- ✅ **IAM authentication enabled by default** (enhanced security)
- ✅ **Deletion protection enabled by default** (prevents accidents)
- ✅ **Private by default** (not publicly accessible)
- Use security groups to restrict access
- Store credentials in Secrets Manager
- Use SSL/TLS for connections
- **NEW:** Export logs to CloudWatch using `cloudwatchLogsExports`
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

## CloudWatch Logs Export (NEW)

Export database logs to CloudWatch for monitoring, compliance, and security analysis.

*)

stack "DatabaseWithLogging" {
    let! appVpc = vpc "AppVPC" { maxAzs 2 }

    rdsInstance "MonitoredDatabase" {
        vpc appVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.SMALL))
        databaseName "monitored"

        // Enable CloudWatch Logs export for audit trails
        cloudwatchLogsExports [ "postgresql"; "upgrade" ]

        // Retention and encryption enabled by default
        backupRetentionDays 7.0
    }
}

(**
### PostgreSQL Log Types

- **"postgresql"** - General database logs (connections, queries, errors)
- **"upgrade"** - Database upgrade logs

### MySQL/MariaDB Log Types

- **"error"** - Error logs
- **"general"** - General query logs
- **"slowquery"** - Slow query logs
- **"audit"** - Audit logs (if enabled)

### SQL Server Log Types

- **"agent"** - SQL Server Agent logs
- **"error"** - SQL Server error logs

### Oracle Log Types

- **"trace"** - Oracle trace files
- **"audit"** - Oracle audit files
- **"alert"** - Oracle alert logs
- **"listener"** - Oracle listener logs

## Default Settings (UPDATED)

The RDS instance builder applies these **secure-by-default** best practices:

- **Instance Type**: t3.micro (optimize for cost in dev)
- **Backup Retention**: 7 days
- **Delete Automated Backups**: true
- **Multi-AZ**: false (enable explicitly for production)
- **Publicly Accessible**: ✅ **false (explicitly private)**
- **Storage Encrypted**: ✅ **true (encrypted by default)**
- **Deletion Protection**: ✅ **true (prevent accidental deletion)**
- **IAM Authentication**: ✅ **true (enhanced security)**
- **CloudWatch Logs**: ✅ **Ready to configure (use cloudwatchLogsExports)**
- **Auto Minor Version Upgrade**: true

## RDS Proxy Considerations

**Important limitation:** RDS Proxies receive only private IP addresses, regardless of subnet placement. External connections (from local machines, CI/CD, etc.) require:

- **Bastion host** - Temporary EC2 instance for administrative access
- **VPN/Direct Connect** - Private network connectivity
- **AWS Systems Manager Session Manager** - Secure tunneling without SSH

For development workflows requiring external access, consider direct RDS connections in non-production environments.

## Environment-Specific Cost Optimization

Balance security and cost based on environment:

| Configuration | Dev | Production |
|---------------|-----|------------|
| **Multi-AZ** | `false` ($25/mo) | `true` ($50/mo) |
| **Backup Retention** | 1 day | 7-30 days |
| **Instance Class** | t3.micro | r5.large |
| **Deletion Protection** | `false` | `true` |
| **Performance Insights** | optional | recommended |

Dev environments can use single-AZ, minimal backups, and smaller instances to reduce costs by 50-70%.

## Resources

- [Amazon RDS Documentation](https://docs.aws.amazon.com/rds/)
- [RDS Best Practices](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_BestPractices.html)
- [RDS Security](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.html)
- [Performance Insights](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_PerfInsights.html)
*)
