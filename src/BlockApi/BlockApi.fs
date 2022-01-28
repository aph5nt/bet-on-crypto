namespace BlockApi

open Shared
open FSharp.Data

module BlockApi =
    open Types

    let endpoint = Settings.PaymentGateway.ApiUrl
    
    let post endpoint query onSuccess onFailure =
        try
            let response = Http.Request(endpoint, query, httpMethod="POST", customizeHttpRequest = (fun req -> req.Timeout <- 10000; req))
            match response.StatusCode with
            | code when code = 200 -> 
                match response.Body with
                | Text message ->
                    onSuccess message |> Success
                | _ -> failwith "unsupported operation"
            | code when code >= 400 ->
                match response.Body with
                | Text message ->  
                    onFailure message |> Fail
                | _ -> failwith "unsupported operation"
            | _ -> failwith (sprintf <| "unsupported statuscode %d" <| response.StatusCode)
         with exn ->
            try
                let s = exn.Message.IndexOf("{")
                let e = exn.Message.LastIndexOf("}")
                let json = exn.Message.Substring(s, e-s+1)
                onFailure json |> Fail
            with _ ->
                Logger.Error(exn, "Unable to execute request")
                Fail "Unable to execute request"
 
    let onBalanceSuccess message = 
                 let result = GetAddressBalanceType.Parse message
                 result.Data.Balances
                 |> Array.map(fun i -> { Balance.Network = result.Data.Network; Address = i.Address; Balance = i.AvailableBalance})

    let onFail message =
        let result = FailType.Parse message
        Logger.Error("Invalid request. {@ErrorMessage}", result.Data.ErrorMessage)
        result.Data.ErrorMessage

    let GetAddressBalanceByAddresses : GetAddressBalanceByAddresses = 
        fun apiKey addresses ->
            let query = [ 
                ("api_key", apiKey);
                ("addresses", System.String.Join(",", addresses)); ]
            post
            <| (sprintf "%s/%s" <| endpoint <| "get_address_balance")
            <| query
            <| onBalanceSuccess
            <| onFail

    let GetAddressBalanceByLabels : GetAddressBalanceByLabels = 
        fun apiKey labels ->
            let query = [ 
                ("api_key", apiKey);
                ("labels", System.String.Join(",", labels)); ]
            post
            <| (sprintf "%s/%s" <| endpoint <| "get_address_balance")
            <| query
            <| onBalanceSuccess
            <| onFail

    let GetNewAddress : GetNewAddress = 
        fun label ->
            let apiKey = Settings.PaymentGateway.ApiKey
            let query = [ 
                ("api_key", apiKey);]
                //("label", label); ] // un comment it when creating new users will be fixed (registration )
            let onSuccess message = 
                let address = GetNewAddressType.Parse message
                {
                    Address = address.Data.Address
                    Label = address.Data.Label
                    Network = address.Data.Network
                    UserId = address.Data.UserId
                }
            post
            <| (sprintf "%s/%s" <| endpoint <| "get_new_address")
            <| query
            <| onSuccess
            <| onFail

    let GetMyAddresses : GetMyAddresses =
        fun apiKey ->
            let query = [("api_key", apiKey)]
            let onSuccess message = 
                let response = GetMyAddressesType.Parse message
                let addresses = 
                    response.Data.Addresses 
                    |> Array.map(fun a -> { UserId = a.UserId; Address = a.Address; Label = a.Label; AvailableBalance = a.AvailableBalance; PendingReceivedBalance = a.PendingReceivedBalance })
                {
                  Network = response.Data.Network
                  Data = addresses 
                }
            post
            <| (sprintf "%s/%s" <| endpoint <| "get_my_addresses")
            <| query
            <| onSuccess
            <| onFail
                

    let GetNetworkFeeEstimate : GetNetworkFeeEstimate = 
        fun apiKey toAddress amounts ->
            let mapAmount (amount : decimal) =
                amount.ToString().Replace(",", ".")
            let query = [ 
                ("api_key", apiKey);
                ("to_addresses", System.String.Join(",",toAddress)); 
                ("amounts", System.String.Join(",", (amounts |> Array.map mapAmount)))]
            let onSuccess message = 
                let estimation = EstimateNetworkFeeType.Parse message
                estimation.Data.EstimatedNetworkFee
            post
            <| (sprintf "%s/%s" <| endpoint <| "get_network_fee_estimate")
            <| query
            <| onSuccess
            <| onFail
        

    let WithdrawFromAddresses : WithdrawFromAddresses =
        fun apiKey pin fromAddress toAddress amounts ->
            let mapAmount (amount : decimal) =
                amount.ToString().Replace(",", ".")
            let query = [ 
                ("api_key", apiKey);
                ("priority","low");
                ("from_addresses", System.String.Join(",",fromAddress)); 
                ("to_addresses", System.String.Join(",",toAddress)); 
                ("amounts", System.String.Join(",", (amounts |> Array.map mapAmount))); 
                ("pin", pin); ]
            let onSuccess message = 
                let widhraw = WithdrawFromAddressesType.Parse message
                {
                    Withdrawn.AmountSent = widhraw.Data.AmountSent
                    Withdrawn.AmountWithdrawn = widhraw.Data.AmountWithdrawn
                    Withdrawn.BlockIoFee = widhraw.Data.BlockioFee
                    Withdrawn.Network = widhraw.Data.Network
                    Withdrawn.NetworkFee = widhraw.Data.NetworkFee
                    Withdrawn.TxId = widhraw.Data.Txid
                }
            post
            <| (sprintf "%s/%s" <| endpoint <| "withdraw_from_addresses")
            <| query
            <| onSuccess
            <| onFail
 
    
    let Api() = { 
        GetAddressBalanceByAddresses = GetAddressBalanceByAddresses
        GetAddressBalanceByLabels = GetAddressBalanceByLabels
        GetNewAddress = GetNewAddress 
        GetMyAddresses = GetMyAddresses
        WithdrawFromAddresses = WithdrawFromAddresses 
        GetNetworkFeeEstimate = GetNetworkFeeEstimate }
