namespace BeamHead.Client.Tests

open BeamHead.Domain
open Xunit
open TestSession

module TestSessionTests =
    /// Defines a minimal check without coupling tests to the browser catalogue.
    let private definition steps = {
        Id = "check"
        Chapter = Domain
        Group = "Tests"
        Statement = "Test"
        Steps = steps
    }

    /// Creates valid measured evidence independently of production constructors.
    let private evidence = {
        Detail = Some "Measured"
        Failure = None
        Geometry =
            Some {
                Measurements = [
                    {
                        Component = "Fixture"
                        Requirement = "Distance"
                        Actual = 1.0
                        Expected = 1.0
                        Tolerance = 0.000001
                    }
                ]
                MlcShadowHalfWidth = 198.0<mm>
                MlcFieldClearance = 0.0<mm>
            }
    }

    /// Starts a minimal versioned run.
    let private running steps =
        init [ definition steps ] |> start JawField.fullSize [ definition steps ]

    /// Completes the expected next step.
    let private finish outcome model =
        completeStep model.RunId model.NextStep "check" outcome model

    /// Checks that untouched controls never grant export permission.
    [<Fact>]
    let ``initial session has no passing evidence`` () =
        let model = init [ definition 1 ]
        Assert.Equal(NotRun, model.Phase)
        Assert.False(canExport JawField.fullSize model)

    /// Checks snapshot capture and distinct run identities.
    [<Fact>]
    let ``starting again resets progress and changes run identity`` () =
        let first = running 1 |> finish (Ok evidence)
        let second = start JawField.fullSize [ definition 1 ] first
        Assert.Equal(first.RunId + 1, second.RunId)
        Assert.Equal(Some JawField.fullSize, second.Snapshot)
        Assert.Equal(0, second.NextStep)
        Assert.False(canExport JawField.fullSize second)

    /// Checks that callbacks from an old run cannot credit a new run.
    [<Fact>]
    let ``old run results are ignored`` () =
        let model = running 1
        Assert.Equal(model, completeStep (model.RunId - 1) 0 "check" (Ok evidence) model)

    /// Checks that duplicate or reordered callbacks cannot skip work.
    [<Fact>]
    let ``out of order steps are ignored`` () =
        let model = running 2
        Assert.Equal(model, completeStep model.RunId 1 "check" (Ok evidence) model)
        let next = finish (Ok evidence) model
        Assert.Equal(next, completeStep next.RunId 0 "check" (Ok evidence) next)

    /// Checks that a different row cannot steal the expected step's credit.
    [<Fact>]
    let ``unexpected check identity is ignored`` () =
        let model = running 1
        Assert.Equal(model, completeStep model.RunId 0 "foreign" (Ok evidence) model)

    /// Checks immediate revocation of previously passing results.
    [<Fact>]
    let ``field edits invalidate completed evidence`` () =
        let model = running 1 |> finish (Ok evidence) |> invalidate
        Assert.Equal(Outdated, model.Phase)
        Assert.False(canExport JawField.fullSize model)

    /// Checks that editing the field rejects pending callbacks.
    [<Fact>]
    let ``field edits reject late running results`` () =
        let before = running 1
        let after = invalidate before
        Assert.Equal(after, completeStep before.RunId 0 "check" (Ok evidence) after)

    /// Checks explicit cancellation and rejection of late callbacks.
    [<Fact>]
    let ``stopping prevents late completion`` () =
        let before = running 1
        let after = cancel before
        Assert.Equal(Cancelled, after.Phase)
        Assert.Equal(after, completeStep before.RunId 0 "check" (Ok evidence) after)
        Assert.False(canExport JawField.fullSize after)

    /// Checks that passing assertions cannot replace missing geometry measurements.
    [<Fact>]
    let ``export requires a measured geometry report`` () =
        let model = running 1 |> finish (Ok { evidence with Geometry = None })
        Assert.Equal(Completed, model.Phase)
        Assert.False(canExport JawField.fullSize model)

    /// Checks that passing assertions cannot override a measured failure.
    [<Fact>]
    let ``failed geometry measurements block export`` () =
        let report = evidence.Geometry.Value

        let failed = {
            report with
                Measurements = report.Measurements |> List.map (fun row -> { row with Actual = 2.0 })
        }

        let model = running 1 |> finish (Ok { evidence with Geometry = Some failed })
        Assert.False(canExport JawField.fullSize model)

    /// Checks that permission belongs only to the completed run's exact field.
    [<Fact>]
    let ``passing evidence authorizes only its captured field`` () =
        let model = running 1 |> finish (Ok evidence)
        let other = JawField.create 360.0<mm> 200.0<mm> |> Result.defaultWith failwith
        Assert.True(canExport JawField.fullSize model)
        Assert.False(canExport other model)

    /// Checks that later success cannot erase an earlier failed setting.
    [<Fact>]
    let ``multi step failures survive later success`` () =
        let model = running 2 |> finish (Error "Defect") |> finish (Ok evidence)
        Assert.Equal(Completed, model.Phase)
        Assert.Equal<string list>([ "Defect" ], model.Checks.Head.Failures)
        Assert.False(checkPassed model.Checks.Head)
        Assert.False(canExport JawField.fullSize model)

    /// Checks that measured failures retain their detailed report for inspection.
    [<Fact>]
    let ``measured failure evidence is retained`` () =
        let model =
            running 1
            |> finish (
                Ok {
                    evidence with
                        Failure = Some "Plane mismatch"
                }
            )

        Assert.True(model.Geometry.IsSome)
        Assert.Equal<string list>([ "Plane mismatch" ], model.Checks.Head.Failures)
        Assert.False(canExport JawField.fullSize model)

    /// Checks that a completed session cannot accept additional callbacks.
    [<Fact>]
    let ``completed runs ignore additional steps`` () =
        let model = running 1 |> finish (Ok evidence)
        Assert.Equal(model, completeStep model.RunId 1 "check" (Error "Late") model)