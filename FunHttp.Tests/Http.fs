module FunHttp.Tests.Http

open System
open FSharp.Data
open FunHttp
open FunHttp.HttpRequestHeaders
open Xunit

let inline shouldEqual (x:'a) (y:'a) = Assert.Equal<'a>(x, y)
let inline shouldThrow<'a when 'a :> exn> (y: unit -> unit) = Assert.Throws<'a>(y)
[<Fact>]
let ``Don't throw exceptions on http error`` () = 
    let response = Http.Request("http://api.themoviedb.org/3/search/movie", silentHttpErrors = true)
    response.StatusCode |> shouldEqual 401

[<Fact>]
let ``Throw exceptions on http error`` () = 
    let exceptionThrown = 
        try
            Http.RequestString("http://api.themoviedb.org/3/search/movie") |> ignore
            false
        with e -> 
            true
    exceptionThrown |> shouldEqual true

[<Fact>]
let ``If the same header is added multiple times, throws an exception`` () =
    (fun () -> Http.RequestString("http://www.google.com", headers = [ UserAgent "ua1"; UserAgent "ua2" ]) |> ignore)
    |> shouldThrow<Exception>

[<Fact>]
let ``If a custom header with the same name is added multiple times, an exception is thrown`` () =
    (fun () -> Http.RequestString("http://www.google.com", headers = [ "c1", "v1"; "c1", "v2" ]) |> ignore)
    |> shouldThrow<Exception>

[<Fact>]
let ``Two custom header with different names don't throw an exception`` () =
    Http.RequestString("http://www.google.com", headers = [ "c1", "v1"; "c2", "v2" ]) |> ignore

[<Fact>]
let ``A request with an invalid url throws an exception`` () =
    (fun() -> Http.Request "www.google.com" |> ignore) |> shouldThrow<UriFormatException>

[<Fact>]
let ``Cookies with commas are parsed correctly`` () =
    let uri = Uri "http://www.nasdaq.com/symbol/ibm/dividend-history"
    let cookieHeader = "selectedsymboltype=IBM,COMMON STOCK,NYSE; domain=.nasdaq.com; expires=Sun, 21-May-2017 15:29:03 GMT; path=/,selectedsymbolindustry=IBM,technology; domain=.nasdaq.com; expires=Sun, 21-May-2017 15:29:03 GMT; path=/,NSC_W.TJUFEFGFOEFS.OBTEBR.80=ffffffffc3a08e3045525d5f4f58455e445a4a423660;expires=Sat, 21-May-2016 15:39:03 GMT;path=/;httponly"
    let cookies = 
        CookieHandling.getAllCookiesFromHeader cookieHeader uri 
        |> Array.map (snd >> (fun c -> c.Name, c.Value))
    cookies |> shouldEqual
        [| "selectedsymboltype", "IBM,COMMON STOCK,NYSE"
           "selectedsymbolindustry", "IBM,technology"
           "NSC_W.TJUFEFGFOEFS.OBTEBR.80", "ffffffffc3a08e3045525d5f4f58455e445a4a423660" |]
[<Fact>]
let ``Timeout argument is used`` () = 
    let exc = Assert.Throws<System.Net.WebException> (fun () -> 
        Http.Request("http://deelay.me/100?http://api.themoviedb.org/3/search/movie", timeout = 1) |> ignore)
    shouldEqual typeof<System.Net.WebException>, exc.InnerException.GetType()