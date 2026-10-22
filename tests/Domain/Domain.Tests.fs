namespace BeamHead.Domain.Tests

open BeamHead.Domain
open Xunit

module TrueBeamJawTests =
    let private placement axis side =
        (TrueBeam.jaws JawField.fullSize)
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
    let ``nominal field guide half width is derived from the 400 millimetre clinical field`` () =
        let nominalFieldGuideHalfWidth = (JawField.xSize JawField.fullSize) / 2.0

        Assert.Equal(400.0<mm>, (JawField.xSize JawField.fullSize))
        Assert.Equal(200.0<mm>, nominalFieldGuideHalfWidth)
        Assert.Equal(200.0<mm>, (JawField.xSize JawField.fullSize / 2.0))
        Assert.Equal(-200.0<mm>, -(JawField.xSize JawField.fullSize / 2.0))

    /// Checks that both aperture rays start at the source centre.
    [<Fact>]
    let ``jaw aperture rays pass through the source centre`` () =
        for side in [ Negative; Positive ] do
            let ray = TrueBeamJaws.apertureLine JawField.fullSize X side
            assertClose 0.0<mm> (DivergentApertureLine.coordinateAt TrueBeamGeometry.isocentreZ 0.0<mm> ray)

    /// Checks that the aperture matches the selected field at isocentre.
    [<Fact>]
    let ``400 millimetre field has plus and minus 200 millimetre jaw edges`` () =
        let positive = TrueBeamJaws.apertureLine JawField.fullSize X Positive
        let negative = TrueBeamJaws.apertureLine JawField.fullSize X Negative

        assertClose 200.0<mm> positive.IsocentrePlaneEdge
        assertClose -200.0<mm> negative.IsocentrePlaneEdge
        assertClose 400.0<mm> (positive.IsocentrePlaneEdge - negative.IsocentrePlaneEdge)

    [<Fact>]
    let ``jaw physical thickness is exactly 78 millimetres`` () =
        Assert.Equal(78.0<mm>, TrueBeamJaws.jawThickness)

        (TrueBeam.jaws JawField.fullSize)
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

    /// Checks unrotated axial references without treating them as rotated solid bounds.
    [<Fact>]
    let ``X jaw unrotated axial references are 367 and 445 millimetres`` () =
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

    /// Checks the Y-jaw position after transforming the source-centred geometry.
    [<Fact>]
    let ``Y jaw reference transforms to isocentre frame`` () =
        let sourcePlacement = placement Y Positive
        let transformed = IsocentreFrame.fromSourceJawPlacement sourcePlacement

        assertClose -687.194764454597<mm> transformed.ApertureFaceMidpoint.Z
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

    /// Checks jaw positions and tilt against source-centred divergence.
    [<Fact>]
    let ``positive jaw reference positions and tilt are calculated from divergence`` () =
        let positiveX = placement X Positive
        let positiveY = placement Y Positive
        let expectedAngle = atan 0.2

        assertClose 81.2<mm> positiveX.ApertureFaceMidpoint.X
        assertClose 406.0<mm> positiveX.ApertureFaceMidpoint.Z
        assertClose 62.5610471090807<mm> positiveY.ApertureFaceMidpoint.Y
        assertClose 312.805235545404<mm> positiveY.ApertureFaceMidpoint.Z
        assertAngleClose expectedAngle positiveX.ApertureFaceAngleRadians
        assertAngleClose expectedAngle positiveY.ApertureFaceAngleRadians

    /// Checks that every placed aperture face projects to the source and selected field edge.
    [<Fact>]
    let ``aperture faces project to source centre and selected isocentre edges`` () =
        for jaw in (TrueBeam.jaws JawField.fullSize) do
            let referenceCoordinate =
                match jaw.Axis with
                | X -> jaw.ApertureFaceMidpoint.X
                | Y -> jaw.ApertureFaceMidpoint.Y

            /// Projects the placed jaw face to a source-frame axial position.
            let projectedCoordinate z =
                referenceCoordinate
                + tan jaw.ApertureFaceAngleRadians * (z - jaw.ApertureFaceMidpoint.Z)

            let expectedFieldEdge =
                match jaw.Side with
                | Negative -> -200.0<mm>
                | Positive -> 200.0<mm>

            assertClose 0.0<mm> (projectedCoordinate TrueBeamGeometry.sourcePlaneZ)
            assertClose expectedFieldEdge (projectedCoordinate TrueBeamGeometry.isocentreZ)

    [<Fact>]
    let ``domain jaw geometry retains millimetre units through the rendering boundary input`` () =
        let requiresMillimetres (_: float<mm>) = ()

        for jaw in (TrueBeam.jaws JawField.fullSize) do
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
    let ``delivery tip setting is 201 millimetres at isocentre`` () =
        Assert.Equal(201.0<mm>, TrueBeamMlc.deliveryTipSettingAtIsocentre)

    [<Fact>]
    let ``nominal leaf coverage and simplified bank envelope remain separate`` () =
        Assert.Equal(400.0<mm>, TrueBeamMlc.nominalLeafCoverageAtIsocentre)
        Assert.Equal(410.0<mm>, TrueBeamMlc.simplifiedBankEnvelopeAtIsocentre)

    [<Fact>]
    let ``physical tip reference is derived from rounded profile tangent`` () =
        let projectedBackToIsocentre =
            TrueBeamMlc.projectFromPlaneToIsocentre TrueBeamMlc.tipReferenceXAtMlcPlane TrueBeamMlc.mlcMidplaneZ

        assertClose 103.90517<mm> TrueBeamMlc.tipReferenceXAtMlcPlane
        assertClose 204.1358939096267<mm> projectedBackToIsocentre

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

    /// Checks declared metadata separately from geometric conformance of the reconstructed profile.
    [<Fact>]
    let ``declared rounded tip radius and outer angle are retained`` () =
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
    let ``simplified body depth is derived from the projected published leaf length`` () =
        let profileRearExtent = TrueBeamMlc.localProfile |> List.maxBy _.X |> _.X

        let projectedBackToIsocentre =
            TrueBeamMlc.projectFromPlaneToIsocentre TrueBeamMlc.simplifiedBodyDepthBehindTip TrueBeamMlc.mlcMidplaneZ

        Assert.Equal(150.0<mm>, TrueBeamMlc.leafLengthAtIsocentre)
        assertClose 76.35<mm> TrueBeamMlc.simplifiedBodyDepthBehindTip
        assertClose 76.35<mm> profileRearExtent
        assertClose 150.0<mm> projectedBackToIsocentre

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
    let ``MLC bank references match the tangent compensated physical placement`` () =
        let negative = placement NegativeBank
        let positive = placement PositiveBank

        assertClose -103.90517<mm> negative.TipReference.X
        assertClose 103.90517<mm> positive.TipReference.X
        Assert.Equal(-TrueBeamMlc.tipReferenceXAtMlcPlane, negative.TipReference.X)
        Assert.Equal(TrueBeamMlc.tipReferenceXAtMlcPlane, positive.TipReference.X)

        for bank in [ negative; positive ] do
            Assert.Equal(0.0<mm>, bank.TipReference.Y)
            Assert.Equal(509.0<mm>, bank.TipReference.Z)

    [<Fact>]
    let ``MLC rounded profiles are tangent at plus and minus 201 millimetres`` () =
        for bank in TrueBeam.mlcBanks do
            let sign = if bank.Side = PositiveBank then 1.0 else -1.0
            let tangent =
                bank.EnvelopeProfile
                |> List.map (fun point ->
                    (sign * bank.TipReference.X + point.X)
                    * TrueBeamGeometry.isocentreZ
                    / (bank.TipReference.Z + point.Z))
                |> List.min

            assertClose TrueBeamMlc.roundedTipTangentAtIsocentre tangent

    /// Checks that the reported tangent setting clears the maximum jaw field.
    [<Fact>]
    let ``rounded tip setting is one millimetre outside the maximum jaw field`` () =
        let clearance =
            TrueBeamMlc.roundedTipTangentAtIsocentre
            - TrueBeamMlc.maximumJawFieldHalfWidthAtIsocentre

        Assert.Equal(200.0<mm>, TrueBeamMlc.maximumJawFieldHalfWidthAtIsocentre)
        Assert.Equal(201.0<mm>, TrueBeamMlc.deliveryTipSettingAtIsocentre)
        Assert.Equal(201.0<mm>, TrueBeamMlc.roundedTipTangentAtIsocentre)
        assertClose 204.1358939096267<mm> TrueBeamMlc.physicalTipReferenceAtIsocentre
        assertClose 1.0<mm> clearance

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
module JawFieldTests =
    /// Extracts a successful result or fails the test with its error.
    let private valid result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    /// Checks that both supported limits are accepted independently.
    [<Fact>]
    let ``field dimensions support 5 to 400 millimetres`` () =
        let field = JawField.create 5.0<mm> 400.0<mm> |> valid
        Assert.Equal(5.0<mm>, JawField.xSize field)
        Assert.Equal(400.0<mm>, JawField.ySize field)

    /// Checks rejection of invalid dimensions before geometry generation.
    [<Fact>]
    let ``invalid or fractional millimetre dimensions return errors`` () =
        for value in [ 0.0; 4.0; 400.1; 5.5; System.Double.NaN; System.Double.PositiveInfinity ] do
            Assert.True(JawField.create (value * 1.0<mm>) 100.0<mm> |> Result.isError)
            Assert.True(JawField.create 100.0<mm> (value * 1.0<mm>) |> Result.isError)

    /// Checks input rounding at a valid slider step.
    [<Fact>]
    let ``floating point slider values normalize to exact millimetres`` () =
        let field = JawField.create ((0.1 + 0.2) * 100.0<mm>) 5.0<mm> |> valid
        Assert.Equal(30.0<mm>, JawField.xSize field)

    /// Checks all jaw faces against the requested rectangular or square field.
    [<Fact>]
    let ``selected jaw faces project to the correct independent field edges`` () =
        let sizes = [ 5.0<mm>, 5.0<mm>; 5.0<mm>, 400.0<mm>; 360.0<mm>, 200.0<mm> ]
        let squares = FieldControls.presets |> List.map (fun size -> size, size)
        for x, y in sizes @ squares do
            let field = JawField.create x y |> valid
            for jaw in TrueBeam.jaws field do
                let coordinate =
                    match jaw.Axis with
                    | X -> jaw.ApertureFaceMidpoint.X
                    | Y -> jaw.ApertureFaceMidpoint.Y
                let sign = if jaw.Side = Positive then 1.0 else -1.0
                let expectedEdge = sign * JawField.size jaw.Axis field / 2.0
                let slope = tan jaw.ApertureFaceAngleRadians
                let atSource = coordinate - slope * jaw.ApertureFaceMidpoint.Z
                let atIsocentre = coordinate + slope * (1000.0<mm> - jaw.ApertureFaceMidpoint.Z)
                Assert.InRange(abs atSource, 0.0<mm>, 0.000001<mm>)
                Assert.InRange(abs (atIsocentre - expectedEdge), 0.0<mm>, 0.000001<mm>)

    /// Checks that changing Y does not move the X jaws.
    [<Fact>]
    let ``changing one field axis leaves the other jaw pair unchanged`` () =
        let first = JawField.create 360.0<mm> 200.0<mm> |> valid
        let second = JawField.create 360.0<mm> 5.0<mm> |> valid
        let firstX = TrueBeam.jaws first |> List.filter (fun jaw -> jaw.Axis = X)
        let secondX = TrueBeam.jaws second |> List.filter (fun jaw -> jaw.Axis = X)
        Assert.True((firstX = secondX))

module FieldControlsTests =
    /// Checks that linked controls follow X down to the smallest field.
    [<Fact>]
    let ``square field mirrors X into Y at the minimum size`` () =
        let model = FieldControls.init () |> FieldControls.setSliderSize X "0.5"
        Assert.True(model.SquareField)
        Assert.Equal(5.0<mm>, JawField.xSize model.Field)
        Assert.Equal(5.0<mm>, JawField.ySize model.Field)
        Assert.Equal("0.5", model.YInput)

    /// Checks that disabled Y edits cannot change linked dimensions.
    [<Fact>]
    let ``Y edits are ignored while square field is checked`` () =
        let initial = FieldControls.init () |> FieldControls.selectPreset X 360.0<mm>
        let edited = initial |> FieldControls.editSize Y "20.0" |> FieldControls.selectPreset Y 100.0<mm>
        Assert.Equal(initial, edited)

    /// Checks independent edits followed by relinking Y to the current X.
    [<Fact>]
    let ``square checkbox copies X into Y after rectangular editing`` () =
        let rectangular =
            FieldControls.init ()
            |> FieldControls.setSquareField false
            |> FieldControls.selectPreset X 360.0<mm>
            |> FieldControls.editSize Y "20.0"
        Assert.Equal(360.0<mm>, JawField.xSize rectangular.Field)
        Assert.Equal(200.0<mm>, JawField.ySize rectangular.Field)
        let square = FieldControls.setSquareField true rectangular
        Assert.Equal(360.0<mm>, JawField.ySize square.Field)
        Assert.Equal("36.0", square.YInput)

    /// Checks that invalid numeric edits cannot replace valid export geometry.
    [<Fact>]
    let ``invalid input keeps last valid field and disables export until corrected`` () =
        let valid = FieldControls.init () |> FieldControls.selectPreset X 360.0<mm>
        for input in [ ""; "0.4"; "40.1"; "NaN"; "0.55"; "abc" ] do
            let invalid = FieldControls.editSize X input valid
            Assert.Equal(valid.Field, invalid.Field)
            Assert.False(FieldControls.canExport invalid)
            let corrected = FieldControls.editSize X "0.5" invalid
            Assert.True(FieldControls.canExport corrected)

    /// Checks that presets synchronize numeric values and export names.
    [<Fact>]
    let ``presets produce matching field dimensions and STL filenames`` () =
        for size in FieldControls.presets do
            let model = FieldControls.init () |> FieldControls.selectPreset X size
            let millimetres = int (float size)
            Assert.Equal(size, JawField.xSize model.Field)
            Assert.Equal(size, JawField.ySize model.Field)
            Assert.Equal(model.XInput, model.YInput)
            Assert.Equal(sprintf "truebeam-%dx%dmm-mlc201mm.stl" millimetres millimetres, FieldControls.exportFileName model.Field)

    /// Checks the full-width filename for the minimum rectangular field.
    [<Fact>]
    let ``rectangular filename uses both selected dimensions`` () =
        let model =
            FieldControls.init ()
            |> FieldControls.setSquareField false
            |> FieldControls.setSliderSize X "0.5"
            |> FieldControls.setSliderSize Y "36.0"
        Assert.Equal("truebeam-5x360mm-mlc201mm.stl", FieldControls.exportFileName model.Field)

    /// Checks that pending errors on either independent axis prevent export.
    [<Fact>]
    let ``correcting X does not hide an invalid independent Y input`` () =
        let model =
            FieldControls.init ()
            |> FieldControls.setSquareField false
            |> FieldControls.editSize Y ""
            |> FieldControls.selectPreset X 360.0<mm>
        Assert.False(FieldControls.canExport model)
        Assert.True(model.YError.IsSome)

    /// Checks decimal formatting when numeric editing ends.
    [<Fact>]
    let ``numeric input formats to one decimal on commit`` () =
        let model = FieldControls.init () |> FieldControls.editSize X "36" |> FieldControls.commitSize X
        Assert.Equal("36.0", model.XInput)
        Assert.Equal("36.0", model.YInput)
