module Colours

let grey = "#bbbbbb"
let black = "#000000"

let areaOpacity = 0.7
let borderWeight = 3.0

let rateMin = 0.0
let rateMid = 200.0
let rateMax = 600.0

let toHex (x: float) =
    let clamped = max 0.0 (min 1.0 x)
    let scaled = int (clamped * 255.0)
    scaled.ToString("X2")

let interpMinMid minRatio =
    sprintf "#%sdd00" (toHex (1.0 - minRatio))

let interpMaxMid maxRatio =
    sprintf "#ff%s00" (toHex (1.0 - maxRatio))

let interpGreenYellowRed rateOpt =
    match rateOpt with
    | None -> grey
    | Some r when r <= rateMin -> interpMinMid 1.0
    | Some r when r >= rateMax -> interpMaxMid 1.0
    | Some r when r < rateMid -> interpMinMid ((rateMid - r) / (rateMid - rateMin))
    | Some r -> interpMaxMid ((r - rateMid) / (rateMax - rateMid))

let colourMin = interpGreenYellowRed (Some rateMin)
let colourMid = interpGreenYellowRed (Some rateMid)
let colourMax = interpGreenYellowRed (Some rateMax)
