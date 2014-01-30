
#r "System.IO"
#r "System.Net.Http"
#r "System.Runtime"
#r "System.Collections"

#r "./bin/debug/Newtonsoft.Json.dll"  
#r "./bin/debug/Wireclub.Boundary.dll"
#r "./bin/debug/Wireclub.AppClient.dll"  



open System
open System.Collections.Concurrent
open System.Net.Http
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Wireclub.Boundary

module Serialization =
    let deserializeUser (payload:JToken) =
        {
            Id = payload.[0].Value<string>()
            Name = payload.[1].Value<string>()
            Slug = payload.[2].Value<string>()
            Avatar20 = payload.[3].Value<string>()
            Blocked = Convert.ToBoolean (payload.[4].Value<int>())
            Friend = Convert.ToBoolean (payload.[5].Value<int>())
            Moderator = Convert.ToBoolean (payload.[6].Value<int>())
            Staff = Convert.ToBoolean (payload.[7].Value<int>())
            Gender = payload.[8].Value<string>()
            Age = payload.[9].Value<int>()
            Location = payload.[10].Value<string>()
            Url = payload.[11].Value<string>()
            Membership = payload.[12].Value<string>()
            Avatar35 = payload.[13].Value<string>()
        }

    let deserializeEventList (events:JToken) =
        [|
            for message in events ->
                let _ = message.[2] // ???
                let payload = message.[3]
                { 
                    Sequence = message.[0].Value<int64>() 
                    User = message.[4].Value<string>()
                    Event = 
                        match message.[1].Value<int>() with
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
                        | 102 -> PrivateMessage
                        | 103 -> PrivateMessageSent
                        | 1000 -> PeekAvailable
                        | 10000 -> BingoRoundChanged
                        | 10001 -> BingoRoundDraw
                        | 10002 -> BingoRoundWon
                        | _ -> Unknown
                }
        |]

    let deserializeEvents (payload:JArray) =
        payload.[0].Value<int64>(),
        if payload.Count > 1 then (
            [|
            for channelMessages in (payload.[1]) ->
                channelMessages.[0].Value<string>(),
                deserializeEventList (channelMessages.[1])
        |]) else [| |]


module ChannelClient =
    let channelServer = "chat.wireclub.com"

    let url hash sequence watching ignoring =
        sprintf "http://%s/channel/%s/%i?w=%s&i=%s" channelServer hash sequence (String.concat "," watching) (String.concat "," ignoring)

    let handlers = ConcurrentDictionary<string, MailboxProcessor<ChannelEvent>>()

    let client = new HttpClient()
    let rec poll sequence = async {
        try
            printfn "Poll: %i" sequence

            let! resp = 
                client.GetStringAsync (url "QXJlVFBkcGtMQVUzeG0yRlNWQm5wUC9PZmh6YTJoKzA4MkhyVSthVWtSRDM3dldmQTZQQUdnPT01" sequence [ "TqieYwU8Cw28bUOH0" ] [])
                |> Async.AwaitTask
        
            let payload = JsonConvert.DeserializeObject resp :?> JArray
            let nextSequence, channels = Serialization.deserializeEvents payload
            for channel, events in channels do            
                match handlers.TryGetValue channel with
                | true, handler ->                 
                    events |> Array.iter handler.Post
                | _-> ()

            return! poll (nextSequence)
        with
        | ex -> 
            printfn "Poll error: %s" (ex.ToString())
            // TODO: Backoff
            do! Async.Sleep (10 * 1000)
            return! poll sequence
    }
        
    Async.StartAsTask (poll 1L)



type ChatRoom = {
    Users: ConcurrentDictionary<string, User>
}

module ChatClient = 
    let join (url:string) = async {
        let client = new HttpClient()
        let! resp = Async.AwaitTask (client.GetStringAsync url)
        let resp = JsonConvert.DeserializeObject resp :?> JToken
        let resp = {
            Events = Serialization.deserializeEventList (resp.["Events"])
            EventGap = resp.["EventGap"].Value<int>()
            Sequence = resp.["Sequence"].Value<int64>()
            Html = resp.["Html"].Value<string>()
            JoinPlaque = resp.["JoinPlaque"].Value<string>()
            Accepted = resp.["Accepted"].Value<bool>()
            HistoricMembers = (resp.["HistoricMembers"] :?> JArray) |> Seq.map (Serialization.deserializeUser) |> Seq.toArray
            Members = (resp.["Members"] :?> JArray) |> Seq.map (Serialization.deserializeUser) |> Seq.toArray
            Channel = JsonConvert.DeserializeObject<ChatRoomDataViewModel>(resp.["Channel"].ToString())
        }

        let users = ConcurrentDictionary<string, User>()
        let addUser = (fun (user:User) -> users.AddOrUpdate (user.Id, user, Func<string,User,User>(fun _ _ -> user)) |> ignore)
        resp.Members |> Array.iter addUser
        resp.HistoricMembers |> Array.iter addUser

        let processor = new MailboxProcessor<ChannelEvent>(fun inbox ->
            let rec loop () = async {
                let! event = inbox.Receive()
                let historic = event.Sequence < resp.Sequence
                match event.Event with
                | Message (color, font, message) -> 
                    let nameplate = 
                        match users.TryGetValue event.User with
                        | true, user -> user.Name
                        | _ -> sprintf "[%s]" event.User
                    printfn "%s: %s" nameplate message
                | Join user -> 
                    if historic = false then
                        addUser user
                    printfn "[join] %s %s" user.Id user.Name
                | Leave user -> 
                    if historic = false then
                        match users.TryRemove user with
                        | true, user -> printfn "[leave] %s" user.Name
                        | _ -> printfn "[leave] %s already gone" user
                | _ -> ()
                return! loop ()
            }

            loop ()
        )

        processor.Start()
        resp.Events |> Array.iter processor.Post

        ChannelClient.handlers.TryAdd(resp.Channel.Id, processor) |> ignore
        
        return resp
    }

    let resp = Async.RunSynchronously (join "http://www.wireclub.com/chat/room/private_chat_lobby/join?csrf-token=UXo1ZkdQditIVzdzUnplWURsamdVVnVPYzRSaFp2anMxaGMwd3dSVktMbVVtdWd2RTRBNzNnPT01&_t=1390943292613")

    let leave room = async {
        ()
    }

    let send room = ()


