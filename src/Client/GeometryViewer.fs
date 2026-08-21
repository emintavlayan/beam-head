module GeometryViewer

open Browser.Types
open Fable.Core
open Feliz

[<Import("startViewer", "./BeamHeadJscad.js")>]
let private startJscadViewer (container: HTMLElement) (geometry: obj array) : unit -> unit = jsNative

/// Renders JSCAD geometry with mouse and touch rotation and wheel zoom.
[<ReactComponent>]
let View (geometry: obj array) =
    let container = React.useElementRef ()

    React.useEffectOnce (fun () ->
        match container.current with
        | Some element -> React.createDisposable (startJscadViewer element geometry)
        | None -> React.createDisposable ignore)

    Html.div [
        prop.ref container
        prop.className
            "h-[30rem] w-full cursor-grab touch-none overflow-hidden rounded-box bg-slate-50 lg:h-[calc(100vh-13rem)] lg:min-h-[36rem]"
        prop.ariaLabel "Interactive view of four static TrueBeam jaws"
    ]