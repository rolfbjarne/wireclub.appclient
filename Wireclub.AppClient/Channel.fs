// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module ChannelClient

open System
open System.Collections.Concurrent
open System.Net.Http
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open ChannelEvent
open Wireclub.Models
open Wireclub.Boundary.Models
open Wireclub.Boundary
open Wireclub.Boundary.Chat
open WebSocketSharp
open Utility

let deserializeUser (payload:JToken) =
    // Rewrite the avatar to be an image token
    let avatar = payload.[3].Value<string>().Split('/')
    let avatar = avatar.[avatar.Length - 2] + "/" + avatar.[avatar.Length - 1]
    {
        Id = payload.[0].Value<string>()
        Name = payload.[1].Value<string>()
        Slug = payload.[2].Value<string>()
        Avatar = avatar
        Blocked = Convert.ToBoolean (payload.[4].Value<int>())
        Friend = Convert.ToBoolean (payload.[5].Value<int>())
        Moderator = Convert.ToBoolean (payload.[6].Value<int>())
        Staff = Convert.ToBoolean (payload.[7].Value<int>())
        Gender = payload.[8].Value<string>()
        Age = payload.[9].Value<string>()
        Location = payload.[10].Value<string>()
        Url = payload.[11].Value<string>()
        Membership = enum<MembershipTypePublic>(payload.[12].Value<int>())
    }

let deserializeAppEvent (payload:JToken) =
    match payload.["Type"].Value<int>() with
    | 0 -> UserPresenceChanged
    | 1 -> UserRelationshipChanged (payload.["Data"].["Id"].Value<string>(), payload.["Data"].["IsBlocked"].Value<bool>())
    | 2 -> ChatNotification
    | 3 -> ChatNotificationClear
    | 4 -> ChatPreferencesChanged
    | 5 -> ClubMembershipChanged
    | 6 -> EntitySubscriptionChanged
    | 7 -> NavigateTo
    | 8 -> SuspendedFromRoom
    | 9 -> SuspendedGlobally
    | 10 -> JoinRoom
    | 11 -> LeaveRoom
    | 12 -> NotificationsChanged
    | 13 -> ActiveChannelsChanged
    | 14 -> DebugEval
    | 15 -> CreditsBalanceChanged (payload.["Data"].Value<int>())
    | 16 -> BingoTicketsCountChanged
    | 17 -> NewFeedItems 
    | 18 -> SlotsTicketsCountChanged
    | 19 -> BlackjackTicketsCountChanged
    | 20 -> BingoBonusWon
    | 21 -> ToastMessage
    | 22 -> PokerStateChanged
    | _ -> AppEventType.Unknown 
 
let deserializeEvent sequence eventType stamp (payload:JToken) user channel =
    { 
        Sequence = sequence
        User = user
        Channel = channel
        EventType = eventType
        Stamp = stamp
        Event = 
            match eventType with
            | 0 -> Notification (payload.Value<string>())
            | 1 -> Message (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
            | 2 -> Join (deserializeUser payload)
            | 3 -> Leave (payload.Value<string>())
            | 4 -> Modifier
            | 5 -> Drink
            | 6 -> ThumbsUp
            | 7 -> ThumbsDown
            | 8 -> Preference
            | 9 -> AddedInvitation
            | 10 -> AddedModerator
            | 11 -> RemovedModerator
            | 12 -> Ticker
            | 13 -> AppEvent (deserializeAppEvent payload, payload.ToString())
            | 14 -> AcceptDrink
            | 15 -> CustomAppEvent (payload.ToString())
            | 16 -> StartApp
            | 17 -> QuitApp
            | 18 -> GameChallenge
            | 19 -> GameMatch
            | 20 -> KeepAlive
            | 21 -> DisposableMessage
            | 102 -> PrivateMessage (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
            | 103 -> PrivateMessageSent (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
            | 1000 -> PeekAvailable
            | 10000 -> BingoRoundChanged (payload.ToString())
            | 10001 -> BingoRoundDraw (payload.ToString())
            | 10002 -> BingoRoundWon (payload.ToString())
            | _ -> Unknown
    }

let deserializeEventList (events:JToken) channel =
    events
    |> Seq.map (fun event -> 
        let sequence = event.[0].Value<int64>()
        let eventType = event.[1].Value<int>()
        let stamp = event.[2].Value<uint64>()
        let payload = event.[3]
        let user = event.[4].Value<string>()
        deserializeEvent sequence eventType stamp payload user channel
    )
    |> Seq.toArray

let deserializeEvents (payload:JArray) channel =
    payload.[0].Value<int64>(),
    if payload.Count > 1 then (
        [|
        for channelMessages in (payload.[1]) ->
            channelMessages.[0].Value<string>(),
            deserializeEventList (channelMessages.[1]) channel
    |]) else [| |]


let mutex = obj()
let sequence = ref 0L
let (webSocket:Ref<WebSocket option>) = ref None
let mutable cancelReconnect = new Threading.CancellationTokenSource()

let init handler =
    cancelReconnect.Cancel()
    cancelReconnect <- new Threading.CancellationTokenSource()
    Async.Start(Timer.ticker (fun _ -> 
        lock mutex (fun _ ->
            match !webSocket with
            | Some current when current.ReadyState = WebSocketState.Open && current.Ping() -> ()
            | _ -> 
                try 
                    let client = new WebSocket(Api.channelServer, Compression = CompressionMethod.Deflate)
                    webSocket := Some client

                    client.OnMessage.Add (fun data -> 
                        try
                            let event = JsonConvert.DeserializeObject data.Data :?> JArray
                            let channel = event.[0].Value<string>()
                            let nextSequence = event.[1].Value<int64>()
                            let eventType = event.[2].Value<int>()
                            let stamp = event.[3].Value<uint64>()
                            let payload = event.[4]
                            let user = event.[5].Value<string>()
                            let event = deserializeEvent nextSequence eventType stamp payload user channel
                            sequence := nextSequence
                            handler channel event
                        with ex ->  Logger.log (ex)
                    )

                    client.OnError.Add (fun e -> Logger.log (new Exception(String.Format("[Channel] Websocket error: {0}", e.Message))))
                    client.OnClose.Add (fun e -> Logger.log (new Exception(String.Format("[Channel] Websocket closed: {0}", e.Reason))))
                    client.OnOpen.Add (fun e ->
                        printfn "[Channel] Connection authenticating"
                        client.Send("auth=" + Api.userToken)
                        client.Send("seq=" + (!sequence).ToString())
                        printfn "[Channel] Connection authenticated"
                    )

                    printfn "[Channel] Opening websocket connection %s" Api.channelServer
                    client.Connect()

                with ex -> Logger.log (ex)
        )
    ) (10 * 1000), cancelReconnect.Token)

let close () =
    lock mutex (fun _ ->
        match !webSocket with
        | Some current ->
            cancelReconnect.Cancel()
            if current.ReadyState <> WebSocketState.Closing && current.ReadyState <> WebSocketState.Closed then
                current.CloseAsync(CloseStatusCode.Normal, "Connection closed explicitly")
            webSocket := None
        | _-> ()
    )
