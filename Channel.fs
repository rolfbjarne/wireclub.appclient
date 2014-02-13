module ChannelClient

open System
open System.Collections.Concurrent
open System.Net.Http
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open ChannelEvent
open Wireclub.Boundary.Models
open Wireclub.Boundary
open Wireclub.Boundary.Chat
open WebSocketSharp

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
        Age = payload.[9].Value<int>()
        Location = payload.[10].Value<string>()
        Url = payload.[11].Value<string>()
        Membership = payload.[12].Value<string>()
    }

let deserializeEvent sequence eventType stamp (payload:JToken) user =
    { 
        Sequence = sequence
        User = user
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
            | 13 -> AppEvent
            | 14 -> AcceptDrink
            | 15 -> CustomAppEvent
            | 16 -> StartApp
            | 17 -> QuitApp
            | 18 -> GameChallenge
            | 19 -> GameMatch
            | 20 -> KeepAlive
            | 21 -> DisposableMessage
            | 102 -> PrivateMessage (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
            | 103 -> PrivateMessageSent (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
            | 1000 -> PeekAvailable
            | 10000 -> BingoRoundChanged
            | 10001 -> BingoRoundDraw
            | 10002 -> BingoRoundWon
            | _ -> Unknown
    }

let deserializeEventToken (event:JToken) =
    let sequence = event.[0].Value<int64>()
    let eventType = event.[1].Value<int>()
    let stamp = event.[2]
    let payload = event.[3]
    let user = event.[4].Value<string>()
    deserializeEvent sequence eventType stamp payload user

let deserializeEventList (events:JToken) =
    events |> Seq.map deserializeEventToken |> Seq.toArray

let deserializeEvents (payload:JArray) =
    payload.[0].Value<int64>(),
    if payload.Count > 1 then (
        [|
        for channelMessages in (payload.[1]) ->
            channelMessages.[0].Value<string>(),
            deserializeEventList (channelMessages.[1])
    |]) else [| |]

let handlers = ConcurrentDictionary<string, MailboxProcessor<ChannelEvent>>()

let init = 
    let mutex = obj()
    let sequence = ref 0L
    let (webSocket:Ref<WebSocket option>) = ref None

    let rec init () =
        lock mutex (fun _ ->
            if !webSocket = None then
                let client = new WebSocket(Api.channelServer, Compression = CompressionMethod.DEFLATE)
                webSocket := Some client

                client.OnMessage.Add (fun data -> 
                    try
                        let event = JsonConvert.DeserializeObject data.Data :?> JArray
                        let channel = event.[0].Value<string>()
                        let nextSequence = event.[1].Value<int64>()
                        let eventType = event.[2].Value<int>()
                        let stamp = event.[3]
                        let payload = event.[4]
                        let user = event.[5].Value<string>()
                        let event = deserializeEvent nextSequence eventType stamp payload user
                        sequence := nextSequence
                        match handlers.TryGetValue channel with
                        | true, handler -> handler.Post event
                        | _ -> ()
                    with
                    | ex -> printfn "[Channel] Message error: %s" (ex.ToString())
                )

                Async.Start <| async {

                    printfn "[Channel] Opening websocket connection %s" Api.channelServer
                    client.Connect()
                    client.Send("auth=" + Api.userToken)
                    client.Send("seq=" + (!sequence).ToString())
                    printfn "[Channel] Connection open"

                    client.OnError.Add (fun e ->
                        printfn "[Channel] Websocket error: %s" e.Message
                        client.CloseAsync ()
                        webSocket := None
                        init ()
                    )

                    client.OnClose.Add (fun e ->
                        printfn "[Channel] Websocket closed: %s" e.Reason
                        webSocket := None
                        init ()
                    )
                }
            )
    init