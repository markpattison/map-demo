module Map

open Fable.React
open Fable.React.Props

open Types

let attribution = """&copy <a href = "http://osm.org/copyright">OpenStreetMap</a> contributors"""

let toBounds (point1, point2) : Leaflet.LatLngBoundsExpression = [ point1; point2 ] |> ResizeArray<Leaflet.LatLngTuple> |> Fable.Core.U2.Case2

let view model dispatch =
    ReactLeaflet.map
      [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
        ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
      [ yield ReactLeaflet.tileLayer
          [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
            ReactLeaflet.TileLayerProps.Attribution attribution ]
          []
      ]
