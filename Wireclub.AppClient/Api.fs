// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Api

open System
open System.Linq
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open System.Collections.Generic
open System.Diagnostics
open Newtonsoft.Json
open Wireclub.Boundary

let version = "1.0"
let mutable agent = "wireclub-app-client/" + version
#if __ANDROID__
agent <- "wireclub-app-android/" + version
#endif
#if __IOS__
agent <- "wireclub-app-ios/" + version
#endif

let mutable cookies = new CookieContainer()

let debugSlowNetwork = false

let mutable userIdentity: Wireclub.Boundary.Models.User option = None
let mutable userId = ""
let mutable userToken = ""
let mutable userCsrf = ""

let mutable baseUrl = "https://app.wireclub.com"
let mutable webUrl = "https://app.wireclub.com"
let mutable staticBaseUrl = "http://static.wireclub.com"
let mutable channelServer = "wss://chat.wireclub.com:8888/events"

#if DEBUG
#if __ANDROID__
baseUrl <- "http://192.168.0.102"
staticBaseUrl <- "http://192.168.0.102"
channelServer <- "wss://192.168.0.102:8888/events"
#endif
#if __IOS__
baseUrl <- "http://192.168.0.106"
webUrl <- "http://192.168.0.106"
staticBaseUrl <- "http://192.168.0.106"
channelServer <- "ws://192.168.0.106:8888/events"
#endif
#endif

type ApiResult<'A> =
| ApiOk of 'A
| BadRequest of ApiError list
| Unauthorized
| Timeout
| HttpError of int * string
| Deserialization of Exception * string
| Exception of Exception

// Await untyped Task
let awaitTask (t: Task) = t |> Async.AwaitIAsyncResult |> Async.Ignore

let fullUrl (url:string) =
    match url.Contains("://") with
    | true -> url
    | false -> Uri(Uri(baseUrl), url).ToString()

let respParse<'A> (url:string) (resp:HttpResponseMessage) = async {
    let stringContent () = 
            Async.AwaitTask (resp.Content.ReadAsStringAsync())

    match resp.Headers.TryGetValues("Set-Cookie") with
    | true, header -> if header.Any() then cookies.SetCookies(new Uri(url), header.First())
    | _ -> ()

    match int resp.StatusCode with
    | 200 when typeof<'A> = typeof<byte[]> -> 
        let! content = Async.AwaitTask (resp.Content.ReadAsByteArrayAsync ())
        return ApiOk (content :> obj :?> 'A)
    | 200 when typeof<'A> = typeof<string> -> 
        let! content = stringContent ()
        return ApiOk (content :> obj :?> 'A)
    | 200 when typeof<'A> = typeof<unit> ->
        return ApiOk (() :> obj :?> 'A)
    | 200 ->
        let! content = stringContent ()
        try
            return ApiOk (JsonConvert.DeserializeObject<'A>(content))
        with
        | ex -> return Deserialization (ex, content)
    | 400 -> 
        let! content = stringContent ()
        try
            return BadRequest (JsonConvert.DeserializeObject<ApiError[]>(content) |> List.ofArray)
        with
        | ex -> return Deserialization (ex, content)
    | 401
    | 403 -> return Unauthorized
    | status -> 
        let! content = stringContent ()
        return HttpError (status, content)
}

let req<'A> (url:string) (httpMethod:string) (data:(string*string) list)  = async {
    try
        let stopwatch = Stopwatch()
        stopwatch.Start()

        let url = fullUrl url

        if debugSlowNetwork then
            do! Async.Sleep (5 * 1000)

        let message = 
            match httpMethod.ToUpperInvariant() with
            | "GET" -> new HttpRequestMessage(HttpMethod.Get, url)
            | "POST" -> new HttpRequestMessage(HttpMethod.Post, url, Content = new FormUrlEncodedContent(data |> List.map (fun (k, v) -> KeyValuePair(k,v))))
            | _ -> failwithf "Unsupported method: %s" httpMethod
        message.Headers.TryAddWithoutValidation("Cookie", cookies.GetCookieHeader(new Uri (url))) |> ignore
        message.Headers.TryAddWithoutValidation("User-Agent", agent) |> ignore
        message.Headers.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json")) |> ignore

        let client = new HttpClient(new ModernHttpClient.NSUrlSessionHandler())

        let! resp = Async.AwaitTask (client.SendAsync message)
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "[API] %s %s %i %ims" httpMethod url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)

        let! resp = respParse<'A> url resp
        return resp
    with
    | ex -> return Exception ex
}

let post<'A> url = req<'A> url "POST" []
let get<'A> url = req<'A> url "GET" []

let upload<'A> url name filename (data:byte []) =  async {
    try
        let stopwatch = Stopwatch()
        stopwatch.Start()

        let url = fullUrl url

        let content = new MultipartFormDataContent()
        let fileContent = new ByteArrayContent(data)
        fileContent.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/octet-stream")
        fileContent.Headers.ContentDisposition <- new ContentDispositionHeaderValue("form-data");
        fileContent.Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue("name", sprintf "\"%s\"" name));
        fileContent.Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue("filename", sprintf "\"%s\"" filename));

        content.Add(fileContent)

        let message = new HttpRequestMessage(HttpMethod.Post, url, Content = content)
        message.Headers.TryAddWithoutValidation("Cookie", cookies.GetCookieHeader(new Uri (url))) |> ignore
        message.Headers.TryAddWithoutValidation("User-Agent", agent) |> ignore
        message.Headers.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json")) |> ignore

        let client = new HttpClient(new ModernHttpClient.NSUrlSessionHandler())

        let task = client.SendAsync(message)

        let! resp = Async.AwaitTask task
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "[API] %s %s %i %ims" "POST" url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)

        let! resp = respParse<'A> url resp
        return resp
    with
    | ex -> return Exception ex
}



