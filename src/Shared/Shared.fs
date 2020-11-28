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
        WeeklyCasesPer100k: Map<DateTime, float>
    }

type ONSCode = | ONSCode of string

type Area =
    {
        ONSCode: ONSCode
        Name: string
        Boundary: Boundary
        Data: CovidRates option
    }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type ICovidMapApi =
    { getDates : unit -> Async<DateTime []>
      getData : unit -> Async<Area []>
    }
