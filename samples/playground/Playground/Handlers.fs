module Playground.Handlers

open Amazon.Lambda.Core
open Amazon.Lambda.Serialization.SystemTextJson

[<LambdaSerializer(typeof<DefaultLambdaJsonSerializer>)>]
let sayHello (message: {| Name: string; Message: string |}) (_lambdaContext: ILambdaContext) =
    printfn "Hello %s, %s" message.Name message.Message
