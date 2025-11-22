module FsCDK.Tests.ElastiCacheTests

open Expecto
open FsCDK

[<Tests>]
let elasticache_redis_tests =
    testList
        "ElastiCache Redis DSL"
        [ test "defaults constructId to cluster name" {
              let cluster = redisCluster "TestCache" { () }
              Expect.equal cluster.ClusterName "TestCache" "Cluster name should be set"
              Expect.equal cluster.ConstructId "TestCache" "ConstructId should default to cluster name"
          }

          test "applies AWS best practices by default" {
              let cluster = redisCluster "TestCache" { () }
              Expect.equal cluster.ClusterName "TestCache" "Should have correct cluster name"
          }

          test "accepts custom cache node type" {
              let configTest () =
                  redisCluster "TestCache" {
                      cacheNodeType "cache.t3.small"
                      numCacheNodes 2
                  }
                  |> ignore

              Expect.isOk (Result.Ok(configTest ())) "Should accept cache node type configuration"
          }

          test "NodeTypes helpers provide standard sizes" {
              Expect.equal ElastiCacheHelpers.NodeTypes.micro "cache.t3.micro" "Micro type should match"
              Expect.equal ElastiCacheHelpers.NodeTypes.small "cache.t3.small" "Small type should match"
          } ]
    |> testSequenced
