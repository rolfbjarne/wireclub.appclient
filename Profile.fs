module Profile

open System
open Wireclub.Models
open Wireclub.Boundary.Models

let avatar data =
    Api.upload<Image> "settings/doAvatar" "avatar" "avatar.jpg" data

let update username (gender:GenderType) (birthday:DateTime) (color:int) location bio = async {
    let! result = 
        Api.req<User> "settings/doProfile" "post" 
            [
                "userId", Api.userId
                "username", username
                "gender", string gender
                "birthday-year", string birthday.Year
                "birthday-month", string birthday.Month
                "birthday-day", string birthday.Day
                "bio", bio
                "favoriteColorId", string color
                "locationId", location
            ]
    return result
}