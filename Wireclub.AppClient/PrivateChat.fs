﻿module PrivateChat

open Newtonsoft.Json
open Wireclub.Models
open Wireclub.Boundary
open Wireclub.Boundary.Chat
open ChannelEvent

let online () =
    Api.req<PrivateChatFriendsOnline> "privateChat/online" "get" []

let session id = 
    Api.req<SessionResponse> ("privateChat/session/" + id) "post" []

let send receiver message = 
    Api.req<PrivateChatSendResponse> "privateChat/sendPrivateMessage3" "post" (
        [
            "receiver", receiver
            "line", message
        ])

let changeOnlineState (state:OnlineStateType) = 
    Api.req<unit> ("privateChat/changeOnlineState") "post" [ "state", string (int state) ]
