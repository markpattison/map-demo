module CovidData

open System

open FSharp.Data
open FSharp.Data.CsvExtensions

open Shared

type CovidData =
    {
        ONSCode: ONSCode
        Date: DateTime
        NewCasesBySpecimenDate: float
    }

let private readRow (row: CsvRow) =
    let newCasesBySpecimenDate = row?newCasesBySpecimenDate

    {
        ONSCode = ONSCode row?areaCode
        Date = row?date.AsDateTime()
        NewCasesBySpecimenDate = 
            if String.IsNullOrWhiteSpace(newCasesBySpecimenDate) then 0.0 else newCasesBySpecimenDate.AsFloat()
    }

let read (filepath: string) startDate endDate =

    let dateFilter (row: CsvRow) =
        let date = row?date.AsDateTime()
        date >= startDate && date <= endDate

    let csv = CsvFile.Load(filepath)

    csv.Rows
    |> Seq.filter dateFilter
    |> Seq.map readRow
    |> Seq.toArray
