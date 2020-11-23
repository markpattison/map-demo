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

Here we'll look at the shape of the data that the server will be providing to the client.

Fundamentally our data will be a list of local authority areas, with an attaching ONS code (a standard way of referencing administrative areas in the UK), local authority name, boundary data (to draw the area on a map) and covid data.

This code lives in the `Shared.fs` file which is referenced by the both the server and client projects.  This guarantees that our data types match.

#### Boundary data

In general, a geographic region can be made up of several disconnected areas.  Each of these could in turn have holes within them.  This can be mapped nicely to F# as follows:

A `Loop` is a simple boundary made up of an array of latitude/longitude pairs.

    type Loop =
        {
            LatLongs: (float * float) []
        }

A `Shape` has one outer `Loop` and zero or more holes, which are themselves represented as `Loop` elemenets.

    type Shape =
        {
            OuterBoundary: Loop
            Holes: Loop []
        }

A `Boundary` is made up of at least one `Shape`.

    type Boundary =
        {
            Shapes: Shape []
        }

This model is rich enough to represent any geographic area.

#### Covid data

We'll use a simple data structure to hold a few weeks' data:

    type CovidRates =
        {
            WeekToOct30: float option
            WeekToNov06: float option
            WeekToNov13: float option
        }

#### API

The following interface specifies our API.  In our case it's extremely simple, with only one method - to retrieve all the data.

    type ITodosApi =
        { getData : unit -> Async<Area []> }

Again, this is shared between the client and server implementations - [Fable Remoting](https://zaid-ajaj.github.io/Fable.Remoting/) will magically take care of the rest!

"""


let server = """
## Server implementation
"""


let clientPlainMap = """
## Client: Drawing a map
"""


let clientShowData = """
## Client: Showing our data
"""