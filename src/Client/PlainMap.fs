module PlainMap

open Fable.React
open Fable.React.Props

open LeafletHelpers
open Types

let attribution = """&copy <a href = "http://osm.org/copyright">OpenStreetMap</a> contributors"""

let view model =
    ReactLeaflet.map
      [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
        ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
      [ yield ReactLeaflet.tileLayer
          [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
            ReactLeaflet.TileLayerProps.Attribution attribution ]
          []
      ]
