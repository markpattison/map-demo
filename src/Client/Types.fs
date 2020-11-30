module Types

open Shared

type Page =
    | Introduction
    | Shared
    | Server
    | ClientPlainMap
    | ClientShowData
    | Results

type Model =
    { CurrentPage: Page 
      Data: int option
      MapBounds: (float * float) * (float * float) }

type Msg =
    | ShowPage of Page
    | GotData of int
