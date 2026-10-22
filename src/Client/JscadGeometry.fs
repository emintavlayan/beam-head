module JscadGeometry

open BeamHead.Domain
open Fable.Core
open FsToolkit.ErrorHandling

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
        placement.EnvelopeProfile
        |> List.map (fun point -> [| box (float point.X); box (float point.Z); box (float point.HalfSpan) |])
        |> List.toArray

    createJscadMlcBank
        side
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

/// Reads the resolved physical polygon coordinates of one JSCAD solid.
[<Import("readSolidPolygons", "./BeamHeadJscad.js")>]
let private readSolidPolygons (solid: obj) : float array array array = jsNative

/// Converts resolved JSCAD polygons to measured physical points before display mirroring.
let readSurface solid : IsocentreFramePoint list list =
    readSolidPolygons solid
    |> Array.map (Array.map (fun point -> ({
        X = point[0] * 1.0<mm>
        Y = point[1] * 1.0<mm>
        Z = point[2] * 1.0<mm>
    } : IsocentreFramePoint)) >> Array.toList)
    |> Array.toList

/// Creates the same physical pyramid used by debug rendering before its display-only mirror.
[<Import("createFieldLight", "./BeamHeadJscad.js")>]
let private createFieldLight (source: float array) (corners: float array array) : obj = jsNative

/// Creates a physical field-light pyramid from explicit isocentre-plane dimensions.
let fieldLightSurfaceForDimensions (xSize: float<mm>) (ySize: float<mm>) =
    let halfX = xSize / 2.0
    let halfY = ySize / 2.0
    let corners =
        [| -halfX, -halfY; halfX, -halfY; halfX, halfY; -halfX, halfY |]
        |> Array.map (fun (x, y) -> [|float x; float y; 0.0|])
    let source = IsocentreFrame.source
    createFieldLight [|float source.X; float source.Y; float source.Z|] corners |> readSurface

/// Reads the actual field-light solid for geometric tests and live validation.
let fieldLightSurface field =
    fieldLightSurfaceForDimensions (JawField.xSize field) (JawField.ySize field)

/// Inspects the actual generated solids against the selected field and geometric specification.
let inspectBeamHead field (jaws: obj array) (banks: obj array) =
    let jawLabels = [ X, Negative; X, Positive; Y, Negative; Y, Positive ]
    let bankLabels = [ NegativeBank; PositiveBank ]
    if jaws.Length <> 4 || banks.Length <> 2 then
        Error "Geometry QA requires four jaw solids and two MLC bank solids."
    else
        let jawSurfaces = List.zip jawLabels (Array.toList jaws) |> List.map (fun ((axis, side), solid) -> axis, side, readSurface solid)
        let bankSurfaces = List.zip bankLabels (Array.toList banks) |> List.map (fun (side, solid) -> side, readSurface solid)
        result {
            let! report = GeometryQa.inspect field jawSurfaces bankSurfaces
            let! fieldLight = fieldLightSurface field |> GeometryQa.inspectFieldLight field
            return { report with Measurements = fieldLight @ report.Measurements }
        }
