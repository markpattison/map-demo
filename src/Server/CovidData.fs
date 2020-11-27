module CovidData

open System

open FSharp.Data
open FSharp.Data.CsvExtensions

type CovidData =
    {
        OnsCode: string
        Date: DateTime
        NewCasesBySpecimenDate: float
    }

let private readRow (row: CsvRow) =
    {
        OnsCode = row?areaCode
        Date = row?date.AsDateTime()
        NewCasesBySpecimenDate = row?newCasesBySpecimenDate.AsFloat()
    }

let read (filepath: string) (startDate: DateTime) (endDate: DateTime) =

    let dateFilter (row: CsvRow) =
        let date = row?date.AsDateTime()
        date >= startDate && date <= endDate

    let csv = CsvFile.Load(filepath)

    csv.Rows
    |> Seq.filter dateFilter
    |> Seq.map readRow
    |> Seq.toArray
