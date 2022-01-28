namespace Shared

open System.Globalization
open System
open DigitallyCreated.FSharp.Azure.TableStorage
open Shared

[<AutoOpen>]
module DataModel = 
 
    (* AZURE STORAGE *)
    type SendBack = {
        [<PartitionKey>] PartitionKey : string
        [<RowKey>] RowKey : string
        TransactionTimeStamp: int64;
        TransactionDate: DateTime;
        AmountNQT : int64
        Reason : string }

    type Widthraw = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : Account
        TransactionTimeStamp: int64;
        TransactionDate: DateTime;
        AmountNQT : int64
    }

    type Refund = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : Account
        TransactionTimeStamp: int64;
        TransactionDate: DateTime;
        AmountNQT : int64
        Reason : string
    }

    type Profit = {
        [<PartitionKey>] PartitionKey : GameName
        [<RowKey>] RowKey : Account
        GamePlacedAt: DateTime;
        AmountNQT : int64
    }

    type Snapshots = {
        [<PartitionKey>] PartitionKey : string
        [<RowKey>] RowKey : string
        Id: Guid;
        AggregateId : string
        Type: string
        SequenceNr: int64
        SnapshotState: string
    }
 
    type Rate = 
        { [<PartitionKey>] PartitionKey : string; 
          [<RowKey>] RowKey : string;
          Date: DateTime;
          Hour1 : string; 
          Hour24 : string;
          Day7: string  }
     
    let asDecimal str =
        try
            System.Decimal.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture)
        with 
        | exn -> 0m
           
    
        
       
  