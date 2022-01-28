namespace Shared
open System

[<AutoOpen>]
module RateTypes =

    [<CLIMutable>]
    type Rate = 
        { Date: DateTime
          Day7: decimal
          Network : Network  }

    [<CLIMutable>]
    type RateDataSet =
        { Key : string
          Data : decimal[] }

    [<CLIMutable>]
    type GetRate =
        { Labels : DateTime[]
          DataSets : RateDataSet list }
    
    [<CLIMutable>]
    type UpdateRate =
        {  Label : DateTime
           Data: decimal list }
        static member Create (date : DateTime) day7 =
            { 
                Label = date;
                Data = [day7]
            }