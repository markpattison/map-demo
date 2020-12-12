module Blog

let introduction = """
## Introduction

This site is part of the [F# Advent Calendar in English 2020](https://sergeytihon.com/2020/10/22/f-advent-calendar-in-english-2020/).<img style="float: right;" src="map.png">

In this walkthrough I'll show how to quickly and easily visualise data on an interactive map using [F#](https://fsharp.org/), [Leaflet](https://leafletjs.com/) and the [SAFE Stack](https://safe-stack.github.io/).

As you'd expect from a SAFE Stack application, we're going to be using [Fable](https://fable.io/) to render the map and a simple [Saturn](https://saturnframework.org/) server to provide the data.

The starting point for this project was v2.2.0 of the SAFE Stack template, with the testing projects and Azure deployment parts removed.

All source code can be found on [Github](https://github.com/markpattison/map-demo).

#### Sample data - COVID-19 case rates in the United Kingdom

Although we've probably all seen enough maps and charts of the pandemic, it's at least an up-to-date (and possibly even useful) example of geographically-based data.

COVID-19 data for the UK can be explored and downloaded from [this page](https://coronavirus.data.gov.uk/details/download) - I've used a CSV file containing daily data for new cases split by local authority areas (i.e. local government subdivisions), of which there are around 380 in the UK.  [This link](https://api.coronavirus.data.gov.uk/v2/data?areaType=ltla&metric=newCasesByPublishDate&metric=newCasesBySpecimenDate&format=csv) will always get the latest version of the file.

The data for the local authority boundaries was downloaded from [here](https://geoportal.statistics.gov.uk/datasets/local-authority-districts-december-2019-boundaries-uk-buc) in [KML](https://developers.google.com/kml/documentation/kml_tut) format.  I've used the ultra-generalised version as this is a much smaller file, but is easily good enough for visualising at a national level.

Finally, I've used population estimates from the [ONS](https://www.ons.gov.uk/peoplepopulationandcommunity/populationandmigration/populationestimates/datasets/populationestimatesforukenglandandwalesscotlandandnorthernireland) to convert absolute case numbers into rates per 100,000 population, which is a commonly quoted metric.
"""


let shared = """
## Shared data types and API

Here we'll look at the shape of the data that the server will be providing to the client.  One of the SAFE Stack's best features is the extremely concise way in which we can specify an API.

Fundamentally our data will be a list of local authority areas, with an attaching ONS code (a standard way of referencing administrative areas in the UK), local authority name, boundary data (to draw the area on a map) and covid data.

We'd also like to return a list of dates which the user can select from.

This code all lives in the `Shared.fs` file which is referenced by the both the server and client projects.  This guarantees that our data types match.

#### Boundary data

In general, a geographic region can be made up of several disconnected areas.  Each of these could in turn have holes within them.  This can be mapped nicely to an F# domain model as follows.

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

    type ONSCode = | ONSCode of string

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

We have three data files (geographic boundaries, Covid rates, population estimates) in the `src\Server\data` folder.  In a production app we would probably reload the Covid data from the government portal's API every few hours and cache it in between, but for this walkthrough we'll use static data.

#### Reading the boundaries

This section mosly involves just converting from one domain model to another.

The geographic data is stored in a KML file.  This is a type of XML, so we can easily open and inspect it, which helps with understanding the hierarchy of elements.

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

The first `Placemark` entry in our file looks like this.  We want to extract the area code and name as well as the boundary details (cut short here for brevity).

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
            OuterBoundary = extractPoints poly.OuterBoundary.LinearRing
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

Fun fact: the only local authority to actually have a hole in is South Cambridgeshire.  No prizes for guessing that the hole is Cambridge!

#### Reading Covid rates and populations

The start of our Covid rates CSV file looks like this:

    date,areaType,areaCode,areaName,newCasesByPublishDate,newCasesBySpecimenDate
    "2020-11-22",ltla,E06000001,Hartlepool,31,
    "2020-11-21",ltla,E06000001,Hartlepool,59,0
    ...

We're using the CSV Parser from the [FSharp.Data](https://fsharp.github.io/FSharp.Data/) package to read the CSV file.

First we'll create a type to represent the data from one row of the file (i.e. a unique combination of area and date):

    type CovidData =
        {
            ONSCode: ONSCode
            Date: DateTime
            NewCasesBySpecimenDate: float
        }

Next up is a function to read that data from an actual CSV row.  I found that some rows (including the first one) had blanks which I've replaced with zero values.

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

Reading the population data is very similar so we won't go through it here.

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

        { WeeklyCasesPer100k = Map.ofList weeklyRates }

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

#### Implementing and testing the API

Bringing this all together in our server implementation using some example dates, we can implement the API by reading the relevant data for Covid rates (just keeping relevant dates), populations and geographical boundaries and calling the join function above:

    let dates = [ DateTime(2020, 11, 19); DateTime(2020, 11, 20); DateTime(2020, 11, 21) ]

    let getAllData() =

        let firstDate = (List.min dates).AddDays(-8.0)
        let lastDate = List.max dates
        let covidData = CovidData.read "./data/ltla_2020-11-22.csv" firstDate lastDate

        let populations = Populations.read "./data/population_estimates.csv"
        let boundaries = Geography.readBoundaries "./data/Local_Authority_Districts__December_2019__Boundaries_UK_BUC.kml"

        JoinData.join dates covidData populations boundaries

    let covidMapApi =
        { getDates = fun () -> async { return List.toArray dates }
          getData = fun () -> async { return getAllData() }
        }

Our API should now be working.  Assuming we've run our project (e.g. with `dotnet fake build -t run`) we can test it using something like [Postman](https://www.postman.com/) or even just by typing the automatically-generated URL into a browser.

Calling `http://localhost:8085/api/ICovidMapApi/getDates` returns (in prettified form):

    [
        "2020-11-19T00:00:00.0000000",
        "2020-11-20T00:00:00.0000000",
        "2020-11-21T00:00:00.0000000"
    ]

And calling `http://localhost:8085/api/ICovidMapApi/getData` returns 1MB of data which looks like:

    [
        {
            "ONSCode": {
                "ONSCode": "E06000001"
            },
            "Name": "Hartlepool",
            "Boundary": {
                "Shapes": [
                    {
                        "OuterBoundary": {
                            "LatLongs": [
                                [
                                    54.723193897637,
                                    -1.24099446513821
                                ],
                                ...
                            ]
                        },
                        "Holes": []
                    }
                ]
            },
            "Data": {
                "WeeklyCasesPer100k": {
                    "\"2020-11-19T00:00:00.0000000\"": 458.02504724384227,
                    "\"2020-11-20T00:00:00.0000000\"": 404.64217460469985,
                    "\"2020-11-21T00:00:00.0000000\"": 348.0563296072088
                }
            }
        },
        ...
Looks like our API is working!
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

Here each `float * float` pair holds the latitude and longitude of a point, with the two points holding the minimum and maximum values we'd like to see when first viewing the map.  We don't bother updating these as the map is scrolled, which means the map will revert to the default view if it's redrawn.

These values will show the southern part of the UK:

    let defaultBounds = (51.0, -5.0), (55.0, 1.5)

#### Drawing the map

The only helper function we need is something to turn our pair of bounds into a `LatLngBoundsExpression` which Fable.Leaflet can use.  This took a bit of experimentation to get right:

    let toBounds (point1, point2) : Leaflet.LatLngBoundsExpression = [ point1; point2 ] |> ResizeArray<Leaflet.LatLngTuple> |> Fable.Core.U2.Case2

At last we're ready to draw the map!  The Fable/React bindings make this clean and declarative.

Following the [example](https://react-leaflet.js.org/docs/start-setup) we can add a tile layer from OpenStreetMap (with attribution, of course) and immediately having a working interactive map on our page.

    let view model =
        ReactLeaflet.map
          [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
            ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
          [ yield ReactLeaflet.tileLayer
              [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
                ReactLeaflet.TileLayerProps.Attribution attribution ] [] ]

Here's our map - not bad for under 10 lines of code!
"""


let clientData = """
## Client: Data and state

As well as showing colour-coded covid case rates for geographical areas, our interactive map will have a range of dates from which we can select, and will show extra information when we hover over an area.

#### State Types

This functionality is reflected in our state type:

    type Model =
        { PossibleDates: DateTime[] option
          SelectedDate: DateTime option
          Areas: AreaView[] option
          HoveredArea: AreaView option
          MapBounds: (float * float) * (float * float) }

`PossibleDates` will list the available dates which a user can select and `Areas` will hold all the geographical data in a new `AreaView` type.  Both of these will be `option` types, because when our page is first loaded we won't have retrieved this information from the server yet.  Once retrieved, these values won't change.

`SelectedDate` will be the currently chosen date.  Again, this is optional as there will be no date selected initially.  Finally, `HoveredArea` will reference the area currently being hovered over, if any.

The `AreaView` type is a pretty close match to the `Area` type in our shared domain model, but with the boundary data pre-transformed into Leaflet types - more on this below.

#### Message types and updates

Our list of possible messages is pretty straightforward:

    type Msg =
        | GotDates of DateTime[]
        | GotData of Area[]
        | SelectDate of DateTime
        | Hover of AreaView

So is the `update` function:

    let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
        match msg with
        | GotDates dates -> { model with PossibleDates = Some dates; SelectedDate = Some (dates.[0]) }, Cmd.none
        | GotData areas -> { model with Areas = Some (Array.map LeafletHelpers.processArea areas) }, Cmd.none
        | SelectDate date -> { model with SelectedDate = Some date; HoveredArea = None }, Cmd.none
        | Hover area -> { model with HoveredArea = Some area }, Cmd.none

The first two messages/updates handle receiving data from the server, and simply update the model with the relevant data.  See below for how the areas are processed.

When the dates are received, we arbitrarily choose the first one to be initially selected.  Note that this would crash if an empty list were returned - a more robust implementaion would handle this case!

The `SelectDate` and `Hover` messages just updates the chosen date and highlighted area.  Note that `SelectDate` also clears the `HoveredArea` field - I found this to be the easiest way to get back to a "clean" map with no area highlighted.

#### Initial state and calling the API for data

Whilst the initial state is not very exciting, I love how expressive the `init()` function becomes by combining this with the API calls to make immediately on page load.

    let covidMapApi =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.buildProxy<ICovidMapApi>

    let loadDates = Cmd.OfAsync.perform covidMapApi.getDates () GotDates
    let loadData = Cmd.OfAsync.perform covidMapApi.getData () GotData

    let init(): Model * Cmd<Msg> =
        let model =
            { PossibleDates = None
              SelectedDate = None
              Areas = None
              HoveredArea = None
              MapBounds = defaultBounds }
        
        model, Cmd.batch [ loadDates; loadData ]

When the data is received, each `Area` is transformed into an `AreaView` type.  Doing this once when the data arrives is more efficient than every time an area is drawn.  We can also use an empty `Map` in the case when no data is available.

    let processArea (area: Area) : AreaView =
        { ONSCode = area.ONSCode
          Name = area.Name
          Data =
            match area.Data with
            | None -> { WeeklyCasesPer100k = Map.empty }
            | Some data -> data
          LeafletBoundary = processBoundary area.Boundary
        }

The `processBoundary` function just converts our geographic domain types into the corresponding Leaflet types.  This is in the `LeafletHelpers.fs` file if you want to see it.
"""


let clientRender = """
## Client: Render the data

Our main `view` method will call the following:

    Map.view model dispatch

So all this function has to do is to draw the map as before and render the areas in appropriate colours.  Oh, and the buttons, map legend and an information box when we hover.  Plus wiring up the events and messages and making sure it's not too slow.  Should be simple, right?

We'll start at the top with the `Map.view` method itself:

    let view model dispatch =    
        let infoBox =
            match model.HoveredArea, model.SelectedDate with
            | Some area, Some date -> [ MapLegend.areaInfo area ]
            | _ -> []

        let mapAreas =
            match model.Areas, model.SelectedDate with
            | Some areas, Some date -> createMapAreas areas date model.HoveredArea dispatch
            | _ -> [| |]
        
        let dateButtons = DateButtons.create model dispatch
        
        div []
          [ ReactLeaflet.map
              [ ReactLeaflet.MapProps.Style [ Height 900; Width 1200]
                ReactLeaflet.MapProps.Bounds (toBounds model.MapBounds) ]
              [ yield ReactLeaflet.tileLayer
                  [ ReactLeaflet.TileLayerProps.Url "https://{s}.tile.osm.org/{z}/{x}/{y}.png"
                    ReactLeaflet.TileLayerProps.Attribution attribution ] []
                yield MapLegend.legend
                yield! infoBox
                yield! mapAreas ]
            br []
            dateButtons ]

The declarative nature of the Fable/React ecosystem makes this easy to follow.  The only additions to our view (compared with the plain map) are the extra children of the map element: the legend, info box and map areas.

Note how the legend is always shown (`yield legend`) whereas the info box is shown only when an area has been hovered over (`yield! infoBox` with either an empty or single-element list).  Similarly the `mapAreas` will be empty if the data hasn't yet loaded, or an array otherwise.

The buttons are shown below the map using a separate call.

#### Map areas

We'll start with the most important part and look at the `createMapAreas` function.  In theory all this has to do is to create a coloured `ReactLeaflet.polygon` for each `AreaView`.  However, there are a few extra considerations.

###### Selecting dates

The areas will change colour when we select a different date.  To do this we'll use a new type, `MapAreaProps` which will be represent an area/date combination, i.e. having a specific colour.  We can create one from an `AreaView` once we know the selected date and whether the area is hovered:

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

It also includes the callback function `OnHover` which will send a message when the hover event is triggered.

###### Highlighting hovered areas

We'll be highlighting an area (by drawing a border around it) when it's hovered over.  It turns out it's best to draw the highlighted area last, so the border doesn't get partially overdrawn by neighbouring areas.  We achieve this by putting the hovered area (if any) last in the list:

    let createMapAreas areas date hoveredArea dispatch =
        let hovered, unhovered = areas |> Array.partition (fun a -> hoveredArea = Some a)

        Array.append
            (unhovered |> Array.map (toProps date dispatch hoveredArea) |> Array.map createMemoizedReactMapArea)
            (hovered |> Array.map (toProps date dispatch hoveredArea) |> Array.map createMemoizedReactMapArea)

###### Memoizing

To keep the map snappy, we don't want to redraw all the areas when anything changes (e.g. a new area is hovered over).

To achieve this we'll create a `FunctionComponent` for each area.  A [function component](https://fable.io/blog/Announcing-Fable-React-5.html) lets us avoid re-rendering something if its properties haven't changed.

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

The `createReactMapArea` function is a React component because it takes a `props` object and returns a React element.  This is quite simple - the border will only be shown if the area is hovered and we use some helper functions from the `Colours` module to provide colours based on the Covid rate.

In the `createMemoizedReactMapArea` function, the `memoizeWith = equalsButFunctions` parameter tells React to check whether anything in the `props` object (other than members which are themselves functions) has changed when determining whether to re-render the element.

The `withKey` parameter tells React which elements correspond to each other when the [list](https://reactjs.org/docs/lists-and-keys.html) of elements changes.  This allows it to avoid re-rendering the entire list of areas when we change its order (e.g. as a resulting of hovering).

We now have all we need to draw our areas efficiently and update them when a new date is selected or a hover event occurs!

#### Map legend and info box

The map legend uses the `react-leaflet-control` package we installed earlier to show React elements on top of the map.

This lets us create the legend as a React element and include it as a child of the map object.

Firstly a boilerplate helper function which wraps that package.

    let inline customControl (props: ReactLeaflet.MapControlProps list) (children: ReactElement list) : ReactElement =
        ofImport
            "default"
            "react-leaflet-control"
            (Fable.Core.JsInterop.keyValueList Fable.Core.CaseRules.LowerFirst props)
            children

Now a rather tedious function to draw a little square box alongside some text:

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

Finally we can create the legend itself: <img style="float: right;" src="legend.png">

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

The info box is created similarly and is shown in the top-right corner when an area has been hovered.

#### Date buttons

Standard Fulma buttons are used below the map for selecting the date to show.

    let button txt onClick isSelected =
        Control.div []
          [ Button.button
              [ if isSelected then yield Button.Color IsPrimary
                yield Button.OnClick onClick ]
              [ str txt ] ]

    let create model dispatch =
        match model.PossibleDates, model.SelectedDate with
        | Some dates, Some selectedDate ->
            Field.div [ Field.IsGroupedMultiline ]
              (dates |> List.ofArray |> List.map (fun d -> button (d.ToShortDateString()) (fun _ -> dispatch (SelectDate d)) (selectedDate = d)))
        | _ -> Field.div [] [ str "Loading data..." ]

We can see that a button with a date `d` is highlighted only if `selectedDate = d`, and the `onClick` event is set to dispatch a message `SelectDate d`.
"""


let results = """
## The resulting map

Select different dates or hover over an area to see its history of covid rates.
"""