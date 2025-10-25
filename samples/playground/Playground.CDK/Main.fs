﻿module Playground.Main

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

            lambda $"playground-{envName}-sayHello" {
                runtime Runtime.DOTNET_8
                handler "Playground::Playground.Handlers::sayHello"
                code "../Playground/bin/Release/net8.0/publish"
                timeout 30.0
            }
        }

    createStack "stage"
    createStack "prod"
    0
