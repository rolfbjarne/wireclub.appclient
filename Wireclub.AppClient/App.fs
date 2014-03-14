module App

open Wireclub.Boundary.Models

let mutable identity:User option = None

let tokenDecode (input:string) =
    let length = input.Length
    let num = (int input.[length - 1]) - 48
    let inArray = Array.zeroCreate<char> (length - 1 + num)
    for index in 0 .. length - 2 do
        match input.[index] with
        | '-' -> inArray.[index] <- '+'
        | '_' -> inArray.[index] <- '/'
        | c -> inArray.[index] <- c
        
    for index in length - 1 .. inArray.Length - 1 do
        inArray.[index] <- '='

    System.Convert.FromBase64CharArray (inArray, 0, inArray.Length)
    |> System.Text.Encoding.Default.GetString

let tokenEncode (input:string) =
    let input = System.Text.Encoding.Default.GetBytes input
    let str = System.Convert.ToBase64String (input)
    let mutable length = str.Length
    while (length > 0 && (int str.[length - 1]) = 61) do
        length <- length - 1
    let chArray = Array.zeroCreate<char> (length + 1)
    chArray.[length] <- (char (48 + str.Length - length))
    for index in 0 .. length - 1 do
        match str.[index] with
        | '+' -> chArray.[index] <- '-'
        | '/' -> chArray.[index] <- '_'
        | '=' as ch
        | ch ->
            chArray.[index] <- ch

    System.String.Concat (chArray)
    
let imageUrl (imageId:string) size = 
    match imageId.Replace("/images", "").Trim('/').Split('/') with
    | [| imageId; dimensions |] ->
        match (tokenDecode dimensions).Split ',' with
        | [| width; height; hash |] ->
            sprintf "%s/images/%s/%s" Api.staticBaseUrl imageId (tokenEncode (sprintf "%i,%i,%s" size size hash))
        | _ -> failwith "Invalid imageId (dimension)"
    | _ -> failwith "Invalid image id"


let fetchActiveChannels () =
    Api.req<Wireclub.Boundary.Chat.ChatRoomDataViewModel> "home/fetchActiveChannels"

