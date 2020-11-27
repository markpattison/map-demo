module State

open Elmish
open Fable.Remoting.Client

open Shared
open Types

let covidMapApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ICovidMapApi>

let init(): Model * Cmd<Msg> =
    let model =
        { CurrentPage = Introduction; Data = None }
    //let cmd = Cmd.OfAsync.perform covidMapApi.getData () GotData
    model, Cmd.none

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | ShowPage page ->
        { model with CurrentPage = page }, Cmd.none
    | GotData data ->
        { model with Data = Some data }, Cmd.none