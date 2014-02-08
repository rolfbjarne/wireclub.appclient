module Web

open System

let embedUrl url =
    let appendQuery q (url:string) =
        if url.Contains "?" then
            url + "&" + q
        else
            url + "?" + q

    url
    |> appendQuery ("app-session=" + Api.userHash)
    |> appendQuery "mobileApp=true" // Apps should add the appropriate agent header to all requests, but this is nice for testing