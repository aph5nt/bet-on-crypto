open System.Windows.Forms
open FSharp.Charting
open FSharp.Charting.ChartTypes

[<EntryPoint>]
let main argv = 

   Analitics.Service.Run()

  
   
   System.Console.ReadKey() |> ignore
   0
