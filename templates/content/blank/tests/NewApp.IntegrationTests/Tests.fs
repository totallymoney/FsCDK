namespace NewApp.IntegrationTests

open Expecto

module Sample =
    [<Tests>]
    let tests =
        testList "integration" [
            ptestCase "placeholder integration test" (fun _ -> ())
        ]
