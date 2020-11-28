module JoinData

open System

open CovidData
open Shared

let private totalCasesInWeekTo (covidData: CovidData []) (date: DateTime) =
    let weekBefore = date.AddDays(-7.0)

    covidData
    |> Seq.filter (fun cd -> cd.Date >= weekBefore && cd.Date <= date)
    |> Seq.sumBy (fun cd -> cd.NewCasesBySpecimenDate)

let private extractRates (dates: DateTime list) (areaData: CovidData []) population =

    let weeklyRates =
        dates
        |> List.map (fun date -> date, (totalCasesInWeekTo areaData date) * 100000.0 / population)

    {
        WeeklyCasesPer100k = Map.ofList weeklyRates
    }

let joinData (dates: DateTime list) (covidData: CovidData []) (populations: Map<ONSCode, float>) (boundaries: (ONSCode * string * Boundary) []) : Area [] =

    let getArea (onsCode, name, boundary) =

        let population = Map.tryFind onsCode populations
        let areaData = covidData |> Array.filter (fun cd -> cd.ONSCode = onsCode)

        let covidRates =

            match population, Array.isEmpty areaData with
            | Some pop, false when pop > 0.0 -> extractRates dates areaData pop |> Some
            | _ -> None

        {
            ONSCode = onsCode
            Name = name
            Boundary = boundary
            Data = covidRates
        }

    boundaries |> Array.map getArea