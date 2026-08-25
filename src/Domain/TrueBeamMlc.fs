namespace BeamHead.Domain

/// Provides the simplified, fully retracted Millennium 120 MLC geometry used for TSEBT.
[<RequireQualifiedAccess>]
module TrueBeamMlc =
    /// The MLC reference midplane distance downstream from the source.
    let mlcMidplaneZ = 509.0<mm>

    /// The fully retracted positive tip setting projected at isocentre.
    let retractedTipAtIsocentre = 203.6<mm>

    /// The nominal Millennium leaf coverage perpendicular to leaf travel at isocentre.
    let nominalLeafCoverageAtIsocentre = 400.0<mm>

    /// The simplified physical bank envelope projected at isocentre, modelled 5 mm beyond the nominal field on each side.
    let simplifiedBankEnvelopeAtIsocentre = 410.0<mm>

    /// The supported physical MLC thickness along the beam axis.
    let thickness = 67.0<mm>

    /// Half of the supported physical MLC thickness.
    let halfThickness = thickness / 2.0

    /// The upstream MLC surface distance downstream from the source.
    let mlcUpstreamZ = mlcMidplaneZ - halfThickness

    /// The downstream MLC surface distance downstream from the source.
    let mlcDownstreamZ = mlcMidplaneZ + halfThickness

    /// The published radius of the rounded Millennium leaf tip.
    let tipRadius = 80.0<mm>

    /// The published approximate outer tip angle in degrees.
    let outerTipAngleDegrees = 11.3

    /// Projects an isocentre-plane coordinate back to a source-origin plane.
    let projectFromIsocentreToPlane (projectedCoordinate: float<mm>) (planeZ: float<mm>) =
        projectedCoordinate * planeZ / TrueBeamGeometry.isocentreZ

    /// Projects a physical coordinate at a source-origin plane forward to isocentre.
    let projectFromPlaneToIsocentre (coordinate: float<mm>) (planeZ: float<mm>) =
        coordinate * TrueBeamGeometry.isocentreZ / planeZ

    /// The derived positive-bank tip reference coordinate at the physical MLC plane.
    let tipReferenceXAtMlcPlane =
        projectFromIsocentreToPlane retractedTipAtIsocentre mlcMidplaneZ

    /// Derives the physical Y span of the simplified bank envelope at a source-frame Z plane.
    let bankSpanAtPlane planeZ =
        projectFromIsocentreToPlane simplifiedBankEnvelopeAtIsocentre planeZ

    /// Derives the physical Y half-span of the simplified bank envelope at a source-frame Z plane.
    let bankHalfSpanAtPlane planeZ = bankSpanAtPlane planeZ / 2.0

    /// Literature-supported Millennium leaf length projected at isocentre.
    let leafLengthAtIsocentre = 150.0<mm>

    /// Simplified physical body depth behind the tip at the MLC midplane.
    let simplifiedBodyDepthBehindTip =
        projectFromIsocentreToPlane leafLengthAtIsocentre mlcMidplaneZ

    /// The reconstructed local X-Z scattering-face profile, from tip into the positive bank body.
    let localProfile = [
        {
            X = simplifiedBodyDepthBehindTip
            Z = halfThickness
        }
        {
            X = simplifiedBodyDepthBehindTip
            Z = -halfThickness
        }
        { X = 9.18152<mm>; Z = -halfThickness }
        { X = 8.36303<mm>; Z = -33.41531<mm> }
        { X = 7.56497<mm>; Z = -33.16338<mm> }
        { X = 6.80694<mm>; Z = -32.75286<mm> }
        { X = 6.10778<mm>; Z = -32.19685<mm> }
        { X = 5.48493<mm>; Z = -31.51279<mm> }
        { X = 4.95382<mm>; Z = -30.72226<mm> }
        { X = 4.53725<mm>; Z = -29.84063<mm> }
        { X = 1.41883<mm>; Z = -15.0<mm> }
        { X = 0.91991<mm>; Z = -12.1<mm> }
        { X = 0.54683<mm>; Z = -9.2<mm> }
        { X = 0.28941<mm>; Z = -6.3<mm> }
        { X = 0.13143<mm>; Z = -3.4<mm> }
        { X = 0.04883<mm>; Z = -1.8<mm> }
        { X = 0.0<mm>; Z = 0.0<mm> }
        { X = 0.04883<mm>; Z = 1.8<mm> }
        { X = 0.13143<mm>; Z = 3.4<mm> }
        { X = 0.28941<mm>; Z = 6.3<mm> }
        { X = 0.54683<mm>; Z = 9.2<mm> }
        { X = 0.91991<mm>; Z = 12.1<mm> }
        { X = 1.41883<mm>; Z = 15.0<mm> }
        { X = 4.53725<mm>; Z = 29.84063<mm> }
        { X = 4.95382<mm>; Z = 30.72226<mm> }
        { X = 5.48493<mm>; Z = 31.51279<mm> }
        { X = 6.10778<mm>; Z = 32.19685<mm> }
        { X = 6.80694<mm>; Z = 32.75286<mm> }
        { X = 7.56497<mm>; Z = 33.16338<mm> }
        { X = 8.36303<mm>; Z = 33.41531<mm> }
        { X = 9.18152<mm>; Z = halfThickness }
    ]

    /// The local bank profile with its Y half-span derived from each point's source-frame Z.
    let localEnvelopeProfile =
        localProfile
        |> List.map (fun point -> {
            X = point.X
            Z = point.Z
            HalfSpan = bankHalfSpanAtPlane (mlcMidplaneZ + point.Z)
        })

    let private placement side tipReferenceX : SourceFrameMlcBankPlacement = {
        Side = side
        TipReference = {
            X = tipReferenceX
            Y = 0.0<mm>
            Z = mlcMidplaneZ
        }
        EnvelopeProfile = localEnvelopeProfile
    }

    /// The final source-frame placements of the opposing, fully retracted MLC bank envelopes.
    let placements = [
        placement NegativeBank -tipReferenceXAtMlcPlane
        placement PositiveBank tipReferenceXAtMlcPlane
    ]