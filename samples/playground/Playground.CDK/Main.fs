module Playground.Main

open Amazon.CDK
open Amazon.CDK.AWS.DynamoDB
open Amazon.CDK.AWS.Lambda
open FsCDK
open dotenv.net


[<EntryPoint>]
let main _ =
    DotEnv.Load()

    let config = Config.get ()

    let createStack envName =
        stack envName {
            environment {
                account config.Account
                region config.Region
            }

            let! functionSpec =
                lambda $"playground-{envName}-sayHello" {
                    runtime Runtime.DOTNET_8
                    description "Playground sayHello function"
                    handler "Playground::Playground.Handlers::sayHello"
                    code "../Playground/bin/Release/net8.0/publish"
                    timeout 30.0
                    timeout 30.0
                }

            table $"playground-{envName}-table" {
                partitionKey "Id" AttributeType.STRING
                removalPolicy RemovalPolicy.DESTROY
                billingMode BillingMode.PAY_PER_REQUEST
                pointInTimeRecovery true
                grantReadData functionSpec
            }
        }

    createStack "stage"
    createStack "prod"
    0
