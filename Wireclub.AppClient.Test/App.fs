module AppTests

open NUnit.Framework

[<Test>]
let ``Token Encode / Decode`` () =
    let test = "axv/234*#@$(*&23a'-dfdfa-_2342adf"
    let encoded = App.tokenEncode test
    let decoded = App.tokenDecode encoded   
    Assert.AreEqual (test, decoded)
