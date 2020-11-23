module Index

open Elmish
open Fable.Remoting.Client
open Shared

type Page =
    | Introduction
    | Shared
    | Server
    | ClientPlainMap
    | ClientShowData

type Model =
    { CurrentPage: Page 
      Data: int option }

type Msg =
    | ShowPage of Page
    | GotData of int

let todosApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init(): Model * Cmd<Msg> =
    let model =
        { CurrentPage = Introduction; Data = None }
    let cmd = Cmd.OfAsync.perform todosApi.getData () GotData
    model, cmd

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | ShowPage page ->
        { model with CurrentPage = page }, Cmd.none
    | GotData data ->
        { model with Data = Some data }, Cmd.none

open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fulma

importAll "./sass/main.sass"

let menuLink currentPage dispatch label page =
    Menu.Item.li
      [ Menu.Item.IsActive (page = currentPage)
        Menu.Item.Props [ OnClick (fun _ -> ShowPage page |> dispatch) ] ]
      [ str label ]

let menu currentPage dispatch =
  let menuItem = menuLink currentPage dispatch
  Menu.menu []
    [ Menu.label []
        [ str "Tutorial" ]
      Menu.list []
        [ menuItem "Introduction" Introduction
          menuItem "Shared" Shared
          menuItem "Server" Server
          menuItem "Client - plain map" ClientPlainMap
          menuItem "Client - show data" ClientShowData ] ]

let pageContent (model : Model) (dispatch : Msg -> unit) =
  match model.CurrentPage with
  | Introduction -> div [] []
  | Shared -> div [] []
  | Server -> div [] []
  | ClientPlainMap -> div [] []
  | ClientShowData -> div [] []

let view (model : Model) (dispatch : Msg -> unit) =
  div []
    [ Navbar.view
      Section.section []
        [ Container.container []
            [ Columns.columns []
                [ Column.column
                    [ Column.Width (Screen.All, Column.Is3) ]
                    [ menu model.CurrentPage dispatch ]
                  Column.column []
                    [ pageContent model dispatch ] ] ] ] ]
