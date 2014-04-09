// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module AppTests

open System
open Helpers
open NUnit.Framework
open Helpers
open Wireclub.Models

open NUnit.Framework

[<Test>]
let ``Token Encode / Decode`` () =
    let test = "axv/234*#@$(*&23a'-dfdfa-_2342adf"
    let encoded = App.tokenEncode test
    let decoded = App.tokenDecode encoded   
    Assert.AreEqual (test, decoded)

[<Test>]
let ``Report Errors`` () =
    App.reportErrors [ "error 1" ; "error 2" ]
    |> run
    |> assertApiResult
    |> ignore
