(**
---
title: DynamoDB Tables
category: docs
index: 23
---

# Amazon DynamoDB

Amazon DynamoDB is a fully managed NoSQL database service that provides fast and predictable performance
with seamless scalability. DynamoDB lets you offload the administrative burdens of operating and scaling
a distributed database.

## Quick Start

*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB

(**
## Basic Table

Create a simple DynamoDB table with a partition key.
*)

stack "BasicDynamoDB" {
    table "Users" {
        partitionKey "userId" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
    }
}

(**
## Table with Sort Key

Create a table with both partition and sort keys for complex queries.
*)

stack "TableWithSortKey" {
    table "Orders" {
        partitionKey "customerId" AttributeType.STRING
        sortKey "orderDate" AttributeType.NUMBER
        billingMode BillingMode.PAY_PER_REQUEST
    }
}

(**
## Table with Provisioned Capacity

Use provisioned capacity for predictable workloads.
*)

(**
Note: Provisioned capacity configuration must be done using the CDK Table construct directly.
*)

(**
## Table with Point-in-Time Recovery

Enable point-in-time recovery for production tables.
*)

stack "TableWithPITR" {
    table "ProductionData" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        pointInTimeRecovery true
        removalPolicy RemovalPolicy.RETAIN
    }
}

(**
## Table with DynamoDB Streams

Enable DynamoDB Streams for change data capture.
*)

stack "TableWithStreams" {
    table "Events" {
        partitionKey "eventId" AttributeType.STRING
        sortKey "timestamp" AttributeType.NUMBER
        billingMode BillingMode.PAY_PER_REQUEST
        stream StreamViewType.NEW_AND_OLD_IMAGES
    }
}

(**
## Development Table

Optimized settings for development and testing.
*)

stack "DevTable" {
    table "DevData" {
        partitionKey "id" AttributeType.STRING
        billingMode BillingMode.PAY_PER_REQUEST
        removalPolicy RemovalPolicy.DESTROY
    }
}

(**
## Best Practices

### Performance

- Use partition keys with high cardinality to distribute load
- Design sort keys to support query patterns
- Use sparse indexes for infrequent attributes
- Enable DAX (DynamoDB Accelerator) for read-heavy workloads
- Use batch operations for bulk reads/writes
- Implement exponential backoff for retries

### Security

- Enable encryption at rest (enabled by default)
- Use IAM roles for fine-grained access control
- Enable point-in-time recovery for production
- Use VPC endpoints for private access
- Audit access with CloudTrail
- Implement least-privilege IAM policies

### Cost Optimization

- Use PAY_PER_REQUEST for unpredictable workloads
- Use PROVISIONED capacity for steady, predictable traffic
- Enable auto-scaling for provisioned capacity
- Use TTL to automatically delete expired items
- Monitor unused indexes and remove them
- Use DynamoDB Standard-IA for infrequently accessed data

### Reliability

- Enable point-in-time recovery for production tables
- Use global tables for multi-region applications
- Set appropriate removal policies (RETAIN for production)
- Monitor capacity metrics and throttling
- Implement circuit breakers for downstream failures

### Operational Excellence

- Use descriptive table names with purpose
- Tag tables with project and environment
- Monitor CloudWatch metrics (throttles, capacity)
- Set up alarms for capacity and errors
- Document partition and sort key design
- Version your table schemas
*)

(**
## Billing Modes

### PAY_PER_REQUEST (On-Demand)
- No capacity planning required
- Pay per request
- Great for unpredictable workloads
- No minimum fees
- Automatically scales

### PROVISIONED
- Pre-provision read/write capacity
- Lower cost for consistent workloads
- Requires capacity planning
- Can use auto-scaling
- Reserved capacity available for further savings

## Stream View Types

- **KEYS_ONLY**: Only key attributes
- **NEW_IMAGE**: Entire item after modification
- **OLD_IMAGE**: Entire item before modification
- **NEW_AND_OLD_IMAGES**: Both before and after (recommended)

## 📚 Learning Resources from DynamoDB Experts

### Alex DeBrie - The DynamoDB Authority

**Essential Reading & Books:**
- **[The DynamoDB Book](https://www.dynamodbbook.com/)** - The definitive 300+ page guide to DynamoDB (highly recommended!)
- **[DynamoDB Guide](https://www.dynamodbguide.com/)** - Free comprehensive online guide
- [Single-Table Design in DynamoDB](https://www.alexdebrie.com/posts/dynamodb-single-table/) - Advanced data modeling pattern
- [DynamoDB Strategies for One-to-Many Relationships](https://www.alexdebrie.com/posts/dynamodb-one-to-many/) - Essential modeling patterns
- [Secondary Indexes in DynamoDB](https://www.alexdebrie.com/posts/dynamodb-secondary-indexes/) - GSI and LSI explained

**Real-World Examples:**
- [DynamoDB Design Patterns](https://www.alexdebrie.com/posts/dynamodb-patterns-serverless/) - Common application patterns
- [DynamoDB Filter Expressions](https://www.alexdebrie.com/posts/dynamodb-filter-expressions/) - When and how to use filters
- [DynamoDB Condition Expressions](https://www.alexdebrie.com/posts/dynamodb-condition-expressions/) - Atomic operations and constraints

### Rick Houlihan - AWS Principal Engineer (Former)

**Legendary re:Invent Sessions:**
- [Advanced Design Patterns (2019)](https://www.youtube.com/watch?v=6yqfmXiZTlM) - Master class in single-table design (most-watched DynamoDB talk!)
- [Advanced Design Patterns (2018)](https://www.youtube.com/watch?v=HaEPXoXVf2k) - Original advanced patterns session
- [Data Modeling with DynamoDB (2017)](https://www.youtube.com/watch?v=jzeKPKpucS0) - Fundamentals of NoSQL data modeling
- [Advanced Design Patterns (2020)](https://www.youtube.com/watch?v=MF9a1UNOAQo) - Latest patterns and best practices

**Key Concepts from Rick:**
- **Single-table design** - Store all entities in one table for optimal performance
- **Composite keys** - Use concatenated values for flexible queries
- **Inverted indexes** - Create reverse relationships with GSIs
- **Adjacency lists** - Model hierarchical and graph relationships
- **Sparse indexes** - GSIs on optional attributes for efficient filtering

### AWS Official Documentation

**Getting Started:**
- [DynamoDB Developer Guide](https://docs.aws.amazon.com/dynamodb/) - Official complete documentation
- [DynamoDB Core Components](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.CoreComponents.html) - Tables, items, attributes
- [Primary Keys](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.CoreComponents.html#HowItWorks.CoreComponents.PrimaryKey) - Partition and sort keys explained

**Best Practices:**
- [DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html) - Official AWS recommendations
- [Partition Key Design](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-partition-key-design.html) - Avoid hot partitions
- [Sort Key Design](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-sort-keys.html) - Query optimization strategies
- [GSI Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-indexes.html) - When and how to use indexes

**Advanced Features:**
- [DynamoDB Streams](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html) - Change data capture
- [DynamoDB Transactions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/transaction-apis.html) - ACID transactions across items
- [Time To Live (TTL)](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/TTL.html) - Automatic item expiration
- [Global Tables](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GlobalTables.html) - Multi-region replication

### Data Modeling Deep Dives

**Single-Table Design:**
- [Why Single-Table?](https://www.alexdebrie.com/posts/dynamodb-single-table/) - Benefits and trade-offs
- [Single-Table Design Patterns](https://aws.amazon.com/blogs/compute/creating-a-single-table-design-with-amazon-dynamodb/) - AWS blog post
- [When NOT to Use Single-Table](https://www.alexdebrie.com/posts/dynamodb-patterns-serverless/#when-not-to-use-single-table-design) - Trade-offs to consider

**Access Pattern Design:**
- [Start with Access Patterns](https://www.alexdebrie.com/posts/dynamodb-design-process/) - Design process walkthrough
- [Query vs Scan](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-query-scan.html) - Why you should avoid scans
- [Composite Sort Keys](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-sort-keys.html) - Enable range queries

**Relationship Patterns:**
- [One-to-Many Relationships](https://www.alexdebrie.com/posts/dynamodb-one-to-many/) - Three common patterns
- [Many-to-Many Relationships](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-adjacency-graphs.html) - Adjacency list pattern
- [Hierarchical Data](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-adjacency-graphs.html#bp-adjacency-lists) - Tree structures in DynamoDB

### Performance Optimization

**Capacity Planning:**
- [Read/Write Capacity Modes](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.ReadWriteCapacityMode.html) - On-demand vs provisioned
- [Auto Scaling](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/AutoScaling.html) - Scale capacity automatically
- [DynamoDB Pricing](https://aws.amazon.com/dynamodb/pricing/) - Understanding costs
- [Cost Optimization](https://aws.amazon.com/blogs/database/cost-optimization-for-amazon-dynamodb/) - Strategies to reduce spend

**Query Optimization:**
- [Efficient Queries](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/bp-query-scan.html) - Use Query instead of Scan
- [Projection Expressions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.ProjectionExpressions.html) - Reduce data transfer
- [Batch Operations](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/WorkingWithItems.html#WorkingWithItems.BatchOperations) - BatchGetItem and BatchWriteItem
- [Parallel Scans](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Scan.html#Scan.ParallelScan) - When you must scan

**DynamoDB Accelerator (DAX):**
- [DAX Overview](https://aws.amazon.com/dynamodb/dax/) - Microsecond read latency
- [When to Use DAX](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DAX.html) - Read-heavy workloads
- [DAX vs ElastiCache](https://aws.amazon.com/blogs/database/amazon-dynamodb-accelerator-dax-vs-amazon-elasticache-for-redis-which-is-right-for-you/) - Choosing the right cache

### Security & Operations

**Security Best Practices:**
- [DynamoDB Encryption](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/EncryptionAtRest.html) - Encryption at rest (default)
- [IAM Policies for DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/iam-policy-structure.html) - Fine-grained access control
- [VPC Endpoints](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/vpc-endpoints-dynamodb.html) - Private connectivity
- [DynamoDB and HIPAA](https://aws.amazon.com/compliance/hipaa-compliance/) - Compliance considerations

**Monitoring & Troubleshooting:**
- [CloudWatch Metrics](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/monitoring-cloudwatch.html) - Monitor table performance
- [CloudWatch Contributor Insights](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/contributorinsights.html) - Find hot keys
- [X-Ray Integration](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/transaction-apis.html) - Trace DynamoDB operations
- [Common Error Messages](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Programming.Errors.html) - Throttling, capacity, etc.

**Backup & Disaster Recovery:**
- [Point-in-Time Recovery](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/PointInTimeRecovery.html) - Continuous backups
- [On-Demand Backups](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/BackupRestore.html) - Manual snapshots
- [Global Tables](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GlobalTables.html) - Multi-region disaster recovery

### Video Tutorials

**Beginner to Intermediate:**
- [DynamoDB Fundamentals](https://www.youtube.com/watch?v=sI-zciHAh-4) - AWS tutorial for beginners
- [DynamoDB Core Concepts](https://www.youtube.com/watch?v=2k2GINpO308) - Keys, indexes, and queries
- [Single-Table Design Explained](https://www.youtube.com/watch?v=BnDKD_Zv0og) - Visual walkthrough

**Advanced:**
- [Rick Houlihan's Advanced Patterns](https://www.youtube.com/watch?v=6yqfmXiZTlM) - Must-watch for advanced users
- [Data Modeling Workshop](https://www.youtube.com/watch?v=fiP2e-g-r4g) - Hands-on modeling session
- [DynamoDB Streams Deep Dive](https://www.youtube.com/watch?v=CyoWLN6UqRI) - Event-driven patterns

### Community Tools

**Data Modeling Tools:**
- [NoSQL Workbench](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/workbench.html) - Official data modeling tool from AWS
- [DynamoDB Toolbox](https://github.com/jeremydaly/dynamodb-toolbox) - Jeremy Daly's single-table library
- [Dynobase](https://dynobase.dev/) - DynamoDB GUI client and data browser

**Local Development:**
- [DynamoDB Local](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.html) - Run DynamoDB on your laptop
- [LocalStack](https://localstack.cloud/) - Full AWS cloud emulator
- [DynamoDB Admin](https://github.com/aaronshaf/dynamodb-admin) - Web GUI for local development

**Testing & Migration:**
- [AWS Data Pipeline](https://aws.amazon.com/datapipeline/) - Import/export data
- [AWS Database Migration Service](https://aws.amazon.com/dms/) - Migrate from other databases
- [PartiQL](https://partiql.org/) - SQL-compatible query language for DynamoDB

### Recommended Learning Path

**Week 1 - Fundamentals:**
1. Read [DynamoDB Core Components](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.CoreComponents.html)
2. Watch [DynamoDB Fundamentals Video](https://www.youtube.com/watch?v=sI-zciHAh-4)
3. Create your first table with FsCDK (examples above)
4. Practice queries and scans with NoSQL Workbench

**Week 2 - Data Modeling:**
1. Read [Alex DeBrie's One-to-Many Relationships](https://www.alexdebrie.com/posts/dynamodb-one-to-many/)
2. Study [Access Pattern Design](https://www.alexdebrie.com/posts/dynamodb-design-process/)
3. Model your application's entities and access patterns
4. Learn about [Secondary Indexes](https://www.alexdebrie.com/posts/dynamodb-secondary-indexes/)

**Week 3 - Advanced Patterns:**
1. Watch [Rick Houlihan's re:Invent Talk](https://www.youtube.com/watch?v=6yqfmXiZTlM) (MUST WATCH!)
2. Read [Single-Table Design](https://www.alexdebrie.com/posts/dynamodb-single-table/)
3. Learn [DynamoDB Transactions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/transaction-apis.html)
4. Explore [DynamoDB Streams](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html)

**Ongoing - Mastery:**
- Buy [The DynamoDB Book](https://www.dynamodbbook.com/) by Alex DeBrie
- Follow [Alex DeBrie's Blog](https://www.alexdebrie.com/)
- Join [AWS Data Hero Program](https://aws.amazon.com/developer/community/heroes/)
- Practice with [DynamoDB Toolbox](https://github.com/jeremydaly/dynamodb-toolbox)

### Common Pitfalls & How to Avoid Them

**❌ DON'T:**
1. **Use Scan for application queries** → Use Query with proper keys
2. **Design tables like SQL** → Design for access patterns, not entities
3. **Create a table per entity** → Consider single-table design
4. **Ignore hot partition warnings** → Use high-cardinality partition keys
5. **Over-index your table** → Each GSI costs storage and write capacity

**✅ DO:**
1. **Start with access patterns** → Know your queries before designing
2. **Use composite sort keys** → Enable multiple query patterns
3. **Leverage sparse indexes** → GSIs don't need all items
4. **Enable point-in-time recovery** → Protect production data
5. **Use DynamoDB Streams** → Build event-driven architectures

### DynamoDB Experts to Follow

- **[Alex DeBrie (@alexbdebrie)](https://twitter.com/alexbdebrie)** - The DynamoDB Book author
- **[Rick Houlihan](https://www.linkedin.com/in/rick-houlihan-7a72a/)** - Former AWS Principal Engineer, DynamoDB specialist
- **[Jeremy Daly (@jeremy_daly)](https://twitter.com/jeremy_daly)** - Creator of DynamoDB Toolbox
- **[Danilo Poccia (@danilop)](https://twitter.com/danilop)** - AWS Principal Developer Advocate

### FsCDK DynamoDB Features

- Type-safe table definitions with computation expressions
- Automatic encryption at rest (AWS managed keys)
- Support for streams, TTL, and point-in-time recovery
- Seamless integration with Lambda via grants

For implementation details, see [src/DynamoDB.fs](../src/DynamoDB.fs) in the FsCDK repository.
*)
