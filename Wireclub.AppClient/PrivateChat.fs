// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module PrivateChat

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

let setMobile () =
    Api.req<unit> "api/settings/setMobile" "post" []

let updatePresence () =
    Api.req<unit> "home/presence" "post" []
