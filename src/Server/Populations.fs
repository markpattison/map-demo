module Populations

open FSharp.Data
open FSharp.Data.CsvExtensions

open Shared

let private readRow (row: CsvRow) =
    let onsCode = ONSCode row?Code
    let population = row?Population.AsFloat()

    onsCode, population

let read (filepath: string) =
    let csv = CsvFile.Load(filepath)

    csv.Rows
    |> Seq.map readRow
    |> Map.ofSeq
