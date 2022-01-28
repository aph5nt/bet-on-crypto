namespace GameEngine.Rates
open Akka.FSharp

module RateActor = 
    open Shared
    open RateCommand

    let GetLastNRates : GetLastNRates = 
        fun network services n  ->
            services.Storage.GetLastNRates network n
            |> Array.map(fun r -> { Rate.Date = r.Date; Rate.Day7 = (r.Day7 |> AsDecimal); Rate.Network = network; })
            |> Array.rev
            |> Array.toList

    let convert (rates : Rate list) = 
        { GetRate.Labels = rates |> List.toArray |> Array.map(fun (i) -> i.Date);
          GetRate.DataSets = [ { RateDataSet.Key = "7d";  RateDataSet.Data = rates |> List.toArray |> Array.map(fun (i) -> i.Day7) } ] }

    let run system network limit services =
        spawn system "rate"
            (fun mailbox ->
                let rec loop (rates : Rate list) = actor {
                    let! message = mailbox.Receive()
                    let sender = mailbox.Context.Sender
                    match box message with
                    | :? Query -> 
                        sender <! convert rates
                    | :? Current -> 
                        sender <! services.RateQueryService.Get network
                    | :? Fetch ->
                        let result = services.RateQueryService.Get network
                        match result with
                        | Some(rate) ->
                            services.Storage.InsertRate rate 
                            mailbox.Context.System.EventStream.Publish(rate)
                            match rates.Length with
                            | x when x < limit -> return! loop ((rate::(rates |> List.rev) |> List.rev))
                            | _  ->               return! loop <| ( rate:: ((rates |> List.tail) |> List.rev ) |> List.rev)
                        | None -> ()
                    | _ -> mailbox.Unhandled message
                    return! loop rates
                }

                loop (GetLastNRates <| Model.mapNetwork network <| services <| limit))

