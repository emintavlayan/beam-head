namespace BeamHead.Domain.Tests

open BeamHead.Domain
open Xunit

module TrueBeamJawTests =
    let private placement axis side =
        TrueBeam.jaws
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
    let ``source transforms to minus 1000 millimetres in isocentre frame`` () =
        let source = IsocentreFrame.fromSourcePoint SourceFrame.source

        Assert.Equal(-1000.0<mm>, source.Z)

    [<Fact>]
    let ``isocentre transforms to zero in isocentre frame`` () =
        let isocentre = IsocentreFrame.fromSourcePoint SourceFrame.isocentre

        Assert.Equal(0.0<mm>, isocentre.Z)

    [<Fact>]
    let ``frame transformation leaves X and Y coordinates unchanged`` () =
        let sourcePoint: SourceFramePoint = {
            X = 123.0<mm>
            Y = -456.0<mm>
            Z = 789.0<mm>
        }

        let transformed = IsocentreFrame.fromSourcePoint sourcePoint

        Assert.Equal(sourcePoint.X, transformed.X)
        Assert.Equal(sourcePoint.Y, transformed.Y)

    [<Fact>]
    let ``viewer display mirrors Z without changing X or Y`` () =
        let realPoint: IsocentreFramePoint = {
            X = 82.091<mm>
            Y = -12.5<mm>
            Z = -594.0<mm>
        }

        let displayPoint = ViewerDisplay.fromIsocentrePoint realPoint

        Assert.Equal(realPoint.X, displayPoint.X)
        Assert.Equal(realPoint.Y, displayPoint.Y)
        Assert.Equal(594.0<mm>, displayPoint.Z)

    [<Fact>]
    let ``viewer display source is derived at positive 1000 millimetres for BEV`` () =
        let displaySource = IsocentreFrame.source |> ViewerDisplay.fromIsocentrePoint

        Assert.Equal(0.0<mm>, displaySource.X)
        Assert.Equal(0.0<mm>, displaySource.Y)
        Assert.Equal(1000.0<mm>, displaySource.Z)

    [<Fact>]
    let ``nominal jaw setting has plus and minus 200 millimetre edges at isocentre`` () =
        Assert.Equal(400.0<mm>, TrueBeamJaws.fieldSizeAtIsocentre)
        Assert.Equal(200.0<mm>, TrueBeamJaws.nominalFieldEdgeAtIsocentre)
        Assert.Equal(-200.0<mm>, -TrueBeamJaws.nominalFieldEdgeAtIsocentre)

    [<Fact>]
    let ``source focus has plus and minus 1 point 5 millimetre edges`` () =
        Assert.Equal(1.5<mm>, TrueBeamJaws.sourcePlaneHalfFocus)
        Assert.Equal(1.5<mm>, (TrueBeamJaws.apertureLine Positive).SourcePlaneEdge)
        Assert.Equal(-1.5<mm>, (TrueBeamJaws.apertureLine Negative).SourcePlaneEdge)

    [<Fact>]
    let ``physical jaw aperture has plus and minus 201 point 5 millimetre edges at isocentre`` () =
        Assert.Equal(201.5<mm>, TrueBeamJaws.jawPhysicalEdgeAtIsocentre)

        Assert.Equal(
            TrueBeamJaws.nominalFieldEdgeAtIsocentre + TrueBeamJaws.sourcePlaneHalfFocus,
            TrueBeamJaws.jawPhysicalEdgeAtIsocentre
        )

        Assert.Equal(201.5<mm>, (TrueBeamJaws.apertureLine Positive).IsocentrePlaneEdge)
        Assert.Equal(-201.5<mm>, (TrueBeamJaws.apertureLine Negative).IsocentrePlaneEdge)

    [<Fact>]
    let ``nominal 400 millimetre setting produces 403 millimetre physical jaw separation`` () =
        Assert.Equal(403.0<mm>, TrueBeamJaws.jawPhysicalSeparationAtIsocentre)

    [<Fact>]
    let ``jaw physical thickness is exactly 78 millimetres`` () =
        Assert.Equal(78.0<mm>, TrueBeamJaws.jawThickness)

        TrueBeam.jaws
        |> List.iter (fun placement -> Assert.Equal(78.0<mm>, placement.BodyDimensions.Thickness))

    [<Fact>]
    let ``lower X jaws use the simplified axis-specific body extents`` () =
        Assert.Equal(203.0<mm>, TrueBeamJaws.xJawCrossAxisExtent)
        Assert.Equal(110.0<mm>, TrueBeamJaws.xJawClosingAxisExtent)

        for side in [ Negative; Positive ] do
            let dimensions = (placement X side).BodyDimensions
            Assert.Equal(203.0<mm>, dimensions.CrossAxisExtent)
            Assert.Equal(110.0<mm>, dimensions.ClosingAxisExtent)

    [<Fact>]
    let ``upper Y jaws use the simplified axis-specific body extents`` () =
        Assert.Equal(156.25<mm>, TrueBeamJaws.yJawCrossAxisExtent)
        Assert.Equal(110.0<mm>, TrueBeamJaws.yJawClosingAxisExtent)

        for side in [ Negative; Positive ] do
            let dimensions = (placement Y side).BodyDimensions
            Assert.Equal(156.25<mm>, dimensions.CrossAxisExtent)
            Assert.Equal(110.0<mm>, dimensions.ClosingAxisExtent)

    [<Fact>]
    let ``X jaw reference Z is exactly 406 millimetres`` () =
        Assert.Equal(406.0<mm>, TrueBeamJaws.xJawReferenceZ)
        Assert.Equal(406.0<mm>, (placement X Negative).ApertureFaceMidpoint.Z)
        Assert.Equal(406.0<mm>, (placement X Positive).ApertureFaceMidpoint.Z)

    [<Fact>]
    let ``X jaw reference transforms to minus 594 millimetres`` () =
        let transformed = placement X Positive |> IsocentreFrame.fromSourceJawPlacement

        Assert.Equal(-594.0<mm>, transformed.ApertureFaceMidpoint.Z)

    [<Fact>]
    let ``X jaw midplane and half thickness give supported upstream and downstream surfaces`` () =
        let halfThickness = TrueBeamJaws.jawThickness / 2.0
        let upstreamSurface = TrueBeamJaws.xJawReferenceZ - halfThickness
        let downstreamSurface = TrueBeamJaws.xJawReferenceZ + halfThickness

        assertClose 367.0<mm> upstreamSurface
        assertClose 445.0<mm> downstreamSurface

    [<Fact>]
    let ``Y jaw aperture face midpoint radius is exactly 319 millimetres`` () =
        Assert.Equal(319.0<mm>, TrueBeamJaws.yJawReferenceRadius)

        for side in [ Negative; Positive ] do
            let point = (placement Y side).ApertureFaceMidpoint
            let radius = sqrt (point.Y * point.Y + point.Z * point.Z)
            assertClose 319.0<mm> radius

    [<Fact>]
    let ``Y jaw midpoint radius and half thickness give supported upstream trajectory radius`` () =
        let upstreamTrajectoryRadius =
            TrueBeamJaws.yJawReferenceRadius - TrueBeamJaws.jawThickness / 2.0

        Assert.Equal(280.0<mm>, TrueBeamJaws.yJawUpstreamTrajectoryRadius)
        assertClose 280.0<mm> upstreamTrajectoryRadius

    [<Fact>]
    let ``Y jaw reference transforms to isocentre frame`` () =
        let sourcePlacement = placement Y Positive
        let transformed = IsocentreFrame.fromSourceJawPlacement sourcePlacement

        assertClose -687.486551165241<mm> transformed.ApertureFaceMidpoint.Z
        Assert.Equal(sourcePlacement.ApertureFaceMidpoint.X, transformed.ApertureFaceMidpoint.X)
        Assert.Equal(sourcePlacement.ApertureFaceMidpoint.Y, transformed.ApertureFaceMidpoint.Y)

    [<Fact>]
    let ``jaw frame transformation preserves orientation and body dimensions`` () =
        let sourcePlacement = placement X Positive
        let transformed = IsocentreFrame.fromSourceJawPlacement sourcePlacement

        Assert.Equal(sourcePlacement.Axis, transformed.Axis)
        Assert.Equal(sourcePlacement.Side, transformed.Side)
        Assert.Equal(sourcePlacement.ApertureFaceAngleRadians, transformed.ApertureFaceAngleRadians)
        Assert.Equal(sourcePlacement.BodyDimensions, transformed.BodyDimensions)

    [<Fact>]
    let ``positive and negative jaw placements are symmetric`` () =
        let negativeX = placement X Negative
        let positiveX = placement X Positive
        let negativeY = placement Y Negative
        let positiveY = placement Y Positive

        Assert.Equal(-positiveX.ApertureFaceMidpoint.X, negativeX.ApertureFaceMidpoint.X)
        Assert.Equal(positiveX.ApertureFaceMidpoint.Z, negativeX.ApertureFaceMidpoint.Z)
        Assert.Equal(-positiveX.ApertureFaceAngleRadians, negativeX.ApertureFaceAngleRadians)
        Assert.Equal(-positiveY.ApertureFaceMidpoint.Y, negativeY.ApertureFaceMidpoint.Y)
        Assert.Equal(positiveY.ApertureFaceMidpoint.Z, negativeY.ApertureFaceMidpoint.Z)
        Assert.Equal(-positiveY.ApertureFaceAngleRadians, negativeY.ApertureFaceAngleRadians)

    [<Fact>]
    let ``positive jaw reference positions and focusing angle are calculated from divergence`` () =
        let positiveX = placement X Positive
        let positiveY = placement Y Positive
        let expectedAngle = atan 0.2

        assertClose 82.7<mm> positiveX.ApertureFaceMidpoint.X
        assertClose 406.0<mm> positiveX.ApertureFaceMidpoint.Z
        assertClose 64.0026897669517<mm> positiveY.ApertureFaceMidpoint.Y
        assertClose 312.513448834759<mm> positiveY.ApertureFaceMidpoint.Z
        assertAngleClose expectedAngle positiveX.ApertureFaceAngleRadians
        assertAngleClose expectedAngle positiveY.ApertureFaceAngleRadians

    [<Fact>]
    let ``aperture faces project to source focus and corrected physical isocentre edges`` () =
        for jaw in TrueBeam.jaws do
            let referenceCoordinate =
                match jaw.Axis with
                | X -> jaw.ApertureFaceMidpoint.X
                | Y -> jaw.ApertureFaceMidpoint.Y

            let projectedCoordinate z =
                referenceCoordinate
                + tan jaw.ApertureFaceAngleRadians * (z - jaw.ApertureFaceMidpoint.Z)

            let expectedSourceEdge, expectedPhysicalEdge =
                match jaw.Side with
                | Negative -> -TrueBeamJaws.sourcePlaneHalfFocus, -TrueBeamJaws.jawPhysicalEdgeAtIsocentre
                | Positive -> TrueBeamJaws.sourcePlaneHalfFocus, TrueBeamJaws.jawPhysicalEdgeAtIsocentre

            assertClose expectedSourceEdge (projectedCoordinate TrueBeamGeometry.sourcePlaneZ)
            assertClose expectedPhysicalEdge (projectedCoordinate TrueBeamGeometry.isocentreZ)

    [<Fact>]
    let ``domain jaw geometry retains millimetre units through the rendering boundary input`` () =
        let requiresMillimetres (_: float<mm>) = ()

        for jaw in TrueBeam.jaws do
            requiresMillimetres jaw.ApertureFaceMidpoint.X
            requiresMillimetres jaw.ApertureFaceMidpoint.Y
            requiresMillimetres jaw.ApertureFaceMidpoint.Z
            requiresMillimetres jaw.BodyDimensions.ClosingAxisExtent
            requiresMillimetres jaw.BodyDimensions.CrossAxisExtent
            requiresMillimetres jaw.BodyDimensions.Thickness

module TrueBeamMlcTests =
    let private placement side =
        TrueBeam.mlcBanks |> List.find (fun placement -> placement.Side = side)

    let private assertClose (expected: float<mm>) (actual: float<mm>) =
        Assert.InRange(actual, expected - 0.000001<mm>, expected + 0.000001<mm>)

    [<Fact>]
    let ``MLC midplane is 509 millimetres from source`` () =
        Assert.Equal(509.0<mm>, TrueBeamMlc.mlcMidplaneZ)

        TrueBeam.mlcBanks
        |> List.iter (fun bank -> Assert.Equal(509.0<mm>, bank.TipReference.Z))

    [<Fact>]
    let ``retracted tip setting is 203 point 6 millimetres at isocentre`` () =
        Assert.Equal(203.6<mm>, TrueBeamMlc.retractedTipAtIsocentre)

    [<Fact>]
    let ``nominal leaf coverage and simplified bank envelope remain separate`` () =
        Assert.Equal(400.0<mm>, TrueBeamMlc.nominalLeafCoverageAtIsocentre)
        Assert.Equal(410.0<mm>, TrueBeamMlc.simplifiedBankEnvelopeAtIsocentre)

    [<Fact>]
    let ``tip reference at MLC plane is derived from its isocentre setting`` () =
        let projectedBackToIsocentre =
            TrueBeamMlc.projectFromPlaneToIsocentre TrueBeamMlc.tipReferenceXAtMlcPlane TrueBeamMlc.mlcMidplaneZ

        assertClose 103.6324<mm> TrueBeamMlc.tipReferenceXAtMlcPlane
        assertClose 203.6<mm> projectedBackToIsocentre

    [<Fact>]
    let ``bank span follows source projection through the MLC thickness`` () =
        let expectedSpans = [
            TrueBeamMlc.mlcUpstreamZ, 194.955<mm>
            TrueBeamMlc.mlcMidplaneZ, 208.690<mm>
            TrueBeamMlc.mlcDownstreamZ, 222.425<mm>
        ]

        for planeZ, expectedSpan in expectedSpans do
            let physicalSpan = TrueBeamMlc.bankSpanAtPlane planeZ

            let projectedToIsocentre =
                TrueBeamMlc.projectFromPlaneToIsocentre physicalSpan planeZ

            assertClose expectedSpan physicalSpan
            assertClose 410.0<mm> projectedToIsocentre

    [<Fact>]
    let ``bank span increases monotonically from upstream to downstream`` () =
        let upstream = TrueBeamMlc.bankSpanAtPlane TrueBeamMlc.mlcUpstreamZ
        let midplane = TrueBeamMlc.bankSpanAtPlane TrueBeamMlc.mlcMidplaneZ
        let downstream = TrueBeamMlc.bankSpanAtPlane TrueBeamMlc.mlcDownstreamZ

        Assert.True(upstream < midplane)
        Assert.True(midplane < downstream)

    [<Fact>]
    let ``MLC placements transform to minus 491 millimetres in isocentre frame`` () =
        for bank in TrueBeam.mlcBanks do
            let transformed = IsocentreFrame.fromSourceMlcBankPlacement bank

            Assert.Equal(-491.0<mm>, transformed.TipReference.Z)

    [<Fact>]
    let ``supported MLC thickness is 67 millimetres with 33 point 5 millimetre half thickness`` () =
        Assert.Equal(67.0<mm>, TrueBeamMlc.thickness)
        Assert.Equal(33.5<mm>, TrueBeamMlc.halfThickness)
        Assert.Equal(475.5<mm>, TrueBeamMlc.mlcUpstreamZ)
        Assert.Equal(542.5<mm>, TrueBeamMlc.mlcDownstreamZ)

        let profileMinimum = TrueBeamMlc.localProfile |> List.minBy _.Z
        let profileMaximum = TrueBeamMlc.localProfile |> List.maxBy _.Z

        Assert.Equal(-33.5<mm>, profileMinimum.Z)
        Assert.Equal(33.5<mm>, profileMaximum.Z)

    [<Fact>]
    let ``supported rounded tip parameters are retained`` () =
        Assert.Equal(80.0<mm>, TrueBeamMlc.tipRadius)
        Assert.Equal(11.3, TrueBeamMlc.outerTipAngleDegrees)

    [<Fact>]
    let ``reconstructed profile contains the local tip reference and all supplied points`` () =
        let hasTip =
            TrueBeamMlc.localProfile
            |> List.exists (fun point -> point.X = 0.0<mm> && point.Z = 0.0<mm>)

        Assert.True(hasTip)
        Assert.Equal(31, TrueBeamMlc.localProfile.Length)

    [<Fact>]
    let ``simplified body depth behind tip remains an explicit 106 point 89 millimetre approximation`` () =
        let profileRearExtent = TrueBeamMlc.localProfile |> List.maxBy _.X |> _.X

        Assert.Equal(106.89<mm>, TrueBeamMlc.simplifiedBodyDepthBehindTip)
        Assert.Equal(106.89<mm>, profileRearExtent)

    [<Fact>]
    let ``profile rear points use the body depth approximation and half thickness`` () =
        let upperRearPoint = TrueBeamMlc.localProfile |> List.item 0
        let lowerRearPoint = TrueBeamMlc.localProfile |> List.item 1

        Assert.Equal(TrueBeamMlc.simplifiedBodyDepthBehindTip, upperRearPoint.X)
        Assert.Equal(TrueBeamMlc.halfThickness, upperRearPoint.Z)
        Assert.Equal(TrueBeamMlc.simplifiedBodyDepthBehindTip, lowerRearPoint.X)
        Assert.Equal(-TrueBeamMlc.halfThickness, lowerRearPoint.Z)

    [<Fact>]
    let ``bank envelope uses a source-projected Y half-span at every X-Z profile point`` () =
        for bank in TrueBeam.mlcBanks do
            Assert.Equal(TrueBeamMlc.localProfile.Length, bank.EnvelopeProfile.Length)

            (TrueBeamMlc.localProfile, bank.EnvelopeProfile)
            ||> List.iter2 (fun profilePoint envelopePoint ->
                let sourceZ = TrueBeamMlc.mlcMidplaneZ + profilePoint.Z
                let expectedHalfSpan = TrueBeamMlc.bankHalfSpanAtPlane sourceZ

                Assert.Equal(profilePoint.X, envelopePoint.X)
                Assert.Equal(profilePoint.Z, envelopePoint.Z)
                assertClose expectedHalfSpan envelopePoint.HalfSpan)

            let distinctHalfSpans = bank.EnvelopeProfile |> List.map _.HalfSpan |> List.distinct

            Assert.True(distinctHalfSpans.Length > 1)

    [<Fact>]
    let ``positive and negative MLC bank placements are symmetric`` () =
        let negative = placement NegativeBank
        let positive = placement PositiveBank

        Assert.Equal(-positive.TipReference.X, negative.TipReference.X)
        Assert.Equal(positive.TipReference.Y, negative.TipReference.Y)
        Assert.Equal(positive.TipReference.Z, negative.TipReference.Z)
        Assert.True(positive.EnvelopeProfile = negative.EnvelopeProfile)

    [<Fact>]
    let ``MLC bank tip references match the existing TOPAS positions`` () =
        let negative = placement NegativeBank
        let positive = placement PositiveBank

        assertClose -103.6324<mm> negative.TipReference.X
        assertClose 103.6324<mm> positive.TipReference.X
        Assert.Equal(-TrueBeamMlc.tipReferenceXAtMlcPlane, negative.TipReference.X)
        Assert.Equal(TrueBeamMlc.tipReferenceXAtMlcPlane, positive.TipReference.X)

        for bank in [ negative; positive ] do
            Assert.Equal(0.0<mm>, bank.TipReference.Y)
            Assert.Equal(509.0<mm>, bank.TipReference.Z)

    [<Fact>]
    let ``MLC bank tips project to plus and minus 203 point 6 millimetres at isocentre`` () =
        let negativeProjection =
            TrueBeamMlc.projectFromPlaneToIsocentre (placement NegativeBank).TipReference.X TrueBeamMlc.mlcMidplaneZ

        let positiveProjection =
            TrueBeamMlc.projectFromPlaneToIsocentre (placement PositiveBank).TipReference.X TrueBeamMlc.mlcMidplaneZ

        assertClose -203.6<mm> negativeProjection
        assertClose 203.6<mm> positiveProjection

    [<Fact>]
    let ``retracted MLC tips are 2 point 1 millimetres outside the physical jaw edges`` () =
        let clearance =
            TrueBeamMlc.retractedTipAtIsocentre - TrueBeamJaws.jawPhysicalEdgeAtIsocentre

        Assert.Equal(201.5<mm>, TrueBeamJaws.jawPhysicalEdgeAtIsocentre)
        Assert.Equal(203.6<mm>, TrueBeamMlc.retractedTipAtIsocentre)
        assertClose 2.1<mm> clearance

    [<Fact>]
    let ``MLC frame transformation changes only Z`` () =
        let sourcePlacement = placement PositiveBank
        let transformed = IsocentreFrame.fromSourceMlcBankPlacement sourcePlacement

        Assert.Equal(sourcePlacement.Side, transformed.Side)
        Assert.Equal(sourcePlacement.TipReference.X, transformed.TipReference.X)
        Assert.Equal(sourcePlacement.TipReference.Y, transformed.TipReference.Y)
        Assert.Equal(sourcePlacement.TipReference.Z - 1000.0<mm>, transformed.TipReference.Z)
        Assert.True(sourcePlacement.EnvelopeProfile = transformed.EnvelopeProfile)

    [<Fact>]
    let ``domain MLC geometry retains millimetre units through the rendering boundary input`` () =
        let requiresMillimetres (_: float<mm>) = ()

        for bank in TrueBeam.mlcBanks do
            requiresMillimetres bank.TipReference.X
            requiresMillimetres bank.TipReference.Y
            requiresMillimetres bank.TipReference.Z

            for point in bank.EnvelopeProfile do
                requiresMillimetres point.X
                requiresMillimetres point.Z
                requiresMillimetres point.HalfSpan