// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

module Api

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open System.Diagnostics
open Newtonsoft.Json
open Wireclub.Boundary
open MonoTouch.Foundation

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

let respParse<'A> status (data:byte[]) = async {
    let content = System.Text.Encoding.UTF8.GetString(data)
    printfn "%s" content

    match int status with
    | 200 when typeof<'A> = typeof<byte[]> -> 
        return ApiOk (data :> obj :?> 'A)
    | 200 when typeof<'A> = typeof<string> -> 
        return ApiOk (content :> obj :?> 'A)
    | 200 when typeof<'A> = typeof<unit> ->
        return ApiOk (() :> obj :?> 'A)
    | 200 ->
        try
            return ApiOk (JsonConvert.DeserializeObject<'A>(content))
        with
        | ex -> return Deserialization (ex, content)
    | 400 -> 
        try
            return BadRequest (JsonConvert.DeserializeObject<ApiError[]>(content) |> List.ofArray)
        with
        | ex -> return Deserialization (ex, content)
    | 401
    | 403 -> return Unauthorized
    | status -> 
        return HttpError (status, content)
}

let connections = new Dictionary<Guid, NSUrlConnection>()

let req<'A> (url:string) (httpMethod:string) (data:(string*string) list)  = async {
    try
        let stopwatch = Stopwatch()
        stopwatch.Start()

        let url = fullUrl url

        if debugSlowNetwork then
            do! Async.Sleep (5 * 1000)

        let headers = new NSMutableDictionary()
        let request = new NSMutableUrlRequest(NSUrl.FromString url)
        request.HttpMethod <- httpMethod.ToUpperInvariant()
        headers.SetValueForKey(NSString.op_Explicit agent, NSString.op_Explicit "User-Agent")
        headers.SetValueForKey(NSString.op_Explicit (cookies.GetCookieHeader(new Uri(url))), NSString.op_Explicit "Cookie")
        headers.SetValueForKey(NSString.op_Explicit "application/json", NSString.op_Explicit "Accept")
        headers.SetValueForKey(NSString.op_Explicit userCsrf, NSString.op_Explicit "x-csrf-token")

        if httpMethod.ToUpperInvariant() = "POST" then
            request.Body <- NSData.FromString(String.concat "&" [ for k, v in data do yield sprintf "%s=%s" k v ], NSStringEncoding.UTF8)
            headers.SetValueForKey(NSString.op_Explicit (request.Body.Length.ToString()), NSString.op_Explicit "Content-Length")
            headers.SetValueForKey(NSString.op_Explicit "application/x-www-form-urlencoded charset=utf-8", NSString.op_Explicit "Content-Type")

        request.Headers <- headers

        let! status, data = Async.FromContinuations(fun (cont, econt, _) ->
            let data = ref (new NSMutableData ())
            let status = ref 500
            let id = Guid.NewGuid()
            let connection =
                new NSUrlConnection(
                    request,
                    {
                        new NSUrlConnectionDelegate() with
                        override this.ReceivedResponse(connection, response) = 
                            let response = response :?> NSHttpUrlResponse
                            match response.AllHeaderFields.TryGetValue(NSString.op_Explicit "Set-Cookie") with
                            | true, cookie -> cookies.SetCookies(new Uri (url), NSString.op_Implicit (cookie :?> NSString))
                            | _ -> ()

                            status := response.StatusCode
                            data := new NSMutableData ()
                        override this.ReceivedData(connection, d) =
                            (!data).AppendData(d)
                        override this.FailedWithError(connection, error) =
                            connections.Remove(id) |> ignore
                            econt (new Exception(sprintf "%s\n%s" error.Description error.DebugDescription))
                        override this.FinishedLoading(connection) =
                            let dataBytes= Array.zeroCreate<byte> (Convert.ToInt32(data.Value.Length))
                            System.Runtime.InteropServices.Marshal.Copy(data.Value.Bytes, dataBytes, 0, Convert.ToInt32(data.Value.Length))
                            connections.Remove(id) |> ignore
                            cont (!status, dataBytes)
                    })

            connections.Add(id, connection)
            connection.Start()
        )

        printfn "[API] %s %s %i %ims" httpMethod url status (stopwatch.ElapsedMilliseconds)

        let! resp = (respParse<'A> status data)
        return resp
    with
    | ex -> return Exception ex
}

let post<'A> url = req<'A> url "POST" []
let get<'A> url = req<'A> url "GET" []

let upload<'A> url name filename (data:byte []) =  async {
    let! resp = respParse<'A> 500 (Array.zeroCreate<byte> 0)
    return resp
//
//    try
//        let stopwatch = Stopwatch()
//        stopwatch.Start()
//
//        let url = fullUrl url
//
//        let content = new MultipartFormDataContent()
//        let fileContent = new ByteArrayContent(data)
//        fileContent.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/octet-stream")
//        fileContent.Headers.ContentDisposition <- new ContentDispositionHeaderValue("form-data");
//        fileContent.Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue("name", sprintf "\"%s\"" name));
//        fileContent.Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue("filename", sprintf "\"%s\"" filename));
//
//        content.Add(fileContent)
//        let task = client.PostAsync(url, content)
//
//        let! resp = Async.AwaitTask task
//        do! (awaitTask (resp.Content.LoadIntoBufferAsync()))
//
//        printfn "[API] %s %s %i %ims" "POST" url (int resp.StatusCode) (stopwatch.ElapsedMilliseconds)
//
//        let! resp = respParse<'A> resp
//        return resp
//    with
//    | ex -> return Exception ex
}



