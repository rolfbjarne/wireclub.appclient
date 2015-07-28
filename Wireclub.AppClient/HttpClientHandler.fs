// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.
//
// Adapted from: ModernHttpClient by Paul Betts to F#
// https://github.com/paulcbetts/ModernHttpClient
//
// ORIGINAL LICENSE:
//    Copyright (c) 2013 Paul Betts
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of
//    this software and associated documentation files (the "Software"), to deal in
//    the Software without restriction, including without limitation the rights to
//    use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//    the Software, and to permit persons to whom the Software is furnished to do so,
//    subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
//    FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
//    COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
//    IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//    CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace iOS

open System
open System.Net.Http
open System.Threading.Tasks
open System.Threading
open Foundation
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net

type Request () =
    member val Request:HttpRequestMessage = null with get, set
    member val Callback:((HttpResponseMessage -> unit) * (exn -> unit) * (OperationCanceledException -> unit)) = ((fun _ -> ()), (fun _ -> ()), (fun _ -> ())) with get, set

    member val ResponseBody:NSMutableData = new NSMutableData() with get, set
    member val Response:NSHttpUrlResponse = null with get, set

type HttpClientHandler () =
    inherit HttpMessageHandler ()

    let requests = new Dictionary<NSUrlSessionTask, Request>()
    let fetchRequestByTask(task:NSUrlSessionTask) = lock requests (fun _ ->  requests.[task])

//    let nsUrlSessionDelegate =
//        {
//            new NSUrlSessionDataDelegate () with
//
//            override this.DidReceiveResponse(session, dataTask, response, complete) =
//                let data = fetchRequestByTask(dataTask)
//                let _, econt, _ = data.Callback
//                try
//                    data.Response <- response :?> NSHttpUrlResponse
//                with 
//                | ex -> econt ex
//
//                complete.Invoke(NSUrlSessionResponseDisposition.Allow)
//
//            override this.DidCompleteWithError (session, dataTask, error) =
//                let data = fetchRequestByTask(dataTask)
//                let cont, econt, _ = data.Callback
//                if error <> null then
//                    if error.Description.StartsWith("cancel", StringComparison.OrdinalIgnoreCase) then
//                        econt (new OperationCanceledException())
//                    else
//                        econt (new WebException(error.LocalizedDescription))
//                else
//                    let status =  enum<HttpStatusCode>(int data.Response.StatusCode)
//                    let response =
//                        new HttpResponseMessage(status,
//                            Content = new StreamContent(data.ResponseBody.AsStream()),
//                            RequestMessage = data.Request
//                        )
//
//                    for header in data.Response.AllHeaderFields do
//                        if header.Key <> null && header.Value <> null then
//                            response.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString()) |> ignore
//                            response.Content.Headers.TryAddWithoutValidation(header.Key.ToString(), header.Value.ToString()) |> ignore
//
//                    lock requests (fun _ -> requests.Remove(dataTask) |> ignore)
//
//                    cont(response)
//
//            override this.DidReceiveData (session, dataTask, byteData) =
//                let data = fetchRequestByTask(dataTask)
//                data.ResponseBody.AppendData(byteData)
//        }
//
    let session:NSUrlSession = NSUrlSession.FromConfiguration(NSUrlSessionConfiguration.DefaultSessionConfiguration, null, null)

    member this.ClearCookies () =
        for cookie in NSUrlSessionConfiguration.DefaultSessionConfiguration.HttpCookieStorage.Cookies do
            NSUrlSessionConfiguration.DefaultSessionConfiguration.HttpCookieStorage.DeleteCookie cookie

    override this.SendAsync(request:HttpRequestMessage, cancellationToken:CancellationToken) =
        Async.StartAsTask (async {
            use memoryStream = new MemoryStream()
            if request.Content <> null then
                do! Async.Ignore (Async.AwaitIAsyncResult (request.Content.CopyToAsync(memoryStream)))

            let headers = new NSMutableDictionary()
            if request.Content <> null && request.Content.Headers.Any() then
                for header in request.Headers.Union(request.Content.Headers) do
                    headers.Add(new NSString(header.Key), new NSString(header.Value.LastOrDefault()))
            else
                for header in request.Headers do
                    headers.Add(new NSString(header.Key), new NSString(header.Value.LastOrDefault()))
                
            let urlRequest =
                new NSMutableUrlRequest(
                    AllowsCellularAccess = true,
                    Body = NSData.FromArray(memoryStream.ToArray()),
                    CachePolicy = NSUrlRequestCachePolicy.UseProtocolCachePolicy,
                    Headers = headers,
                    HttpMethod = request.Method.ToString().ToUpperInvariant(),
                    Url = NSUrl.FromString(request.RequestUri.AbsoluteUri)
                )

            let! response = Async.FromContinuations(fun callback ->
                let task = session.CreateDataTask((null : NSUrlRequest))
                lock requests (fun _ -> requests.[task] <- new Request(Request = request, Callback = callback))
                task.Resume();
            )

            return response
        })

    