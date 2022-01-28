namespace GameEngine.Games

open Akka.FSharp

[<AutoOpen>]
module Drawing = 
    open Shared
 
    let drawWinners : DrawWinners =
        fun game ->  game.Bets |> Seq.choose(fun(bet) -> match bet.VoteFor with
                                                            | x when abs <| (x - game.WinningVote.Value) <= (decimal Settings.Game.Radius) -> Some(bet)
                                                            | _ -> None) |> Seq.toList
    let getWin : GetWin =
        fun bets winningBets accumulation ->

            let accumulation = roundAmount <| accumulation
            let winnerCountR = winningBets.Length |> decimal
            let totalBets = bets |> Seq.sumBy(fun(i) -> i.Amount)

            let totalWin =  (((totalBets) + accumulation) * 0.9m)

            let profit = roundAmount <| (((totalBets) + accumulation) * 0.02m)
            let prize =  roundAmount <| (((totalBets) + accumulation) * 0.08m)
            if winnerCountR = 0m then 
                (0m, 0m, roundAmount <| accumulation + totalBets)
            else 
                let won =  roundAmount <| totalWin / winnerCountR
                (won, profit, prize)

    let drawGame : DrawGame =
        fun services state (game : Game)  ->
            let winningBets = drawWinners game
            let win, profit, accumulation = getWin game.Bets winningBets state.Accumulation
            if win > 0m then

                // widthraw
                winningBets
                |> List.iter(fun(bet) -> services.SendActorRef <! Widthraw(win, bet))

                // profit
                services.SendActorRef <! SendCommand.Profit(game.Name, game.OpenedAt, profit)

                { 
                  Name = game.Name
                  Win = win
                  Profit = profit
                  Winners = winningBets
                  Status = GameStatus.Drawed
                  WinningVote = game.WinningVote
                  Accumulation = accumulation }
            else
                { 
                  Name = game.Name
                  Win = 0m
                  Profit = 0m
                  Winners = []
                  Status = GameStatus.Drawed
                  WinningVote = game.WinningVote
                  Accumulation = accumulation }

