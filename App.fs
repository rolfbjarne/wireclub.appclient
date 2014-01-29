module App

let fetchActiveChannels () = async {
    let channels = Api.req "home/fetchActiveChannels"
    ()
}
