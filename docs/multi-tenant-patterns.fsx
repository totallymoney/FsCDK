(**
---
title: Multi-Tenant SaaS Patterns
category: Tutorials
categoryindex: 2
---

# Multi-Tenant SaaS Architecture Patterns

Build secure, isolated multi-tenant applications with tenant-specific data, credentials, and configurations.

## Tenant Isolation Strategies

### Schema-per-Tenant (PostgreSQL)

Isolate tenant data using PostgreSQL schemas with row-level security.

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
open Amazon.CDK.AWS.SecretsManager

(**
## Tenant Registry Pattern

Use DynamoDB to track tenant metadata, status, and configuration.
*)

stack "TenantRegistry" {
    // Central tenant registry
    table "tenant-registry" {
        partitionKey "tenantId" AttributeType.STRING
        sortKey "environment" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        pointInTimeRecovery true

        // Global Secondary Index for querying by status
        globalSecondaryIndexWithSort "status-index" ("status", AttributeType.STRING) ("createdAt", AttributeType.STRING)
    }

    // Tenant configuration data
    table "tenant-config" {
        partitionKey "tenantId" AttributeType.STRING
        sortKey "configKey" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        stream StreamViewType.NEW_AND_OLD_IMAGES
    }
}

(**
## Per-Tenant Secrets

Store tenant-specific API keys, credentials, and configuration in Secrets Manager.
*)

stack "TenantSecrets" {
    // Template for tenant secrets (create per tenant programmatically)
    secret "tenant-template-config" {
        description "Template for tenant-specific configuration"

        // Store tenant-specific secrets like:
        // - API keys for third-party services
        // - Database credentials
        // - Encryption keys
        generateSecretString (
            SecretsManagerHelpers.generateJsonSecret """{"apiEndpoint": "https://api.example.com"}""" "apiKey"
        )
    }
}

(**
## Multi-Tenant Database Architecture

### Option 1: Schema-per-Tenant (Single Database)

Best for: 100-1000 tenants, simplified operations, cost efficiency
*)

stack "SchemaPerTenant" {
    let appVpc = vpc "AppVPC" { maxAzs 2 }

    rdsInstance "SharedDatabase" {
        vpc appVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.LARGE))
        databaseName "multitenant"

        // Large instance for many tenants
        allocatedStorage 100
        multiAz true

        // Enable IAM authentication for application access
        iamAuthentication true

        // Strong encryption for tenant data
        storageEncrypted true
        deletionProtection true
    }
}

(**
### PostgreSQL Schema Isolation Pattern

```sql
-- Create tenant-specific schema
CREATE SCHEMA tenant_acme;

-- Row-level security policy
ALTER TABLE tenant_acme.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON tenant_acme.users
    USING (tenant_id = current_setting('app.current_tenant_id')::UUID);

-- Grant application user access
GRANT USAGE ON SCHEMA tenant_acme TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA tenant_acme TO app_user;
```

### Option 2: Database-per-Tenant (Large Tenants)

Best for: <100 tenants, enterprise customers, regulatory isolation
*)

stack "DatabasePerTenant" {
    let appVpc = vpc "AppVPC" { maxAzs 2 }

    // Template - deploy per large tenant
    rdsInstance "EnterpriseCustomerDB" {
        vpc appVpc
        postgresEngine PostgresEngineVersion.VER_15
        instanceType (InstanceType.Of(InstanceClass.MEMORY5, InstanceSize.XLARGE))
        databaseName "enterprise_tenant"

        multiAz true
        backupRetentionDays 30.0

        storageEncrypted true
        deletionProtection true
        enablePerformanceInsights true
    }
}

(**
## Tenant Context Middleware Pattern

Pass tenant context through request headers for isolation.

```fsharp
// API Gateway passes tenant ID from authorizer
let tenantId = request.Headers.["X-Tenant-ID"]
let tenantRole = request.Headers.["X-Tenant-Role"]

// Set PostgreSQL session variable for RLS
Database.executeRaw $"SET app.current_tenant_id = '{tenantId}'"

// All queries now isolated to tenant
let users = Users.getAll() // Only returns tenant's users
```
*)

(**
## Cost Optimization by Tenant Tier

Adjust infrastructure based on tenant size/importance:

| Tier | Database | Backup | Instance | Monthly Cost |
|------|----------|--------|----------|--------------|
| **Free** | Shared schema | 1 day | Shared | ~$0 |
| **Standard** | Shared schema | 7 days | Shared | ~$5/tenant |
| **Premium** | Shared schema | 30 days | Shared | ~$15/tenant |
| **Enterprise** | Dedicated DB | 30 days | Dedicated | ~$500+/tenant |

## Security Best Practices

### Tenant Isolation Checklist

- [x] Database row-level security (RLS) enforced
- [x] Application validates tenant ID on every request
- [x] Secrets isolated per tenant in Secrets Manager
- [x] CloudWatch logs include tenant context
- [x] IAM policies scoped to tenant resources
- [x] Audit trail includes tenant ID
- [x] Cross-tenant queries blocked at database level

### Prevent Cross-Tenant Data Leaks

```fsharp
// ❌ BAD: No tenant filtering
let user = Users.getById(userId)

// ✅ GOOD: Always include tenant context
let user = Users.getById(userId, tenantId)

// ✅ BETTER: Tenant context in session
Database.setTenantContext(tenantId)
let user = Users.getById(userId) // RLS enforces isolation
```

## Tenant Provisioning Workflow

1. **Create tenant record** in DynamoDB registry
2. **Generate secrets** in Secrets Manager
3. **Provision database schema** (if schema-per-tenant)
4. **Create IAM users/roles** (if dedicated resources)
5. **Initialize default data** for new tenant
6. **Mark tenant as active** in registry

## Monitoring Multi-Tenant Systems

Tag CloudWatch metrics with tenant ID for per-tenant observability in your application code (not IaC). 
Use AWS SDK CloudWatch client to emit metrics with tenant dimensions.
*)

(**
## Resources

- [AWS Multi-Tenant SaaS Architecture](https://docs.aws.amazon.com/whitepapers/latest/saas-architecture-fundamentals-aws/multi-tenant-saas-architecture.html)
- [PostgreSQL Row Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [AWS SaaS Factory](https://aws.amazon.com/partners/programs/saas-factory/)
- [Multi-Tenant Database Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/sharding)
*)
