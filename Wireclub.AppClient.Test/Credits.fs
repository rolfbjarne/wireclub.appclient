// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module CreditsTests

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
let ``Fetch Bundles`` () =
    Credits.bundles()
    |> run
    |> assertApiResult
    |> ignore
