module Api

open System.Net
open System.Net.Http
open System.Collections.Generic

let cookies = new CookieContainer()
let handler = new HttpClientHandler()
handler.CookieContainer <- cookies

let client = new HttpClient(handler)
client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "wireclub-app-android/1.0") |> ignore
client.DefaultRequestHeaders.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("application/json"))

//let baseUrl = "http://www.wireclub.com"
let baseUrl = "http://dev.wireclub.com"

type ApiFailureType =
| Timeout
| Exception

type ApiResult<'A> =
| ApiOk of 'A
| ApiError of ApiFailureType * string

let req (url:string) (httpMethod:string) (data:Map<string,string>)  = async {
    try
        let url = sprintf "%s/%s" baseUrl url
        let task =
            match httpMethod.ToUpperInvariant() with
            | "GET" -> 
                client.GetAsync url

            | "POST" -> 
                client.PostAsync(url, new FormUrlEncodedContent(data |> Map.toArray |> Array.map (fun (k, v) -> KeyValuePair(k,v))))
            | _ -> failwithf "Unsupported method: %s" httpMethod

        let! resp = Async.AwaitTask task

        printfn "HTTP:%s %s %i" httpMethod url (int resp.StatusCode)

        // TODO: HTTP Status

        let! resp = Async.AwaitTask (resp.Content.ReadAsStringAsync())
        return ApiOk resp
    with
    | ex -> return ApiError (Exception, ex.ToString())
}
