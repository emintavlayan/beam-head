namespace BeamHead.Domain

/// Calculates TrueBeam jaw geometry for a validated field at isocentre.
[<RequireQualifiedAccess>]
module TrueBeamJaws =
    /// The exact physical thickness of every TrueBeam jaw.
    let jawThickness = 78.0<mm>

    /// The fixed aperture-face-midpoint Z reference for each X jaw.
    let xJawReferenceZ = 406.0<mm>

    /// The supported upstream-face trajectory radius for each Y jaw.
    let yJawUpstreamTrajectoryRadius = 280.0<mm>

    /// The aperture-face-midpoint trajectory radius for each Y jaw.
    let yJawReferenceRadius = yJawUpstreamTrajectoryRadius + jawThickness / 2.0

    /// The simplified, non-vendor-exact physical Y width of each lower X-jaw body.
    let xJawCrossAxisExtent = 203.0<mm>

    /// The simplified, non-vendor-exact physical X length of each lower X-jaw body.
    let xJawClosingAxisExtent = 110.0<mm>

    /// The simplified, non-vendor-exact physical X width of each upper Y-jaw body.
    let yJawCrossAxisExtent = 156.25<mm>

    /// The simplified, non-vendor-exact physical Y length of each upper Y-jaw body.
    let yJawClosingAxisExtent = 110.0<mm>

    /// Applies the jaw side to an unsigned aperture edge.
    let private signedEdge side positiveEdge =
        match side with
        | Positive -> positiveEdge
        | Negative -> -positiveEdge

    /// Creates a ray from the source centre to the jaw field edge at isocentre.
    let apertureLine field axis side = {
        IsocentrePlaneEdge = signedEdge side (JawField.size axis field / 2.0)
    }

    let private xJawBodyDimensions = {
        ClosingAxisExtent = xJawClosingAxisExtent
        CrossAxisExtent = xJawCrossAxisExtent
        Thickness = jawThickness
    }

    let private yJawBodyDimensions = {
        ClosingAxisExtent = yJawClosingAxisExtent
        CrossAxisExtent = yJawCrossAxisExtent
        Thickness = jawThickness
    }

    /// Places an X jaw on its fixed axial reference plane.
    let private xJaw field side : SourceFrameJawPlacement =
        let line = apertureLine field X side

        let referenceCoordinate =
            DivergentApertureLine.coordinateAt TrueBeamGeometry.isocentreZ xJawReferenceZ line

        {
            Axis = X
            Side = side
            ApertureFaceMidpoint = {
                X = referenceCoordinate
                Y = 0.0<mm>
                Z = xJawReferenceZ
            }
            ApertureFaceAngleRadians = DivergentApertureLine.angleRadians TrueBeamGeometry.isocentreZ line
            BodyDimensions = xJawBodyDimensions
        }

    /// Finds the downstream intersection of a source-centred ray with a trajectory radius.
    let private positiveRadiusIntersection (radius: float<mm>) (line: DivergentApertureLine) =
        let slope = line.IsocentrePlaneEdge / TrueBeamGeometry.isocentreZ
        radius / sqrt (1.0 + slope * slope)

    /// Places a Y jaw on its circular reference trajectory.
    let private yJaw field side : SourceFrameJawPlacement =
        let line = apertureLine field Y side
        let referenceZ = positiveRadiusIntersection yJawReferenceRadius line

        let referenceCoordinate =
            DivergentApertureLine.coordinateAt TrueBeamGeometry.isocentreZ referenceZ line

        {
            Axis = Y
            Side = side
            ApertureFaceMidpoint = {
                X = 0.0<mm>
                Y = referenceCoordinate
                Z = referenceZ
            }
            ApertureFaceAngleRadians = DivergentApertureLine.angleRadians TrueBeamGeometry.isocentreZ line
            BodyDimensions = yJawBodyDimensions
        }

    /// The final source-frame rigid poses of the two X jaws and two Y jaws.
    let placements field = [ xJaw field Negative; xJaw field Positive; yJaw field Negative; yJaw field Positive ]
