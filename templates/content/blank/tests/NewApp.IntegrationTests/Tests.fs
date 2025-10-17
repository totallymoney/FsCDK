namespace NewApp.IntegrationTests

open Expecto

module Sample =
    [<Tests>]
    let tests =
        testList "integration" [ testCase "placeholder integration test" (fun _ -> ()) ]
