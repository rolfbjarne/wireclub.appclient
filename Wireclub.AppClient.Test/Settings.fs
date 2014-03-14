module Places

open System
open Helpers
open NUnit.Framework
open Helpers
open Wireclub.Models

[<SetUp>]
let setup () =
    Helpers.setup ()
    Helpers.login ()

[<Test>]
let ``Update Profile`` () =
    Settings.profile username GenderType.Male  (DateTime.UtcNow.AddYears(-30)) "ca" "British Columbia" "Surrey" "A little about myself"
    |> run
    |> assertApiResult
    |> ignore

[<Test>]
let ``Fetch Countries`` () = 
    let countries = 
        Settings.countries ()
        |> run
        |> assertApiResult

    printfn "%A" countries
    ()

[<Test>]
let ``Fetch Regions`` () = 
    let regions = 
        Settings.regions "CA"
        |> run
        |> assertApiResult

    printfn "%A" regions
    ()