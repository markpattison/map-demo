module DateButtons

open Fable.React
open Fulma

open Types

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
