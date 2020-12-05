module Map

open Fable.React
open Fable.React.Props
open Fulma

open LeafletHelpers
open Types

let attribution = """&copy <a href = "http://osm.org/copyright">OpenStreetMap</a> contributors"""

let inline customControl (props: ReactLeaflet.MapControlProps list) (children: ReactElement list) : ReactElement =
    ofImport "default" "react-leaflet-control" (Fable.Core.JsInterop.keyValueList Fable.Core.CaseRules.LowerFirst props) children

let legendEntry colour text =
    div []
     [ div
         [ Style
             [ Width "10px"
               Height "10px"
               BackgroundColor colour
               Display DisplayOptions.InlineBlock
               MarginRight "5px"
               BorderColor Colours.grey
               BorderStyle "solid"
               BorderWidth "1px" ] ] []
       str text ]

let legend =
    customControl
      [ ReactLeaflet.MapControlProps.Position ReactLeaflet.ControlPosition.Bottomright ]
      [ Box.box' []
          [ str "Weekly cases per 100k"
            br []
            br []
            legendEntry Colours.red "High"
            legendEntry Colours.green "Low"
            legendEntry Colours.grey "No data" ] ]

let view model dispatch =
    ReactLeaflet.map
      [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
        ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
      [ yield ReactLeaflet.tileLayer
          [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
            ReactLeaflet.TileLayerProps.Attribution attribution ] []
        yield legend ]
