module Api

open System
open System.Net
open System.Net.Http
open System.Collections.Generic
open Newtonsoft.Json

let cookies = new CookieContainer()
let handler = new HttpClientHandler()
handler.CookieContainer <- cookies
handler.AllowAutoRedirect <- false

let client = new HttpClient(handler)
client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "wireclub-app-android/1.0") |> ignore
client.DefaultRequestHeaders.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json"))

// TEMPORARY HAX
let mutable userId = ""
let mutable userHash = ""

//let baseUrl = "http://www.wireclub.com"

#if __ANDROID__
let baseUrl = "http://192.168.0.102"
#else
let baseUrl = "http://dev.wireclub.com"
#endif

type ApiFailureType =
| Timeout
| HttpError of int
| Deserialization
| Exception

type ApiResult<'A> =
| ApiOk of 'A
| ApiError of ApiFailureType * string

let req<'A> (url:string) (httpMethod:string) (data:(string*string) list)  = async {
    try        
        let url = sprintf "%s/%s" baseUrl url
        let task =
            match httpMethod.ToUpperInvariant() with
            | "GET" -> 
                client.GetAsync url

            | "POST" -> 
                client.PostAsync(url, new FormUrlEncodedContent(data |> List.toArray |> Array.map (fun (k, v) -> KeyValuePair(k,v))))
            | _ -> failwithf "Unsupported method: %s" httpMethod

        let! resp = Async.AwaitTask task

        printfn "HTTP:%s %s %i" httpMethod url (int resp.StatusCode)
        
        let! content = Async.AwaitTask (resp.Content.ReadAsStringAsync())

        match int resp.StatusCode with
        | 200 ->
            if typeof<'A> = typeof<string> then
                return ApiOk (content :> obj :?> 'A)
            else
                try
                    return ApiOk (JsonConvert.DeserializeObject<'A>(content))
                with
                | ex -> return ApiError (Deserialization, ex.ToString())

        | status -> return ApiError (HttpError status, content)
    with
    | ex -> return ApiError (Exception, ex.ToString())
}