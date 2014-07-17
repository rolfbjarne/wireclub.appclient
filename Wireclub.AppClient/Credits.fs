// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Credits

open System
open Wireclub.Models
open Wireclub.Boundary.Models
open Wireclub.Boundary.Settings

let bundles () =
    Api.req<CreditBundles> "api/credits/bundles" "get" []

let appStorePurchase tx receipt =
    Api.req<string> "api/credits/appStorePurchase" "post" [ "tx", tx ; "receipt", receipt ]
