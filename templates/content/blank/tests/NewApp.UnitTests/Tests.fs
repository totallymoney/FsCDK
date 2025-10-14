namespace NewApp.UnitTests

open Expecto

module Sample =
    [<Tests>]
    let tests =
        testList "sample" [
            testCase "true is true" (fun _ -> Expect.isTrue true "should be true")
        ]
