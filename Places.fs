module Places

open Wireclub.Boundary.Models

let countries () =
    Api.req<LocationCountry[]> "api/places/countries" "post" []

let regions (country:string) =
    Api.req<LocationRegion[]> "api/places/regions" "post" [ "country", country ]