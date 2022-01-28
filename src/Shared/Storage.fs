namespace Shared

open System.Threading
open DigitallyCreated.FSharp.Azure.TableStorage
open Microsoft.WindowsAzure.Storage

module Storage =
 
    let account = CloudStorageAccount.Parse <| Settings.Storage
    let tableClient = account.CreateCloudTableClient()
    let blobClient = account.CreateCloudBlobClient()
    let cultureInfo = System.Globalization.CultureInfo.InvariantCulture

    let log operation (result: OperationResult) =
        let msgPattern = "Storage returned {@Code} httpStatusCode for operation {@Operation}"
        match result.HttpStatusCode with
        | code when code >= 200 && code < 300 -> Logger.Debug(msgPattern, code, operation) 
        | code when code >= 400 && code < 500 -> Logger.Error(msgPattern, code, operation) 
        | code when code >= 500 && code < 600 -> Logger.Fatal(msgPattern, code, operation)
        | code ->                                Logger.Warning(msgPattern, code, operation)
 
    let CleanUpBlobs() =
        try
            let snapshotsBlob = blobClient.GetContainerReference("snapshots")
            snapshotsBlob.ListBlobs()
            |> Seq.map(fun(snapshot)-> snapshot :?> Blob.CloudBlob)
            |> Seq.iter(fun(snapshot)-> snapshot.Delete())
        with
        | exn ->
            ()

        let table = tableClient.GetTableReference("Events")
        try
            let exists = table.DeleteIfExists()
            if exists then
                Thread.Sleep(40000)
        with
        | exn ->
           ()
 