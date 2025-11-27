module FsCDK.Tests.DynamoDBTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.IAM

[<Tests>]
let dynamo_table_dsl_tests =
    testList
        "DynamoDB table DSL"
        [ test "fails when partition key is missing" {
              let thrower () = table "MyTable" { () } |> ignore
              Expect.throws thrower "Table builder should throw when partition key is missing"
          }

          test "defaults constructId to table name" {
              let spec = table "Users" { partitionKey "pk" AttributeType.STRING }

              Expect.equal spec.TableName "Users" "TableName should be set"
              Expect.equal spec.ConstructId "Users" "ConstructId should default to table name"
          }

          test "uses custom constructId when provided" {
              let spec =
                  table "Users" {
                      constructId "UsersTable"
                      partitionKey "pk" AttributeType.STRING
                  }

              Expect.equal spec.ConstructId "UsersTable" "Custom constructId should be used"
          }

          test "applies sort key when configured" {
              let spec =
                  table "Orders" {
                      partitionKey "orderId" AttributeType.STRING
                      sortKey "createdAt" AttributeType.NUMBER
                  }

              Expect.isNotNull (box spec.Props.SortKey) "SortKey should be set"
              Expect.equal spec.Props.SortKey.Name "createdAt" "SortKey name should match"
              Expect.equal spec.Props.SortKey.Type AttributeType.NUMBER "SortKey type should match"
          }

          test "applies billing mode when configured" {
              let importSourceSpecification =
                  { new IImportSourceSpecification with
                      member this.Bucket = failwith "todo"
                      member this.InputFormat = failwith "todo" }

              let spec =
                  table "Billing" {
                      partitionKey "pk" AttributeType.STRING
                      billingMode BillingMode.PAY_PER_REQUEST

                      importSourceSpecification

                  }

              Expect.equal spec.Props.BillingMode.Value BillingMode.PAY_PER_REQUEST "Billing mode should be set"
          }

          test "applies removal policy when configured" {
              let spec =
                  table "Tmp" {
                      partitionKey "pk" AttributeType.STRING
                      removalPolicy RemovalPolicy.DESTROY
                  }

              Expect.equal spec.Props.RemovalPolicy.Value RemovalPolicy.DESTROY "Removal policy should be set"
          }

          test "enables point-in-time recovery when set to true" {
              let spec =
                  table "History" {
                      partitionKey "pk" AttributeType.STRING
                      pointInTimeRecovery true
                  }

              Expect.isNotNull (box spec.Props.PointInTimeRecoverySpecification) "PITR spec should be created"

              Expect.isTrue
                  spec.Props.PointInTimeRecoverySpecification.PointInTimeRecoveryEnabled
                  "PITR should be enabled"
          }

          test "optional sort key remains unset when not provided" {
              let spec = table "Simple" { partitionKey "pk" AttributeType.STRING }

              Expect.isNull (box spec.Props.SortKey) "SortKey should be null when not configured"
          }

          test "applies import source when configured via class" {
              let app = App()
              let stack = Stack(app, "Test")
              let bucket = Bucket(stack, "Bucket")
              let importSpec = ImportSourceSpecification()
              importSpec.Bucket <- bucket
              importSpec.InputFormat <- InputFormat.DynamoDBJson()
              importSpec.KeyPrefix <- "prefix"

              let spec =
                  table "Import" {
                      partitionKey "pk" AttributeType.STRING
                      importSpec
                      stream StreamViewType.NEW_IMAGE
                  }

              Expect.isNotNull (box spec.Props.ImportSource) "ImportSource should be set"
          }

          test "applies import source when configured via builder" {
              let app = App()
              let stack = Stack(app, "Test2")
              let myBucket = Bucket(stack, "Bucket2")

              let spec =
                  table "Import2" {
                      partitionKey "pk" AttributeType.STRING

                      importSource {
                          bucket myBucket
                          inputFormat (InputFormat.DynamoDBJson())
                          keyPrefix "prefix"
                      }
                  }

              Expect.isNotNull (box spec.Props.ImportSource) "ImportSource should be set via builder"
          }

          // Production defaults tests (Alex DeBrie / Rick Houlihan best practices)
          test "defaults to PAY_PER_REQUEST billing mode" {
              let spec = table "DefaultBilling" { partitionKey "pk" AttributeType.STRING }

              Expect.equal spec.Props.BillingMode.Value BillingMode.PAY_PER_REQUEST "Should default to PAY_PER_REQUEST"
          }

          test "defaults to point-in-time recovery enabled" {
              let spec = table "DefaultPITR" { partitionKey "pk" AttributeType.STRING }

              Expect.isNotNull
                  (box spec.Props.PointInTimeRecoverySpecification)
                  "PITR spec should be created by default"

              Expect.isTrue
                  spec.Props.PointInTimeRecoverySpecification.PointInTimeRecoveryEnabled
                  "PITR should be enabled by default"
          }

          test "allows disabling point-in-time recovery" {
              let spec =
                  table "NoPITR" {
                      partitionKey "pk" AttributeType.STRING
                      pointInTimeRecovery false
                  }

              Expect.isNull
                  (box spec.Props.PointInTimeRecoverySpecification)
                  "PITR spec should not be created when disabled"
          }

          // TTL tests
          test "applies time-to-live attribute when configured" {
              let spec =
                  table "WithTTL" {
                      partitionKey "pk" AttributeType.STRING
                      timeToLive "expiresAt"
                  }

              Expect.isNotNull (box spec.Props.TimeToLiveAttribute) "TTL attribute should be set"
              Expect.equal spec.Props.TimeToLiveAttribute "expiresAt" "TTL attribute name should match"
          }

          // GSI tests
          test "adds global secondary index with partition key only" {
              let spec =
                  table "WithGSI" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING
                      globalSecondaryIndex "GSI1" ("gsi1pk", AttributeType.STRING)
                  }

              Expect.equal spec.GlobalSecondaryIndexes.Length 1 "Should have 1 GSI"
              Expect.equal spec.GlobalSecondaryIndexes.[0].IndexName "GSI1" "GSI name should match"
              let pkName, _ = spec.GlobalSecondaryIndexes.[0].PartitionKey
              Expect.equal pkName "gsi1pk" "GSI partition key should match"
          }

          test "adds global secondary index with sort key" {
              let spec =
                  table "WithGSISort" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING

                      globalSecondaryIndexWithSort
                          "GSI1"
                          ("gsi1pk", AttributeType.STRING)
                          ("gsi1sk", AttributeType.NUMBER)
                  }

              Expect.equal spec.GlobalSecondaryIndexes.Length 1 "Should have 1 GSI"
              let sortKey = spec.GlobalSecondaryIndexes.[0].SortKey
              Expect.isSome sortKey "GSI should have a sort key"
              let skName, skType = sortKey.Value
              Expect.equal skName "gsi1sk" "GSI sort key should match"
              Expect.equal skType AttributeType.NUMBER "GSI sort key type should match"
          }

          test "adds multiple global secondary indexes" {
              let spec =
                  table "WithMultipleGSI" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING
                      globalSecondaryIndex "GSI1" ("gsi1pk", AttributeType.STRING)
                      globalSecondaryIndex "GSI2" ("gsi2pk", AttributeType.STRING)

                      globalSecondaryIndexWithSort
                          "GSI3"
                          ("gsi3pk", AttributeType.STRING)
                          ("gsi3sk", AttributeType.NUMBER)
                  }

              Expect.equal spec.GlobalSecondaryIndexes.Length 3 "Should have 3 GSIs"
          }

          test "adds global secondary index with projection" {
              let spec =
                  table "WithGSIProjection" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING

                      globalSecondaryIndexWithProjection
                          "GSI1"
                          ("gsi1pk", AttributeType.STRING)
                          None
                          ProjectionType.KEYS_ONLY
                          []
                  }

              Expect.equal
                  spec.GlobalSecondaryIndexes.[0].ProjectionType
                  (Some ProjectionType.KEYS_ONLY)
                  "GSI projection type should match"
          }

          // LSI tests
          test "adds local secondary index" {
              let spec =
                  table "WithLSI" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING
                      localSecondaryIndex "LSI1" ("lsi1sk", AttributeType.NUMBER)
                  }

              Expect.equal spec.LocalSecondaryIndexes.Length 1 "Should have 1 LSI"
              Expect.equal spec.LocalSecondaryIndexes.[0].IndexName "LSI1" "LSI name should match"
              let skName, _ = spec.LocalSecondaryIndexes.[0].SortKey
              Expect.equal skName "lsi1sk" "LSI sort key should match"
          }

          test "adds multiple local secondary indexes" {
              let spec =
                  table "WithMultipleLSI" {
                      partitionKey "pk" AttributeType.STRING
                      sortKey "sk" AttributeType.STRING
                      localSecondaryIndex "LSI1" ("lsi1sk", AttributeType.NUMBER)
                      localSecondaryIndex "LSI2" ("lsi2sk", AttributeType.STRING)
                  }

              Expect.equal spec.LocalSecondaryIndexes.Length 2 "Should have 2 LSIs"
          }

          // Table class tests
          test "applies table class when configured" {
              let spec =
                  table "InfrequentAccess" {
                      partitionKey "pk" AttributeType.STRING
                      tableClass TableClass.STANDARD_INFREQUENT_ACCESS
                  }

              Expect.equal spec.Props.TableClass.Value TableClass.STANDARD_INFREQUENT_ACCESS "Table class should be set"
          }

          // Contributor insights tests
          test "enables contributor insights when configured" {
              let spec =
                  table "WithInsights" {
                      partitionKey "pk" AttributeType.STRING
                      contributorInsights true
                  }

              Expect.isNotNull (box spec.Props.ContributorInsightsSpecification) "Contributor insights should be set"
              Expect.isTrue spec.Props.ContributorInsightsSpecification.Enabled "Contributor insights should be enabled"
          }

          // Grant support tests
          test "grantReadData sets GrantReadData on spec" {
              let app = App()

              stack "GrantReadDataStack" {
                  scope app

                  let! role = role "ReaderRole" { assumedBy (ServicePrincipal "lambda.amazonaws.com") }

                  let table =
                      table "GrantReadDataTable" {
                          partitionKey "pk" AttributeType.STRING
                          grantReadData role
                      }

                  match table.Grant with
                  | Some(GrantReadData grantee) -> Expect.equal grantee role "Grantee should match the provided role"
                  | _ -> failtest "Expected GrantReadData to be set"
              }

          }

          test "grantFullAccess sets GrantFullAccess on spec" {
              let app = App()

              stack "GrantFullAccessStack" {
                  scope app

                  let! role = role "FullAccessRole" { assumedBy (ServicePrincipal "lambda.amazonaws.com") }

                  let table =
                      table "GrantFullAccessTable" {
                          partitionKey "pk" AttributeType.STRING
                          grantFullAccess role

                      }

                  match table.Grant with
                  | Some(GrantFullAccess grantee) -> Expect.equal grantee role "Grantee should match the provided role"
                  | _ -> failtest "Expected GrantFullAccess to be set"
              }
          }

          test "grantReadWriteData sets GrantReadWriteData on spec" {
              let app = App()

              stack "GrantReadWriteStack" {
                  scope app

                  let! role = role "ReadWriteRole" { assumedBy (ServicePrincipal "lambda.amazonaws.com") }

                  let table =
                      table "GrantReadWriteTable" {
                          partitionKey "pk" AttributeType.STRING
                          grantReadWriteData role
                      }

                  match table.Grant with
                  | Some(GrantReadWriteData grantee) ->
                      Expect.equal grantee role "Grantee should match the provided role"
                  | _ -> failtest "Expected GrantReadWriteData to be set"

              }
          }

          test "grantWriteData sets GrantWriteData on spec" {
              let app = App()

              stack "GrantWriteStack" {
                  scope app

                  let! role = role "WriterRole" { assumedBy (ServicePrincipal "lambda.amazonaws.com") }

                  let table =
                      table "GrantWriteTable" {
                          partitionKey "pk" AttributeType.STRING
                          grantWriteData role

                      }

                  match table.Grant with
                  | Some(GrantWriteData grantee) -> Expect.equal grantee role "Grantee should match the provided role"
                  | _ -> failtest "Expected GrantWriteData to be set"
              }
          }

          test "grantStreamRead sets GrantStreamRead on spec" {
              let app = App()
              let stack = Stack(app, "GrantStreamReadStack")

              let role =
                  Role(stack, "StreamReaderRole", RoleProps(AssumedBy = ServicePrincipal("lambda.amazonaws.com")))

              let spec =
                  table "GrantStreamReadTable" {
                      partitionKey "pk" AttributeType.STRING
                      stream StreamViewType.NEW_AND_OLD_IMAGES
                      grantStreamRead role
                  }

              match spec.Grant with
              | Some(GrantStreamRead grantee) -> Expect.equal grantee role "Grantee should match the provided role"
              | _ -> failtest "Expected GrantStreamRead to be set"
          }

          test "grantStream sets GrantStream with actions on spec" {
              let app = App()
              let stack = Stack(app, "GrantStreamStack")

              let role =
                  Role(stack, "StreamRole", RoleProps(AssumedBy = ServicePrincipal("lambda.amazonaws.com")))

              let actions = [ "dynamodb:DescribeStream"; "dynamodb:GetRecords" ]

              let spec =
                  table "GrantStreamTable" {
                      partitionKey "pk" AttributeType.STRING
                      stream StreamViewType.NEW_IMAGE
                      grantStream role actions
                  }

              match spec.Grant with
              | Some(GrantStream(grantee, acts)) ->
                  Expect.equal grantee role "Grantee should match the provided role"
                  Expect.sequenceEqual acts actions "Actions should be preserved"
              | _ -> failtest "Expected GrantStream to be set"
          }

          test "grantTableListStreams sets GrantTableListStreams on spec" {
              let app = App()
              let stack = Stack(app, "GrantTableListStreamsStack")

              let role =
                  Role(stack, "ListStreamsRole", RoleProps(AssumedBy = ServicePrincipal("lambda.amazonaws.com")))

              let spec =
                  table "GrantTableListStreamsTable" {
                      partitionKey "pk" AttributeType.STRING
                      grantTableListStreams role
                  }

              match spec.Grant with
              | Some(GrantTableListStreams grantee) ->
                  Expect.equal grantee role "Grantee should match the provided role"
              | _ -> failtest "Expected GrantTableListStreams to be set"
          }

          test "grant sets custom actions on spec" {
              let app = App()

              stack "GrantCustomStack" {
                  scope app

                  let! role = role "CustomRole" { assumedBy (ServicePrincipal "lambda.amazonaws.com") }

                  let spec =
                      table "GrantCustomTable" {
                          partitionKey "pk" AttributeType.STRING
                          grant role [ "dynamodb:BatchGetItem"; "dynamodb:Scan" ]
                      }

                  match spec.Grant with
                  | Some(Grant(grantee, acts)) ->
                      Expect.equal grantee role "Grantee should match the provided role"

                      Expect.sequenceEqual
                          acts
                          [ "dynamodb:BatchGetItem"; "dynamodb:Scan" ]
                          "Actions should be preserved"
                  | _ -> failtest "Expected Grant with custom actions to be set"
              }
          } ]
    |> testSequenced
