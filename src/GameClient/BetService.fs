namespace GameClient.Payments

open System
open Shared
open Akka.FSharp
open BlockApi.Types
open DigitallyCreated.FSharp.Azure.TableStorage

module WithdrawStorageTypes = 
    type IpAddress = string
    type TranasctionId = string
    type InsertUserWithdraw = Address -> TranasctionId -> Address -> Amount ->  IpAddress -> unit
    type Init = unit -> unit
    type CleanUp = unit -> unit
    type StorageService = { Init : Init; CleanUp : CleanUp; InsertUserWithdraw : InsertUserWithdraw }
    type UserWidthraw = {
        [<PartitionKey>] PartitionKey : Address // from address
        [<RowKey>] RowKey : string // transactionId
        ToAddress : Address
        Amount : string
        WidthrawedAt: DateTime;
        IpAddress : IpAddress }

module WithdrawStorage = 
    open WithdrawStorageTypes
    open Microsoft.WindowsAzure.Storage

    let account = CloudStorageAccount.Parse <| Settings.Storage
    let tableClient = account.CreateCloudTableClient()
    let inUserWithdraws withdraw = inTable tableClient "UserWithdraws" withdraw
    let fromUserWithdraws q = fromTable tableClient "UserWithdraws" q

    let Init : Init =
        fun() ->  
            tableClient.GetTableReference("UserWithdraws").CreateIfNotExists() |> ignore

    let CleanUp : CleanUp =
        fun() ->
            try
                Query.all<UserWidthraw> |> fromUserWithdraws |> Seq.iter (fun(entity, _) -> entity |> ForceDelete |> inUserWithdraws |> ignore)
            with _ -> ()

    let InsertUserWithdraw : InsertUserWithdraw =
        fun fromAddress transactionId toAddress amount ipAddress ->
            let userWidthraw = { UserWidthraw.PartitionKey = fromAddress; RowKey = transactionId; ToAddress = toAddress; WidthrawedAt = SystemTime.UtcNow(); Amount = amount.ToString(Shared.Storage.cultureInfo); IpAddress = ipAddress }
            userWidthraw |> InsertOrReplace |> inUserWithdraws |> Storage.log "insertUserWidthraw"

    let StorageService = {
        Init = Init
        CleanUp = CleanUp
        InsertUserWithdraw = InsertUserWithdraw
    }



module WithdrawServiceTypes = 
    type IpAddress = string
    type WithdrawAmount = Address -> Address -> Amount -> IpAddress -> Response<Withdrawn>
 
module WithdrawService =
    open WithdrawServiceTypes

    let WithdrawAmount : WithdrawAmount =
        fun fromAddress toAddress amount ipAddress ->
            try
                let withdrawResult = 
                    BlockApi.BlockApi.Api().WithdrawFromAddresses
                    <| Settings.PaymentGateway.ApiKey
                    <| Settings.PaymentGateway.Pin
                    <| [|fromAddress|]
                    <| [|toAddress|]
                    <| [|amount|]

                match withdrawResult with
                | Success(withdraw) ->
                    WithdrawStorage.StorageService.InsertUserWithdraw
                    <| fromAddress
                    <| withdraw.TxId
                    <| toAddress
                    <| amount
                    <| ipAddress
                | Fail err -> Logger.Error(err)

                withdrawResult
            with exn ->
                Logger.Error("{@Exception}", exn)
                Fail("Internal Server Error")
            
module BetServiceTypes =
    type PlaceBet = UserName -> Address -> Vote -> Akka.Actor.IActorRef ->  Response<Withdrawn>

module BetService = 
    open BetServiceTypes
 
    let PlaceBet : PlaceBet =
        fun userName address voteFor gameActor ->
            try
                let withdrawResult = 
                    BlockApi.BlockApi.Api().WithdrawFromAddresses
                    <| Settings.PaymentGateway.ApiKey
                    <| Settings.PaymentGateway.Pin
                    <| [|address|]
                    <| [|Settings.Game.IncomingAddress|]
                    <| [|decimal Settings.Game.Bet|]

                match withdrawResult with
                | Success(value) -> 
                    let bet = {
                        Bet.GameName = Game.GetName <| SystemTime.UtcNow()
                        Bet.Address = address
                        Bet.Amount = decimal Settings.Game.Bet
                        Bet.PlacedAt = SystemTime.UtcNow()
                        Bet.TransactionId = value.TxId
                        Bet.VoteFor = voteFor
                        Bet.UserName = userName }
                    gameActor <! GameCommand.Bet(bet)
                    withdrawResult
                | Fail(msg) -> 
                    Logger.Error(msg)
                    withdrawResult
            with exn ->
                Logger.Error("{@Exception}", exn)
                Fail("Internal Server Error")
              