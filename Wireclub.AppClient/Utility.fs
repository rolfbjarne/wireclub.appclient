namespace Utility

open System

module Timer =

    let rec ticker fn interval = async {
        fn ()        

        do! Async.Sleep interval
        return! ticker fn interval
    }
