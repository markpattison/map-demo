module LeafletHelpers

open Shared
open Types

let toBounds (point1, point2) : Leaflet.LatLngBoundsExpression = [ point1; point2 ] |> ResizeArray<Leaflet.LatLngTuple> |> Fable.Core.U2.Case2

let toLatLong point: Leaflet.LatLngExpression = Fable.Core.U3.Case3 point

let processLoop loop =
    Array.map toLatLong loop.LatLongs

let processBoundary boundary : LeafletBoundary =
    boundary.Shapes
    |> Array.map (fun shape ->
        let outer = processLoop shape.OuterBoundary
        let holes = Array.map processLoop shape.Holes
        Array.concat [ [| outer |]; holes ])
    |> Fable.Core.U3.Case3

let processArea (area: Area) : AreaView =
    { ONSCode = area.ONSCode
      Name = area.Name
      Data =
        match area.Data with
        | None -> { WeeklyCasesPer100k = Map.empty }
        | Some data -> data
      LeafletBoundary = processBoundary area.Boundary
    }
