namespace GameEngineTests.Games

open Xunit
open FsUnit.Xunit
open System
open Shared
open GameEngine.Games
open GameEngineTests
open Akka.Actor

[<Trait("Drawing", "UnitTest")>]
type GameDrawingTest() =
    do Logging.Init()
   
    [<Fact>]
    let  ``DrawWinners returns list of accounts matching winning vote``() = 
        let winnersR = Drawing.drawWinners game1Winners
        winnersR |> List.find(fun i -> i.Address ="addr-5" ) |> ignore
        winnersR |> List.find(fun i -> i.Address ="addr-6" ) |> ignore
        winnersR |> List.find(fun i -> i.Address ="addr-7" ) |> ignore

    [<Fact>]
    let ``GetWin - If one account has won n times then we should send n times the won``() =
        let gameWinners = { game1Winners with Bets = game1Winners.Bets @ game1Winners.Bets }
 
        let rateActorRef = Foq.Mock<IActorRef>().Create()
        let sendActorRef = Foq.Mock<IActorRef>().Create()

        let services = {
            RateActorRef = rateActorRef
            SendActorRef = sendActorRef }

        let result = 
            Drawing.drawGame 
            <| services
            <| { Network = BTCTEST; Games = Map.ofList([gameWinners.Name, gameWinners]); Accumulation = 1m; }
            <| gameWinners
        ()

    [<Fact>]
    member x.``GetWin returns win, profit, prize``()=


        let generate = seq {
            yield (1, 0.0m, 0.0m, 0.005m)
            yield (5, 0.0225m, 0.0005m, 0.002m)
            yield (7, 0.0105m, 0.0007m, 0.0028m)
            yield (100, 0.15m, 0.01m, 0.04m)
            yield (1000, 1.5m, 0.1m, 0.4m)
        }

        let test (item : (int * decimal * decimal * decimal)) = 
            let betNum, winExpected, profitExpected, accumulationExpected = item
            let winningGame = { game1Winners with WinningVote = Some(1.2m); Bets = seq { for i in 1 .. betNum do yield GameCommon.bet i date } |> Seq.toList;  }
            let winners = Drawing.drawWinners winningGame
            let win, profit, accumulation = Drawing.getWin winningGame.Bets winners 0m

            win |> should equal (decimal winExpected)
            profit |> should equal (decimal profitExpected)
            accumulation |> should equal (decimal accumulationExpected)

        generate |> Seq.iter test