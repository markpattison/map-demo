module Colours

let red = "#ff0000"
let yellow = "#ffff00"
let green = "#00ff00"
let grey = "#bbbbbb"

let areaOpacity = 0.7

let rateMin = 0.0
let rateMid = 200.0
let rateMax = 600.0

let toHex (x: float) =
    let clamped = max 0.0 (min 1.0 x)
    let scaled = int (clamped * 255.0)
    scaled.ToString("X2")

let interpGreenYellowRed rateOpt =
    match rateOpt with
    | None -> grey
    | Some r when r <= rateMin -> green
    | Some r when r >= rateMax -> red
    | Some r when r < rateMid ->
        let greenRatio = (rateMid - r) / (rateMid - rateMin)
        sprintf "#%sff00" (toHex (1.0 - greenRatio))
    | Some r ->
        let redRatio = (r - rateMid) / (rateMax - rateMid)
        sprintf "#ff%s00" (toHex (1.0 - redRatio))
