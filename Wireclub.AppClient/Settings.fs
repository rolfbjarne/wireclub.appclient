module Settings

open System
open Wireclub.Models
open Wireclub.Boundary.Models
open Wireclub.Boundary.Settings

let changeUsername () =
    ()

let changePassword () =
    ()

let avatar data =
    Api.upload<Image> "settings/doAvatar" "avatar" "avatar.jpg" data

let profile username (gender:GenderType) (birthday:DateTime) country region city bio =
    Api.req<User> "api/settings/profile" "post" 
        [
            "userId", Api.userId
            "username", username
            "gender", string gender
            "birthday-year", string birthday.Year
            "birthday-month", string birthday.Month
            "birthday-day", string birthday.Day
            "bio", bio
            "country", country
            "region", region
            "city", city
        ]

let countries () =
    Api.req<LocationCountry[]> "api/settings/countries" "post" []

let regions country =
    Api.req<LocationRegion[]> "api/settings/regions" "post" [ "country", country ]

let email email confirmation password =
    Api.req<EmailFormData> "settings/doEmail" "post" [ "email", email; "confirmation", confirmation; "password", password ]