namespace Shared

open System

type Loop =
    {
        LatLongs: (float * float) []
    }

type Shape =
    {
        OuterBoundary: Loop
        Holes: Loop []
    }

type Boundary =
    {
        Shapes: Shape []
    }

type CovidRates =
    {
        WeekToOct30: float option
        WeekToNov06: float option
        WeekToNov13: float option
    }

type ONSCOde = | ONSCode of string

type Area =
    {
        ONSCode: ONSCOde
        Name: string
        Boundary: Boundary
        Data: CovidRates
    }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type ICovidMapApi =
    { getData : unit -> Async<Area []> }
