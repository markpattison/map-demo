module Geography

open SharpKml.Dom
open SharpKml.Engine

open Shared

let codeAttribute = "lad19cd"
let nameAttribute = "lad19nm"

let extractPoints (ring: LinearRing) =
    {
        LatLongs =
            ring.Coordinates
            |> Seq.map (fun c -> c.Latitude, c.Longitude)
            |> Seq.toArray
    }

let extractShape (poly: Polygon) =
    {
        OuterBoundary = poly.OuterBoundary.LinearRing |> extractPoints
        Holes =
            poly.InnerBoundary
            |> Seq.map (fun innerBoundary -> extractPoints innerBoundary.LinearRing)
            |> Seq.toArray
    }

let rec extractBoundary (g: Geometry) =
    match g with
    | :? Polygon as poly -> Array.singleton (extractShape poly)
    | :? MultipleGeometry as multi -> Seq.collect extractBoundary multi.Geometry |> Seq.toArray
    | _ -> failwith "unknown geometry"

let extractCodeNameAndCoords (p: Placemark) =
    let schemaData = Seq.head p.ExtendedData.SchemaData

    let codeData = schemaData.SimpleData |> Seq.find (fun sd -> sd.Name = codeAttribute)
    let areaCode = ONSCode codeData.Text

    let nameData = schemaData.SimpleData |> Seq.find (fun sd -> sd.Name = nameAttribute)
    let name = nameData.Text   

    let boundary = { Shapes = extractBoundary p.Geometry }

    (areaCode, name, boundary)

let asPlacemark (e: Element) =
    match e with
    | :? Placemark as p -> Some p
    | _ -> None

let readBoundaries (filename: string) =
    use reader = System.IO.File.OpenRead(filename)

    let kmlFile = SharpKml.Engine.KmlFile.Load(reader)
    let kml = kmlFile.Root :?> Kml

    kml.Flatten()
    |> Seq.choose asPlacemark
    |> Seq.map extractCodeNameAndCoords
    |> Seq.toArray
