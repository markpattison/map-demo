module Blog

let introduction = """
## Introduction

This site is part of the [F# Advent Calendar in English 2020](https://sergeytihon.com/2020/10/22/f-advent-calendar-in-english-2020/).

In this walkthrough I'll show how to quickly and easily visualise data on an interactive map using [F#](https://fsharp.org/), [Leaflet](https://leafletjs.com/) and the [SAFE Stack](https://safe-stack.github.io/).

As you'd expect from a SAFE Stack application, we're going to be using [Fable](https://fable.io/) to render the map and a simple [Saturn](https://saturnframework.org/) server to provide the data.

All source code can be found on [Github](https://github.com/markpattison/map-demo).

#### Sample data - COVID-19 case rates in the United Kingdom

Although we've probably all seen enough maps and charts of the pandemic, it's at least an up-to-date (and possibly even useful) example of geographically-based data.

COVID-19 data for the UK can be explored and downloaded from [this page](https://coronavirus.data.gov.uk/details/download) - I've used a CSV file containing daily data for new cases split by local authority (council) areas, of which there are around 380 in the UK.  [This link](https://api.coronavirus.data.gov.uk/v2/data?areaType=ltla&metric=newCasesByPublishDate&metric=newCasesBySpecimenDate&format=csv) will always get the latest version of the file.

The data for the local authority boundaries was downloaded from [here](https://geoportal.statistics.gov.uk/datasets/local-authority-districts-december-2019-boundaries-uk-buc) in [KML](https://developers.google.com/kml/documentation/kml_tut) format.  I've used the ultra-generalised version as this is a much smaller file, but is easily good enough for visualising at a national level.

Finally, I've used population estimates from the [ONS](https://www.ons.gov.uk/peoplepopulationandcommunity/populationandmigration/populationestimates/datasets/populationestimatesforukenglandandwalesscotlandandnorthernireland) to convert absolute case numbers into rates per 100,000 population, which is a familiar metric.
"""


let shared = """
## Shared data types and API

Here we'll look at the shape of the data that the server will be providing to the client.  One of the SAFE Stack's best features is the extremely concise way in which we can specify an API.

Fundamentally our data will be a list of local authority areas, with an attaching ONS code (a standard way of referencing administrative areas in the UK), local authority name, boundary data (to draw the area on a map) and covid data.

This code all lives in the `Shared.fs` file which is referenced by the both the server and client projects.  This guarantees that our data types match.

#### Boundary data

In general, a geographic region can be made up of several disconnected areas.  Each of these could in turn have holes within them.  This can be mapped nicely to F# in the following way.

A `Loop` is a simple boundary made up of an array of latitude/longitude pairs: <img style="float: right;" src="shape1.png">

    type Loop =
        {
            LatLongs: (float * float) []
        }

A `Shape` has one outer `Loop` and zero or more holes, which are themselves represented as `Loop` elements: <img style="float: right;" src="shape2.png">

    type Shape =
        {
            OuterBoundary: Loop
            Holes: Loop []
        }

A `Boundary` is made up of at least one `Shape`: <img style="float: right;" src="shape3.png">

    type Boundary =
        {
            Shapes: Shape []
        }

This model is rich enough to represent any geographic area.

#### Covid data

We'll use a simple data structure to hold some weekly data:

    type CovidRates =
        {
            WeeklyCasesPer100k: Map<DateTime, float>
        }

A single-case discriminated union will help keep our data type-safe:

    type ONSCOde = | ONSCode of string

Finally we just need another record type to hold all the relevant data for a single area.  We'll use an option type for the rates in case we have missing data for some areas:

    type Area =
        {
            ONSCode: ONSCOde
            Name: string
            Boundary: Boundary
            Data: CovidRates option
        }

#### API

The following interface specifies our API.  In our case it's extremely simple, with only two methods - one to get the list of dates at which we'll have data, and one to fetch the data itself.

    type ICovidMapApi =
        { getDates : unit -> Async<DateTime []>
          getData : unit -> Async<Area []>
        }

Again, this is shared between the client and server implementations - [Fable Remoting](https://zaid-ajaj.github.io/Fable.Remoting/) will magically take care of the rest!

"""


let server = """
## Server implementation

#### Data

We have three data files (geographic boundaries, Covid rates, population estimates) in the `src\Server\data` folder.  In a production app we would probably reload the Covid data every few hours and cache it in between, but for this walkthrough we'll use static data.

#### Reading the boundaries

This section mosly involves just converting from one domain model to another.

The geographic data is stored in a KML file.  This is a type of XML, so we can easily open and inspect it, which helped with understanding the hierarchy of elements.

To read the file more conveniently we're using [SharpKML](https://github.com/samcragg/sharpkml) which provides a nice .NET wrapper around the [KML format](https://developers.google.com/kml/documentation/kml_tut).

Starting from the top down, we want to open a file and extract the boundary data (plus ONS codes and names):

    let readBoundaries (filename: string) =
        use reader = System.IO.File.OpenRead(filename)

        let kmlFile = SharpKml.Engine.KmlFile.Load(reader)
        let kml = kmlFile.Root :?> Kml

        kml.Flatten()
        |> Seq.choose asPlacemark
        |> Seq.map extractCodeNameAndCoords
        |> Seq.toArray

The `asPlacemark` function just keeps everything from the file which is a `Placemark`, which represents a point or area on Earth.  

    let asPlacemark (e: Element) =
        match e with
        | :? Placemark as p -> Some p
        | _ -> None

Now the first `Placemark` entry in our file looks like this.  We want to extract the area code and name as well as the boundary details (cut short here for brevity).

      <Placemark>
    	<Style><LineStyle><color>ff0000ff</color></LineStyle><PolyStyle><fill>0</fill></PolyStyle></Style>
    	<ExtendedData><SchemaData schemaUrl="#Local_Authority_Districts__December_2019__Boundaries_UK_BUC">
    		<SimpleData name="objectid">1</SimpleData>
    		<SimpleData name="lad19cd">E06000001</SimpleData>
    		<SimpleData name="lad19nm">Hartlepool</SimpleData>
    		<SimpleData name="lad19nmw"></SimpleData>
    		<SimpleData name="bng_e">447160</SimpleData>
    		<SimpleData name="bng_n">531474</SimpleData>
    		<SimpleData name="long">-1.27018</SimpleData>
    		<SimpleData name="lat">54.67614</SimpleData>
    		<SimpleData name="st_areashape">96845510.2463086</SimpleData>
    		<SimpleData name="st_lengthshape">50305.3250576014</SimpleData>
    	</SchemaData></ExtendedData>
          <Polygon><outerBoundaryIs><LinearRing><coordinates>-1.24099446513821,54.723193897637 ...</coordinates></LinearRing></outerBoundaryIs></Polygon>
      </Placemark>

The `extractCodeNameAndCoords` function does this for us, calling `extractBoundary` to get the boundary details:

    let codeAttribute = "lad19cd"
    let nameAttribute = "lad19nm"

    let extractCodeNameAndCoords (p: Placemark) =
        let schemaData = Seq.head p.ExtendedData.SchemaData

        let codeData = schemaData.SimpleData |> Seq.find (fun sd -> sd.Name = codeAttribute)
        let areaCode = ONSCode codeData.Text

        let nameData = schemaData.SimpleData |> Seq.find (fun sd -> sd.Name = nameAttribute)
        let name = nameData.Text   

        let boundary = { Shapes = extractBoundary p.Geometry }

        (areaCode, name, boundary)

It turns out that a `Geometry` object can either be a single `Polygon` or a `MultipleGeometry`, which contains multiple sub-`Geometry` objects.  In F# this can naturally be handled with recursion:

    let rec extractBoundary (g: Geometry) =
        match g with
        | :? Polygon as poly -> Array.singleton (extractShape poly)
        | :? MultipleGeometry as multi -> Seq.collect extractBoundary multi.Geometry |> Seq.toArray
        | _ -> failwith "unknown geometry"

Now we just need to turn the KML `Polygon` object into our own `Shape` (with an `InnerBoundary` being a "hole" in an area):

    let extractShape (poly: Polygon) =
        {
            OuterBoundary = poly.OuterBoundary.LinearRing |> extractPoints
            Holes =
                poly.InnerBoundary
                |> Seq.map (fun innerBoundary -> extractPoints innerBoundary.LinearRing)
                |> Seq.toArray
        }

Finally we just need to convert the list of points into our `Loop` type:

    let extractPoints (ring: LinearRing) =
        {
            LatLongs =
                ring.Coordinates
                |> Seq.map (fun c -> c.Latitude, c.Longitude)
                |> Seq.toArray
        }

#### Reading Covid rates and populations

We're using the CSV Parser from the [FSharp.Data](https://fsharp.github.io/FSharp.Data/) package to read the CSV file.

First we'll create a type to represent the data from one row of the file (i.e. a unique combination of area and date):

    type CovidData =
        {
            ONSCode: ONSCode
            Date: DateTime
            NewCasesBySpecimenDate: float
        }

Next up is a function to read that data from an actual CSV row.  I found that some rows had blanks which I've replaced here with zero values.

Yes, the number of cases per day is really an integer, but I decided to store everything as floats to keep things simple.

    let private readRow (row: CsvRow) =
        let newCasesBySpecimenDate = row?newCasesBySpecimenDate

        {
            ONSCode = ONSCode row?areaCode
            Date = row?date.AsDateTime()
            NewCasesBySpecimenDate = 
                if String.IsNullOrWhiteSpace(newCasesBySpecimenDate) then 0.0 else newCasesBySpecimenDate.AsFloat()
        }

Now we're ready to read in the file and convert it to an array of data using the above function.

As there are something like 120k rows in the original data, there's a filter so we can only keep the range of dates we're interested in.

    let read (filepath: string) startDate endDate =

        let dateFilter (row: CsvRow) =
            let date = row?date.AsDateTime()
            date >= startDate && date <= endDate

        let csv = CsvFile.Load(filepath)

        csv.Rows
        |> Seq.filter dateFilter
        |> Seq.map readRow
        |> Seq.toArray

Reading the population data is very similar so I haven't copied it here.

#### Combining the data

A couple of straightforward functions are used to extract the weekly total of new cases per 100k population for a particular area and list of dates.

    let private totalCasesInWeekTo covidData (date: DateTime) =
        let weekBefore = date.AddDays(-6.0)

        covidData
        |> Seq.filter (fun cd -> cd.Date >= weekBefore && cd.Date <= date)
        |> Seq.sumBy (fun cd -> cd.NewCasesBySpecimenDate)

    let private extractRates dates areaData population =

        let weeklyRates =
            dates
            |> List.map (fun date -> date, (totalCasesInWeekTo areaData date) * 100000.0 / population)

        {
            WeeklyCasesPer100k = Map.ofList weeklyRates
        }

Lastly, we can join the various data together to end up with an array of `Area` records which will be returned by our API:

    let join dates (covidData: CovidData []) populations boundaries =

        let getArea (onsCode, name, boundary) =

            let population = Map.tryFind onsCode populations
            let areaData = covidData |> Array.filter (fun cd -> cd.ONSCode = onsCode)

            let covidRates =

                match population, Array.isEmpty areaData with
                | Some pop, false when pop > 0.0 -> extractRates dates areaData pop |> Some
                | _ -> None

            {
                ONSCode = onsCode
                Name = name
                Boundary = boundary
                Data = covidRates
            }

        boundaries |> Array.map getArea

"""


let clientPlainMap = """
## Client: Drawing a map

We're going to use the [Leaflet](https://leafletjs.com/) JavaScript library to actually render a map.

However, to fit into our nice Elmish/React framework we're also going to use [React Leaflet](https://react-leaflet.js.org/) which wraps a Leaflet map in a React component,
[Fable.ReactLeaflet](https://github.com/MangelMaxime/Fable.ReactLeaflet) to provide Fable bindings for React Leaflet and
[react-leaflet-control](https://www.npmjs.com/package/react-leaflet-control) which will let use React elements inside the map, e.g. as a legend.

Whilst this is a lot to bring in, it will make adding data to the map simple.

#### Setup

We can add the dependencies using the following:

    npm install -s leaflet
    npm install -s react-leaflet@2.7.0
    npm install -s react-leaflet-control

    paket add Fable.ReactLeaflet

Note that version 3 of `react-leaflet` has breaking changes which aren't yet reflected in `Fable.ReactLeaflet`, so we're using an earlier version.

Adding `Fable.ReactLeaflet` automatically brings a reference to [Fable.Leaflet](https://github.com/MangelMaxime/Fable.Leaflet) which contains all the domain types.

We also need to add the Leaflet CSS and JS files to our `index.html` page as described in Leaflet's [Getting Started](https://leafletjs.com/examples/quick-start/) page.

#### Initial view

It's convenient to be able to specify the initial map view in our model.  We'll do this by adding the initial bounds to our state variable:

    MapBounds: (float * float) * (float * float)

Here each `float * float` pair holds the latitude and longitude of a point, with the two points holding the minimum and maximum values we'd like to see when first viewing the map.

These values will show the southern part of the UK:

    let defaultBounds = (51.0, -5.0), (55.0, 1.5)

#### Drawing the map

The only helper function we need is something to turn our pair of bounds into a `LatLngBoundsExpression` which Fable.Leaflet can use.  This took a bit of experimentation to get right.

    let toBounds (point1, point2) : Leaflet.LatLngBoundsExpression = [ point1; point2 ] |> ResizeArray<Leaflet.LatLngTuple> |> Fable.Core.U2.Case2

At last we're ready to draw the map!  The Fable/React bindings make this clean and declarative.

Following the [example](https://react-leaflet.js.org/docs/start-setup) we can add a tile layer from OpenStreetMap (with attribution, of course) and immediately having a working interactive map on our page.

    let view model dispatch =
        ReactLeaflet.map
          [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
            ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
          [ yield ReactLeaflet.tileLayer
              [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
                ReactLeaflet.TileLayerProps.Attribution attribution ]
              []
          ]

Here's our map:
"""


let clientShowData = """
## Client: Showing our data
"""


let results = """
## The resulting map
"""