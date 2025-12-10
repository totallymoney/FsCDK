module FsCDK.Tests.SNSTests

open Expecto
open FsCDK
open Amazon.CDK
open Amazon.CDK.AWS.SNS
open Amazon.CDK.AWS.SNS.Subscriptions
open Amazon.CDK.AWS.SQS
open Amazon.CDK.AWS.Lambda
open System.Collections.Generic

// ============================================================================
// Topic Builder Tests
// ============================================================================

[<Tests>]
let sns_topic_dsl_tests =
    testList
        "SNS topic DSL"
        [ test "defaults constructId to topic name" {
              let spec = topic "MyTopic" { () }

              Expect.equal spec.TopicName "MyTopic" "TopicName should be set"
              Expect.equal spec.ConstructId "MyTopic" "ConstructId should default to topic name"
          }

          test "uses custom constructId when provided" {
              let spec = topic "MyTopic" { constructId "MyTopicConstruct" }

              Expect.equal spec.ConstructId "MyTopicConstruct" "Custom constructId should be used"
          }

          test "configures display name" {
              let spec = topic "MyTopic" { displayName "My Notification Topic" }

              Expect.equal spec.Props.DisplayName "My Notification Topic" "DisplayName should be set"
          }

          test "configures FIFO topic" {
              let spec = topic "MyTopic.fifo" { fifo true }

              Expect.isTrue spec.Props.Fifo.Value "Topic should be configured as FIFO"
          }

          test "enables content-based deduplication" {
              let spec =
                  topic "MyTopic.fifo" {
                      fifo true
                      contentBasedDeduplication true
                  }

              Expect.isTrue spec.Props.ContentBasedDeduplication.Value "ContentBasedDeduplication should be enabled"
          }

          test "enforces SSL when configured" {
              let spec = topic "MyTopic" { enforceSSL true }

              Expect.isTrue spec.Props.EnforceSSL.Value "EnforceSSL should be true"
          }

          test "sets signature version" {
              let spec = topic "MyTopic" { signatureVersion "2" }

              Expect.equal spec.Props.SignatureVersion "2" "SignatureVersion should be set"
          }

          test "sets tracing config" {
              let spec = topic "MyTopic" { tracingConfig TracingConfig.ACTIVE }

              Expect.equal spec.Props.TracingConfig.Value TracingConfig.ACTIVE "TracingConfig should be set"
          }

          test "sets message retention period" {
              let spec = topic "MyTopic" { messageRetentionPeriodInDays 7.0 }

              Expect.equal
                  spec.Props.MessageRetentionPeriodInDays.Value
                  7.0
                  "MessageRetentionPeriodInDays should be set"
          }

          test "sets FIFO throughput scope" {
              let spec =
                  topic "MyTopic.fifo" {
                      fifo true
                      fifoThroughputScope FifoThroughputScope.MESSAGE_GROUP
                  }

              Expect.equal
                  spec.Props.FifoThroughputScope.Value
                  FifoThroughputScope.MESSAGE_GROUP
                  "FifoThroughputScope should be set"
          }

          test "combines multiple configurations" {
              let spec =
                  topic "MyTopic.fifo" {
                      constructId "MyFifoTopic"
                      displayName "My FIFO Topic"
                      fifo true
                      contentBasedDeduplication true
                      enforceSSL true
                      signatureVersion "2"
                  }

              Expect.equal spec.ConstructId "MyFifoTopic" "ConstructId should be set"
              Expect.equal spec.Props.DisplayName "My FIFO Topic" "DisplayName should be set"
              Expect.isTrue spec.Props.Fifo.Value "Fifo should be true"
              Expect.isTrue spec.Props.ContentBasedDeduplication.Value "ContentBasedDeduplication should be true"
              Expect.isTrue spec.Props.EnforceSSL.Value "EnforceSSL should be true"
              Expect.equal spec.Props.SignatureVersion "2" "SignatureVersion should be set"
          }

          test "optional settings remain unset when not provided" {
              let spec = topic "SimpleTopic" { () }

              Expect.isNull (box spec.Props.Fifo) "Fifo should be null when not configured"

              Expect.isNull
                  (box spec.Props.ContentBasedDeduplication)
                  "ContentBasedDeduplication should be null when not configured"

              Expect.isNull (box spec.Props.EnforceSSL) "EnforceSSL should be null when not configured"
              Expect.isNull spec.Props.DisplayName "DisplayName should be null when not configured"
              Expect.isNull spec.Props.SignatureVersion "SignatureVersion should be null when not configured"
          } ]
    |> testSequenced

// ============================================================================
// Lambda Subscription Builder Tests
// ============================================================================

[<Tests>]
let lambda_subscription_builder_tests =
    testList
        "Lambda subscription builder"
        [ test "throws when function not specified" {
              Expect.throws (fun () -> lambdaSubscription { () } |> ignore) "Should throw when function not specified"
          }

          test "creates subscription with function using FsCDK lambda builder" {
              stack "LambdaSubTestStack" {
                  let! testFn =
                      lambda "TestFunction" {
                          handler "index.handler"
                          runtime Runtime.NODEJS_20_X
                          code (Code.FromInline("exports.handler = () => {}"))
                      }

                  let sub = lambdaSubscription { handler testFn }

                  Expect.isNotNull sub "Subscription should be created"
                  Expect.isTrue (sub :? LambdaSubscription) "Should be a LambdaSubscription"
              }
          }

          test "creates subscription with dead letter queue" {
              stack "LambdaSubDLQTestStack" {
                  let! testFn =
                      lambda "TestFunction" {
                          handler "index.handler"
                          runtime Runtime.NODEJS_20_X
                          code (Code.FromInline("exports.handler = () => {}"))
                      }

                  let! dlq = queue "DLQ" { () }

                  let sub =
                      lambdaSubscription {
                          handler testFn
                          deadLetterQueue dlq
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          }

          test "creates subscription with filter policy" {
              stack "LambdaSubFilterTestStack" {
                  let! testFn =
                      lambda "TestFunction" {
                          handler "index.handler"
                          runtime Runtime.NODEJS_20_X
                          code (Code.FromInline("exports.handler = () => {}"))
                      }

                  let filterDict =
                      dict [ "eventType", SubscriptionFilter.StringFilter(StringConditions(Allowlist = [| "order" |])) ]

                  let sub =
                      lambdaSubscription {
                          handler testFn
                          filterPolicy filterDict
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          } ]
    |> testSequenced

// ============================================================================
// SQS Subscription Builder Tests
// ============================================================================

[<Tests>]
let sqs_subscription_builder_tests =
    testList
        "SQS subscription builder"
        [ test "throws when queue not specified" {
              Expect.throws (fun () -> sqsSubscription { () } |> ignore) "Should throw when queue not specified"
          }

          test "creates subscription with queue using FsCDK queue builder" {
              stack "SqsSubTestStack" {
                  let! q = queue "TestQueue" { () }

                  let sub = sqsSubscription { queue q }

                  Expect.isNotNull sub "Subscription should be created"
                  Expect.isTrue (sub :? SqsSubscription) "Should be an SqsSubscription"
              }
          }

          test "creates subscription with raw message delivery" {
              stack "SqsSubRawTestStack" {
                  let! q = queue "TestQueue" { () }

                  let sub =
                      sqsSubscription {
                          queue q
                          rawMessageDelivery true
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          }

          test "creates subscription with dead letter queue" {
              stack "SqsSubDLQTestStack" {
                  let! q = queue "TestQueue" { () }
                  let! dlq = queue "DLQ" { () }

                  let sub =
                      sqsSubscription {
                          queue q
                          deadLetterQueue dlq
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          } ]
    |> testSequenced

// ============================================================================
// Email Subscription Builder Tests
// ============================================================================

[<Tests>]
let email_subscription_builder_tests =
    testList
        "Email subscription builder"
        [ test "throws when email not specified" {
              Expect.throws (fun () -> emailSubscription { () } |> ignore) "Should throw when email not specified"
          }

          test "creates subscription with email address" {
              let sub = emailSubscription { email "admin@example.com" }

              Expect.isNotNull sub "Subscription should be created"
              Expect.isTrue (sub :? EmailSubscription) "Should be an EmailSubscription"
          }

          test "creates subscription with json option" {
              let sub =
                  emailSubscription {
                      email "admin@example.com"
                      json true
                  }

              Expect.isNotNull sub "Subscription should be created"
          }

          test "creates subscription with dead letter queue using FsCDK queue builder" {
              stack "EmailSubDLQTestStack" {
                  let! dlq = queue "DLQ" { () }

                  let sub =
                      emailSubscription {
                          email "admin@example.com"
                          deadLetterQueue dlq
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          } ]
    |> testSequenced

// ============================================================================
// SMS Subscription Builder Tests
// ============================================================================

[<Tests>]
let sms_subscription_builder_tests =
    testList
        "SMS subscription builder"
        [ test "throws when phone number not specified" {
              Expect.throws (fun () -> smsSubscription { () } |> ignore) "Should throw when phone number not specified"
          }

          test "creates subscription with phone number" {
              let sub = smsSubscription { phoneNumber "+1234567890" }

              Expect.isNotNull sub "Subscription should be created"
              Expect.isTrue (sub :? SmsSubscription) "Should be an SmsSubscription"
          }

          test "creates subscription with dead letter queue using FsCDK queue builder" {
              stack "SmsSubDLQTestStack" {
                  let! dlq = queue "DLQ" { () }

                  let sub =
                      smsSubscription {
                          phoneNumber "+1234567890"
                          deadLetterQueue dlq
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          } ]
    |> testSequenced

// ============================================================================
// URL Subscription Builder Tests
// ============================================================================

[<Tests>]
let url_subscription_builder_tests =
    testList
        "URL subscription builder"
        [ test "throws when url not specified" {
              Expect.throws (fun () -> urlSubscription { () } |> ignore) "Should throw when url not specified"
          }

          test "creates subscription with url" {
              let sub = urlSubscription { url "https://example.com/webhook" }

              Expect.isNotNull sub "Subscription should be created"
              Expect.isTrue (sub :? UrlSubscription) "Should be a UrlSubscription"
          }

          test "creates subscription with raw message delivery" {
              let sub =
                  urlSubscription {
                      url "https://example.com/webhook"
                      rawMessageDelivery true
                  }

              Expect.isNotNull sub "Subscription should be created"
          }

          test "creates subscription with protocol" {
              let sub =
                  urlSubscription {
                      url "https://example.com/webhook"
                      protocol SubscriptionProtocol.HTTPS
                  }

              Expect.isNotNull sub "Subscription should be created"
          }

          test "creates subscription with dead letter queue using FsCDK queue builder" {
              stack "UrlSubDLQTestStack" {
                  let! dlq = queue "DLQ" { () }

                  let sub =
                      urlSubscription {
                          url "https://example.com/webhook"
                          deadLetterQueue dlq
                      }

                  Expect.isNotNull sub "Subscription should be created"
              }
          } ]
    |> testSequenced

// ============================================================================
// Topic with Subscriptions Integration Tests
// ============================================================================

[<Tests>]
let topic_with_subscriptions_tests =
    testList
        "Topic with subscriptions"
        [ test "topic with single subscription" {
              let sub = emailSubscription { email "admin@example.com" }

              let spec =
                  topic "MyTopic" {
                      displayName "My Topic"
                      subscription sub
                  }

              Expect.equal spec.Subscriptions.Length 1 "Should have one subscription"
          }

          test "topic with multiple subscriptions using subscriptions" {
              let emailSub = emailSubscription { email "admin@example.com" }
              let smsSub = smsSubscription { phoneNumber "+1234567890" }
              let urlSub = urlSubscription { url "https://example.com/webhook" }

              let spec =
                  topic "MyTopic" {
                      displayName "My Topic"
                      subscriptions [ emailSub; smsSub; urlSub ]
                  }

              Expect.equal spec.Subscriptions.Length 3 "Should have three subscriptions"
          }

          test "topic with mixed subscription operations" {
              let emailSub = emailSubscription { email "admin@example.com" }
              let smsSub = smsSubscription { phoneNumber "+1234567890" }

              let spec =
                  topic "MyTopic" {
                      displayName "My Topic"
                      subscription emailSub
                      subscription smsSub
                  }

              Expect.equal spec.Subscriptions.Length 2 "Should have two subscriptions"
          }

          test "topic with sqs and lambda subscriptions using FsCDK builders" {
              stack "TopicWithSubsTestStack" {
                  let! q = queue "TestQueue" { () }

                  let! testFn =
                      lambda "TestFunction" {
                          handler "index.handler"
                          runtime Runtime.NODEJS_20_X
                          code (Code.FromInline("exports.handler = () => {}"))
                      }

                  let sqsSub =
                      sqsSubscription {
                          queue q
                          rawMessageDelivery true
                      }

                  let lambdaSub = lambdaSubscription { handler testFn }

                  let spec =
                      topic "MyTopic" {
                          displayName "My Topic"
                          subscriptions [ sqsSub; lambdaSub ]
                      }

                  Expect.equal spec.Subscriptions.Length 2 "Should have two subscriptions"
              }
          }

          test "complete topic configuration with all features" {
              let emailSub =
                  emailSubscription {
                      email "admin@example.com"
                      json true
                  }

              let spec =
                  topic "MyTopic.fifo" {
                      constructId "MyTopicConstruct"
                      displayName "My Notification Topic"
                      fifo true
                      contentBasedDeduplication true
                      enforceSSL true
                      signatureVersion "2"
                      subscription emailSub
                  }

              Expect.equal spec.ConstructId "MyTopicConstruct" "ConstructId should be set"
              Expect.equal spec.Props.DisplayName "My Notification Topic" "DisplayName should be set"
              Expect.isTrue spec.Props.Fifo.Value "Fifo should be true"
              Expect.isTrue spec.Props.ContentBasedDeduplication.Value "ContentBasedDeduplication should be true"
              Expect.isTrue spec.Props.EnforceSSL.Value "EnforceSSL should be true"
              Expect.equal spec.Props.SignatureVersion "2" "SignatureVersion should be set"
              Expect.equal spec.Subscriptions.Length 1 "Should have one subscription"
          }

          test "topic with implicit yield for single subscription" {
              let spec =
                  topic "ImplicitYieldTopic" {
                      displayName "Implicit Yield Test"
                      emailSubscription { email "test@example.com" }
                  }

              Expect.equal spec.Subscriptions.Length 1 "Should have one subscription from implicit yield"
          }

          test "topic with implicit yield for multiple subscriptions" {
              let spec =
                  topic "MultiImplicitYieldTopic" {
                      displayName "Multi Implicit Yield Test"
                      emailSubscription { email "admin@example.com" }
                      smsSubscription { phoneNumber "+1234567890" }
                  }

              Expect.equal spec.Subscriptions.Length 2 "Should have two subscriptions from implicit yields"
          }

          test "topic with implicit yield mixed with custom operation" {
              let urlSub = urlSubscription { url "https://webhook.example.com/sns" }

              let spec =
                  topic "MixedYieldTopic" {
                      displayName "Mixed Yield Test"
                      emailSubscription { email "ops@example.com" }
                      subscription urlSub
                      smsSubscription { phoneNumber "+9876543210" }
                  }

              Expect.equal spec.Subscriptions.Length 3 "Should have three subscriptions from mixed yields"
          } ]
    |> testSequenced
