namespace BlockApi

open Shared
open FSharp.Data

module Types = 
    (* Function parameters *)
    type ApiKey = string
    type Pin = string
    type Addresses = string[]
    type Amount = decimal
    type Amounts = Amount[]
    type Label = string
    type Labels = Label[]

   (* Model *)
    type Balance = {
        Address : string
        Balance : Amount
        Network : string }
    type MyAddresses = {
        Network : string
        Data : MyAddressBalance[] }
    and MyAddressBalance = {
        UserId : int
        Address : string
        Label : string
        AvailableBalance : decimal
        PendingReceivedBalance : decimal }
    type UserAddress = {
        UserId : int
        Label : string
        Address : string
        Network : string }
    type Withdrawn = {
        Network : string
        TxId : string
        AmountWithdrawn : Amount
        AmountSent : Amount
        NetworkFee : Amount
        BlockIoFee : Amount }

    (* Json  responses *)
    type GetMyAddressesType  = JsonProvider<"get_my_addresses.json", EmbeddedResource ="BlockApi, get_my_addresses.json">
    type GetAddressBalanceType = JsonProvider<"get_address_balance.json", EmbeddedResource ="BlockApi, get_address_balance.json">
    type GetNewAddressType = JsonProvider<"get_new_address.json", EmbeddedResource ="BlockApi, get_new_address.json">
    type WithdrawFromAddressesType  = JsonProvider<"withdraw_from_addresses.json", EmbeddedResource ="BlockApi, withdraw_from_addresses.json">
    type EstimateNetworkFeeType  = JsonProvider<"estimate_network_fee.json", EmbeddedResource ="BlockApi, estimate_network_fee.json">
    type FailType = JsonProvider<"fail.json", EmbeddedResource ="BlockApi, fail.json">
    type Response<'T> =
        | Success of 'T
        | Fail of string
        member x.TrySuccess(response:'T byref) =
            match x with
            | Success(r) -> response <- r; true
            | _ -> false
        member x.TryFail(message: string byref) =
            match x with
            | Fail(f) -> message <- f; true
            | _ -> false

    (* Function definitions *)
    type GetMyAddresses = ApiKey -> Response<MyAddresses>
    type GetAddressBalanceByAddresses = ApiKey -> Addresses -> Response<Balance[]>
    type GetAddressBalanceByLabels = ApiKey -> Labels ->  Response<Balance[]>
    type GetNewAddress = Label -> Response<UserAddress>
    type WithdrawFromAddresses = ApiKey -> Pin -> Addresses -> Addresses -> Amounts -> Response<Withdrawn>
    type GetNetworkFeeEstimate =  ApiKey -> Addresses -> Amounts -> Response<decimal>

    (* Api *)
    type Api = {
        GetAddressBalanceByAddresses : GetAddressBalanceByAddresses
        GetAddressBalanceByLabels : GetAddressBalanceByLabels
        GetNewAddress : GetNewAddress
        GetMyAddresses : GetMyAddresses
        WithdrawFromAddresses : WithdrawFromAddresses
        GetNetworkFeeEstimate : GetNetworkFeeEstimate
    } 


