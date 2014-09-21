// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Credits

open System
open Wireclub.Models
open Wireclub.Boundary.Models
open Wireclub.Boundary.Settings
open Newtonsoft.Json
open System.Net.Http

let bundles () =
    Api.req<CreditBundles> "api/credits/bundles" "get" []

let appStoreTransaction data receipt =
    let json =
        JsonConvert.SerializeObject 
            {
                AppStoreTransactionRequest.Data = data
                AppStoreTransactionRequest.Receipt = receipt
            }
    let content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    Api.post<AppStoreTransactionResponse> "api/credits/appStoreTransaction"

let appStorePurchase tx receipt =
    Api.req<CreditBundle> "api/credits/appStorePurchase" "post" [ "tx", tx ; "receipt", receipt ]
