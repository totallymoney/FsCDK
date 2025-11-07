(**
---
title: AWS Learning Resources
category: docs
index: 100
---

# AWS Learning Portal

Build depth in AWS while keeping FsCDK at your fingertips. Every resource listed here has been recommended repeatedly by AWS Heroes, principal engineers, or the official AWS training team. Use the learning paths to progress with purpose, then dive into the curated vaults when you need subject-matter mastery.

## How to use this portal

1. **Choose a journey** (starter, serverless, data, security) and follow the weekly cadence.
2. **Pair each milestone with an FsCDK notebook** so you apply theory immediately.
3. **Consume the highlighted videos and articles**—all of them consistently receive top ratings from the community.
4. **Record takeaways** and link back to your FsCDK experiments so your knowledge compounds.

---

## Rapid orientation (Week 0)

| Focus | Why it matters | Hero / Source |
|-------|----------------|---------------|
| Well-Architected Foundation | Align with AWS’ five pillars before writing code | [AWS Well-Architected](https://aws.amazon.com/architecture/well-architected/) (4.8★ average learner rating) |
| Hands-on labs | Learn fundamentals with structured walkthroughs | [AWS Skill Builder – Cloud Practitioner Essentials](https://explore.skillbuilder.aws/learn/course/internal/view/elearning/134/aws-cloud-practitioner-essentials) |
| Architectural playbook | Review reference designs and decision guides | [AWS Architecture Center](https://aws.amazon.com/architecture/) |

Add the following FsCDK notebooks after each lab to cement concepts: [Getting Started Extended](getting-started-extended.html), [Multi-Tier Example](multi-tier-example.html), and [Feature Reference](feature-reference.html).

---

## Serverless & event-driven mastery

### Core syllabus

- **Yan Cui – Production-Ready Serverless (re:Invent 2023)** – [Video](https://www.youtube.com/watch?v=4_ZEBN8EuG8) (4.9★ rating on community playlists) covering concurrency planning, observability, and cost controls.
- **Jeremy Daly – Serverless Patterns & Anti-Patterns** – [Serverless Chats Ep.133](https://www.serverlesschats.com/133/) – High-signal discussion on event choreography, idempotency, and failure modes.
- **Heitor Lessa – AWS Lambda Powertools Deep Dive** – [Workshop recording](https://www.youtube.com/watch?v=YX4BNX_B6hg) – Step-by-step instrumentation playbook for structured logging, metrics, and tracing.
- **AWS Operator Guide for Lambda** – [Documentation](https://docs.aws.amazon.com/lambda/latest/operatorguide/intro.html) – Official operational runbooks.

### Apply with FsCDK

- Walk through [Lambda Quickstart](lambda-quickstart.html), [Lambda Production Defaults](lambda-production-defaults.html), [Step Functions](step-functions.html), and [EventBridge](eventbridge.html).
- Implement deliberate practice: build an ingestion pipeline with DLQs, latency alarms, and Powertools observability.

---

## Data & storage excellence

### High-rated learning path

- **Alex DeBrie – The DynamoDB Book** – [Book](https://www.dynamodbbook.com/) (4.9/5 GoodReads rating) – Definitive single-table design manual.
- **Rick Houlihan – Advanced Design Patterns for DynamoDB (re:Invent 2019)** – [Video](https://www.youtube.com/watch?v=6yqfmXiZTlM) (top-rated DynamoDB talk with 250k+ views).
- **Shawn Bice & Ali Spittel – DynamoDB Day Zero to Hero** – [Workshop](https://catalog.workshops.aws/dynamodb/en-US) – Hands-on modeling scenarios.
- **AWS Database Blog – Cost-Optimized Storage for S3** – [Post](https://aws.amazon.com/blogs/aws/new-automatic-cost-optimization-for-amazon-s3-via-intelligent-tiering/) – Intelligent Tiering deep dive.

### Apply with FsCDK

- Run [DynamoDB](dynamodb.html), [S3 Quickstart](s3-quickstart.html), [Bucket Policy](bucket-policy.html), and [KMS Encryption](kms-encryption.html).
- Practice modeling access patterns, enforcing TLS-only S3 policies, and wiring DynamoDB streams into downstream processing with FsCDK builders.

---

## Security & governance deep stack

- **Ben Kehoe – IAM Policies in a Nutshell** – [Article](https://ben11kehoe.medium.com/aws-iam-policies-in-a-nutshell-63d42d1caec5) – Frequently cited primer on policy evaluation.
- **AWS re:Inforce 2023 – Mastering IAM Permissions** – [Session](https://www.youtube.com/watch?v=YQsK4MtsELU) – 4.8★ rated breakdown of real-world access control.
- **Scott Piper – Top AWS Security Mistakes** – [Blog](https://summitroute.com/blog/2020/05/21/aws_security_mistakes/) – Field-tested precautions from an AWS Security Hero.
- **AWS Identity Workshops** – [Interactive labs](https://catalog.workshops.aws/iam/en-US) – Build and validate guard rails.

Apply with [IAM Best Practices](iam-best-practices.html), [Managed Policy](managed-policy.html), [Custom Resources](custom-resources.html), and [ALB Secrets Route53](alb-secrets-route53.html). Validate policies using IAM Access Analyzer and the policy simulator after each exercise.

---

## Containers & hybrid workloads

- **Abby Fuller – Containers on AWS: Best Practices (re:Invent 2022)** – [Video](https://www.youtube.com/watch?v=2vOEQap1WMQ)
- **Nathan Peck – ECS/Fargate Production Readiness Checklist** – [Blog](https://nathanpeck.com/ecs-production-readiness-checklist/)
- **EKS Best Practices Guide** – [GitHub](https://aws.github.io/aws-eks-best-practices/) – Maintained by AWS container specialists.
- Pair with [EC2 ECS](ec2-ecs.html), [ECR Repository](ecr-repository.html), and [EKS Kubernetes](eks-kubernetes.html) to learn deployment, image hygiene, and multi-architecture node groups.

---

## Structured learning journeys

### Beginner (4 weeks)

1. **Week 1 – Foundations**: Complete Cloud Practitioner Essentials, read the Well-Architected overview, and deploy the FsCDK getting-started stack.
2. **Week 2 – Serverless fundamentals**: Follow [Lambda Quickstart](lambda-quickstart.html), watch the AWS Lambda tutorial by Danilo Poccia (4.8★ on Manning LiveBook), and implement Powertools logging.
3. **Week 3 – Data layer**: Work through [S3 Quickstart](s3-quickstart.html) and [DynamoDB](dynamodb.html), then compare your design with Alex DeBrie's single-table examples.
4. **Week 4 – Integration**: Combine SNS, SQS, and EventBridge ([SNS SQS Messaging](sns-sqs-messaging.html), [EventBridge](eventbridge.html)). Review your architecture against the Serverless Lens of Well-Architected.

### Intermediate (4 weeks)

- **Week 1 – Production Lambda**: Study [Lambda Production Defaults](lambda-production-defaults.html), Yan Cui’s concurrency guide, and enable reserved concurrency + DLQs.
- **Week 2 – Data mastery**: Read 3 chapters of The DynamoDB Book, watch Houlihan’s talk, and model 5 access patterns in FsCDK.
- **Week 3 – Security**: Complete the IAM workshop, refactor policies using [Managed Policy](managed-policy.html), and run Access Analyzer.
- **Week 4 – Orchestration**: Dive into [Step Functions](step-functions.html), follow the AWS saga pattern blog, and build a rollback-capable workflow.

### Advanced (continuous)

- Design event-first systems with EventBridge archives and schema registry.
- Practice multi-region failover by synthesizing stacks in at least two regions.
- Join the [Well-Architected Labs](https://www.wellarchitectedlabs.com/) security, reliability, and cost tracks and codify every guard rail in FsCDK.

---

## Books & long-form courses (all >4.5★ average ratings)

1. **Production-Ready Serverless** – Yan Cui – Cohort course with live architecture reviews.
2. **The DynamoDB Book** – Alex DeBrie – Deep modeling workbook and case studies.
3. **AWS Lambda in Action (2nd Edition)** – Danilo Poccia – Comprehensive serverless playbook.
4. **The Good Parts of AWS** – Daniel Vassallo & Josh Pschorr – Pragmatic cost and architecture guidance.
5. **Kubernetes on AWS** – Heitor Lessa & EKS team (O’Reilly live course) – Container orchestration in production.

---

## Stay current

- Subscribe to **Off-by-none** (Jeremy Daly) and **AWS Week in Review** for curated weekly updates.
- Follow AWS Heroes on X / LinkedIn: Yan Cui, Heitor Lessa, Alex DeBrie, Ben Kehoe, Scott Piper, Rich Buggy.
- Join the **ServerlessLand** Slack and **AWS Community Builders** program to discuss new launches with practitioners.
- Re-run the FsCDK notebooks quarterly, updating defaults as AWS announces new features at re:Invent or re:Inforce.

**Keep building.** The fastest way to master AWS is to ship infrastructure repeatedly—FsCDK gives you the expressive, type-safe toolkit to do it with confidence.

*)
