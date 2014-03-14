module PrivateChatTests

open Wireclub.Models
open Wireclub.Boundary
open Helpers
open NUnit.Framework

[<SetUp>]
let setup () =
    Helpers.setup ()
    Helpers.login ()

[<Test>]
let ``Online Friends`` () = 
    let users = 
        PrivateChat.online ()
        |> run
        |> assertApiResult

    printfn "%A" users
    ()

[<Test>]
let ``Send`` () =
    PrivateChat.send "AAAAAAAAAAAAAAAB0" "Hello, friend"
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Send Error`` () =
    let result =
        PrivateChat.send (Api.userId) "hello, myself"
        |> run
        |> assertApiResult

    match result with
    | { State = EventState.Denied } -> ()
    | _ -> Assert.Fail "Expected denied"
    

[<Test>]
let ``Session`` () =
    PrivateChat.session "AAAAAAAAAAAAAAAB0"
    |> run
    |> assertApiResult
    |> ignore