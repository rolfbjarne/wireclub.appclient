module User

open System

let block slug =
    Api.post<unit> (sprintf "users/%s/block" slug)

let unblock slug =
    Api.post<unit> (sprintf "users/%s/unblock" slug)

let addFriend slug =
    Api.post<unit> (sprintf "users/%s/addFriend" slug)

let removeFriend slug =
    Api.post<unit> (sprintf "users/%s/removeFriend" slug)

let fetch id = 
    //TODO Make this return an actual user
    async {
        let! account = Account.identity()
        return
            match account with
            | Api.ApiOk account -> Api.ApiOk { account with Id = id }
            | error -> Api.Exception (new Exception())
    }
   