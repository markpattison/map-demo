module Types

open System

open Shared

type Page =
    | Introduction
    | Shared
    | Server
    | ClientPlainMap
    | ClientShowData
    | Results

type LeafletBoundary = Fable.Core.U3<Leaflet.LatLngExpression [], Leaflet.LatLngExpression [] [], Leaflet.LatLngExpression [] [] []>

type AreaView =
    { ONSCode: ONSCode
      Name: string
      LeafletBoundary: LeafletBoundary
      Data: CovidRates }

type Model =
    { CurrentPage: Page 
      PossibleDates: DateTime[] option
      SelectedDate: DateTime option
      Areas: AreaView[] option
      HoveredArea: AreaView option
      MapBounds: (float * float) * (float * float) }

type Msg =
    | ShowPage of Page
    | GotDates of DateTime[]
    | GotData of Area[]
    | SelectDate of DateTime
    | Hover of AreaView
