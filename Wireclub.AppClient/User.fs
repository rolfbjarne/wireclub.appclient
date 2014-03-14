module User

open System
open Wireclub.Boundary.Models

let block slug =
    Api.post<unit> (sprintf "users/%s/block" slug)

let unblock slug =
    Api.post<unit> (sprintf "users/%s/unblock" slug)

let addFriend slug =
    Api.post<unit> (sprintf "users/%s/addFriend" slug)

let removeFriend slug =
    Api.post<unit> (sprintf "users/%s/removeFriend" slug)

let fetch (ids:string list) = 
    //TODO Make this return an actual user
    async {
        let! account = Account.identity()
        return
            match account with
            | Api.ApiOk account ->
                Api.ApiOk (  [ for id in ids do yield  { account with Id = id } ] )
            | error -> Api.Exception (new Exception())
    }
    
let entityBySlug slug =
    Api.req<Entity> ("/users/" + slug + "/entityData") "get" [ ]

   