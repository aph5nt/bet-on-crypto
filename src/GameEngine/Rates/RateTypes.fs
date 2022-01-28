namespace GameEngine.Rates

open DigitallyCreated.FSharp.Azure.TableStorage
open System
       
module StorageTypes = 
    open Shared

    type InsertRate = Shared.RateTypes.Rate -> unit
    and GetLastNRates = Network -> int -> Rate[]
    and CleanUp = unit -> unit
    and Init = unit -> unit
    and Rate = 
       { [<PartitionKey>] PartitionKey : string; 
         [<RowKey>] RowKey : string;
         Date: DateTime;
         Day7: string  }

     type StorageService = {
        Init : Init
        CleanUp : CleanUp
        InsertRate : InsertRate
        GetLastNRates : GetLastNRates }

open FSharp.Data
[<AutoOpen>]
module Types =
    open Shared
  
    type Get = Network -> Option<Rate>
    type RateQueryService = { Get : Get }
    type Coinmarketcap = HtmlProvider<"coinmarketcap.html">
    type RateActorServices = { RateQueryService : RateQueryService; Storage : StorageTypes.StorageService }
    type GetLastNRates = Network -> RateActorServices -> int -> Rate list