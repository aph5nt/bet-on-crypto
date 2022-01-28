namespace GameEngine.Games

[<AutoOpen>]
module Querying =
    open Shared
    open System
 
    let matchDate (game : Game) bet = game.OpenedAt.Year = bet.PlacedAt.Year && game.OpenedAt.Month = bet.PlacedAt.Month && game.OpenedAt.Day = bet.PlacedAt.Day && game.OpenedAt.Hour = bet.PlacedAt.Hour

    let tryFindGame state name =
        match Map.containsKey name state with
        | true -> Map.find name state |> Some
        | false -> None

    let findGame state bet =
        let game = Map.find bet.GameName state.Games
        match game.Status with
        | GameStatus.Pending -> game
        | GameStatus.Opened -> game
        | _ -> raise (InvalidGameState("Invalid game state", game.Name))

    let toCurrentGame (game : Game) = 
            {   CurrentGame.Name = game.Name;
                CurrentGame.OpenedAt = game.OpenedAt; 
                CurrentGame.ClosedAt = game.OpenedAt.AddHours(1.0);
                CurrentGame.DrawAt = game.DrawingAt; 
                CurrentGame.Bets = game.Bets |> List.length;
                CurrentGame.Status = game.Status |> toString;
                CurrentGame.TotalAmount = ((decimal (game.Bets |> List.length)) * Settings.Game.Bet); }

    let toPreviousGame (game : Game) =
        let getWinningVote (vote : Option<Vote>) = 
            match vote with
            | Some(value) -> value.ToString()
            | None -> "n/a"

        {   PreviousGame.Name = game.Name;
            PreviousGame.Bets = game.Bets.Length;
            PreviousGame.DrawedAt = game.DrawingAt;
            PreviousGame.Status = toString game.Status;
            PreviousGame.Win = game.Win;
            PreviousGame.Winners = game.Winners.Length;
            PreviousGame.WinningVote = getWinningVote <| game.WinningVote;
            PreviousGame.TotalWin = ((decimal (game.Bets |> List.length)) * Settings.Game.Bet); }
            
    let getCurrentGames (games : Map<string, Game>) =
        let result = 
            games 
            |> Map.toSeq
            |> Seq.filter (fun(_, game) -> game.Status = GameStatus.Pending || game.Status = GameStatus.Opened || game.Status = GameStatus.Closed)
            |> Seq.map (fun(_, game) -> game)
            |> Seq.sortByDescending (fun(game) -> game.DrawingAt) 
            |> Seq.map toCurrentGame
            |> Seq.toArray
        result

    let getPreviousGames (games : Map<string, Game>) =
        let result = 
            games 
            |> Map.toSeq
            |> Seq.filter (fun(_, game) -> game.Status = GameStatus.Drawed || game.Status = GameStatus.Refunded)
            |> Seq.map (fun(_, game) -> game)
            |> Seq.sortByDescending (fun(game) -> game.DrawingAt) 
            |> Seq.map toPreviousGame
            |> Seq.toArray
        result

    let getCurrentBets gameName address (games : Map<string, Game>) =
        if games.ContainsKey(gameName) then
             let game = games.[gameName]
             let result = 
                game.Bets
                |> List.filter(fun b -> b.Address = address)
                |> List.sortBy(fun(b) -> b.PlacedAt)
                |> List.map(fun bet -> { CurrentBet.Account = bet.Address; CurrentBet.PlacedAt = bet.PlacedAt; CurrentBet.VoteFor = bet.VoteFor;})
                |> List.toArray
             result
        else 
            [||]
    let getPastBets gameName  (games : Map<string, Game>) =
        if games.ContainsKey(gameName) then
            let game = games.[gameName]
            let result = 
                game.Winners
                |> List.map(fun(bet) -> { PastBet.Account = bet.Address; PastBet.PlacedAt = bet.PlacedAt; PastBet.VoteFor = bet.VoteFor; })
                |> List.sortBy(fun(b) -> b.Account, b.PlacedAt)
                |> List.toArray
           
            result
        else 
             [||]
            

