// Copyright (c) Wireclub Media Inc. All Rights Reserved. See License.md in the project root for license information.

namespace Utility

open System

module Timer =

    let rec ticker fn interval = async {
        fn ()        

        do! Async.Sleep interval
        return! ticker fn interval
    }
        
module Logger =
    let mutable log: (Exception -> unit) = (fun _ -> failwith "No log handler attached")