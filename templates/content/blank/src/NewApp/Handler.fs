namespace NewApp

open Amazon.Lambda.Core
open Amazon.Lambda.Serialization.SystemTextJson

type HelloRequest =
    { Name: string
      Message: string }

type HelloResponse = { Response: string }

[<LambdaSerializer(typeof<DefaultLambdaJsonSerializer>)>]
let sayHello (request: HelloRequest) (_context: ILambdaContext) =
    let response = $"Hello %s{request.Name}, %s{request.Message}"
    printfn $"%s{response}"
    { Response = response }
