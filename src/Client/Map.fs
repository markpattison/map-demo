module Map

open Fable.React
open Fable.React.Props
open Fulma
open System

open LeafletHelpers
open Shared
open Types

type MapAreaProps =
    { ONSCode: string
      Name: string
      SelectedDate: DateTime
      WeeklyCasesPer100k: float option
      LeafletBoundary: LeafletBoundary
      Hovered: bool
      OnHover: Leaflet.LeafletMouseEvent -> unit
    }

let attribution = """&copy <a href = "http://osm.org/copyright">OpenStreetMap</a> contributors"""

let toProps selectedDate dispatch hoveredArea (area: AreaView) =
    let (ONSCode code) = area.ONSCode
    { ONSCode = code
      Name = area.Name
      SelectedDate = selectedDate
      WeeklyCasesPer100k = Map.tryFind selectedDate area.Data.WeeklyCasesPer100k
      LeafletBoundary = area.LeafletBoundary
      Hovered = (hoveredArea = Some area)
      OnHover = (fun _ -> dispatch (Hover area))
    }

let createReactMapArea (props: MapAreaProps) =
    ReactLeaflet.polygon
     [ ReactLeaflet.PolygonProps.Positions props.LeafletBoundary
       ReactLeaflet.PolygonProps.Weight (if props.Hovered then Colours.borderWeight else 0.0)
       ReactLeaflet.PolygonProps.Color Colours.black
       ReactLeaflet.PolygonProps.FillColor (Colours.interpGreenYellowRed props.WeeklyCasesPer100k)
       ReactLeaflet.PolygonProps.FillOpacity Colours.areaOpacity
       ReactLeaflet.PolygonProps.OnMouseOver props.OnHover ]
     []

let createMemoizedReactMapArea =
    FunctionComponent.Of(createReactMapArea, memoizeWith = equalsButFunctions, withKey = (fun p -> p.ONSCode + if p.Hovered then "-hovered" else ""))

let createMapAreas areas date hoveredArea dispatch =
    let hovered, unhovered = areas |> Array.partition (fun a -> hoveredArea = Some a)

    Array.append
        (unhovered |> Array.map (toProps date dispatch hoveredArea) |> Array.map createMemoizedReactMapArea)
        (hovered |> Array.map (toProps date dispatch hoveredArea) |> Array.map createMemoizedReactMapArea)

let button txt onClick isSelected =
    Control.div []
      [ Button.button
          [ if isSelected then yield Button.Color IsPrimary
            yield Button.OnClick onClick ]
          [ str txt ] ]

let createDateButtons model dispatch =
    match model.PossibleDates, model.SelectedDate with
    | Some dates, Some selectedDate ->
        Field.div [ Field.IsGroupedMultiline ]
          (dates |> List.ofArray |> List.map (fun d -> button (d.ToShortDateString()) (fun _ -> dispatch (SelectDate d)) (selectedDate = d)))
    | _ -> Field.div [] [ str "Loading data..." ]

let view model dispatch =
    let dateButtons = createDateButtons model dispatch
    
    let mapAreas =
        match model.Areas, model.SelectedDate with
        | Some areas, Some date -> createMapAreas areas date model.HoveredArea dispatch
        | _ -> [| |]
    
    div []
      [ ReactLeaflet.map
          [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
            ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
          [ yield ReactLeaflet.tileLayer
              [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
                ReactLeaflet.TileLayerProps.Attribution attribution ] []
            yield MapLegend.legend
            yield! mapAreas ]
        br []
        dateButtons ]
