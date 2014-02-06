module Routes

let (|Route|_|) (route:string) (url:string) =
    let url = url.Split('?').[0]
    let m = System.Text.RegularExpressions.Regex("^" + route).Match(url)
    if m.Success then 
        Some (List.tail [ for x in m.Groups -> System.Net.WebUtility.HtmlDecode x.Value ])
    else 
        None

let route url =
    match url with
    | Route "/chat/room/(.+)$" [ id ] -> ()
    | Route "/users/(.+)/pictures/(.+)/picture/(.+)$" [ user; album; image ] -> ()
    | Route "/users/(.+)$" [ id ] -> ()
    | Route "/topics/(.+)$" [ id ] -> ()
    | Route "/clubs/(.+)$" [ id ] -> ()
    | _ -> ()