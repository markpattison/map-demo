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
    }

let attribution = """&copy <a href = "http://osm.org/copyright">OpenStreetMap</a> contributors"""

let toProps selectedDate (area: AreaView) =
    let (ONSCode code) = area.ONSCode
    { ONSCode = code
      Name = area.Name
      SelectedDate = selectedDate
      WeeklyCasesPer100k = Map.tryFind selectedDate area.Data.WeeklyCasesPer100k
      LeafletBoundary = area.LeafletBoundary
    }

let createReactMapArea (props: MapAreaProps) =
    ReactLeaflet.polygon
     [ ReactLeaflet.PolygonProps.Positions props.LeafletBoundary
       ReactLeaflet.PolygonProps.Weight 0.0
       ReactLeaflet.PolygonProps.FillColor (Colours.interpGreenYellowRed props.WeeklyCasesPer100k)
       ReactLeaflet.PolygonProps.FillOpacity Colours.areaOpacity ]
     []

let createMemoizedReactMapArea =
    FunctionComponent.Of(createReactMapArea, memoizeWith = equalsButFunctions, withKey = (fun p -> p.ONSCode))

let createMapAreas areas date dispatch =
    areas
    |> Array.map (toProps date)
    |> Array.map createMemoizedReactMapArea

let view model dispatch =
    let content =
        match model.Areas, model.SelectedDate with
        | Some areas, Some date -> createMapAreas areas date dispatch
        | _ -> [| |]
    
    ReactLeaflet.map
      [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
        ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
      [ yield ReactLeaflet.tileLayer
          [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
            ReactLeaflet.TileLayerProps.Attribution attribution ] []
        yield MapLegend.legend
        yield! content ]
