// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Api

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open System.Collections.Generic
open System.Diagnostics
open Newtonsoft.Json
open Wireclub.Boundary

let version = "1.1"
let mutable agent = "wireclub-app-client/" + version

let mutable cookies:CookieContainer = null
let mutable handler:HttpMessageHandler = null
let mutable client:HttpClient = null

#if __IOS__
handler <- new iOS.HttpClientHandler()
#endif

let init () =
    cookies <- if cookies <> null then cookies else new CookieContainer()
    handler <- if handler <> null then handler else new HttpClientHandler(CookieContainer = cookies, AllowAutoRedirect = false) :> HttpMessageHandler
    client <- if client <> null then client else new HttpClient(handler)
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", agent) |> ignore
    client.DefaultRequestHeaders.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json")) |> ignore

init ()

let debugSlowNetwork = false

let mutable userIdentity: Wireclub.Boundary.Models.User option = None
let mutable userId = ""
let mutable userToken = ""
let mutable userCsrf = ""

let mutable baseUrl = "https://app.wireclub.com"
let mutable webUrl = "https://app.wireclub.com"
let mutable staticBaseUrl = "http://static.wireclub.com"
let mutable publicUrl = "http://www.wireclub.com"
let mutable channelServer = "wss://chat.wireclub.com:8888/events"

#if DEBUG
#if __ANDROID__
baseUrl <- "http://192.168.0.102"
staticBaseUrl <- "http://192.168.0.102"
channelServer <- "wss://192.168.0.102:8888/events"
#endif
#if __IOS__
baseUrl <- "http://192.168.1.109"
webUrl <- "http://192.168.1.109"
staticBaseUrl <- "http://192.168.1.109"
channelServer <- "ws://192.168.1.109:8888/events"
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

let respParse<'A> (resp:HttpResponseMessage) = async {
    let stringContent () = 
            Async.AwaitTask (resp.Content.ReadAsStringAsync())

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
        | ex ->
            return Deserialization (ex, content)
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

        let task =
            match httpMethod.ToUpperInvariant() with
            | "GET" -> client.GetAsync url
            | "POST" -> client.PostAsync(url, new FormUrlEncodedContent(data |> List.map (fun (k, v) -> KeyValuePair(k,v))))
            | _ -> failwithf "Unsupported method: %s" httpMethod

        let! resp = Async.AwaitTask task
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "[API] %s %s %i %ims" httpMethod url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)

        let! resp = respParse<'A> resp
        return resp
    with
    | ex -> return Exception ex
}

let post<'A> url = req<'A> url "POST" []
let get<'A> url = req<'A> url "GET" []

let postContent<'A> (url:string) content = async {
    try
        let stopwatch = Stopwatch()
        stopwatch.Start()

        let url = fullUrl url

        if debugSlowNetwork then
            do! Async.Sleep (5 * 1000)

        let task = client.PostAsync (url, content)

        let! resp = Async.AwaitTask task
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "HTTP:%s %s %i %ibytes %ims" "POST" url (int resp.StatusCode) resp.Content.Headers.ContentLength.Value (stopwatch.ElapsedMilliseconds)

        let! fullResponse = respParse<'A> resp  
        return fullResponse
    with
    | ex -> return Exception ex
}

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
        let task = client.PostAsync(url, content)

        let! resp = Async.AwaitTask task
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "[API] %s %s %i %ims" "POST" url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)

        let! resp = respParse<'A> resp
        return resp
    with
    | ex -> return Exception ex
}



