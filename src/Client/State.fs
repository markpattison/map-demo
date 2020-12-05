module State

open Elmish
open Fable.Remoting.Client

open Shared
open Types

let covidMapApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ICovidMapApi>

let defaultBounds = (51.0, -5.0), (55.0, 1.5)

let loadDates = Cmd.OfAsync.perform covidMapApi.getDates () GotDates
let loadData = Cmd.OfAsync.perform covidMapApi.getData () GotData

let init(): Model * Cmd<Msg> =
    let model =
        { CurrentPage = Introduction
          PossibleDates = None
          SelectedDate = None
          Areas = None
          MapBounds = defaultBounds }
    
    model, Cmd.batch [ loadDates; loadData ]

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | ShowPage page -> { model with CurrentPage = page }, Cmd.none
    | GotDates dates -> { model with PossibleDates = Some dates; SelectedDate = Some (dates.[0]) }, Cmd.none
    | GotData areas -> { model with Areas = Some (Array.map LeafletHelpers.processArea areas) }, Cmd.none
    | SelectDate date -> { model with SelectedDate = Some date }, Cmd.none
