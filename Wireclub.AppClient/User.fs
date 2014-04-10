// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module User

open System
open Wireclub.Boundary.Models

let fetch slug =
    Api.get<UserProfile> (sprintf "api/users/%s" slug)

let block slug =
    Api.post<unit> (sprintf "users/%s/block" slug)

let unblock slug =
    Api.post<unit> (sprintf "users/%s/unblock" slug)

let addFriend slug =
    Api.post<unit> (sprintf "users/%s/addFriend" slug)

let removeFriend slug =
    Api.post<unit> (sprintf "users/%s/removeFriend" slug)

let entityBySlug slug =
    Api.req<Entity> ("/users/" + slug + "/entityData") "get" [ ]

   
