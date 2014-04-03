﻿module Account

open System
open Newtonsoft.Json
open Wireclub.Boundary
open Wireclub.Boundary.Models

let handleLogin result =
    match result with
    | Api.ApiOk result ->
        Api.client.DefaultRequestHeaders.TryAddWithoutValidation("x-csrf-token", result.Csrf) |> ignore
        Api.userId <- result.Identity.Id
        Api.userIdentity <- Some result.Identity
        Api.userCsrf <- result.Csrf
        Api.userToken <- result.Token
        Api.ApiOk result
    | resp -> resp

let logout () =
    for cookie in Api.handler.CookieContainer.GetCookies(new Uri(Api.baseUrl)) do
        cookie.Expires <- DateTime.UtcNow.AddHours(-1.0)
    Api.userId <- null
    Api.userIdentity <- None
    Api.userCsrf <- null
    Api.userToken <- null

let login username password = async {
    let! resp = 
        Api.req<LoginResult> "account/doLogin" "post" (
            [
                "username", username
                "password", password
            ])
    
    return handleLogin resp
}

let loginToken token = async {
    let! resp = 
        Api.req<LoginResult> "account/tokenLogin" "post" (
            [
                "token", token
            ])
    
    return handleLogin resp
}

let home () =
    Api.req<string> "home" "get" []

let test () = 
    Api.req<string> "test/csrf" "post" []

let identity () =
    Api.req<User> "account/identity" "post" []

let signup email password = async {
    let! result = 
        Api.req<LoginResult> "api/account/signup" "post" 
            [
                "userId", "000000000000000000000000"
                "email", email
                "password", password
            ]

    return handleLogin result
}

let resetPassword email = 
    Api.req<string> "account/doPasswordResetRequest" "post" 
        [
            "email", email
        ]
