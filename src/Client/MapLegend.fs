module MapLegend

open System
open Fable.React
open Fable.React.Props
open Fulma

open Types

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
            legendEntry Colours.colourMin (sprintf "%.0f" Colours.rateMin)
            legendEntry Colours.colourMid (sprintf "%.0f" Colours.rateMid)
            legendEntry Colours.colourMax (sprintf "%.0f" Colours.rateMax)
            legendEntry Colours.grey "No data" ] ]

let private casesText (date: DateTime, cases) =
    [ sprintf "%s: %.0f weekly cases per 100k" (date.ToShortDateString()) cases |> str
      br [] ]

let areaInfo (area: AreaView) =
    customControl
      [ ReactLeaflet.MapControlProps.Position ReactLeaflet.ControlPosition.Topright ]
      [ Box.box' []
          [ str area.Name
            br[]
            br[]
            if Map.isEmpty area.Data.WeeklyCasesPer100k then
                str "No data"
            else
                yield! area.Data.WeeklyCasesPer100k |> Map.toSeq |> Seq.collect casesText ] ]
