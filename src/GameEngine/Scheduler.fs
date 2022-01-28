namespace GameEngine 

open System
open NCrontab
open Shared
open System.Threading

module Scheduler =

    type ScheduleBuilder() =
        
        let schedule (timer : Timer) expr =
            let duetOccurence = CrontabSchedule.Parse(expr).GetNextOccurrence <| SystemTime.UtcNow()
            let due = int (duetOccurence - SystemTime.UtcNow()).TotalMilliseconds

            let intervalOccurance = CrontabSchedule.Parse(expr).GetNextOccurrence duetOccurence
            let interval = int (intervalOccurance - duetOccurence).TotalMilliseconds

            Logger.Debug("Scheduling for {@expr} {@Due}, {@Interval}, {@DuetOccurence}, {@IntervalOccurance}", expr, due, interval, duetOccurence, intervalOccurance)
            timer.Change(due, interval) |> ignore

        let create (expr, callback) =
            let timer = new Timer(fun _ -> callback())      
            schedule timer expr  
            timer  

        member __.Bind((expr, callback), f) = 
            f(expr, callback)

        member __.Return(expr, callback) = 
            create(expr,callback)

    let schedule = new ScheduleBuilder()