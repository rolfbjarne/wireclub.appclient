﻿module Api

open System
open System.Net
open System.Net.Http
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


let cookies = new CookieContainer()
let handler = new HttpClientHandler()
handler.CookieContainer <- cookies
handler.AllowAutoRedirect <- false

let client = new HttpClient(handler)
client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", agent) |> ignore
client.DefaultRequestHeaders.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json")) |> ignore


// TEMPORARY HAX
let mutable userId = ""
let mutable userToken = ""
let mutable userCsrf = ""

let mutable baseUrl = "http://dev.wireclub.com"

#if __ANDROID__
baseUrl <- "http://192.168.0.102"
#endif
#if __IOS__
baseUrl <- "http://dev.wireclub.com"
//baseUrl <- "http://www.wireclub.com"
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
let awaitTask (t: Task) = t.ContinueWith (fun t -> ()) |> Async.AwaitTask

let fullUrl (url:string) =
    match url.Contains("://") with
    | true -> url
    | false -> Uri(Uri(baseUrl), url).ToString()

let req<'A> (url:string) (httpMethod:string) (data:(string*string) list)  = async {
    try
        let stopwatch = Stopwatch()
        stopwatch.Start()

        let url = fullUrl url

        let task =
            match httpMethod.ToUpperInvariant() with
            | "GET" -> client.GetAsync url
            | "POST" -> client.PostAsync(url, new FormUrlEncodedContent(data |> List.map (fun (k, v) -> KeyValuePair(k,v))))
            | _ -> failwithf "Unsupported method: %s" httpMethod

        let! resp = Async.AwaitTask task
        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))

        printfn "HTTP:%s %s %i %ims" httpMethod url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)

        let stringContent () = 
            Async.AwaitTask (resp.Content.ReadAsStringAsync())

        match int resp.StatusCode with
        | 200 when typeof<'A> = typeof<byte[]> -> 
            let! content = Async.AwaitTask (resp.Content.ReadAsByteArrayAsync ())
            return ApiOk (content :> obj :?> 'A)
        | 200 when typeof<'A> = typeof<string> -> 
            let! content = stringContent ()
            return ApiOk (content :> obj :?> 'A)
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
    with
    | ex -> return Exception ex
}
