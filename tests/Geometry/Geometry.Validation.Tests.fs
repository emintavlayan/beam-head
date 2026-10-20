namespace BeamHead.Domain.Tests

open BeamHead.Domain
open Xunit

module GeometryValidationTests =
    /// Creates a measured requirement for tolerance-boundary tests.
    let private measured value = {
        Component = "Test"
        Requirement = "Distance"
        Actual = value
        Expected = 0.0
        Tolerance = GeometryQa.tolerance
    }

    /// Creates the independently specified five vertices of a rectangular field-light pyramid.
    let private pyramid () =
        let points: IsocentreFramePoint array = [|
            {X = 0.0<mm>; Y = 0.0<mm>; Z = -1000.0<mm>}
            {X = -200.0<mm>; Y = -200.0<mm>; Z = 0.0<mm>}
            {X = 200.0<mm>; Y = -200.0<mm>; Z = 0.0<mm>}
            {X = 200.0<mm>; Y = 200.0<mm>; Z = 0.0<mm>}
            {X = -200.0<mm>; Y = 200.0<mm>; Z = 0.0<mm>}
        |]
        [[0;2;1]; [0;3;2]; [0;4;3]; [0;1;4]; [1;2;3;4]]
        |> List.map (List.map (fun index -> points[index]))

    /// Checks acceptance exactly at the numerical construction tolerance.
    [<Fact>]
    let ``measurement tolerance includes its boundary`` () =
        Assert.True(measured GeometryQa.tolerance |> GeometryQa.meetsRequirement)
        Assert.True(measured -GeometryQa.tolerance |> GeometryQa.meetsRequirement)
        Assert.False(measured (GeometryQa.tolerance * 1.01) |> GeometryQa.meetsRequirement)

    /// Checks that nonfinite values never satisfy a measured requirement.
    [<Fact>]
    let ``nonfinite measurements fail`` () =
        for value in [System.Double.NaN; System.Double.PositiveInfinity; System.Double.NegativeInfinity] do
            Assert.False(measured value |> GeometryQa.meetsRequirement)

    /// Checks that missing evidence cannot produce an overall pass.
    [<Fact>]
    let ``empty QA reports cannot pass`` () =
        let report = {Measurements = []; MlcShadowHalfWidth = 0.0<mm>; MlcFieldClearance = 0.0<mm>}
        Assert.False(GeometryQa.passes report)

    /// Checks the source and corner vertices, winding, planarity and closure of a known pyramid.
    [<Fact>]
    let ``independent field light pyramid meets all geometric requirements`` () =
        match GeometryQa.inspectFieldLight JawField.fullSize (pyramid ()) with
        | Ok checks -> Assert.True(List.forall GeometryQa.meetsRequirement checks)
        | Error error -> failwith error

    /// Checks that a displaced guide cannot agree with the requested field.
    [<Fact>]
    let ``displaced field light fails vertex agreement`` () =
        let shifted = pyramid () |> List.map (List.map (fun point -> {point with X = point.X + 1.0<mm>}))
        match GeometryQa.inspectFieldLight JawField.fullSize shifted with
        | Ok checks -> Assert.Contains(checks, fun check -> check.Requirement = "Pyramid vertex error" && not (GeometryQa.meetsRequirement check))
        | Error error -> failwith error

    /// Checks that reversed complete winding is detected even when all edges still pair.
    [<Fact>]
    let ``inward pyramid faces fail half space validation`` () =
        match GeometryQa.inspectFieldLight JawField.fullSize (pyramid () |> List.map List.rev) with
        | Ok checks -> Assert.Contains(checks, fun check -> check.Requirement = "Outward convex half-space violation" && not (GeometryQa.meetsRequirement check))
        | Error error -> failwith error

    /// Checks that an open pyramid cannot pass structural validation.
    [<Fact>]
    let ``missing pyramid face fails closure`` () =
        match GeometryQa.inspectFieldLight JawField.fullSize (pyramid () |> List.tail) with
        | Ok checks -> Assert.Contains(checks, fun check -> check.Requirement = "Unpaired or multiply paired edges" && not (GeometryQa.meetsRequirement check))
        | Error error -> failwith error

    /// Checks malformed or empty guide data returns explicit errors.
    [<Fact>]
    let ``invalid field light surfaces return errors`` () =
        Assert.True(GeometryQa.inspectFieldLight JawField.fullSize [] |> Result.isError)
        Assert.True(GeometryQa.inspectFieldLight JawField.fullSize [[]] |> Result.isError)
        let collapsed = pyramid () |> List.map (List.map (fun _ -> IsocentreFrame.isocentre))
        Assert.True(GeometryQa.inspectFieldLight JawField.fullSize collapsed |> Result.isError)

    /// Checks the required component inventory before any geometric calculation.
    [<Fact>]
    let ``missing physical components return errors`` () =
        Assert.True(GeometryQa.inspect JawField.fullSize [] [] |> Result.isError)
        Assert.True(GeometryQa.inspectJaws JawField.fullSize [] |> Result.isError)

    /// Checks the independent field-guide dimensions for a rectangular field.
    [<Fact>]
    let ``field light corners preserve independent x and y dimensions`` () =
        match JawField.create 360.0<mm> 200.0<mm> with
        | Error error -> failwith error
        | Ok field ->
            let corners = FieldLight.corners field
            Assert.Equal(4, corners.Length)
            Assert.Equal(-180.0<mm>, corners[0].X)
            Assert.Equal(-100.0<mm>, corners[0].Y)
            Assert.Equal(180.0<mm>, corners[2].X)
            Assert.Equal(100.0<mm>, corners[2].Y)
            Assert.All(corners, fun point -> Assert.Equal(0.0<mm>, point.Z))

    /// Creates a known outward cube for independent analytic ray-chord tests.
    let private cube () =
        let points: IsocentreFramePoint array = [|
            for z in [-550.0<mm>; -450.0<mm>] do
                for x, y in [-50.0<mm>, -50.0<mm>; 50.0<mm>, -50.0<mm>; 50.0<mm>, 50.0<mm>; -50.0<mm>, 50.0<mm>] do
                    yield {X = x; Y = y; Z = z}
        |]
        [[0;3;2;1]; [4;5;6;7]; [0;1;5;4]; [1;2;6;5]; [2;3;7;6]; [3;0;4;7]]
        |> List.map (List.map (fun index -> points[index]))

    /// Checks the exact axial chord through a known cube.
    [<Fact>]
    let ``central ray measures known cube thickness`` () =
        match GeometryQa.rayPathLength IsocentreFrame.isocentre (cube ()) with
        | Ok path -> Assert.InRange(path, 99.999999<mm>, 100.000001<mm>)
        | Error error -> failwith error

    /// Checks the analytic oblique chord rather than the cube bounding-box depth.
    [<Fact>]
    let ``oblique material path includes geometric ray length`` () =
        let target: IsocentreFramePoint = {X = 50.0<mm>; Y = 0.0<mm>; Z = 0.0<mm>}
        let expected = 100.0<mm> * sqrt 1.0025
        match GeometryQa.rayPathLength target (cube ()) with
        | Ok path -> Assert.InRange(path, expected - 0.000001<mm>, expected + 0.000001<mm>)
        | Error error -> failwith error

    /// Checks that a ray missing the known cube has zero material path.
    [<Fact>]
    let ``unobstructed ray has zero material path`` () =
        let target: IsocentreFramePoint = {X = 1000.0<mm>; Y = 0.0<mm>; Z = 0.0<mm>}
        Assert.Equal(Ok 0.0<mm>, GeometryQa.rayPathLength target (cube ()))

    /// Checks a zero-length ray returns an error instead of a numeric path.
    [<Fact>]
    let ``source cannot be its own ray target`` () =
        Assert.True(GeometryQa.rayPathLength IsocentreFrame.source (cube ()) |> Result.isError)
        for value in [System.Double.NaN; System.Double.PositiveInfinity] do
            let target: IsocentreFramePoint = {X = value * 1.0<mm>; Y = 0.0<mm>; Z = 0.0<mm>}
            Assert.True(GeometryQa.rayPathLength target (cube ()) |> Result.isError)

    /// Checks a self-crossed planar face fails even when its vertices lie on the expected plane.
    [<Fact>]
    let ``crossed polygon edges fail convex face validation`` () =
        let faces = pyramid ()
        let baseFace = List.last faces
        let crossed = List.take 4 faces @ [[baseFace[0]; baseFace[2]; baseFace[1]; baseFace[3]]]
        match GeometryQa.inspectFieldLight JawField.fullSize crossed with
        | Ok checks -> Assert.Contains(checks, fun check -> check.Requirement = "Convex polygon edge violation" && not (GeometryQa.meetsRequirement check))
        | Error error -> failwith error
