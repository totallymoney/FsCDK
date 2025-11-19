(**
---
title: EKS (Elastic Kubernetes Service) Example
category: Resources
categoryindex: 12
---

# ![EKS](img/icons/Arch_Amazon-Elastic-Kubernetes-Service_48.png) Amazon EKS (Elastic Kubernetes Service) Example

This example demonstrates how to create Amazon EKS (Elastic Kubernetes Service) clusters using FsCDK for container orchestration with Kubernetes.

## What is EKS?

Amazon Elastic Kubernetes Service (EKS) is a managed Kubernetes service that makes it easy to run Kubernetes on AWS without needing to install and operate your own Kubernetes control plane.

**Key Benefits:**
- Fully managed Kubernetes control plane
- Automatic upgrades and patching
- Integration with AWS services (IAM, VPC, CloudWatch)
- High availability across multiple AZs
- Support for both EC2 and Fargate compute

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html) (`npm install -g aws-cdk`)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) for cluster management
- AWS credentials configured (for deployment)

## Basic EKS Cluster
*)

#r "../src/bin/Release/net8.0/publish/Amazon.JSII.Runtime.dll"
#r "../src/bin/Release/net8.0/publish/Constructs.dll"
#r "../src/bin/Release/net8.0/publish/Amazon.CDK.Lib.dll"
#r "../src/bin/Release/net8.0/publish/System.Text.Json.dll"
#r "../src/bin/Release/net8.0/publish/FsCDK.dll"

open Amazon.CDK
open Amazon.CDK.AWS.EC2
open Amazon.CDK.AWS.EKS
open Amazon.CDK.AWS.IAM
open FsCDK

(*** hide ***)
module Config =
    let get () =
        {| Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")
           Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") |}

let config = Config.get ()

stack "BasicEKSStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "Basic EKS cluster with managed node group"

    tags [ "Project", "FsCDK-Examples"; "Service", "EKS"; "ManagedBy", "FsCDK" ]


    // Create VPC for EKS cluster
    let clusterVpc =
        vpc "EKSVpc" {
            maxAzs 3
            natGateways 1
            cidr "10.0.0.0/16"
        }

    // Create EKS cluster with secure defaults
    let cluster =
        eksCluster "MyEKSCluster" {
            vpc clusterVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0
            endpointAccess EndpointAccess.PUBLIC_AND_PRIVATE

            setClusterLogging
                [ ClusterLoggingTypes.API
                  ClusterLoggingTypes.AUDIT
                  ClusterLoggingTypes.AUTHENTICATOR
                  ClusterLoggingTypes.CONTROLLER_MANAGER
                  ClusterLoggingTypes.SCHEDULER ]

            addNodegroupCapacity (
                "StandardNodeGroup",
                NodegroupOptions(
                    InstanceTypes = [| InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM) |],
                    MinSize = 1.,
                    MaxSize = 3.,
                    DesiredSize = 2.,
                    DiskSize = 20.
                )
            )

        }

    ()
}

(**
## Advanced Cluster Configurations

### Fargate Profile for Serverless Pods

Run pods without managing EC2 instances using AWS Fargate.

**Benefits:**
- No node management overhead
- Pay only for pod resources
- Automatic scaling
- Improved security isolation
*)

stack "FargateEKSStack" {
    description "EKS cluster with Fargate profiles"

    let fargateVpc =
        vpc "FargateVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let fargateCluster =
        eksCluster "FargateCluster" {
            vpc fargateVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0

            addFargateProfile (
                "FargateProfile",
                FargateProfileOptions(
                    Selectors =
                        [| Selector(Namespace = "default")
                           Selector(Namespace = "kube-system")
                           Selector(Namespace = "production", Labels = dict [ "compute-type", "serverless" ]) |]
                )
            )

        }

    ()
}

(**
### Multi-Architecture Node Groups

Support both x86 and ARM workloads for cost optimization.

**ARM (Graviton) Benefits:**
- 20% better price/performance
- Lower energy consumption
- Same performance as x86 for most workloads
*)

stack "MultiArchEKSStack" {
    description "EKS cluster with x86 and ARM node groups"

    let multiArchVpc =
        vpc "MultiArchVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let multiArchCluster =
        eksCluster "MultiArchCluster" {
            vpc multiArchVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0

            addNodegroupCapacity (
                "ARMNodeGroup", // ARM-based nodes (Graviton)
                NodegroupOptions(
                    InstanceTypes = [| InstanceType("t4g.medium") |],
                    MinSize = 1.,
                    MaxSize = 5.,
                    DesiredSize = 2.,
                    AmiType = NodegroupAmiType.AL2_ARM_64,
                    Labels = dict [ "arch", "arm64" ]
                )
            )

            addNodegroupCapacity (
                "X86NodeGroup", // x86 nodes
                NodegroupOptions(
                    InstanceTypes = [| InstanceType("t3.medium") |],
                    MinSize = 1.,
                    MaxSize = 5.,
                    DesiredSize = 2.,
                    AmiType = NodegroupAmiType.AL2_X86_64,
                    Labels = dict [ "arch", "amd64" ]
                )
            )
        }

    ()
}

(**
### Spot Instances for Cost Optimization

Use Spot Instances for fault-tolerant workloads to save up to 90%.

**Best for:**
- Batch processing
- CI/CD pipelines
- Stateless applications
- Non-critical workloads
*)

stack "SpotEKSStack" {
    description "EKS cluster with Spot instance node group"

    let spotVpc =
        vpc "SpotVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let spotCluster =
        eksCluster "SpotCluster" {
            vpc spotVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0

            // On-demand nodes for critical workloads
            addNodegroupCapacity (
                "OnDemandNodes",
                NodegroupOptions(
                    InstanceTypes = [| InstanceType("t3.medium") |],
                    MinSize = 1.,
                    MaxSize = 3.,
                    DesiredSize = 2.,
                    Labels = dict [ "capacity-type", "on-demand" ]
                )
            )

            // Spot instance node group
            addNodegroupCapacity (
                "SpotNodes",
                NodegroupOptions(
                    InstanceTypes =
                        [| InstanceType("t3.medium")
                           InstanceType("t3a.medium")
                           InstanceType("t2.medium") |],
                    CapacityType = CapacityType.SPOT,
                    MinSize = 0.,
                    MaxSize = 10.,
                    DesiredSize = 2.,
                    Labels = dict [ "capacity-type", "spot" ]

                )
            )
        }

    ()
}

(**
## Security Best Practices

### IRSA (IAM Roles for Service Accounts)

Grant Kubernetes pods fine-grained IAM permissions without sharing credentials.

**Benefits:**
- Least-privilege access per pod
- No shared credentials
- Audit trail via CloudTrail
- Automatic credential rotation
*)

stack "IRSAEKSStack" {
    description "EKS cluster with IRSA for pod permissions"

    let irsaVpc =
        vpc "IRSAVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let appServiceAccount =
        eksCluster "SpotCluster" {
            vpc irsaVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0

            addServiceAccount ("AppServiceAccount", ServiceAccountOptions(Name = "my-app-sa", Namespace = "default"))
        }

    // Create S3 bucket
    let appBucket =
        s3Bucket "app-bucket" {
            versioned true
            encryption Amazon.CDK.AWS.S3.BucketEncryption.S3_MANAGED
        }

    // Grant S3 access to the service account
    //appBucket.Bucket.Value.GrantReadWrite(appServiceAccount) |> ignore

    ()
}

(**
### Secrets Encryption with KMS

Enable envelope encryption for Kubernetes secrets at rest.

**Security:**
- Protects sensitive data in etcd
- Automatic key rotation
- Audit logs via CloudTrail
*)

stack "SecureEKSStack" {
    description "EKS cluster with KMS secrets encryption"

    let secureVpc =
        vpc "SecureVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    // Create KMS key for secrets encryption
    let secretsKey =
        kmsKey "EKSSecretsKey" {
            description "KMS key for EKS secrets encryption"
            enableKeyRotation
        }

    let secureCluster =
        eksCluster "SecureCluster" {
            vpc secureVpc
            version KubernetesVersion.V1_28
            encryptionKey secretsKey
            endpointAccess EndpointAccess.PRIVATE

            setClusterLogging
                [ ClusterLoggingTypes.API
                  ClusterLoggingTypes.AUDIT
                  ClusterLoggingTypes.AUTHENTICATOR ]
        }

    ()
}

(**
## Kubernetes Deployments

### Deploy Application Manifests

Apply Kubernetes resources directly from CDK.
*)

(*
stack "K8sAppStack" {
    stackProps { description "EKS cluster with Kubernetes application" }

    let appVpc =
        vpc "AppVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let appCluster =
        eksCluster "AppCluster" {
            vpc appVpc
            version KubernetesVersion.V1_28
        }

    // Deploy nginx application
    appCluster.AddManifest(
        "NginxDeployment",
        dict
            [ "apiVersion", box "apps/v1"
              "kind", box "Deployment"
              "metadata", box (dict [ "name", box "nginx"; "namespace", box "default" ])
              "spec",
              box (
                  dict
                      [ "replicas", box 3
                        "selector", box (dict [ "matchLabels", box (dict [ "app", box "nginx" ]) ])
                        "template",
                        box (
                            dict
                                [ "metadata", box (dict [ "labels", box (dict [ "app", box "nginx" ]) ])
                                  "spec",
                                  box (
                                      dict
                                          [ "containers",
                                            box
                                                [| dict
                                                       [ "name", box "nginx"
                                                         "image", box "nginx:1.25"
                                                         "ports", box [| dict [ "containerPort", box 80 ] |]
                                                         "resources",
                                                         box (
                                                             dict
                                                                 [ "requests",
                                                                   box (
                                                                       dict [ "memory", box "128Mi"; "cpu", box "100m" ]
                                                                   )
                                                                   "limits",
                                                                   box (
                                                                       dict [ "memory", box "256Mi"; "cpu", box "200m" ]
                                                                   ) ]
                                                         ) ] |] ]
                                  ) ]
                        ) ]
              ) ]
    )
    |> ignore

    // Deploy service
    appCluster.AddManifest(
        "NginxService",
        dict
            [ "apiVersion", box "v1"
              "kind", box "Service"
              "metadata", box (dict [ "name", box "nginx-service"; "namespace", box "default" ])
              "spec",
              box (
                  dict
                      [ "type", box "LoadBalancer"
                        "selector", box (dict [ "app", box "nginx" ])
                        "ports", box [| dict [ "port", box 80; "targetPort", box 80; "protocol", box "TCP" ] |] ]
              ) ]
    )
    |> ignore

    ()
}
*)

(**
### Install Helm Charts

Install applications using Helm package manager.
*)

stack "HelmChartsStack" {
    description "EKS cluster with Helm chart installations"

    let helmVpc =
        vpc "HelmVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let helmCluster =
        eksCluster "HelmCluster" {
            vpc helmVpc
            version KubernetesVersion.V1_28

            addHelmChart (
                // Install AWS Load Balancer Controller
                "AWSLoadBalancerController",
                HelmChartOptions(
                    Chart = "aws-load-balancer-controller",
                    Repository = "https://aws.github.io/eks-charts",
                    Namespace = "kube-system",
                    Values =
                        dict
                            [ "clusterName", box "HelmCluster"
                              "serviceAccount.create", box false
                              "serviceAccount.name", box "aws-load-balancer-controller" ]
                )
            )

            addHelmChart (
                // Install metrics-server for HPA
                "MetricsServer",
                HelmChartOptions(
                    Chart = "metrics-server",
                    Repository = "https://kubernetes-sigs.github.io/metrics-server/",
                    Namespace = "kube-system",
                    Values = dict [ "replicas", box 2 ]
                )
            )

            addHelmChart (
                // Install Prometheus for monitoring
                "Prometheus",
                HelmChartOptions(
                    Chart = "kube-prometheus-stack",
                    Repository = "https://prometheus-community.github.io/helm-charts",
                    Namespace = "monitoring",
                    CreateNamespace = true,
                    Values =
                        dict
                            [ "prometheus.prometheusSpec.retention", box "30d"
                              "grafana.enabled", box true ]
                )
            )
        }

    ()
}

(**
## Auto Scaling

### Cluster Autoscaler

Automatically adjust node group size based on pod resource requests.
*)

stack "AutoScalingEKSStack" {
    description "EKS cluster with autoscaling"

    let autoScaleVpc =
        vpc "AutoScaleVpc" {
            maxAzs 2
            cidr "10.0.0.0/16"
        }

    let autoScaleCluster =
        eksCluster "AppCluster" {
            vpc autoScaleVpc
            version KubernetesVersion.V1_28

            // Auto-scaling node group
            addNodegroupCapacity (
                "AutoScalingNodes",
                NodegroupOptions(
                    InstanceTypes = [| InstanceType("t3.medium") |],
                    MinSize = 2.,
                    MaxSize = 10.,
                    DesiredSize = 3.,
                    Labels = dict [ "node-group", "autoscaling" ]
                )
            )

            // Install Cluster Autoscaler via Helm
            addHelmChart (
                "ClusterAutoscaler",
                HelmChartOptions(
                    Chart = "cluster-autoscaler",
                    Repository = "https://kubernetes.github.io/autoscaler",
                    Namespace = "kube-system",
                    Values =
                        dict
                            [ "autoDiscovery.clusterName", box "AutoScalingCluster"
                              "awsRegion", box config.Region
                              "rbac.create", box true
                              "rbac.serviceAccount.name", box "cluster-autoscaler" ]
                )
            )

        }

    ()
}

(**
## Complete Production Example
*)

stack "ProductionEKSStack" {
    environment {
        account config.Account
        region config.Region
    }

    description "Production-ready EKS cluster with security and monitoring"
    tags [ "Environment", "Production"; "Project", "K8sCluster"; "ManagedBy", "FsCDK" ]

    // Production VPC with high availability
    let prodVpc =
        vpc "ProductionVPC" {
            maxAzs 3
            natGateways 3 // One NAT gateway per AZ for HA
            cidr "10.0.0.0/16"
        }

    // KMS key for secrets encryption

    let eksKey =
        kmsKey "ProdEKSKey" {
            description "Production EKS secrets encryption key"
            alias "alias/prod-eks-secrets"
            enableKeyRotation
        }

    // Production EKS cluster
    let prodCluster =
        eksCluster "ProductionCluster" {
            vpc prodVpc
            version KubernetesVersion.V1_28
            defaultCapacity 0
            endpointAccess EndpointAccess.PRIVATE // Private API for security
            encryptionKey eksKey

            setClusterLogging
                [ ClusterLoggingTypes.API
                  ClusterLoggingTypes.AUDIT
                  ClusterLoggingTypes.AUTHENTICATOR
                  ClusterLoggingTypes.CONTROLLER_MANAGER
                  ClusterLoggingTypes.SCHEDULER ]

            // On-demand node group for critical workloads
            addNodegroupCapacity (
                "CriticalNodes",
                NodegroupOptions(
                    InstanceTypes = [| InstanceType("t3.large") |],
                    MinSize = 3.,
                    MaxSize = 10.,
                    DesiredSize = 5.,
                    Labels = dict [ "workload-type", "critical"; "capacity-type", "on-demand" ],
                    Tags = dict [ "Name", "eks-critical-node"; "Environment", "production" ]
                )
            )

            // Spot instance node group for batch workloads
            addNodegroupCapacity (
                "BatchNodes",
                NodegroupOptions(
                    InstanceTypes =
                        [| InstanceType("t3.large")
                           InstanceType("t3a.large")
                           InstanceType("t3.xlarge") |],
                    CapacityType = CapacityType.SPOT,
                    MinSize = 0.,
                    MaxSize = 20.,
                    DesiredSize = 3.,
                    Labels = dict [ "workload-type", "batch"; "capacity-type", "spot" ]
                )
            )

            // Install essential add-ons
            addHelmChart (
                "MetricsServer",
                HelmChartOptions(
                    Chart = "metrics-server",
                    Repository = "https://kubernetes-sigs.github.io/metrics-server/",
                    Namespace = "kube-system",
                    Values =
                        dict
                            [ "replicas", box 3
                              "resources.requests.cpu", box "100m"
                              "resources.requests.memory", box "200Mi" ]
                )
            )

            addHelmChart (
                "ClusterAutoscaler",
                HelmChartOptions(
                    Chart = "cluster-autoscaler",
                    Repository = "https://kubernetes.github.io/autoscaler",
                    Namespace = "kube-system",
                    Values =
                        dict
                            [ "autoDiscovery.clusterName", box "ProductionCluster"
                              "awsRegion", box config.Region
                              "extraArgs.scale-down-delay-after-add", box "10m"
                              "extraArgs.skip-nodes-with-local-storage", box false ]
                )
            )

            // Monitoring stack
            addHelmChart (
                "PrometheusStack",
                HelmChartOptions(
                    Chart = "kube-prometheus-stack",
                    Repository = "https://prometheus-community.github.io/helm-charts",
                    Namespace = "monitoring",
                    CreateNamespace = true,
                    Values =
                        dict
                            [ "prometheus.prometheusSpec.retention", box "30d"
                              "prometheus.prometheusSpec.storageSpec.volumeClaimTemplate.spec.resources.requests.storage",
                              box "50Gi"
                              "grafana.enabled", box true
                              "grafana.adminPassword", box "ChangeMeInProduction!"
                              "alertmanager.enabled", box true ]
                )
            )

        }

    ()
}

(**
## Access Control

### Configure kubectl Access

After deployment, configure kubectl to access your cluster:

```bash
# Update kubeconfig
aws eks update-kubeconfig --name ProductionCluster --region us-east-1

# Verify access
kubectl get nodes
kubectl get pods --all-namespaces

# View cluster info
kubectl cluster-info
```

### Grant IAM Users/Roles Access

Grant additional AWS users or roles access to the cluster:
*)

//let developerRole =
//    Role.FromRoleArn(this, "DeveloperRole", "arn:aws:iam::123456789012:role/DeveloperRole")

// prodCluster.AwsAuth.AddRoleMapping(developerRole, AwsAuthMapping(
//     Groups = [| "developers" |],
//     Username = "developer"
// )) |> ignore

(**
## Cost Optimization

### EKS vs ECS Comparison

| Feature | ECS | EKS |
|---------|-----|-----|
| **Control Plane Cost** | Free | $0.10/hour ($73/month) |
| **Compute Cost** | EC2/Fargate pricing | EC2/Fargate pricing |
| **Learning Curve** | Lower | Higher |
| **Portability** | AWS-only | Multi-cloud |
| **Ecosystem** | AWS-focused | Kubernetes ecosystem |
| **Best For** | Simple containers | Complex orchestration |

### Cost Savings Strategies

1. **Use Fargate for variable workloads**: Pay only for pod resources
2. **Spot Instances for batch jobs**: Save up to 90%
3. **Graviton (ARM) instances**: 20% better price/performance
4. **Cluster Autoscaler**: Scale down during low usage
5. **Reserved Instances**: Up to 72% savings for baseline capacity

### Example Monthly Costs

**Small Cluster:**
- Control plane: $73
- 3 t3.medium nodes (on-demand): ~$90
- Total: ~$163/month

**Production Cluster:**
- Control plane: $73
- 5 t3.large nodes (on-demand): ~$305
- 10 t3.large spot nodes (average): ~$60
- Total: ~$438/month

## Deployment

```bash
# Synthesize CloudFormation template
cdk synth ProductionEKSStack

# Deploy to AWS (takes 15-20 minutes)
cdk deploy ProductionEKSStack

# Configure kubectl
aws eks update-kubeconfig --name ProductionCluster --region <region>

# Verify cluster
kubectl get nodes

# Destroy resources when done
cdk destroy ProductionEKSStack
```

## Troubleshooting

### Common Issues

**Issue: Pods stuck in Pending state**
```bash
# Check pod events
kubectl describe pod <pod-name>

# Common causes:
# - Insufficient node capacity (add more nodes)
# - Resource requests too high (adjust limits)
# - Node selector mismatch (check labels)
```

**Issue: Cannot connect to cluster**
```bash
# Verify AWS credentials
aws sts get-caller-identity

# Update kubeconfig
aws eks update-kubeconfig --name <cluster-name>

# Check endpoint access
# Ensure EndpointAccess allows your connection source
```

## Next Steps

- Review [ECS Examples](ec2-ecs.html) for container comparison
- Explore [IAM Best Practices](iam-best-practices.html) for RBAC
- Read [Lambda Integration](lambda-quickstart.html) for serverless hybrid architectures
- Learn about [Kubernetes best practices](https://kubernetes.io/docs/concepts/configuration/overview/)

## Resources

- [AWS EKS Documentation](https://docs.aws.amazon.com/eks/)
- [EKS Best Practices Guide](https://aws.github.io/aws-eks-best-practices/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [AWS CDK EKS Module](https://docs.aws.amazon.com/cdk/api/v2/docs/aws-cdk-lib.aws_eks-readme.html)
- [eksctl](https://eksctl.io/) - CLI for EKS management
- [Helm Charts](https://artifacthub.io/) - Kubernetes package manager
*)

(*** hide ***)
()
