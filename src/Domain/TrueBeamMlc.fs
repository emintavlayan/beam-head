namespace BeamHead.Domain

/// Provides the simplified, fully retracted Millennium 120 MLC geometry used for TSEBT.
[<RequireQualifiedAccess>]
module TrueBeamMlc =
    /// The MLC reference midplane distance downstream from the source.
    let mlcMidplaneZ = 509.0<mm>

    /// The fully retracted positive tip setting projected at isocentre.
    let retractedTipAtIsocentre = 203.6<mm>

    /// The total Millennium bank span perpendicular to leaf travel, projected at isocentre.
    let projectedBankSpanAtIsocentre = 400.0<mm>

    /// The supported physical MLC thickness along the beam axis.
    let thickness = 67.0<mm>

    /// Half of the supported physical MLC thickness.
    let halfThickness = thickness / 2.0

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

    /// The derived physical span of the simplified bank envelope at the MLC plane.
    let bankSpanAtMlcPlane =
        projectFromIsocentreToPlane projectedBankSpanAtIsocentre mlcMidplaneZ

    /// A modelling approximation from the reconstructed Excel closure, providing tungsten behind the scattering face rather than a vendor leaf length.
    let simplifiedBodyDepthBehindTip = 106.89<mm>

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

    let private placement side tipReferenceX : SourceFrameMlcBankPlacement = {
        Side = side
        TipReference = {
            X = tipReferenceX
            Y = 0.0<mm>
            Z = mlcMidplaneZ
        }
        LocalProfile = localProfile
        BankSpan = bankSpanAtMlcPlane
    }

    /// The final source-frame placements of the opposing, fully retracted MLC bank envelopes.
    let placements = [
        placement NegativeBank -tipReferenceXAtMlcPlane
        placement PositiveBank tipReferenceXAtMlcPlane
    ]