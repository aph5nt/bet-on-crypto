namespace GameEngine.Publishers

open Shared
open GameEngine
open System.Threading
open Microsoft.WindowsAzure.Storage.Table
open DigitallyCreated.FSharp.Azure.TableStorage
open Microsoft.WindowsAzure.Storage

module TwitterPublisherStorage = 
 
    let account = CloudStorageAccount.Parse <| Settings.Storage
    let tableClient = account.CreateCloudTableClient()

    let inAspNetIndex sendback = inTable tableClient "AspNetIndex" sendback
    let fromAspNetIndex q = fromTable tableClient "AspNetIndex" q

    type GetTwitterAccounts = unit -> int64 list
    type AspNetIndex = {
        [<PartitionKey>] PartitionKey : string
        [<RowKey>] RowKey : string
        Id : string
        KeyVersion: double }
 
    let GetTwitterAccounts : GetTwitterAccounts =
        fun () ->
            Query.all<AspNetIndex> 
            |> Query.where <@ fun g s -> s.PartitionKey = "TWITTER" @>
            |> fromAspNetIndex
            |> Seq.map(fun(i, _) -> int64 <| i.RowKey.Replace("L_TWITTER_", ""))
            |> Seq.toList