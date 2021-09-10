namespace FsJs

open System
open System.Collections.Generic
open FsCore


module Profiling =
    let private initialTicks = DateTime.Now.Ticks


    let profilingState =
        {|
            CountMap = Dictionary<string, int> ()
            TimestampMap = List<string * float> ()
        |}

    let rec globalProfilingState = Dom.Global.register (nameof globalProfilingState) profilingState

    let rec globalClearProfilingState =
        Dom.Global.register
            (nameof globalClearProfilingState)
            (fun () ->
                let getLocals () =
                    $"CountMap.Count={profilingState.CountMap.Count} TimestampMap.Count={profilingState.TimestampMap.Count} {getLocals ()}"

                Logger.logTrace (fun () -> "Profiling.globalClearProfilingState") getLocals
                profilingState.CountMap.Clear ()
                profilingState.TimestampMap.Clear ())

    let removeCount fn =
        if Dom.globalDebug.Get () then
            let id = fn ()

            match profilingState.CountMap.ContainsKey id with
            | false -> profilingState.CountMap.[id] <- -1
            | true -> profilingState.CountMap.[id] <- profilingState.CountMap.[id] - 1

            let getLocals () =
                $"id={id} profilingState.CountMap.[id]={profilingState.CountMap.[id]} {getLocals ()}"

            Logger.logTrace (fun () -> "Profiling.removeCount") getLocals

    let inline private addCountMap id =
        match profilingState.CountMap.ContainsKey id with
        | false -> profilingState.CountMap.[id] <- 1
        | true -> profilingState.CountMap.[id] <- profilingState.CountMap.[id] + 1

    let addCount fn =
        if Dom.globalDebug.Get () then
            let id = fn ()
            addCountMap id

            let getLocals () =
                $"id={id} profilingState.CountMap.[id]={profilingState.CountMap.[id]} {getLocals ()}"

            Logger.logTrace (fun () -> "Profiling.addCount") getLocals

    let addTimestamp fn =
        if Dom.globalDebug.Get () then
            let id = fn ()
            let newTicks = DateTime.ticksDiff initialTicks
            profilingState.TimestampMap.Add (id, newTicks)
            addCountMap id

            let getLocals () =
                $"id={id} newTicks={newTicks} {getLocals ()}"

            Logger.logTrace (fun () -> "Profiling.addTimestamp") getLocals

    addTimestamp (fun () -> $"{nameof FsJs} | Profiling body")


    let measureTimeN n name fn =
        Browser.Dom.console.time name

        for i in 0 .. n do
            fn ()

        Browser.Dom.console.timeEnd name

    let measureTime = measureTimeN 999999
