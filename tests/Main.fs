module Main

open Expecto

[<EntryPoint>]
let main argv =
    runTestsInAssemblyWithCLIArgs [ No_Spinner ] argv
