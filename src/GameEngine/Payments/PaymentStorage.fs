namespace GameEngine.Payments

open System
open Shared
open DigitallyCreated.FSharp.Azure.TableStorage
open Microsoft.WindowsAzure.Storage

module StorageTypes = 
 
    type InsertWidthraw = Bet -> Amount -> unit
    type WidthrawExists = Bet -> bool
    type InsertRefund =  Bet -> Reason -> unit
    type RefundExists = Bet -> bool
    type InsertProfit = GameName -> ProfitAddress -> PlayedAt -> Amount -> unit
    type ProfitExists = GameName -> ProfitAddress -> bool
    type Init = unit -> unit
    type CleanUp = unit -> unit
    type StorageService = {
        Init : Init
        CleanUp : CleanUp
        InsertWidthraw : InsertWidthraw
        WidthrawExists : WidthrawExists
        InsertRefund : InsertRefund
        RefundExists : RefundExists
        InsertProfit : InsertProfit
        ProfitExists : ProfitExists }
 
    type Widthraw = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : string
        Address : WidthrawAddress
        WidthrawedAt: DateTime;
        Amount : string }

    type Refund = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : TransactionId
        Address : RefundAddress
        RefundedAt: DateTime;
        Amount : string
        Reason : string }
    
    type Profit = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : ProfitAddress
        GamePlacedAt: DateTime;
        Amount : string }

module Storage =
    open StorageTypes
 
    let account = CloudStorageAccount.Parse <| Settings.Storage
    let tableClient = account.CreateCloudTableClient()
    let blobClient = account.CreateCloudBlobClient()
    let inWidthraws widthraw = inTable tableClient "Widthraws" widthraw
    let fromWidthraws q = fromTable tableClient "Widthraws" q
    let fromRefunds q = fromTable tableClient "Refunds" q
    let inRefund refund = inTable tableClient "Refunds" refund
    let fromProfits q = fromTable tableClient "Profits" q
    let inProfits profit = inTable tableClient "Profits" profit
    let cultureInfo = System.Globalization.CultureInfo.InvariantCulture

    let Init : Init =
        fun() ->  
            tableClient.GetTableReference("Widthraws").CreateIfNotExists() |> ignore
            tableClient.GetTableReference("Refunds").CreateIfNotExists() |> ignore
            tableClient.GetTableReference("Profits").CreateIfNotExists() |> ignore
            tableClient.GetTableReference("Rates").CreateIfNotExists() |> ignore
            tableClient.GetTableReference("Events").CreateIfNotExists() |> ignore
         
    let CleanUp : CleanUp =
        fun() ->
            try
                Query.all<Widthraw> |> fromWidthraws |> Seq.iter (fun(entity, _) -> entity |> ForceDelete |> inWidthraws |> ignore)
                Query.all<Refund>   |> fromRefunds   |> Seq.iter (fun(entity, _) -> entity |> ForceDelete |> inRefund    |> ignore)
                Query.all<Profit>  |> fromProfits  |> Seq.iter (fun(entity, _) -> entity |> ForceDelete |> inProfits  |> ignore)
            with _ -> ()

    let InsertWidthraw : InsertWidthraw = 
        fun bet amount ->
            let widthraw = { Widthraw.PartitionKey = bet.GameName; RowKey = bet.TransactionId; Address = bet.Address; WidthrawedAt = SystemTime.UtcNow(); Amount = amount.ToString(cultureInfo)}
            widthraw |> InsertOrReplace |> inWidthraws |> Storage.log "insertWidthraw"
        
    let WidthrawExists : WidthrawExists =
        fun bet ->
            Query.all<Widthraw> 
            |> Query.where <@ fun g s -> s.PartitionKey = bet.GameName &&  s.RowKey = bet.TransactionId @>
            |> fromWidthraws
            |> Seq.isEmpty
            |> not

    let InsertRefund : InsertRefund =
        fun bet reason ->
            let refund = { Refund.PartitionKey = bet.GameName; RowKey = bet.TransactionId; Address = bet.Address; RefundedAt = SystemTime.UtcNow(); Amount = bet.Amount.ToString(cultureInfo); Reason = reason}
            refund |> InsertOrReplace |> inRefund  |> Storage.log "insertRefund"

    let RefundExists : RefundExists = 
        fun  bet ->
            Query.all<Refund> 
            |> Query.where <@ fun g s -> s.PartitionKey = bet.GameName &&  s.RowKey = bet.TransactionId @>
            |> fromRefunds
            |> Seq.isEmpty
            |> not

    let InsertProfit : InsertProfit = 
        fun gameName account playedAt amount ->
            let gameProfit = { Profit.PartitionKey = gameName; RowKey = account; GamePlacedAt = playedAt; Amount = amount.ToString(cultureInfo)}
            gameProfit|> InsertOrReplace |> inProfits |> Storage.log "insertProfit"

    let ProfitExists : ProfitExists = 
        fun  gameName address ->
            Query.all<Profit> 
            |> Query.where <@ fun g s -> s.PartitionKey = gameName &&  s.RowKey = address @>
            |> fromProfits
            |> Seq.isEmpty
            |> not

    let StorageService() = {
        Init = Init
        CleanUp = CleanUp
        InsertWidthraw = InsertWidthraw
        WidthrawExists = WidthrawExists
        InsertRefund = InsertRefund
        RefundExists = RefundExists
        InsertProfit = InsertProfit
        ProfitExists = ProfitExists }