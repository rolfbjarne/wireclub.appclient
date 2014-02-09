module Settings

open System
open Wireclub.Models

let changeUsername () =
    ()

let changePassword () =
    ()

let profile userId (birthday:DateTime) (gender:GenderType) city firstname lastname (useRealName:bool) bio =
    Api.req<string> "settings/doProfile" "POST" [
        "userId", userId
        "birthday", birthday.ToString()
        "city", city
        "firstname", firstname
        "lastname", lastname
        "userRealName", (useRealName.ToString())
        "gender", (int gender).ToString()
        "bio", bio
    ]

let avatar () =
    ()