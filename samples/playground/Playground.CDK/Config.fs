module Playground.Config

open System

module Environment =
    let getEnvVars () =
        seq {
            let vars = Environment.GetEnvironmentVariables()
            for key in vars.Keys -> string key, string vars[key]
        }
        |> Map.ofSeq

    let tryGetItem key = getEnvVars () |> Map.tryFind key

    let getItem key =
        tryGetItem key
        |> function
            | Some x -> Ok x
            | None -> Error $"Missing environment variable{key}"

type Config = { Account: string; Region: string }

let get () =
    let account =
        Environment.getItem "CDK_DEFAULT_ACCOUNT"
        |> Result.defaultWith (fun _ -> "000000000000")

    let region =
        Environment.getItem "CDK_DEFAULT_REGION"
        |> Result.defaultWith (fun _ -> "eu-west-1")

    { Account = account; Region = region }
