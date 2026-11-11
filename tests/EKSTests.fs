module FsCDK.Tests.EKSTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.EKS
open Amazon.CDK.AWS.Lambda

let private kubectl (scope: Constructs.Construct) : ILayerVersion =
    LayerVersion.FromLayerVersionArn(scope, "Kubectl", "arn:aws:lambda:us-east-1:123456789012:layer:kubectl:1")

[<Tests>]
let eks_cluster_tests =
    testSequenced
    <| testList
        "EKS Cluster DSL"
        [ test "cluster synthesizes with a version and a kubectl layer" {
              let app = App()

              stack "EksStack" {
                  scope app
                  let! clusterVpc = vpc "EksVpc" { maxAzs 2 }

                  eksCluster "test-cluster" {
                      vpc clusterVpc
                      version KubernetesVersion.V1_33
                      kubectlLayer kubectl
                      defaultCapacity 0
                  }
              }

              let artifact = app.Synth().GetStackByName("EksStack")
              let template = System.IO.File.ReadAllText artifact.TemplateFullPath
              Expect.stringContains template "Custom::AWSCDK-EKS-Cluster" "The cluster should reach the template"
          }

          test "kubectl layer is required" {
              let app = App()
              let scope = Stack(app, "NoLayerStack")
              let clusterVpc = Amazon.CDK.AWS.EC2.Vpc(scope, "Vpc")

              Expect.throwsT<System.ArgumentException>
                  (fun () ->
                      eksCluster "no-layer" {
                          vpc clusterVpc
                          version KubernetesVersion.V1_33
                      }
                      |> ignore)
                  "A cluster without a kubectl layer should be refused"
          }

          test "version is required" {
              let app = App()
              let scope = Stack(app, "NoVersionStack")
              let clusterVpc = Amazon.CDK.AWS.EC2.Vpc(scope, "Vpc")

              Expect.throwsT<System.ArgumentException>
                  (fun () ->
                      eksCluster "no-version" {
                          vpc clusterVpc
                          kubectlLayer kubectl
                      }
                      |> ignore)
                  "A cluster without a Kubernetes version should be refused"
          } ]
