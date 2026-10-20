module TestSession

open BeamHead.Domain

/// Identifies the three test chapters.
type Chapter =
    | Domain
    | GeometryChecker
    | JscadGeometry

/// Describes one visible check and its independently scheduled work steps.
type CheckDefinition = {
    Id: string
    Chapter: Chapter
    Group: string
    Statement: string
    Steps: int
}

/// Carries actual step evidence and any measured geometry failure.
type Evidence = {
    Detail: string option
    Failure: string option
    Geometry: GeometryQaReport option
}

/// Represents accumulated progress and persistent failures for one check.
type CheckResult = {
    Definition: CheckDefinition
    Completed: int
    Failures: string list
    Detail: string option
}

/// Identifies whether a run is complete, running, stopped or outdated.
type Phase =
    | NotRun
    | Running
    | Completed
    | Outdated
    | Cancelled

/// Represents versioned test results bound to one immutable field snapshot.
type Model = {
    RunId: int
    Phase: Phase
    Snapshot: JawField option
    Checks: CheckResult list
    NextStep: int
    TotalSteps: int
    Geometry: GeometryQaReport option
}

/// Creates an untested session without assuming that any check passed.
let init definitions = {
    RunId = 0
    Phase = NotRun
    Snapshot = None
    NextStep = 0
    Geometry = None
    TotalSteps = definitions |> List.sumBy _.Steps
    Checks =
        definitions
        |> List.map (fun item -> {
            Definition = item
            Completed = 0
            Failures = []
            Detail = None
        })
}

/// Captures the field and gives a new run a distinct identity.
let start field definitions model = {
    init definitions with
        RunId = model.RunId + 1
        Phase = Running
        Snapshot = Some field
}

/// Revokes validity when physical field dimensions change.
let invalidate model =
    if model.Phase = NotRun then
        model
    else
        {
            model with
                RunId = model.RunId + 1
                Phase = Outdated
        }

/// Stops a run while retaining its completed evidence.
let cancel model =
    if model.Phase = Running then
        {
            model with
                RunId = model.RunId + 1
                Phase = Cancelled
        }
    else
        model

/// Accepts only the expected next step of the active run and preserves all failures.
let completeStep runId position checkId (outcome: Result<Evidence, string>) (model: Model) =
    let expected =
        model.Checks |> List.tryFind (fun row -> row.Completed < row.Definition.Steps)

    let known = expected |> Option.exists (fun row -> row.Definition.Id = checkId)

    if
        model.Phase <> Running
        || model.RunId <> runId
        || model.NextStep <> position
        || not known
    then
        model
    else
        let checks =
            model.Checks
            |> List.map (fun row ->
                if row.Definition.Id <> checkId then
                    row
                else
                    match outcome with
                    | Ok evidence -> {
                        row with
                            Completed = row.Completed + 1
                            Detail = evidence.Detail |> Option.orElse row.Detail
                            Failures = row.Failures @ Option.toList evidence.Failure
                      }
                    | Error error -> {
                        row with
                            Completed = row.Completed + 1
                            Failures = row.Failures @ [ error ]
                      })

        let next = model.NextStep + 1

        let geometry =
            match outcome with
            | Ok evidence -> evidence.Geometry |> Option.orElse model.Geometry
            | Error _ -> model.Geometry

        {
            model with
                Checks = checks
                NextStep = next
                Geometry = geometry
                Phase = if next = model.TotalSteps then Completed else Running
        }

/// Checks whether every step of one check passed.
let checkPassed row =
    row.Completed = row.Definition.Steps && List.isEmpty row.Failures

/// Authorizes export only after all tests and measurements passed for the exact captured field.
let canExport field model =
    model.Phase = Completed
    && model.Snapshot = Some field
    && not (List.isEmpty model.Checks)
    && List.forall checkPassed model.Checks
    && (model.Geometry |> Option.exists GeometryQa.passes)

/// Formats a field snapshot for the user interface.
let fieldLabel field =
    $"{FieldControls.formatCentimetres (JawField.xSize field)} × {FieldControls.formatCentimetres (JawField.ySize field)} cm"

/// Describes overall status without presenting partial or outdated results as a pass.
let label model =
    match model.Phase with
    | NotRun -> "Tests have not been run"
    | Running -> "Tests are running"
    | Outdated -> "Geometry changed — run the tests again"
    | Cancelled -> "Test run stopped"
    | Completed ->
        let failed =
            model.Checks
            |> List.filter (fun row -> not (List.isEmpty row.Failures))
            |> List.length

        if failed = 0 then
            $"All {model.Checks.Length} checks passed"
        else
            $"{failed} checks failed"