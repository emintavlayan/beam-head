namespace BeamHead.Domain.Tests

open BeamHead.Domain
open Xunit

module TrueBeamJawTests =
    let private placement axis side =
        TrueBeamJaws.placements
        |> List.find (fun placement -> placement.Axis = axis && placement.Side = side)

    let private assertClose (expected: float<mm>) (actual: float<mm>) =
        Assert.InRange(actual, expected - 0.000001<mm>, expected + 0.000001<mm>)

    let private assertAngleClose expected actual =
        Assert.InRange(actual, expected - 0.000000001, expected + 0.000000001)

    [<Fact>]
    let ``source plane and isocentre use the global Z convention`` () =
        Assert.Equal(0.0<mm>, TrueBeamGeometry.sourcePlaneZ)
        Assert.Equal(1000.0<mm>, TrueBeamGeometry.isocentreZ)

    [<Fact>]
    let ``fixed field edges are plus and minus 200 millimetres at isocentre`` () =
        Assert.Equal(200.0<mm>, TrueBeamGeometry.positiveFieldEdgeAtIsocentre)
        Assert.Equal(-200.0<mm>, TrueBeamGeometry.negativeFieldEdgeAtIsocentre)

    [<Fact>]
    let ``source divergence edges are plus and minus 1 point 5 millimetres`` () =
        Assert.Equal(1.5<mm>, TrueBeamGeometry.positiveSourcePlaneEdge)
        Assert.Equal(-1.5<mm>, TrueBeamGeometry.negativeSourcePlaneEdge)

    [<Fact>]
    let ``jaw physical thickness is exactly 78 millimetres`` () =
        Assert.Equal(78.0<mm>, TrueBeamGeometry.jawThickness)

        TrueBeamJaws.placements
        |> List.iter (fun placement -> Assert.Equal(78.0<mm>, placement.BodyDimensions.Thickness))

    [<Fact>]
    let ``X jaw reference Z is exactly 406 millimetres`` () =
        Assert.Equal(406.0<mm>, TrueBeamGeometry.xJawReferenceZ)
        Assert.Equal(406.0<mm>, (placement X Negative).ApertureFaceMidpoint.Z)
        Assert.Equal(406.0<mm>, (placement X Positive).ApertureFaceMidpoint.Z)

    [<Fact>]
    let ``Y jaw aperture face midpoint radius is exactly 319 millimetres`` () =
        Assert.Equal(319.0<mm>, TrueBeamGeometry.yJawReferenceRadius)

        for side in [ Negative; Positive ] do
            let point = (placement Y side).ApertureFaceMidpoint
            let radius = sqrt (point.Y * point.Y + point.Z * point.Z)
            assertClose 319.0<mm> radius

    [<Fact>]
    let ``positive and negative jaw placements are symmetric`` () =
        let negativeX = placement X Negative
        let positiveX = placement X Positive
        let negativeY = placement Y Negative
        let positiveY = placement Y Positive

        assertClose -positiveX.ApertureFaceMidpoint.X negativeX.ApertureFaceMidpoint.X
        assertClose positiveX.ApertureFaceMidpoint.Z negativeX.ApertureFaceMidpoint.Z
        assertAngleClose -positiveX.ApertureFaceAngleRadians negativeX.ApertureFaceAngleRadians
        assertClose -positiveY.ApertureFaceMidpoint.Y negativeY.ApertureFaceMidpoint.Y
        assertClose positiveY.ApertureFaceMidpoint.Z negativeY.ApertureFaceMidpoint.Z
        assertAngleClose -positiveY.ApertureFaceAngleRadians negativeY.ApertureFaceAngleRadians

    [<Fact>]
    let ``positive jaw reference positions and focusing angle are calculated from divergence`` () =
        let positiveX = placement X Positive
        let positiveY = placement Y Positive
        let expectedAngle = atan (198.5 / 1000.0)

        assertClose 82.091<mm> positiveX.ApertureFaceMidpoint.X
        assertClose 406.0<mm> positiveX.ApertureFaceMidpoint.Z
        assertClose 63.5521674253744<mm> positiveY.ApertureFaceMidpoint.Y
        assertClose 312.605377457806<mm> positiveY.ApertureFaceMidpoint.Z
        assertAngleClose expectedAngle positiveX.ApertureFaceAngleRadians
        assertAngleClose expectedAngle positiveY.ApertureFaceAngleRadians

    [<Fact>]
    let ``aperture faces project to source and fixed isocentre field edges`` () =
        for jaw in TrueBeamJaws.placements do
            let referenceCoordinate =
                match jaw.Axis with
                | X -> jaw.ApertureFaceMidpoint.X
                | Y -> jaw.ApertureFaceMidpoint.Y

            let projectedCoordinate z =
                referenceCoordinate
                + tan jaw.ApertureFaceAngleRadians * (z - jaw.ApertureFaceMidpoint.Z)

            let expectedSourceEdge, expectedFieldEdge =
                match jaw.Side with
                | Negative -> TrueBeamGeometry.negativeSourcePlaneEdge, TrueBeamGeometry.negativeFieldEdgeAtIsocentre
                | Positive -> TrueBeamGeometry.positiveSourcePlaneEdge, TrueBeamGeometry.positiveFieldEdgeAtIsocentre

            assertClose expectedSourceEdge (projectedCoordinate TrueBeamGeometry.sourcePlaneZ)
            assertClose expectedFieldEdge (projectedCoordinate TrueBeamGeometry.isocentreZ)

    [<Fact>]
    let ``domain jaw geometry retains millimetre units through the rendering boundary input`` () =
        let requiresMillimetres (_: float<mm>) = ()

        for jaw in TrueBeamJaws.placements do
            requiresMillimetres jaw.ApertureFaceMidpoint.X
            requiresMillimetres jaw.ApertureFaceMidpoint.Y
            requiresMillimetres jaw.ApertureFaceMidpoint.Z
            requiresMillimetres jaw.BodyDimensions.ClosingAxisExtent
            requiresMillimetres jaw.BodyDimensions.CrossAxisExtent
            requiresMillimetres jaw.BodyDimensions.Thickness