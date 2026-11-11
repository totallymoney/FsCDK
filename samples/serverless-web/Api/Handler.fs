module Api.Handler

open System
open System.Collections.Generic
open System.Text.Json
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.Model
open Amazon.Lambda.Core
open Amazon.Lambda.Serialization.SystemTextJson

/// HTTP API payload format 2.0 response.
type Response =
    { statusCode: int
      headers: IDictionary<string, string>
      body: string }

let private dynamo = lazy (new AmazonDynamoDBClient())
let private tableName = Environment.GetEnvironmentVariable "TABLE_NAME"

let private respond status (body: obj) =
    { statusCode = status
      headers = dict [ "content-type", "application/json" ]
      body = JsonSerializer.Serialize body }

let rec private tryPath (element: JsonElement) (path: string list) =
    match path with
    | [] -> Some element
    | name :: rest when element.ValueKind = JsonValueKind.Object ->
        match element.TryGetProperty name with
        | true, child -> tryPath child rest
        | false, _ -> None
    | _ -> None

/// The caller's user id from the Cognito JWT the HTTP API authorizer already validated.
let private subject (request: JsonElement) =
    tryPath request [ "requestContext"; "authorizer"; "jwt"; "claims"; "sub" ]
    |> Option.map (fun e -> e.GetString())

/// GET /api/items: the caller's items (partition key = user id).
[<LambdaSerializer(typeof<DefaultLambdaJsonSerializer>)>]
let handle (request: JsonElement) (_context: ILambdaContext) =
    task {
        match subject request with
        | None -> return respond 401 {| message = "unauthorized" |}
        | Some userId ->
            let query =
                QueryRequest(
                    TableName = tableName,
                    KeyConditionExpression = "pk = :pk",
                    ExpressionAttributeValues = Dictionary(dict [ ":pk", AttributeValue(S = userId) ])
                )

            let! result = dynamo.Value.QueryAsync query

            let items =
                result.Items
                |> Seq.map (fun item -> item |> Seq.map (fun kv -> kv.Key, kv.Value.S) |> dict)
                |> Array.ofSeq

            return respond 200 {| items = items |}
    }
