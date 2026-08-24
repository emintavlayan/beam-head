module JscadGeometry

open BeamHead.Domain
open Fable.Core

[<Import("createJaw", "./BeamHeadJscad.js")>]
let private createJscadJaw
    (axis: string)
    (side: string)
    (closingAxisExtent: float)
    (crossAxisExtent: float)
    (thickness: float)
    (referenceX: float)
    (referenceY: float)
    (referenceZ: float)
    (apertureFaceAngleRadians: float)
    : obj =
    jsNative

[<Import("createMlcBank", "./BeamHeadJscad.js")>]
let private createJscadMlcBank
    (side: string)
    (bankSpan: float)
    (referenceX: float)
    (referenceY: float)
    (referenceZ: float)
    (profilePoints: obj array array)
    : obj =
    jsNative

[<Import("downloadStl", "./BeamHeadJscad.js")>]
let private downloadJscadStl (fileName: string) (geometry: obj array) : unit = jsNative

[<Import("createViewerDisplay", "./BeamHeadJscad.js")>]
let private createJscadViewerDisplay (geometry: obj array) : obj array = jsNative

/// Translates a calculated domain jaw placement into a JSCAD solid.
let createJaw (placement: IsocentreFrameJawPlacement) =
    let axis =
        match placement.Axis with
        | X -> "x"
        | Y -> "y"

    let side =
        match placement.Side with
        | Negative -> "negative"
        | Positive -> "positive"

    createJscadJaw
        axis
        side
        (float placement.BodyDimensions.ClosingAxisExtent)
        (float placement.BodyDimensions.CrossAxisExtent)
        (float placement.BodyDimensions.Thickness)
        (float placement.ApertureFaceMidpoint.X)
        (float placement.ApertureFaceMidpoint.Y)
        (float placement.ApertureFaceMidpoint.Z)
        placement.ApertureFaceAngleRadians

/// Translates calculated isocentre-frame jaw placements into JSCAD solids.
let createJaws (placements: IsocentreFrameJawPlacement list) =
    placements |> List.map createJaw |> List.toArray

/// Translates a calculated domain MLC bank placement into a JSCAD solid.
let createMlcBank (placement: IsocentreFrameMlcBankPlacement) =
    let side =
        match placement.Side with
        | NegativeBank -> "negative"
        | PositiveBank -> "positive"

    let profilePoints: obj array array =
        placement.LocalProfile
        |> List.map (fun point -> [| box (float point.X); box (float point.Z) |])
        |> List.toArray

    createJscadMlcBank
        side
        (float placement.BankSpan)
        (float placement.TipReference.X)
        (float placement.TipReference.Y)
        (float placement.TipReference.Z)
        profilePoints

/// Translates calculated isocentre-frame MLC bank placements into JSCAD solids.
let createMlcBanks (placements: IsocentreFrameMlcBankPlacement list) =
    placements |> List.map createMlcBank |> List.toArray

/// Mirrors JSCAD solids in Z for viewer display without changing export geometry.
let createViewerDisplay geometry = createJscadViewerDisplay geometry

/// Downloads all supplied JSCAD solids together as one binary STL file.
let downloadStl fileName geometry = downloadJscadStl fileName geometry