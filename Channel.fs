module ChannelClient

open System
open System.Collections.Concurrent
open System.Net.Http
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Wireclub.Boundary
open Wireclub.Boundary.Chat

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
                    | 102 -> PrivateMessage (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
                    | 103 -> PrivateMessageSent (payload.[0].Value<string>(), payload.[1].Value<int>(), payload.[2].Value<string>())
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


let channelServer = "192.168.0.220:10808"

let url hash sequence watching ignoring =
    sprintf "http://%s/channel/%s/%i?w=%s&i=%s" channelServer hash sequence (String.concat "," watching) (String.concat "," ignoring)

let handlers = ConcurrentDictionary<string, MailboxProcessor<ChannelEvent>>()

let client = new HttpClient()
let rec poll sequence = async {
    try
        printfn "Poll: %i | %s" sequence (url Api.userHash sequence [] [])

        let! resp = 
            client.GetStringAsync (url Api.userHash sequence [] [])
            |> Async.AwaitTask
        
        let payload = JsonConvert.DeserializeObject resp :?> JArray
        let nextSequence, channels = deserializeEvents payload
        for channel, events in channels do            
            match handlers.TryGetValue channel with
            | true, handler ->                 
                events |> Array.iter handler.Post
                printfn "%s %i events" channel (events.Length)
            | _-> ()

        return! poll (nextSequence)
    with
    | ex -> 
        printfn "Poll error: %s" (ex.ToString())
        // TODO: Backoff
        do! Async.Sleep (10 * 1000)
        return! poll sequence
}

let mutable polling = false

let init () = 
    if polling = false then
        polling <- true
        Async.StartAsTask (poll 1L) |> ignore
