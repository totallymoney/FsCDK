module Playground.Main

open Amazon.CDK.AWS.Lambda
open FsCDK
open dotenv.net


[<EntryPoint>]
let main _ =
    DotEnv.Load()

    let config = Config.get ()

    let app =
        app {
            stack "MyFirstStack" {
                let stackEnv =
                    environment {
                        account config.Account
                        region config.Region
                    }

                stackProps {
                    env stackEnv
                    description "My first CDK stack in F#"
                    tags [ "project", "FsCDK"; "owner", "me" ]
                }

                lambda "Playground-SayHello" {
                    runtime Runtime.DOTNET_8
                    handler "Playground::Playground.Handlers::sayHello"
                    code "../Playground/bin/Release/net8.0/publish"
                    timeout 30.0
                    memory 256
                    description "A simple hello world lambda"
                }
            }
        }

    app |> _.Synth() |> ignore

    0
