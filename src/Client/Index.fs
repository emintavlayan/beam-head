module Index

open BeamHead.Domain
open Feliz

/// Represents the static TrueBeam workbench state.
type Model = {
    ViewerMode: GeometryViewer.ViewerMode
    ShowDebugGeometry: bool
}

/// Represents messages handled by the static TrueBeam workbench.
type Msg =
    | SetViewerMode of GeometryViewer.ViewerMode
    | SetDebugGeometry of bool

/// Creates the initial BeamHead client model.
let init () = {
    ViewerMode = GeometryViewer.ThreeD
    ShowDebugGeometry = false
}

/// Applies a client message to the current model.
let update msg model =
    match msg with
    | SetViewerMode viewerMode -> { model with ViewerMode = viewerMode }
    | SetDebugGeometry showDebugGeometry -> {
        model with
            ShowDebugGeometry = showDebugGeometry
      }

let private trueBeamJawsForExport =
    TrueBeam.jaws
    |> List.map IsocentreFrame.fromSourceJawPlacement
    |> JscadGeometry.createJaws

let private trueBeamMlcBanksForExport =
    TrueBeam.mlcBanks
    |> List.map IsocentreFrame.fromSourceMlcBankPlacement
    |> JscadGeometry.createMlcBanks

let private trueBeamGeometryForExport =
    Array.append trueBeamJawsForExport trueBeamMlcBanksForExport

let private trueBeamJawsForViewer =
    JscadGeometry.createViewerDisplay trueBeamJawsForExport

let private trueBeamMlcBanksForViewer =
    JscadGeometry.createViewerDisplay trueBeamMlcBanksForExport

let private viewerDisplaySource =
    IsocentreFrame.source |> ViewerDisplay.fromIsocentrePoint

let private viewerDisplayJawReferences axis =
    TrueBeam.jaws
    |> List.filter (fun jaw -> jaw.Axis = axis)
    |> List.map (
        IsocentreFrame.fromSourceJawPlacement
        >> _.ApertureFaceMidpoint
        >> ViewerDisplay.fromIsocentrePoint
    )
    |> List.toArray

let private viewerDisplayMlcReferences =
    TrueBeam.mlcBanks
    |> List.map (
        IsocentreFrame.fromSourceMlcBankPlacement
        >> _.TipReference
        >> ViewerDisplay.fromIsocentrePoint
    )
    |> List.toArray

let private nominalFieldGuideHalfWidth = TrueBeamJaws.fieldSizeAtIsocentre / 2.0

let private nominalFieldCorners =
    let corner x y : IsocentreFramePoint = {
        X = x
        Y = y
        Z = IsocentreFrame.isocentre.Z
    }

    [|
        corner -nominalFieldGuideHalfWidth -nominalFieldGuideHalfWidth
        corner nominalFieldGuideHalfWidth -nominalFieldGuideHalfWidth
        corner nominalFieldGuideHalfWidth nominalFieldGuideHalfWidth
        corner -nominalFieldGuideHalfWidth nominalFieldGuideHalfWidth
    |]
    |> Array.map ViewerDisplay.fromIsocentrePoint

let private viewerDebugGeometry: GeometryViewer.DebugGeometry = {
    Isocentre = IsocentreFrame.isocentre |> ViewerDisplay.fromIsocentrePoint
    XJawReferences = viewerDisplayJawReferences X
    YJawReferences = viewerDisplayJawReferences Y
    MlcReferences = viewerDisplayMlcReferences
    NominalFieldCorners = nominalFieldCorners
}

let private staticFieldSize (label: string) =
    Html.label [
        prop.className "form-control w-full"
        prop.children [
            Html.div [
                prop.className "label"
                prop.children [ Html.span [ prop.className "label-text font-medium"; prop.text label ] ]
            ]
            Html.div [
                prop.className "input input-bordered flex items-center gap-2 bg-base-200"
                prop.children [
                    Html.span [ prop.className "grow"; prop.text "400" ]
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "mm" ]
                ]
            ]
        ]
    ]

let private controlsCard =
    Html.aside [
        prop.className "card border border-base-300 bg-base-100 shadow-md"
        prop.children [
            Html.div [
                prop.className "card-body gap-5 p-5"
                prop.children [
                    Html.h2 [ prop.className "card-title text-lg"; prop.text "Geometry" ]

                    Html.label [
                        prop.className "form-control w-full"
                        prop.children [
                            Html.div [
                                prop.className "label"
                                prop.children [
                                    Html.span [ prop.className "label-text font-medium"; prop.text "Machine model" ]
                                ]
                            ]
                            Html.div [
                                prop.className "select select-bordered flex w-full items-center bg-base-200"
                                prop.text "TrueBeam"
                            ]
                        ]
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Static jaws" ]

                    Html.div [
                        prop.className "space-y-2"
                        prop.children [ staticFieldSize "X field size"; staticFieldSize "Y field size" ]
                    ]

                    Html.p [
                        prop.className "text-sm text-base-content/70"
                        prop.text "Fixed 400 x 400 mm field at isocentre"
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Static MLC" ]

                    Html.div [
                        prop.className "rounded-box border border-base-300 bg-base-200 p-3"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center justify-between gap-3"
                                prop.children [
                                    Html.span [ prop.className "font-medium"; prop.text "Millennium 120" ]
                                    Html.span [ prop.className "badge badge-neutral"; prop.text "Retracted" ]
                                ]
                            ]
                            Html.p [
                                prop.className "mt-1 text-sm text-base-content/60"
                                prop.text "Simplified two-bank geometry"
                            ]
                        ]
                    ]

                    Html.button [
                        prop.className "btn btn-primary mt-1 w-full"
                        prop.onClick (fun _ ->
                            JscadGeometry.downloadStl "truebeam-400x400mm-retracted-mlc.stl" trueBeamGeometryForExport)
                        prop.text "Export beam head STL"
                    ]
                ]
            ]
        ]
    ]

let private viewerModeButton
    (selectedMode: GeometryViewer.ViewerMode)
    (mode: GeometryViewer.ViewerMode)
    (label: string)
    (dispatch: Msg -> unit)
    =
    let stateClass =
        if selectedMode = mode then
            "btn-active btn-primary"
        else
            "btn-ghost"

    Html.button [
        prop.className $"btn btn-sm join-item {stateClass}"
        prop.onClick (fun _ -> dispatch (SetViewerMode mode))
        prop.text label
    ]

let private debugGeometryToggle showDebugGeometry dispatch =
    Html.label [
        prop.className "flex cursor-pointer items-center gap-2 whitespace-nowrap"
        prop.children [
            Html.span [ prop.className "text-sm font-medium"; prop.text "Debug geometry" ]
            Html.input [
                prop.type' "checkbox"
                prop.className "toggle toggle-sm toggle-primary"
                prop.isChecked showDebugGeometry
                prop.onChange (fun enabled -> dispatch (SetDebugGeometry enabled))
            ]
        ]
    ]

let private viewerCard selectedMode showDebugGeometry dispatch =
    Html.section [
        prop.className "card overflow-hidden border border-base-300 bg-base-100 shadow-md"
        prop.children [
            Html.div [
                prop.className "border-b border-base-300 px-5 py-4"
                prop.children [
                    Html.div [
                        prop.className "flex items-start justify-between gap-4"
                        prop.children [
                            Html.div [
                                Html.h2 [ prop.className "text-lg font-semibold"; prop.text "3D preview" ]
                                Html.p [
                                    prop.className "text-sm text-base-content/60"
                                    prop.text "TrueBeam jaws and retracted Millennium 120 MLC"
                                ]
                            ]
                            Html.div [
                                prop.className "flex shrink-0 items-center gap-4"
                                prop.children [
                                    debugGeometryToggle showDebugGeometry dispatch
                                    Html.div [
                                        prop.className "join"
                                        prop.children [
                                            viewerModeButton selectedMode GeometryViewer.ThreeD "3D" dispatch
                                            viewerModeButton selectedMode GeometryViewer.BeamEyeView "BEV" dispatch
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "p-3"
                prop.children [
                    GeometryViewer.View(
                        selectedMode,
                        showDebugGeometry,
                        viewerDisplaySource,
                        viewerDebugGeometry,
                        trueBeamJawsForViewer,
                        trueBeamMlcBanksForViewer
                    )
                ]
            ]
        ]
    ]

/// Renders the current BeamHead client.
let view model dispatch =
    Html.main [
        prop.className "min-h-screen w-full bg-base-200 text-base-content"
        prop.children [
            Html.section [
                prop.className "mx-auto w-[96vw] max-w-[1600px] p-4"
                prop.children [
                    Html.div [
                        prop.className "navbar rounded-box border border-base-300 bg-base-100 px-4 shadow-md"
                        prop.children [
                            Html.div [
                                prop.className "navbar-start"
                                prop.children [
                                    Html.h1 [ prop.className "text-xl font-bold tracking-tight"; prop.text "BeamHead" ]
                                ]
                            ]
                        ]
                    ]

                    Html.div [
                        prop.className "mt-4 grid items-start gap-4 lg:grid-cols-[20rem_minmax(0,1fr)]"
                        prop.children [ controlsCard; viewerCard model.ViewerMode model.ShowDebugGeometry dispatch ]
                    ]
                ]
            ]
        ]
    ]