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

