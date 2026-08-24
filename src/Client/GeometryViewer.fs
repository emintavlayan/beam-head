module GeometryViewer

open BeamHead.Domain
open Browser.Types
open Fable.Core
open Feliz

[<Import("startViewer", "./BeamHeadJscad.js")>]
let private startJscadViewer
    (container: HTMLElement)
    (geometry: obj array)
    (viewerMode: string)
    (sourceX: float)
    (sourceY: float)
    (sourceZ: float)
    : unit -> unit =
    jsNative

/// Identifies the active BeamHead presentation camera.
type ViewerMode =
    | ThreeD
    | BeamEyeView

let private viewerModeName mode =
    match mode with
    | ThreeD -> "threeD"
    | BeamEyeView -> "beamEyeView"

/// Renders JSCAD geometry using either the interactive 3D camera or fixed Beam's Eye View.
[<ReactComponent>]
let View (mode: ViewerMode, displaySource: ViewerDisplayPoint, geometry: obj array) =
    let container = React.useElementRef ()

    React.useEffect (
        (fun () ->
            match container.current with
            | Some element ->
                React.createDisposable (
                    startJscadViewer
                        element
                        geometry
                        (viewerModeName mode)
                        (float displaySource.X)
                        (float displaySource.Y)
                        (float displaySource.Z)
                )
            | None -> React.createDisposable ignore),
        [| box mode |]
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