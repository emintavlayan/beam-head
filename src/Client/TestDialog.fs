module TestDialog

open BeamHead.Domain
open Fable.Core
open Feliz
open TestSession

/// Opens or closes the native modal without rebuilding the test session.
[<Import("setDialogOpen", "./TestDialogInterop.js")>]
let private setOpen (element: obj) (visible: bool) : unit = jsNative

/// Binds native dismissal events and returns their cleanup function.
[<Import("bindDialog", "./TestDialogInterop.js")>]
let private bind (element: obj) (onClose: unit -> unit) : (unit -> unit) = jsNative

/// Scrolls the active check into view when test execution moves forward.
[<Import("scrollActiveCheck", "./TestDialogInterop.js")>]
let private scrollActiveCheck (element: obj) (checkId: string) : unit = jsNative

/// Formats a browser-facing number with a fixed number of decimal places.
let private formatFixed decimals value =
    let scale = pown 10 decimals
    let scaled = int (System.Math.Round(value * float scale))
    let absolute = abs scaled
    let fraction = string (absolute % scale)
    let padding = String.replicate (decimals - fraction.Length) "0"
    let sign = if scaled < 0 then "-" else ""
    $"{sign}{absolute / scale}.{padding}{fraction}"

/// Renders a read-only status square that cannot be manually checked by the user.
let private indicator status =
    let colors, text =
        match status with
        | "Passed" -> "border-emerald-600 bg-emerald-600 text-white", "✓"
        | "Failed" -> "border-red-600 bg-red-600 text-white", "×"
        | "Running" -> "border-sky-500 bg-sky-50 text-sky-700", ""
        | _ -> "border-slate-300 bg-slate-50 text-slate-400", ""

    Html.span [
        prop.role "img"
        prop.ariaLabel status
        prop.title status
        prop.className
            $"flex h-6 w-6 shrink-0 items-center justify-center rounded-md border-2 text-sm font-bold {colors}"
        prop.children [
            if status = "Running" then
                Html.span [ prop.className "loading loading-spinner loading-xs" ]
            else
                Html.text text
        ]
    ]

/// Determines a row's real status without presenting outdated evidence as a current pass.
let private rowStatus model active row =
    match model.Phase with
    | Outdated -> "Outdated"
    | _ when active = Some row.Definition.Id -> "Running"
    | _ when row.Completed = row.Definition.Steps && not row.Failures.IsEmpty -> "Failed"
    | _ when checkPassed row -> "Passed"
    | Cancelled -> "Stopped"
    | _ -> "Not run"

/// Displays actual physical measurements beside their expected values and tolerances.
let private measuredValues report =
    Html.div [
        prop.className "mt-3 overflow-x-auto rounded-lg border border-slate-200 bg-white"
        prop.children [
            Html.table [
                prop.className "table table-xs w-full"
                prop.children [
                    Html.thead [
                        Html.tr [
                            Html.th "Requirement"
                            Html.th "Measured"
                            Html.th "Expected"
                            Html.th "Tolerance"
                            Html.th "Status"
                        ]
                    ]
                    Html.tbody [
                        for item in report.Measurements do
                            Html.tr [
                                prop.className (
                                    if GeometryQa.meetsRequirement item then
                                        ""
                                    else
                                        "bg-red-50 text-red-800"
                                )
                                prop.children [
                                    Html.td $"{item.Component}: {item.Requirement}"
                                    Html.td (formatFixed 6 item.Actual)
                                    Html.td (formatFixed 6 item.Expected)
                                    Html.td (formatFixed 6 item.Tolerance)
                                    Html.td [
                                        indicator (
                                            if GeometryQa.meetsRequirement item then
                                                "Passed"
                                            else
                                                "Failed"
                                        )
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]
    ]

/// Renders a single statement with progress and expandable failure or measurement evidence.
let private checkRow model active row =
    let status = rowStatus model active row

    Html.details [
        prop.key row.Definition.Id
        if status = "Failed" then prop.custom ("open", true)
        prop.custom ("data-check-id", row.Definition.Id)
        prop.custom ("data-check-status", status)
        prop.className "border-t border-slate-100"
        prop.children [
            Html.summary [
                prop.className
                    "flex cursor-pointer list-none items-center justify-between gap-4 px-5 py-3 hover:bg-slate-50"
                prop.children [
                    Html.div [
                        Html.p [
                            prop.className "text-sm leading-6 text-slate-800"
                            prop.text row.Definition.Statement
                        ]
                        if row.Definition.Steps > 1 then
                            Html.p [
                                prop.className "text-xs tabular-nums text-slate-500"
                                prop.text $"{row.Completed} / {row.Definition.Steps} settings checked"
                            ]
                    ]
                    Html.div [
                        prop.className "flex shrink-0 items-center gap-3"
                        prop.children [
                            Html.span [ prop.className "hidden text-xs text-slate-500 sm:inline"; prop.text status ]
                            indicator status
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "border-t border-slate-100 bg-slate-50 px-5 py-3 text-sm text-slate-600"
                prop.children [
                    if model.Phase = Outdated then
                        Html.p
                            "These results belong to the previous field. Run the tests again for the current geometry."
                    for failure in row.Failures do
                        Html.p [ prop.className "whitespace-pre-wrap text-red-700"; prop.text failure ]
                    match row.Detail with
                    | Some detail -> Html.p detail
                    | None -> Html.none
                    if row.Definition.Id = "current-geometry" then
                        match model.Geometry with
                        | Some report -> measuredValues report
                        | None -> Html.p "Measured values appear after this check runs."
                    elif row.Failures.IsEmpty && row.Detail.IsNone then
                        Html.p (
                            if status = "Passed" then
                                "The shared test completed without an assertion failure."
                            else
                                "Evidence appears when the test runs."
                        )
                ]
            ]
        ]
    ]

/// Renders a numbered collapsible chapter with its result count aligned at the right.
let private chapter model active kind (number: string) (title: string) (description: string) =
    let rows = model.Checks |> List.filter (fun row -> row.Definition.Chapter = kind)
    let passed = rows |> List.filter checkPassed |> List.length
    let failed = rows |> List.exists (fun row -> not row.Failures.IsEmpty)
    let activeInChapter =
        active
        |> Option.exists (fun checkId -> rows |> List.exists (fun row -> row.Definition.Id = checkId))
    let complete = rows |> List.forall (fun row -> row.Completed = row.Definition.Steps)
    let openChapter = failed || (model.Phase = Running && activeInChapter)
    let resultClass, resultText =
        if model.Phase = Outdated then
            "bg-amber-100 text-amber-800", "Outdated"
        elif failed then
            "bg-red-100 text-red-800", $"× {rows.Length - passed} failed"
        elif complete then
            "bg-emerald-100 text-emerald-800", $"✓ {passed} / {rows.Length} passed"
        else
            "bg-slate-200 text-slate-700", $"{passed} / {rows.Length} passed"

    Html.details [
        prop.custom ("data-chapter", number)
        if openChapter then prop.custom ("open", true)
        prop.className "overflow-hidden rounded-xl border border-slate-200 bg-white"
        prop.children [
            Html.summary [
                prop.className "flex cursor-pointer list-none items-start justify-between gap-4 bg-slate-50 px-5 py-5"
                prop.children [
                    Html.div [
                        prop.className "flex items-start gap-4"
                        prop.children [
                            Html.span [
                                prop.className
                                    "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-slate-900 text-sm font-semibold text-white"
                                prop.text number
                            ]
                            Html.div [
                                Html.h2 [
                                    prop.className "text-lg font-semibold text-slate-900 outline-none"
                                    prop.text title
                                ]
                                Html.p [
                                    prop.className "mt-1 max-w-xl text-sm leading-6 text-slate-600"
                                    prop.text description
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className $"shrink-0 rounded-full px-3 py-1 text-xs font-medium tabular-nums {resultClass}"
                        prop.text resultText
                    ]
                ]
            ]
            if failed then
                Html.p [
                    prop.className "border-b border-red-100 bg-red-50 px-5 py-2 text-xs font-medium text-red-700"
                    prop.text "One or more checks failed. Failure details are open below."
                ]
            for group in rows |> List.map _.Definition.Group |> List.distinct do
                Html.div [
                    Html.h3 [
                        prop.className
                            "border-y border-slate-100 bg-slate-50/50 px-5 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500"
                        prop.text group
                    ]
                    for row in rows |> List.filter (fun row -> row.Definition.Group = group) do
                        checkRow model active row
                ]
        ]
    ]

/// Renders a native modal with fixed actions, one vertical scroll area and actual test progress.
[<ReactComponent>]
let View
    (
        visible: bool,
        field: JawField,
        inputsValid: bool,
        model: TestSession.Model,
        startError: string option,
        automaticExport: bool,
        onRun: unit -> unit,
        onStop: unit -> unit,
        onClose: unit -> unit
    ) =
    let element = React.useElementRef ()

    React.useEffect (
        (fun () ->
            match element.current with
            | Some dialog -> setOpen (box dialog) visible
            | None -> ()),
        [| box visible |]
    )

    React.useEffect (
        (fun () ->
            match element.current with
            | Some dialog -> React.createDisposable (bind (box dialog) onClose)
            | None -> React.createDisposable ignore),
        [||]
    )

    let active =
        if model.Phase = Running then
            model.Checks
            |> List.tryFind (fun row -> row.Completed < row.Definition.Steps)
            |> Option.map _.Definition.Id
        else
            None

    let progress =
        if model.TotalSteps = 0 then
            0.0
        else
            100.0 * float model.NextStep / float model.TotalSteps

    React.useEffect (
        (fun () ->
            match element.current, active with
            | Some dialog, Some checkId -> scrollActiveCheck (box dialog) checkId
            | _ -> ()),
        [| box active |]
    )

    Html.dialog [
        prop.ref element
        prop.className "qa-dialog"
        prop.custom ("aria-labelledby", "qa-dialog-title")
        prop.custom ("aria-modal", "true")
        prop.children [
            Html.header [
                prop.className "shrink-0 border-b border-slate-200 bg-white px-5 pb-4 pt-5 sm:px-6"
                prop.children [
                    Html.div [
                        prop.className "flex items-start justify-between gap-4"
                        prop.children [
                            Html.div [
                                Html.p [
                                    prop.className "text-xs font-semibold uppercase tracking-widest text-slate-500"
                                    prop.text "Beam head verification"
                                ]
                                Html.h1 [
                                    prop.id "qa-dialog-title"
                                    prop.className "mt-1 text-2xl font-semibold text-slate-900"
                                    prop.text "Geometry tests"
                                ]
                            ]
                            Html.button [
                                prop.type' "button"
                                prop.autoFocus true
                                prop.className "btn btn-ghost btn-sm text-lg"
                                prop.ariaLabel "Close test dialog"
                                prop.onClick (fun _ -> onClose ())
                                prop.text "×"
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "mt-3 flex flex-wrap items-center justify-between gap-3"
                        prop.children [
                            Html.div [
                                Html.p [
                                    prop.className "text-sm text-slate-600"
                                    prop.text $"Current field: {fieldLabel field} at isocentre"
                                ]
                                if model.Phase = Outdated && model.Snapshot.IsSome then
                                    Html.p [
                                        prop.className "mt-1 text-xs text-amber-700"
                                        prop.text $"Previous run: {fieldLabel model.Snapshot.Value}"
                                    ]
                            ]
                            Html.div [
                                prop.className "flex items-center gap-2"
                                prop.children [
                                    if model.Phase = Running then
                                        Html.button [
                                            prop.className "btn btn-outline btn-sm"
                                            prop.onClick (fun _ -> onStop ())
                                            prop.text "Stop"
                                        ]
                                    Html.button [
                                        prop.className "btn btn-primary btn-sm"
                                        prop.disabled (model.Phase = Running || not inputsValid || automaticExport)
                                        prop.onClick (fun _ -> onRun ())
                                        prop.text (
                                            if automaticExport then
                                                "Preparing export…"
                                            elif model.Phase = Running then
                                                "Running tests…"
                                            else
                                                "Run all tests"
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "mt-3 flex items-center justify-between gap-4 text-sm"
                        prop.children [
                            Html.p [
                                prop.role "status"
                                prop.className "font-medium text-slate-700"
                                prop.text (label model)
                            ]
                            Html.span [
                                prop.className "shrink-0 text-xs tabular-nums text-slate-500"
                                prop.text $"{model.NextStep} / {model.TotalSteps} steps"
                            ]
                        ]
                    ]
                    Html.progress [
                        prop.className "progress progress-primary mt-2 h-2 w-full"
                        prop.max 100
                        prop.value progress
                        prop.ariaLabel "Test progress"
                    ]
                ]
            ]
            Html.div [
                prop.className "qa-dialog-scroll space-y-4 bg-slate-100/70 p-4 sm:p-5"
                prop.children [
                    match startError with
                    | Some error -> Html.p [ prop.role "alert"; prop.className "text-sm text-red-700"; prop.text error ]
                    | None -> Html.none
                    if not inputsValid then
                        Html.p [
                            prop.className "text-sm text-red-700"
                            prop.text "Correct the field dimensions before running tests."
                        ]
                    if automaticExport then
                        Html.p [
                            prop.className "rounded-lg border border-sky-200 bg-sky-50 p-3 text-sm text-sky-800"
                            prop.text "The STL will download automatically after successful verification."
                        ]
                    match model.Geometry with
                    | Some report when model.Phase = Completed ->
                        Html.div [
                            prop.className "rounded-lg border border-slate-200 bg-white p-3 text-sm"
                            prop.children [
                                Html.p
                                    $"Rounded MLC shadow half-width: {formatFixed 3 (float report.MlcShadowHalfWidth)} mm at isocentre."
                                if report.MlcFieldClearance < 0.0<mm> then
                                    Html.p [
                                        prop.className "mt-2 text-amber-800"
                                        prop.text
                                            $"Geometric observation: the rounded MLC shadow enters the requested X opening by {formatFixed 3 (float (-report.MlcFieldClearance))} mm. This is separate from construction pass/fail."
                                    ]
                            ]
                        ]
                    | _ -> Html.none
                    chapter
                        model
                        active
                        Domain
                        "01"
                        "Domain tests"
                        "Parameters, calculated placements, coordinate transforms and field controls."
                    chapter
                        model
                        active
                        GeometryChecker
                        "02"
                        "Geometry checker tests"
                        "Known geometry and deliberate defects check the measuring algorithms themselves."
                    chapter
                        model
                        active
                        JscadGeometry
                        "03"
                        "JSCAD geometry tests"
                        "Production constructors build real solids whose resolved surfaces must meet the geometric requirements."
                ]
            ]
            Html.footer [
                prop.className "shrink-0 border-t border-slate-200 bg-white px-5 py-3 text-xs leading-5 text-slate-500"
                prop.text (
                    if automaticExport then
                        "Verification is required for export. The STL downloads automatically after all checks pass."
                    elif model.Phase = Running then
                        "Closing this window keeps the tests running. Use Stop to cancel."
                    else
                        "These checks validate the listed model geometry. Physical-machine and dose validation remain separate."
                )
            ]
        ]
    ]
