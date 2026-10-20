module Index

open BeamHead.Domain
open Feliz

/// Represents the TrueBeam field workbench state.
type Model = {
    ViewerMode: GeometryViewer.ViewerMode
    ShowDebugGeometry: bool
    FieldControls: FieldControls.Model
}

/// Represents changes to the field controls and geometry viewer.
type Msg =
    | SetViewerMode of GeometryViewer.ViewerMode
    | SetDebugGeometry of bool
    | EditFieldSize of JawAxis * string
    | CommitFieldSize of JawAxis
    | SetSliderSize of JawAxis * string
    | SelectPreset of JawAxis * float<mm>
    | SetSquareField of bool

/// Creates the initial BeamHead client model.
let init () = {
    ViewerMode = GeometryViewer.ThreeD
    ShowDebugGeometry = false
    FieldControls = FieldControls.init ()
}

/// Applies a client message to the current model.
let update msg model =
    match msg with
    | SetViewerMode viewerMode -> { model with ViewerMode = viewerMode }
    | SetDebugGeometry showDebugGeometry -> {
        model with
            ShowDebugGeometry = showDebugGeometry
      }
    | EditFieldSize (axis, text) ->
        { model with FieldControls = FieldControls.editSize axis text model.FieldControls }
    | CommitFieldSize axis ->
        { model with FieldControls = FieldControls.commitSize axis model.FieldControls }
    | SetSliderSize (axis, text) ->
        { model with FieldControls = FieldControls.setSliderSize axis text model.FieldControls }
    | SelectPreset (axis, size) ->
        { model with FieldControls = FieldControls.selectPreset axis size model.FieldControls }
    | SetSquareField enabled ->
        { model with FieldControls = FieldControls.setSquareField enabled model.FieldControls }

/// Creates export solids from the selected jaw field.
let private trueBeamJawsForExport field =
    TrueBeam.jaws field
    |> List.map IsocentreFrame.fromSourceJawPlacement
    |> JscadGeometry.createJaws

let private trueBeamMlcBanksForExport =
    TrueBeam.mlcBanks
    |> List.map IsocentreFrame.fromSourceMlcBankPlacement
    |> JscadGeometry.createMlcBanks

let private trueBeamMlcBanksForViewer =
    JscadGeometry.createViewerDisplay trueBeamMlcBanksForExport

let private viewerDisplaySource =
    IsocentreFrame.source |> ViewerDisplay.fromIsocentrePoint

/// Reads jaw reference points from the selected field.
let private viewerDisplayJawReferences field axis =
    TrueBeam.jaws field
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

/// Creates the rectangular field guide from the selected isocentre dimensions.
let private nominalFieldCorners field =
    let halfX = JawField.xSize field / 2.0
    let halfY = JawField.ySize field / 2.0

    /// Creates one field-guide corner in the isocentre plane.
    let corner x y : IsocentreFramePoint = {
        X = x
        Y = y
        Z = IsocentreFrame.isocentre.Z
    }

    [|
        corner -halfX -halfY
        corner halfX -halfY
        corner halfX halfY
        corner -halfX halfY
    |]
    |> Array.map ViewerDisplay.fromIsocentrePoint

/// Creates the field-dependent diagnostic geometry for the viewer.
let private viewerDebugGeometry field : GeometryViewer.DebugGeometry = {
    Isocentre = IsocentreFrame.isocentre |> ViewerDisplay.fromIsocentrePoint
    XJawReferences = viewerDisplayJawReferences field X
    YJawReferences = viewerDisplayJawReferences field Y
    MlcReferences = viewerDisplayMlcReferences
    NominalFieldCorners = nominalFieldCorners field
}

/// Names a jaw axis for accessible field controls.
let private axisName axis =
    match axis with
    | X -> "X"
    | Y -> "Y"

/// Renders synchronized slider, numeric input and presets for one jaw axis.
let private fieldSizeControl axis (model: FieldControls.Model) dispatch =
    let name = axisName axis
    let disabled = axis = Y && model.SquareField
    let size = JawField.size axis model.Field
    let text, error =
        match axis with
        | X -> model.XInput, model.XError
        | Y -> model.YInput, model.YError
    let inputId = $"field-{name.ToLowerInvariant()}-size"

    Html.fieldSet [
        prop.disabled disabled
        prop.className (if disabled then "space-y-2 opacity-50" else "space-y-2")
        prop.children [
            Html.div [
                prop.className "flex items-center justify-between gap-3"
                prop.children [
                    Html.label [ prop.htmlFor inputId; prop.className "font-medium"; prop.text $"{name} field size" ]
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.input [
                                prop.id inputId
                                prop.type' "number"
                                prop.className "input input-bordered input-sm w-24 text-right"
                                prop.min 0.5
                                prop.max 40.0
                                prop.step 0.1
                                prop.value text
                                prop.ariaLabel $"{name} field size in centimetres"
                                prop.onChange (fun (value: string) -> dispatch (EditFieldSize (axis, value)))
                                prop.onBlur (fun _ -> dispatch (CommitFieldSize axis))
                            ]
                            Html.span [ prop.className "text-sm"; prop.text "cm" ]
                        ]
                    ]
                ]
            ]
            Html.input [
                prop.type' "range"
                prop.className "range range-primary range-sm w-full"
                prop.min 0.5
                prop.max 40.0
                prop.step 0.1
                prop.value (float size / 10.0)
                prop.ariaLabel $"{name} field size slider"
                prop.onChange (fun (value: string) -> dispatch (SetSliderSize (axis, value)))
            ]
            Html.div [
                prop.className "flex flex-wrap gap-1"
                prop.children [
                    for preset in FieldControls.presets do
                        Html.button [
                            prop.type' "button"
                            prop.className (if size = preset then "btn btn-xs btn-primary" else "btn btn-xs btn-outline")
                            prop.ariaLabel $"{name} preset {int (float preset / 10.0)} cm"
                            prop.onClick (fun _ -> dispatch (SelectPreset (axis, preset)))
                            prop.text $"{int (float preset / 10.0)}"
                        ]
                    Html.span [ prop.className "self-center px-1 text-xs text-base-content/60"; prop.text "cm" ]
                ]
            ]
            match error with
            | Some message -> Html.p [ prop.role "alert"; prop.className "text-sm text-error"; prop.text message ]
            | None -> Html.none
        ]
    ]

/// Renders the checkbox that links the Y dimension to X.
let private squareFieldToggle enabled dispatch =
    Html.label [
        prop.className "flex cursor-pointer items-center gap-2"
        prop.children [
            Html.input [
                prop.type' "checkbox"
                prop.className "checkbox checkbox-primary checkbox-sm"
                prop.isChecked enabled
                prop.onChange (fun value -> dispatch (SetSquareField value))
            ]
            Html.span [ prop.className "font-medium"; prop.text "Square field" ]
        ]
    ]

/// Renders field editing controls and exports the selected physical geometry.
let private controlsCard (model: FieldControls.Model) jawGeometry dispatch =
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

                    Html.div [ prop.className "divider my-0"; prop.text "Jaws" ]

                    Html.div [
                        prop.className "space-y-5"
                        prop.children [
                            squareFieldToggle model.SquareField dispatch
                            fieldSizeControl X model dispatch
                            fieldSizeControl Y model dispatch
                        ]
                    ]

                    Html.p [
                        prop.className "text-sm text-base-content/70"
                        prop.text $"{FieldControls.formatCentimetres (JawField.xSize model.Field)} x {FieldControls.formatCentimetres (JawField.ySize model.Field)} cm at isocentre"
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Static MLC" ]

                    Html.div [
                        prop.className "rounded-box border border-base-300 bg-base-200 p-3"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center justify-between gap-3"
                                prop.children [
                                    Html.span [ prop.className "font-medium whitespace-nowrap"; prop.text "Millennium 120" ]
                                    Html.span [ prop.className "badge badge-neutral shrink-0 whitespace-nowrap"; prop.text "Delivery" ]
                                ]
                            ]
                            Html.p [
                                prop.className "mt-1 text-sm text-base-content/60"
                                prop.text $"Leaf tips: +/-{float TrueBeamMlc.deliveryTipAtIsocentre} mm at isocentre"
                            ]
                        ]
                    ]

                    Html.button [
                        prop.className "btn btn-primary mt-1 w-full"
                        prop.disabled (not (FieldControls.canExport model))
                        prop.onClick (fun _ ->
                            if FieldControls.canExport model then
                                JscadGeometry.downloadStl
                                    (FieldControls.exportFileName model.Field)
                                    (Array.append jawGeometry trueBeamMlcBanksForExport))
                        prop.text "Export beam head STL"
                    ]
                ]
            ]
        ]
    ]

/// Renders a button selecting the geometry camera.
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

/// Renders the diagnostic geometry visibility switch.
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

/// Renders the camera controls and selected field geometry.
let private viewerCard field jawGeometry selectedMode showDebugGeometry dispatch =
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
                                    prop.text "TrueBeam jaws and Millennium 120 MLC at the TSEBT delivery position"
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
                        viewerDebugGeometry field,
                        jawGeometry,
                        field,
                        trueBeamMlcBanksForViewer
                    )
                ]
            ]
        ]
    ]

/// Renders the current BeamHead client.
let view model dispatch =
    let field = model.FieldControls.Field
    let jawGeometry = trueBeamJawsForExport field
    let viewerJawGeometry = JscadGeometry.createViewerDisplay jawGeometry

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
                        prop.children [
                            controlsCard model.FieldControls jawGeometry dispatch
                            viewerCard field viewerJawGeometry model.ViewerMode model.ShowDebugGeometry dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]