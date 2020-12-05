module MapLegend

open Fable.React
open Fable.React.Props
open Fulma

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
            legendEntry Colours.green (sprintf "%.0f" Colours.rateMin)
            legendEntry Colours.yellow (sprintf "%.0f" Colours.rateMid)
            legendEntry Colours.red (sprintf "%.0f" Colours.rateMax)
            legendEntry Colours.grey "No data" ] ]
