// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.


#r "System.IO"
#r "System.Net.Http"
#r "System.Runtime"
#r "System.Collections"

#r "../bin/debug/Newtonsoft.Json.dll"  
#r "../bin/debug/Wireclub.Boundary.dll"
#r "../bin/debug/Wireclub.AppClient.dll"  

open System
open System.Collections.Concurrent
open System.Net.Http
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Wireclub.Boundary
open Wireclub.Boundary.Chat


Async.RunSynchronously <| 
    async {
        let! _ = Account.login "unitman" "testtest"
        let! _ = PrivateChat.initSession "AAAAAAAAAAAAAAAB0"
        ()
    }

Api.userHash
Api.userId


type ChatRoom = {
    Users: ConcurrentDictionary<string, ChatUser>
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

        let users = ConcurrentDictionary<string, ChatUser>()
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


