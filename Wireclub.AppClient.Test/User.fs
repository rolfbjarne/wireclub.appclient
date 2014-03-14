module UserTests

open Helpers
open NUnit.Framework


[<SetUp>]
let setup () =
    Helpers.setup ()

[<Test>]
let ``Fetch Entity by Slug`` () =
    User.entityBySlug "chris"
    |> run
    |> assertApiResult
    |> printfn "%A"

// ## Join errors