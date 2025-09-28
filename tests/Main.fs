module Main

open Expecto

[<EntryPoint>]
let main argv =
    // Run tests sequentially to avoid JSII runtime contention during CDK static initializations
    Tests.runTestsInAssemblyWithCLIArgs [ No_Spinner; Sequenced ] argv
