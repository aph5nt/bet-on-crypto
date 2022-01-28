namespace GameEngineTests

open Shared 

[<AutoOpen>]
module Logging =
     open Serilog
     let Init() =
        let loggerCfg = new LoggerConfiguration()
        Serilog.Log.Logger <- loggerCfg.MinimumLevel.Verbose().WriteTo.File(@"logs/tests.txt").WriteTo.Console().CreateLogger()
 
     let displayGames (games : Game[]) =
        Logger.Debug("--------------------------------------------------------\n")
        games |> Array.iter(fun(game) -> Logger.Debug(sprintf "Name: %s, Status %s\n" game.Name (GameStatus.toString game.Status)))
 
[<AutoOpen>]
module Common = 

    open Chessie.ErrorHandling
    open FsUnit.Xunit
    let sleep seconds = System.Threading.Thread.Sleep(seconds * 1000)

    let assertFailure result error = 
            failed result |> should equal true
            result |> failureTee (fun (msgs) -> 
                                    match msgs.Head with
                                    | error -> ()
                                    | _ -> failwith "invalid error message")
                                    |> ignore

[<AutoOpen>]
module TestData = 
    open System
 
    let date = new DateTime(2015, 07, 31, 10, 23, 15, 0)
    let bet = { Bet.TransactionId = "2";
                Bet.UserName = "usr1"
                Bet.Amount = 0.01m;
                Bet.PlacedAt = date;
                Bet.VoteFor = 0.45m;
                Bet.Address = "ADR-01";
                Bet.GameName = Game.GetName date }
