module Introduction

let markdown = """
## Introduction

This site is part of the [F# Advent Calendar in English 2020](https://sergeytihon.com/2020/10/22/f-advent-calendar-in-english-2020/).

In this walkthrough I'll show how to quickly and easily visualise data on an interactive map using [F#](https://fsharp.org/), [Leaflet](https://leafletjs.com/) and the [SAFE Stack](https://safe-stack.github.io/).

All source code can be found on [Github](https://github.com/markpattison/map-demo).

### Sample data - COVID-19 case rates in the United Kingdom

Although we've probably all seen enough maps and charts of the pandemic, it's at least an up-to-date (and possibly even useful) example of geographically-based data.

COVID-19 data for the UK can be explored and downloaded from [this page](https://coronavirus.data.gov.uk/details/download) - I've used a CSV file containing daily data for new cases split by local authority areas, of which there are around 380 in the UK.  [This link](https://api.coronavirus.data.gov.uk/v2/data?areaType=ltla&metric=newCasesByPublishDate&metric=newCasesBySpecimenDate&format=csv) will always get the latest version of the file.

The data for the local authority boundaries was downloaded from [here](https://geoportal.statistics.gov.uk/datasets/local-authority-districts-december-2019-boundaries-uk-buc) in [KML](https://developers.google.com/kml/documentation/kml_tut) format.  I've used the ultra-generalised version as this is a much smaller file, but is easily good enough for visualising at a national level.
"""