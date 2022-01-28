namespace GameEngine.Rates
open System
open DigitallyCreated.FSharp.Azure.TableStorage
open Shared
open Microsoft.WindowsAzure.Storage


[<AutoOpen>]
module Storage =
    open StorageTypes
    open Shared.Storage
     
    let inRate rate = inTable tableClient "Rates" rate
    let fromRate q = fromTable tableClient "Rates" q
     
    let Init : Init = 
        fun() ->
            tableClient.GetTableReference("Rates").CreateIfNotExists() |> ignore

    let CleanUp : CleanUp =
        fun() ->
            try
                Query.all<Rate>  |> fromRate  |> Seq.iter (fun(entity, _) -> entity |> ForceDelete |> inRate  |> ignore)
            with _ -> ()

    let InsertRate : InsertRate =
        fun rate -> 
            let dataRate = {
                StorageTypes.Rate.Date = rate.Date;
                Day7 = rate.Day7.ToString()
                PartitionKey = (Network.toString <| Model.mapNetwork rate.Network) 
                RowKey = System.String.Format("{0:D19}", System.DateTime.MaxValue.Ticks - SystemTime.UtcNow().Ticks);
            }
            dataRate |> Insert |> inRate |> Storage.log Logger 

    let GetLastNRates : GetLastNRates =
        fun network n ->
            let result = 
                Query.all<StorageTypes.Rate> 
                        |> Query.where <@ fun g s -> s.PartitionKey = (Network.toString <| Model.mapNetwork network) @>
                        |> Query.take n
                        |> fromRate
                        |> Seq.map(fun(r, _) -> r) 
                        |> Seq.toArray
            result

    let StorageService() = {
        Init = Init
        CleanUp = CleanUp
        GetLastNRates = GetLastNRates;
        InsertRate = InsertRate }