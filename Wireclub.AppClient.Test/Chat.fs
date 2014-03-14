module ChatTests

open Helpers
open NUnit.Framework


[<SetUp>]
let setup () =
    Helpers.setup ()
    Helpers.login ()

[<Test>]
let Directory () =
    Chat.directory ()
    |> run
    |> assertApiResult
    |> printfn "%A"

[<Test>]
let ``Fetch Entity by Slug`` () =
    Chat.entityBySlug "private_chat_lobby"
    |> run
    |> assertApiResult
    |> printfn "%A"

// ## Join errors

[<Test>]
let Join () =
    Chat.join "private_chat_lobby"
    |> run
    |> assertApiResult
    |> printfn "%A"