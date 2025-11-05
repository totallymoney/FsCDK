(**
---
title: AWS Learning Resources
category: docs
index: 100
---

# AWS Learning Portal

This comprehensive learning portal provides curated resources from AWS Heroes, AWS official documentation, and highly-rated community content to help you master AWS services while using FsCDK.

## 🎓 Getting Started with AWS

**Essential AWS Fundamentals**
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/) - The five pillars of building on AWS
- [AWS Skillbuilder](https://skillbuilder.aws/) - Free official AWS training courses
- [AWS Architecture Center](https://aws.amazon.com/architecture/) - Reference architectures and diagrams

**AWS Heroes & Community Leaders**
- [Yan Cui (The Burning Monk)](https://theburningmonk.com/) - AWS Serverless Hero specializing in Lambda, Step Functions, and cost optimization
- [Alex DeBrie](https://www.alexdebrie.com/) - DynamoDB expert and author of "The DynamoDB Book"
- [Ben Kehoe](https://ben11kehoe.medium.com/) - AWS Serverless Hero focusing on IAM and security
- [Heitor Lessa](https://twitter.com/heitor_lessa) - AWS Principal Solutions Architect, creator of AWS Lambda Powertools

## 📚 Service-Specific Learning Paths

### AWS Lambda & Serverless

**Must-Read Blog Posts by Yan Cui:**
- [AWS Lambda Best Practices](https://theburningmonk.com/2019/09/all-you-need-to-know-about-lambda-concurrency/) - Deep dive into concurrency, reserved capacity, and provisioned concurrency
- [Lambda Cold Starts](https://theburningmonk.com/2018/01/im-afraid-youre-thinking-about-aws-lambda-cold-starts-all-wrong/) - Understanding and optimizing cold start performance
- [Production-Ready Serverless](https://productionreadyserverless.com/) - Yan Cui's comprehensive course on building production serverless applications

**Official AWS Resources:**
- [AWS Lambda Operator Guide](https://docs.aws.amazon.com/lambda/latest/operatorguide/intro.html) - Best practices for operating Lambda at scale
- [Lambda Powertools Documentation](https://docs.powertools.aws.dev/lambda/) - Official docs for structured logging, metrics, and tracing

**Video Content:**
- [AWS re:Invent - Optimizing Lambda Performance](https://www.youtube.com/watch?v=4_ZEBN8EuG8) - AWS Lambda performance best practices
- [Yan Cui - Serverless Observability](https://www.youtube.com/watch?v=YX4BNX_B6hg) - How to monitor and debug serverless applications

### Amazon DynamoDB

**Expert Resources by Alex DeBrie:**
- [The DynamoDB Book](https://www.dynamodbbook.com/) - The definitive guide to DynamoDB data modeling
- [DynamoDB Guide](https://www.dynamodbguide.com/) - Free comprehensive guide to DynamoDB concepts
- [Single-Table Design](https://www.alexdebrie.com/posts/dynamodb-single-table/) - Advanced DynamoDB patterns

**AWS Official Resources:**
- [Rick Houlihan's re:Invent Sessions](https://www.youtube.com/results?search_query=rick+houlihan+dynamodb) - Former AWS Principal Engineer explaining advanced DynamoDB patterns
- [DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html) - Official AWS guidance

### IAM & Security

**Security Best Practices:**
- [AWS IAM Policy Simulator](https://policysim.aws.amazon.com/) - Test IAM policies before deployment
- [IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html) - Official AWS IAM security recommendations
- [Ben Kehoe's IAM Posts](https://ben11kehoe.medium.com/) - Deep dives into IAM and AWS security

**Video Content:**
- [AWS re:Inforce - IAM Policy Mastery](https://www.youtube.com/results?search_query=aws+reinforce+iam) - Security-focused sessions
- [Become an IAM Policy Master](https://www.youtube.com/watch?v=YQsK4MtsELU) - Comprehensive IAM tutorial

### Step Functions & Orchestration

**Official Resources:**
- [AWS Step Functions Workshop](https://catalog.workshops.aws/stepfunctions/en-US) - Hands-on learning with real-world scenarios
- [Saga Pattern Implementation](https://aws.amazon.com/blogs/compute/implementing-the-saga-pattern-with-aws-step-functions-and-amazon-dynamodb/) - Distributed transaction patterns

### Amazon S3

**Security & Best Practices:**
- [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html) - Official AWS security guide
- [S3 Cost Optimization](https://aws.amazon.com/s3/cost-optimization/) - Reduce storage costs with lifecycle policies
- [S3 Intelligent-Tiering Deep Dive](https://aws.amazon.com/blogs/aws/new-automatic-cost-optimization-for-amazon-s3-via-intelligent-tiering/) - Automated cost savings

## 🎥 Essential AWS Video Series

**AWS re:Invent Sessions (Highest Rated):**
- [Building Production-Ready Serverless Apps](https://www.youtube.com/results?search_query=aws+reinvent+serverless+best+practices) - Annual updates on serverless patterns
- [Advanced Architectures](https://www.youtube.com/results?search_query=aws+reinvent+advanced+architecture) - Enterprise-scale AWS architectures
- [Cost Optimization](https://www.youtube.com/results?search_query=aws+reinvent+cost+optimization) - Strategies to reduce AWS bills

**Community Video Content:**
- [ServerlessLand YouTube Channel](https://www.youtube.com/c/ServerlessLand) - AWS-created serverless content
- [FooBar Serverless](https://www.youtube.com/@foobar_codes) - Yan Cui's video series on serverless

## 📖 Recommended Books

1. **[The DynamoDB Book](https://www.dynamodbbook.com/)** by Alex DeBrie - Master DynamoDB data modeling
2. **[AWS Lambda in Action](https://www.manning.com/books/aws-lambda-in-action)** by Danilo Poccia - Comprehensive Lambda guide
3. **[The Good Parts of AWS](https://gumroad.com/l/aws-good-parts)** by Daniel Vassallo & Josh Pschorr - Practical AWS advice
4. **[Serverless Architectures on AWS](https://www.manning.com/books/serverless-architectures-on-aws-second-edition)** - End-to-end serverless patterns

## 🛠️ Tools & Resources

**AWS CLI & SDKs:**
- [AWS CLI Documentation](https://docs.aws.amazon.com/cli/) - Command-line interface for AWS
- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/v2/guide/home.html) - Infrastructure as Code (FsCDK wraps this!)

**Cost Management:**
- [AWS Cost Explorer](https://aws.amazon.com/aws-cost-management/aws-cost-explorer/) - Visualize and analyze AWS spending
- [AWS Budgets](https://aws.amazon.com/aws-cost-management/aws-budgets/) - Set custom cost alerts
- [CloudPing.info](https://www.cloudping.info/) - Test latency to AWS regions

**Monitoring & Observability:**
- [AWS CloudWatch](https://aws.amazon.com/cloudwatch/) - Monitoring and observability service
- [AWS X-Ray](https://aws.amazon.com/xray/) - Distributed tracing for microservices
- [Lumigo](https://lumigo.io/) - Serverless monitoring platform (commercial)

## 💡 How FsCDK Implements Best Practices

**Production-Safe Defaults:**
- FsCDK implements Yan Cui's serverless best practices by default (see [Lambda Production Defaults](lambda-production-defaults.fsx))
- Auto-creates Dead Letter Queues (DLQs) for Lambda functions
- Enables X-Ray tracing and structured JSON logging
- Sets conservative concurrency limits to prevent runaway costs

**Security by Default:**
- S3 buckets block public access and enforce SSL/TLS
- Lambda environment variables encrypted with KMS
- Security groups deny all outbound traffic by default (opt-in model)

## 🌟 AWS Community Resources

**Blogs to Follow:**
- [The Burning Monk (Yan Cui)](https://theburningmonk.com/) - Serverless best practices
- [AWS News Blog](https://aws.amazon.com/blogs/aws/) - Official AWS updates
- [AWS Compute Blog](https://aws.amazon.com/blogs/compute/) - Lambda, containers, and compute
- [Last Week in AWS](https://www.lastweekinaws.com/) - Corey Quinn's humorous AWS commentary

**Podcasts:**
- [AWS Podcast](https://aws.amazon.com/podcasts/aws-podcast/) - Official AWS podcast
- [Serverless Chats](https://www.serverlesschats.com/) - Jeremy Daly's serverless podcast
- [Screaming in the Cloud](https://www.screaminginthecloud.com/) - Corey Quinn's cloud economics podcast

**Newsletters:**
- [Off-by-none](https://offbynone.io/) - Jeremy Daly's serverless newsletter
- [AWS Week in Review](https://aws.amazon.com/blogs/aws/category/week-in-review/) - Weekly AWS updates

## 🎯 Recommended Learning Paths

### Beginner Path (4 weeks)

**Week 1 - AWS Fundamentals:**
1. Complete [AWS Skillbuilder Cloud Essentials](https://skillbuilder.aws/)
2. Read [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)
3. Set up AWS account with [IAM best practices](iam-best-practices.fsx)

**Week 2 - Serverless Basics:**
1. Read [Lambda Quickstart](lambda-quickstart.fsx)
2. Watch [AWS Lambda Tutorial for Beginners](https://www.youtube.com/watch?v=eOBq__h4OJ4)
3. Build your first Lambda with FsCDK

**Week 3 - Data & Storage:**
1. Read [S3 Quickstart](s3-quickstart.fsx)
2. Study [DynamoDB Core Concepts](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.CoreComponents.html)
3. Create your first table and bucket with FsCDK

**Week 4 - Integration:**
1. Learn [SNS/SQS messaging patterns](sns-sqs-messaging.fsx)
2. Build event-driven architecture with S3 + Lambda
3. Deploy a complete stack with FsCDK

### Intermediate Path (4 weeks)

**Week 1 - Production Lambda:**
1. Study [Lambda Production Defaults](lambda-production-defaults.fsx)
2. Read [Yan Cui's Lambda Best Practices](https://theburningmonk.com/tag/best-practice/)
3. Implement Lambda Powertools in your functions

**Week 2 - DynamoDB Mastery:**
1. Read [The DynamoDB Book](https://www.dynamodbbook.com/) (first 3 chapters)
2. Watch [Rick Houlihan's re:Invent Talk](https://www.youtube.com/watch?v=6yqfmXiZTlM)
3. Design single-table data model for your application

**Week 3 - Security:**
1. Complete [IAM Best Practices](iam-best-practices.fsx)
2. Take [IAM Workshop](https://catalog.workshops.aws/iam/en-US)
3. Implement least-privilege policies in your stacks

**Week 4 - Orchestration:**
1. Read [Step Functions documentation](step-functions.fsx)
2. Learn [Saga Pattern](https://aws.amazon.com/blogs/compute/implementing-the-saga-pattern-with-aws-step-functions-and-amazon-dynamodb/)
3. Build a workflow with Step Functions

### Advanced Path (Ongoing)

**Distributed Systems:**
- [Event-Driven Architecture](https://aws.amazon.com/event-driven-architecture/)
- [Microservices Patterns](https://microservices.io/patterns/index.html)
- [Saga Pattern for Transactions](https://microservices.io/patterns/data/saga.html)

**Cost Optimization:**
- [Lambda Cost Optimization](https://theburningmonk.com/2020/07/how-to-reduce-your-aws-lambda-costs/)
- [S3 Storage Class Analysis](https://docs.aws.amazon.com/AmazonS3/latest/userguide/analytics-storage-class.html)
- [AWS Cost Optimization Hub](https://aws.amazon.com/aws-cost-management/)

**Observability:**
- [Serverless Observability](https://theburningmonk.com/2019/03/serverless-observability-what-can-you-use-out-of-the-box/)
- [Distributed Tracing with X-Ray](https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html)
- [CloudWatch Logs Insights](https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/AnalyzingLogData.html)

## 👥 AWS Heroes & Experts to Follow

**Serverless & Lambda:**
- [Yan Cui (@theburningmonk)](https://twitter.com/theburningmonk) - The Serverless Authority
- [Jeremy Daly (@jeremy_daly)](https://twitter.com/jeremy_daly) - Serverless Advocate
- [Ben Kehoe (@ben11kehoe)](https://twitter.com/ben11kehoe) - IAM & Serverless

**Data & Databases:**
- [Alex DeBrie (@alexbdebrie)](https://twitter.com/alexbdebrie) - DynamoDB Expert
- [Rick Houlihan](https://www.linkedin.com/in/rick-houlihan-7a72a/) - DynamoDB Pioneer

**Security:**
- [Scott Piper (@0xdabbad00)](https://twitter.com/0xdabbad00) - Cloud Security
- [Chris Farris (@jcfarris)](https://twitter.com/jcfarris) - AWS Security & Compliance

**AWS Advocates:**
- [Danilo Poccia (@danilop)](https://twitter.com/danilop) - AWS Principal Developer Advocate
- [Heitor Lessa (@heitor_lessa)](https://twitter.com/heitor_lessa) - Lambda Powertools Creator

## 🚀 Next Steps

1. **Start with FsCDK** - Check out the [Getting Started Guide](getting-started-extended.fsx)
2. **Pick a service** - Choose Lambda, DynamoDB, or S3 to start learning
3. **Build something** - Create a real project using FsCDK
4. **Join the community** - Follow AWS Heroes and engage on Twitter/GitHub
5. **Keep learning** - AWS releases new features every day!

---

**Remember:** The best way to learn AWS is by building. FsCDK makes it easy to experiment, iterate, and deploy production-ready infrastructure with confidence.

*)
