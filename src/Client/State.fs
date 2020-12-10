module State

open System
open Elmish
open Fable.Core
open Fable.Remoting.Client

open Shared
open Types

// for details of how to deal with virtual path, see:
// https://github.com/Zaid-Ajaj/SAFE.Simplified/blob/master/client/src/Server.fs

let virtualPath : string =
    #if MOCHA_TESTS
    "/"
    #else
    JS.eval("window.location.pathname")
    #endif

let combine (paths: string list) =
    paths
    |> List.map (fun path -> List.ofArray (path.Split('/')))
    |> List.concat
    |> List.filter (fun segment -> not (segment.Contains(".")))
    |> List.filter (String.IsNullOrWhiteSpace >> not)
    |> String.concat "/"
    |> sprintf "/%s"

let normalize (path: string) = combine [ virtualPath; path ]

let normalizeRoutes typeName methodName =
    Route.builder typeName methodName
    |> normalize

let covidMapApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder normalizeRoutes
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
          HoveredArea = None
          MapBounds = defaultBounds }
    
    model, Cmd.batch [ loadDates; loadData ]

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | ShowPage page -> { model with CurrentPage = page }, Cmd.none
    | GotDates dates -> { model with PossibleDates = Some dates; SelectedDate = Some (dates.[0]) }, Cmd.none
    | GotData areas -> { model with Areas = Some (Array.map LeafletHelpers.processArea areas) }, Cmd.none
    | SelectDate date -> { model with SelectedDate = Some date; HoveredArea = None }, Cmd.none
    | Hover area -> { model with HoveredArea = Some area }, Cmd.none
