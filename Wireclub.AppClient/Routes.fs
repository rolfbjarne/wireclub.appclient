// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Routes

open System

let (|Route|_|) (route:string) (url:string) =
    let uri = new Uri (url)
    let url = uri.PathAndQuery.Split('?').[0].TrimEnd([| '/' |])
    let m = System.Text.RegularExpressions.Regex("^" + route).Match(url)

    if m.Success then 
        Some (List.tail [ for x in m.Groups -> System.Net.WebUtility.HtmlDecode x.Value ])
    else 
        None
    
let (|User|_|) = function | Route "/users/(.+?)(/|$)" [ id ] -> Some id | _ -> None
let (|ChatRoom|_|) = function | Route "/chat/room/(.+?)(/|$)" [ id ] -> Some id | _ -> None
let (|ChatSession|_|) = function | Route "/privateChat/session/(.+?)(/|$)" [ id ] -> Some id | _ -> None
let (|YouTube|_|) = function | Route "/video/youtube/(.+?)(/|$)" [ id ] -> Some id | _ -> None
let (|ExternalRedirect|_|) = function | Route "/redirect/url/(.+?)(/|$)" [ id ] -> Some id | _ -> None
