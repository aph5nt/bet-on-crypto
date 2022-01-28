namespace Shared

[<AutoOpen>]
module GameTypes = 
    open System
    open Shared

    [<CLIMutable>]
    type ShortStat = {
        TotalWins: int64
        HighestWin : Amount;
        TotalWon: Amount;
    }

    type GetShortStats() = class end

    type [<CLIMutable>] CurrentGame = 
        { 
          Name : string;
          OpenedAt : DateTime;
          ClosedAt : DateTime;
          DrawAt : DateTime;
          Status : string;
          Bets : int;
          TotalAmount : Amount; }

     type PreviousGame = 
        { Name : string;
          DrawedAt: DateTime;
          Status : string;
          Bets : int;
          Winners: int;
          Win: Amount;
          TotalWin: Amount;
          WinningVote: string; }
 
    type [<CLIMutable>] GameDetail = 
       { Profit : Amount;
         Winners : string list; }
  
    type ReloadData =
        { CurrentGames : CurrentGame[];
          PreviousGames: PreviousGame[];
          Accumulation: Amount; }
      
    type PastBet = {
        Account: string
        VoteFor: Vote
        PlacedAt: DateTime }

    type CurrentBet = {
       Account: string
       VoteFor: Vote
       PlacedAt: DateTime }
        
    type GameDrawedData = { Name : string; Win: Amount; Profit: Amount; Winners : Bet list; Accumulation: Amount; WinningVote : Option<Vote> }

    module GameUIEvents =
        type OnBetPlaced = { CurrentGame : CurrentGame }
        type Reload = { ReloadData : ReloadData}
        type OnShortStat = { ShortStat : ShortStat }
        type OnGameDrawed = { GameDrawedData : GameDrawedData}


