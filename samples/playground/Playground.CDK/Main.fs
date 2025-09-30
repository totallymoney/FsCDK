module Playground.Main

open Amazon.CDK.AWS.Lambda
open FsCDK

[<EntryPoint>]
let main _ =
    app {
        stack "MyFirstStack" {
            lambda "Playground-SayHello" {
                runtime Runtime.DOTNET_8
                handler "Playground::Handlers::sayHello"
                code (Code.FromAsset("../Playground/bin/Release/net8.0/publish"))
                timeout 30.0
                memory 256
                description "A simple hello world lambda"
            }
        }
    }
    |> _.Synth()
    |> ignore

    0
