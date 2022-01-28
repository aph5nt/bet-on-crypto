namespace Shared
open Model
open System

[<AutoOpen>]
module Commands =
     
    type SendCommand =
        | Widthraw of amount : Amount * bet : Bet
        | Refund of bet : Bet * failure : FailureMessage
        | Profit of gameName: GameName * playedAt : DateTime * profit: Amount

    (* COMMANDS *)
    type GameCommand = 
        | Init
        | Tick
        | Bet of Bet
        | QueryAll

    type ConnectionState = { IsConnected : bool }
    type ClientState = 
        | Get
        | Set of ConnectionState

    module ClientCommand = 
        type ClientStartUp() = class end
        type ClientShutdown() = class end
        type OnServerDisconnected() = class end

    module ServerCommand = 
        type OnClientConnected = { GameProxyAddress : string; RateProxyAddress : string; BalanceProxyAddress : string }
        type OnClientDisconnect = { GameProxyAddress : string; RateProxyAddress : string; BalanceProxyAddress : string }
        type ServerShutdown() = class end
     
    module GameViewCommand =
        type QueryAll() = class end
        type QueryCurrent() = class end
        type QueryPrevious() = class end
        type QueryAccumulation() = class end
        type QueryPastBets = { GameName : string }
        type QueryCurrentBets = { GameName : string; Account : string }

    module RateCommand = 
        type Current() = class end
        type Fetch() = class end
        type Query() = class end