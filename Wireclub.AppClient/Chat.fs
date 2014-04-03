﻿    
module Chat

open System
open Wireclub.Boundary.Models
open Wireclub.Boundary.Chat
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let directory () =
    Api.req<ChatDirectoryViewModel> "chat/directory" "get" []

let join slug = async {
        let! resp = Api.req<JoinResult> ("chat/room/" + slug + "/join") "post" []
        let resp =
            match resp with
            | Api.ApiOk result ->
                try
                    let events = 
                        ChannelClient.deserializeEventList (JsonConvert.DeserializeObject result.Events :?> JToken) result.Channel.Id

                    Api.ApiOk (result, events)
                with
                // ## TODO handle failer better
                | ex -> Api.Exception ex
            | error -> Api.Exception (new Exception())
        return resp
    }

let keepAlive slug =
    Api.req<unit> ("/api/chat/keepAlive/" + slug) "post" [ ]

let leave slug =
    Api.req<string> ("/chat/room/" + slug + "/leave") "post" [ ]

let send slug line =
    Api.req<ChannelPostResult> ("/chat/room/" + slug + "/send2") "post" [
        "line", line
    ]

let entityBySlug slug =
    Api.req<Entity> ("/chat/room/" + slug + "/entityData") "get" [ ]

let acceptDrink () = ()

let rejectDrink () = ()

let report () = ()

let getAd () = ()

let changePreferences (font:int) (color:int) =
    Api.req<unit> "chat/changePreferences" "post" [
            "font", font.ToString()
            "color", color.ToString()
        ]
