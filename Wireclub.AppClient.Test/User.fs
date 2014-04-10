// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module UserTests

open Helpers
open NUnit.Framework


[<SetUp>]
let setup () =
    Helpers.setup ()
    
[<Test>]
let ``Fetch User`` () =
    User.fetch "chris"
    |> run
    |> assertApiResult
    |> printfn "%A"

[<Test>]
let ``Fetch Entity by Slug`` () =
    User.entityBySlug "chris"
    |> run
    |> assertApiResult
    |> printfn "%A"

// ## Join errors
