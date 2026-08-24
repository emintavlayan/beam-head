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

/// Mirrors JSCAD solids in Z for viewer display without changing export geometry.
let createViewerDisplay geometry = createJscadViewerDisplay geometry

/// Downloads all supplied JSCAD solids together as one binary STL file.
let downloadStl fileName geometry = downloadJscadStl fileName geometry