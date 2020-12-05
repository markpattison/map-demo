module Server

open System

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared

let dates = [ DateTime(2020, 11, 4); DateTime(2020, 11, 10); DateTime(2020, 11, 17) ]

let getAllData() =

    let firstDate = (List.min dates).AddDays(-8.0)
    let lastDate = List.max dates
    let covidData = CovidData.read "./data/ltla_2020-11-22.csv" firstDate lastDate

    let populations = Populations.read "./data/population_estimates.csv"
    let boundaries = Geography.readBoundaries "./data/Local_Authority_Districts__December_2019__Boundaries_UK_BUC.kml"

    JoinData.join dates covidData populations boundaries

let covidMapApi =
    { getDates = fun () -> async { return List.toArray dates }
      getData = fun () -> async { return getAllData() }
    }

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue covidMapApi
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
