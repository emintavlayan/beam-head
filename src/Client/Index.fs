module Index

open BeamHead.Domain
open Feliz
open Elmish

/// Represents the geometry workbench and its versioned verification dialog.
type Model = {
    ViewerMode: GeometryViewer.ViewerMode
    ShowDebugGeometry: bool
    ShowTests: bool
    FieldControls: FieldControls.Model
    Tests: TestSession.Model
    Workflow: TestCatalog.Workflow option
    TestError: string option
    PendingExport: int option
}

/// Represents edits, dialog actions and actual asynchronous test results.
type Msg =
    | SetViewerMode of GeometryViewer.ViewerMode
    | SetDebugGeometry of bool
    | SetTestsVisible of bool
    | EditFieldSize of JawAxis * string
    | CommitFieldSize of JawAxis
    | SetSliderSize of JawAxis * string
    | SelectPreset of JawAxis * float<mm>
    | SetSquareField of bool
    | RunTests
    | StopTests
    | RequestExport
    | TestStepFinished of int * int * string * Result<TestSession.Evidence, string>
    | AutomaticExportReady of int
    | AutomaticExportFinished of int * Result<unit, string>

/// Creates the workbench without assuming any validation result.
let init () =
    {
        ViewerMode = GeometryViewer.ThreeD
        ShowDebugGeometry = false
        ShowTests = false
        FieldControls = FieldControls.init ()
        Tests = TestSession.init TestCatalog.definitions
        Workflow = None
        TestError = None
        PendingExport = None
    },
    Cmd.none

/// Schedules one actual step after yielding so the browser can paint progress and process interaction.
let private nextTestStep model =
    match model.Workflow with
    | Some workflow when
        model.Tests.Phase = TestSession.Running
        && model.Tests.NextStep < workflow.Steps.Length
        ->
        let position, runId = model.Tests.NextStep, model.Tests.RunId
        let step = workflow.Steps[position]

        Cmd.OfAsync.perform
            (fun () -> async {
                do! Async.Sleep 1

                return
                    try
                        step.Execute()
                    with error ->
                        Error error.Message
            })
            ()
            (fun result -> TestStepFinished(runId, position, step.CheckId, result))
    | _ -> Cmd.none

/// Finds captured solids belonging to the currently selected field.
let private currentScene model =
    model.Workflow
    |> Option.filter (fun workflow -> workflow.Scene.Field = model.FieldControls.Field)

/// Waits briefly so a successful automatic verification remains visible before download.
let private scheduleAutomaticExport runId =
    Cmd.OfAsync.perform
        (fun () -> async {
            do! Async.Sleep 2000
            return runId
        })
        ()
        AutomaticExportReady

/// Downloads the exact solids captured and measured by the successful run.
let private downloadCapturedScene runId field (workflow: TestCatalog.Workflow) =
    Cmd.OfAsync.perform
        (fun () -> async {
            return
                try
                    JscadGeometry.downloadStl
                        (FieldControls.exportFileName field)
                        (Array.append workflow.Scene.Jaws workflow.Scene.Banks)
                    Ok ()
                with error ->
                    Error error.Message
        })
        ()
        (fun result -> AutomaticExportFinished(runId, result))

/// Starts a fresh verification run for manual review or automatic export.
let private startTests automaticExport model =
    let starting =
        TestSession.start model.FieldControls.Field TestCatalog.definitions model.Tests

    try
        let workflow = TestCatalog.prepare model.FieldControls.Field
        let updated = {
            model with
                ShowTests = true
                Tests = starting
                Workflow = Some workflow
                TestError = None
                PendingExport = if automaticExport then Some starting.RunId else None
        }
        updated, nextTestStep updated
    with error ->
        {
            model with
                ShowTests = true
                Tests = TestSession.cancel starting
                Workflow = None
                TestError = Some error.Message
                PendingExport = None
        },
        Cmd.none

/// Revokes evidence only when the accepted physical field actually changes.
let private changeField (change: FieldControls.Model -> FieldControls.Model) (model: Model) =
    let controls = change model.FieldControls

    if controls.Field = model.FieldControls.Field then
        { model with FieldControls = controls }
    else
        {
            model with
                FieldControls = controls
                Tests = TestSession.invalidate model.Tests
                Workflow = None
                TestError = None
                PendingExport = None
        }

/// Applies edits and accepts results only from the expected active test run.
let update msg model =
    match msg with
    | SetViewerMode mode -> { model with ViewerMode = mode }, Cmd.none
    | SetDebugGeometry enabled ->
        {
            model with
                ShowDebugGeometry = enabled
        },
        Cmd.none
    | SetTestsVisible visible -> { model with ShowTests = visible }, Cmd.none
    | EditFieldSize(axis, text) -> changeField (FieldControls.editSize axis text) model, Cmd.none
    | CommitFieldSize axis -> changeField (FieldControls.commitSize axis) model, Cmd.none
    | SetSliderSize(axis, text) -> changeField (FieldControls.setSliderSize axis text) model, Cmd.none
    | SelectPreset(axis, size) -> changeField (FieldControls.selectPreset axis size) model, Cmd.none
    | SetSquareField enabled -> changeField (FieldControls.setSquareField enabled) model, Cmd.none
    | StopTests ->
        {
            model with
                Tests = TestSession.cancel model.Tests
                PendingExport = None
        },
        Cmd.none
    | RunTests when
        model.Tests.Phase <> TestSession.Running
        && FieldControls.canExport model.FieldControls
        ->
        startTests false model
    | RunTests -> model, Cmd.none
    | RequestExport when FieldControls.canExport model.FieldControls ->
        match model.Tests.Phase, currentScene model with
        | TestSession.Running, Some _ ->
            {
                model with
                    ShowTests = true
                    PendingExport = Some model.Tests.RunId
            },
            Cmd.none
        | _, Some workflow when TestSession.canExport model.FieldControls.Field model.Tests ->
            let updated = {
                model with
                    ShowTests = true
                    PendingExport = Some model.Tests.RunId
            }
            updated, scheduleAutomaticExport model.Tests.RunId
        | _ -> startTests true model
    | RequestExport -> model, Cmd.none
    | TestStepFinished(runId, position, checkId, outcome) ->
        if
            model.Tests.Phase <> TestSession.Running
            || model.Tests.RunId <> runId
            || model.Tests.NextStep <> position
        then
            model, Cmd.none
        else
            let updated = {
                model with
                    Tests = TestSession.completeStep runId position checkId outcome model.Tests
            }

            if updated.Tests.Phase = TestSession.Completed then
                match updated.PendingExport with
                | Some pendingRun when
                    pendingRun = runId
                    && TestSession.canExport updated.FieldControls.Field updated.Tests
                    && (currentScene updated |> Option.isSome)
                    ->
                    updated, scheduleAutomaticExport runId
                | Some _ -> { updated with PendingExport = None }, Cmd.none
                | None -> updated, Cmd.none
            else
                updated, nextTestStep updated
    | AutomaticExportReady runId when model.PendingExport = Some runId ->
        match currentScene model with
        | Some workflow when TestSession.canExport model.FieldControls.Field model.Tests ->
            model, downloadCapturedScene runId model.FieldControls.Field workflow
        | _ -> { model with PendingExport = None }, Cmd.none
    | AutomaticExportReady _ -> model, Cmd.none
    | AutomaticExportFinished(runId, Ok ()) when model.PendingExport = Some runId ->
        {
            model with
                ShowTests = false
                PendingExport = None
        },
        Cmd.none
    | AutomaticExportFinished(runId, Error error) when model.PendingExport = Some runId ->
        {
            model with
                TestError = Some error
                PendingExport = None
        },
        Cmd.none
    | AutomaticExportFinished _ -> model, Cmd.none

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
    FieldLight.corners field |> Array.map ViewerDisplay.fromIsocentrePoint

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
                    Html.label [
                        prop.htmlFor inputId
                        prop.className "font-medium"
                        prop.text $"{name} field size"
                    ]
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.input [
                                prop.id inputId
                                prop.type' "text"
                                prop.custom ("inputMode", "decimal")
                                prop.className "input input-bordered input-sm w-24 text-right"
                                prop.value text
                                prop.ariaLabel $"{name} field size in centimetres"
                                prop.onChange (fun (value: string) -> dispatch (EditFieldSize(axis, value)))
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
                prop.onChange (fun (value: string) -> dispatch (SetSliderSize(axis, value)))
            ]
            Html.div [
                prop.className "flex flex-wrap gap-1"
                prop.children [
                    for preset in FieldControls.presets do
                        Html.button [
                            prop.type' "button"
                            prop.className (
                                if size = preset then
                                    "btn btn-xs btn-primary"
                                else
                                    "btn btn-xs btn-outline"
                            )
                            prop.ariaLabel $"{name} preset {int (float preset / 10.0)} cm"
                            prop.onClick (fun _ -> dispatch (SelectPreset(axis, preset)))
                            prop.text $"{int (float preset / 10.0)}"
                        ]
                    Html.span [
                        prop.className "self-center px-1 text-xs text-base-content/60"
                        prop.text "cm"
                    ]
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
let private controlsCard (model: FieldControls.Model) pendingExport dispatch =
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
                        prop.text
                            $"{FieldControls.formatCentimetres (JawField.xSize model.Field)} x {FieldControls.formatCentimetres (JawField.ySize model.Field)} cm at isocentre"
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Static MLC" ]

                    Html.div [
                        prop.className "rounded-box border border-base-300 bg-base-200 p-3"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center justify-between gap-3"
                                prop.children [
                                    Html.span [
                                        prop.className "font-medium whitespace-nowrap"
                                        prop.text "Millennium 120"
                                    ]
                                    Html.span [
                                        prop.className "badge badge-neutral shrink-0 whitespace-nowrap"
                                        prop.text "Delivery"
                                    ]
                                ]
                            ]
                            Html.p [
                                prop.className "mt-1 text-sm text-base-content/60"
                                prop.text $"Machine setting: +/-{float TrueBeamMlc.deliveryTipSettingAtIsocentre} mm at isocentre"
                            ]
                        ]
                    ]

                    Html.button [
                        prop.className "btn btn-primary mt-1 w-full"
                        prop.disabled (not (FieldControls.canExport model) || (pendingExport |> Option.isSome))
                        prop.onClick (fun _ -> dispatch RequestExport)
                        prop.text (if pendingExport |> Option.isSome then "Verifying geometry…" else "Export beam head STL")
                    ]
                ]
            ]
        ]
    ]

/// Renders the single navigation entry for opening tests and reading their current result.
let private testsNavigationButton (field: JawField) (tests: TestSession.Model) dispatch =
    let failed =
        tests.Checks
        |> List.filter (fun row -> not row.Failures.IsEmpty)
        |> List.length

    let statusClass, statusText =
        match tests.Phase with
        | TestSession.Completed when TestSession.canExport field tests ->
            "badge-success", $"{tests.Checks.Length} / {tests.Checks.Length} passed"
        | TestSession.Completed -> "badge-error", $"{failed} failed"
        | TestSession.Running -> "badge-info", $"{tests.NextStep} / {tests.TotalSteps}"
        | TestSession.Outdated -> "badge-warning", "Outdated"
        | TestSession.Cancelled -> "badge-neutral", "Stopped"
        | TestSession.NotRun -> "badge-ghost", "Not run"

    Html.button [
        prop.id "tests-trigger"
        prop.className "btn btn-outline btn-sm gap-2"
        prop.onClick (fun _ -> dispatch (SetTestsVisible true))
        prop.children [
            Html.span "Tests"
            Html.span [
                prop.className $"badge badge-sm {statusClass}"
                prop.text statusText
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
                            Html.div [
                                prop.className "navbar-end"
                                prop.children [
                                    testsNavigationButton field model.Tests dispatch
                                ]
                            ]
                        ]
                    ]

                    Html.div [
                        prop.className "mt-4 grid items-start gap-4 lg:grid-cols-[20rem_minmax(0,1fr)]"
                        prop.children [
                            controlsCard model.FieldControls model.PendingExport dispatch
                            viewerCard field viewerJawGeometry model.ViewerMode model.ShowDebugGeometry dispatch
                        ]
                    ]
                ]
            ]
            TestDialog.View(
                model.ShowTests,
                field,
                FieldControls.canExport model.FieldControls,
                model.Tests,
                model.TestError,
                (model.PendingExport |> Option.isSome),
                (fun () -> dispatch RunTests),
                (fun () -> dispatch StopTests),
                (fun () -> dispatch (SetTestsVisible false))
            )
        ]
    ]
