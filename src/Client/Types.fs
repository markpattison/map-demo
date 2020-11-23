module Types

open Shared

type Page =
    | Introduction
    | Shared
    | Server
    | ClientPlainMap
    | ClientShowData

type Model =
    { CurrentPage: Page 
      Data: int option }

type Msg =
    | ShowPage of Page
    | GotData of int
