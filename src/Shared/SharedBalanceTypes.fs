namespace Shared

open FSharp.Data
 
module BalanceTypes = 

    type Address = string
    type Balance = decimal
    type BalanceChanged = decimal

    type AddressType = JsonProvider<"address.json", EmbeddedResource = "Shared, address.json">

    module BalanceCommands = 
        type GetBalance = { Address : Address }
        type AddBalance = { Address : Address }
        type UpdateBalance = { Address : Address; BalanceChanged : BalanceChanged }
        type OnBalanceUpdated = { Address : Address; ActualBalance : Balance }
        type Reconnect() = class end
        type PublishBalances() = class end