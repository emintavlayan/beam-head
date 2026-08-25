module GeometryViewer

open BeamHead.Domain
open Browser.Types
open Fable.Core
open Feliz

[<Import("startViewer", "./BeamHeadJscad.js")>]
let private startJscadViewer
    (container: HTMLElement)
    (jawGeometry: obj array)
    (mlcGeometry: obj array)
    (viewerMode: string)
    (showDebugGeometry: bool)
    (sourceX: float)
    (sourceY: float)
    (sourceZ: float)
    (isocentre: float array)
    (xJawReferences: float array array)
    (yJawReferences: float array array)
    (mlcReferences: float array array)
    (nominalFieldCorners: float array array)
    : unit -> unit =
    jsNative

/// Identifies the active BeamHead presentation camera.
type ViewerMode =
    | ThreeD
    | BeamEyeView

/// Contains viewer-display coordinates for the optional diagnostic geometry package.
type DebugGeometry = {
    Isocentre: ViewerDisplayPoint
    XJawReferences: ViewerDisplayPoint array
    YJawReferences: ViewerDisplayPoint array
    MlcReferences: ViewerDisplayPoint array
    NominalFieldCorners: ViewerDisplayPoint array
}

let private viewerModeName mode =
    match mode with
    | ThreeD -> "threeD"
    | BeamEyeView -> "beamEyeView"

let private coordinates (point: ViewerDisplayPoint) = [| float point.X; float point.Y; float point.Z |]

/// Renders JSCAD geometry using either the interactive 3D camera or fixed Beam's Eye View.
[<ReactComponent>]
let View
    (
        mode: ViewerMode,
        showDebugGeometry: bool,
        displaySource: ViewerDisplayPoint,
        debugGeometry: DebugGeometry,
        jawGeometry: obj array,
        mlcGeometry: obj array
    ) =
    let container = React.useElementRef ()

    React.useEffect (
        (fun () ->
            match container.current with
            | Some element ->
                React.createDisposable (
                    startJscadViewer
                        element
                        jawGeometry
                        mlcGeometry
                        (viewerModeName mode)
                        showDebugGeometry
                        (float displaySource.X)
                        (float displaySource.Y)
                        (float displaySource.Z)
                        (coordinates debugGeometry.Isocentre)
                        (debugGeometry.XJawReferences |> Array.map coordinates)
                        (debugGeometry.YJawReferences |> Array.map coordinates)
                        (debugGeometry.MlcReferences |> Array.map coordinates)
                        (debugGeometry.NominalFieldCorners |> Array.map coordinates)
                )
            | None -> React.createDisposable ignore),
        [| box mode; box showDebugGeometry |]
    )

    let cursorClass =
        match mode with
        | ThreeD -> "cursor-grab touch-none"
        | BeamEyeView -> "cursor-default"

    Html.div [
        prop.ref container
        prop.className
            $"h-[30rem] w-full {cursorClass} overflow-hidden rounded-box bg-slate-50 lg:h-[calc(100vh-13rem)] lg:min-h-[36rem]"
        prop.ariaLabel "TrueBeam jaw and MLC geometry viewer"
    ]