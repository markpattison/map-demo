module View

open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fulma

open Types

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
  | Introduction -> Markdown.parseAsReactEl "" Blog.introduction
  | Shared -> Markdown.parseAsReactEl "" Blog.shared
  | Server -> Markdown.parseAsReactEl "" Blog.server
  | ClientPlainMap -> Markdown.parseAsReactEl "" Blog.clientPlainMap
  | ClientShowData -> Markdown.parseAsReactEl "" Blog.clientShowData

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
