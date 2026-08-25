namespace BeamHead.Domain

/// Provides the static 400 x 400 mm TrueBeam jaw geometry.
[<RequireQualifiedAccess>]
module TrueBeamJaws =
    /// The fixed nominal clinical field size at isocentre in both X and Y.
    let fieldSizeAtIsocentre = 400.0<mm>

    /// The unsigned nominal clinical jaw edge at isocentre.
    let nominalFieldEdgeAtIsocentre = fieldSizeAtIsocentre / 2.0

    /// Half of the non-zero 3 x 3 mm TrueBeam source-plane jaw focus.
    let sourcePlaneHalfFocus = 1.5<mm>

    /// The unsigned physical jaw aperture edge at isocentre after the outward focus correction.
    let jawPhysicalEdgeAtIsocentre = nominalFieldEdgeAtIsocentre + sourcePlaneHalfFocus

    /// The total physical jaw separation at isocentre after correcting both jaws.
    let jawPhysicalSeparationAtIsocentre = 2.0 * jawPhysicalEdgeAtIsocentre

    /// The exact physical thickness of every TrueBeam jaw.
    let jawThickness = 78.0<mm>

    /// The fixed aperture-face-midpoint Z reference for each X jaw.
    let xJawReferenceZ = 406.0<mm>

    /// The supported upstream-face trajectory radius for each Y jaw.
    let yJawUpstreamTrajectoryRadius = 280.0<mm>

    /// The aperture-face-midpoint trajectory radius for each Y jaw.
    let yJawReferenceRadius = yJawUpstreamTrajectoryRadius + jawThickness / 2.0

    /// A simplified, non-vendor-exact jaw-body extent perpendicular to jaw motion.
    let simplifiedJawCrossAxisExtent = 200.0<mm>

    /// A simplified, non-vendor-exact jaw-body extent along the jaw closing axis.
    let simplifiedJawClosingAxisExtent = 80.0<mm>

    let private signedEdge side positiveEdge =
        match side with
        | Positive -> positiveEdge
        | Negative -> -positiveEdge

    /// Creates the divergent aperture line for a jaw side.
    let apertureLine side = {
        SourcePlaneEdge = signedEdge side sourcePlaneHalfFocus
        IsocentrePlaneEdge = signedEdge side jawPhysicalEdgeAtIsocentre
    }

    let private bodyDimensions = {
        ClosingAxisExtent = simplifiedJawClosingAxisExtent
        CrossAxisExtent = simplifiedJawCrossAxisExtent
        Thickness = jawThickness
    }

    let private xJaw side : SourceFrameJawPlacement =
        let line = apertureLine side

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
            BodyDimensions = bodyDimensions
        }

    let private positiveRadiusIntersection (radius: float<mm>) (line: DivergentApertureLine) =
        let slope =
            (line.IsocentrePlaneEdge - line.SourcePlaneEdge) / TrueBeamGeometry.isocentreZ

        let linearCoefficient = 2.0 * slope * line.SourcePlaneEdge
        let constant = line.SourcePlaneEdge * line.SourcePlaneEdge - radius * radius

        let discriminant =
            linearCoefficient * linearCoefficient - 4.0 * (1.0 + slope * slope) * constant

        (-linearCoefficient + sqrt discriminant) / (2.0 * (1.0 + slope * slope))

    let private yJaw side : SourceFrameJawPlacement =
        let line = apertureLine side
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
            BodyDimensions = bodyDimensions
        }

    /// The final source-frame rigid poses of the two X jaws and two Y jaws.
    let placements = [ xJaw Negative; xJaw Positive; yJaw Negative; yJaw Positive ]
