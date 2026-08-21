namespace BeamHead.Domain

/// Provides the fixed machine constants for the current TrueBeam geometry slice.
[<RequireQualifiedAccess>]
module TrueBeamGeometry =
    /// The target/source plane Z coordinate and global coordinate-system origin.
    let sourcePlaneZ = 0.0<mm>

    /// The downstream isocentre Z coordinate.
    let isocentreZ = 1000.0<mm>

    /// The fixed full field size at isocentre in both X and Y.
    let fieldSizeAtIsocentre = 400.0<mm>

    /// The positive field edge at isocentre.
    let positiveFieldEdgeAtIsocentre = fieldSizeAtIsocentre / 2.0

    /// The negative field edge at isocentre.
    let negativeFieldEdgeAtIsocentre = -positiveFieldEdgeAtIsocentre

    /// The positive edge of the projected source-plane square.
    let positiveSourcePlaneEdge = 1.5<mm>

    /// The negative edge of the projected source-plane square.
    let negativeSourcePlaneEdge = -positiveSourcePlaneEdge

    /// The exact physical thickness of every TrueBeam jaw.
    let jawThickness = 78.0<mm>

    /// The fixed aperture-face-midpoint Z reference for each X jaw.
    let xJawReferenceZ = 406.0<mm>

    /// The aperture-face-midpoint trajectory radius for each Y jaw.
    let yJawReferenceRadius = 280.0<mm> + jawThickness / 2.0

    /// A simplified, non-vendor-exact jaw-body extent used in both transverse directions.
    /// This affects only the outer rectangular body and can be replaced without changing jaw placement.
    let simplifiedJawOuterBodyExtent = 200.0<mm>

/// Provides the static 400 x 400 mm TrueBeam jaw geometry.
[<RequireQualifiedAccess>]
module TrueBeamJaws =
    let private signedEdge side positiveEdge =
        match side with
        | Positive -> positiveEdge
        | Negative -> -positiveEdge

    /// Creates the divergent aperture line for a jaw side.
    let apertureLine side = {
        SourcePlaneEdge = signedEdge side TrueBeamGeometry.positiveSourcePlaneEdge
        IsocentrePlaneEdge = signedEdge side TrueBeamGeometry.positiveFieldEdgeAtIsocentre
    }

    let private bodyDimensions = {
        ClosingAxisExtent = TrueBeamGeometry.simplifiedJawOuterBodyExtent
        CrossAxisExtent = TrueBeamGeometry.simplifiedJawOuterBodyExtent
        Thickness = TrueBeamGeometry.jawThickness
    }

    let private xJaw side =
        let line = apertureLine side

        let referenceCoordinate =
            DivergentApertureLine.coordinateAt TrueBeamGeometry.isocentreZ TrueBeamGeometry.xJawReferenceZ line

        {
            Axis = X
            Side = side
            ApertureFaceMidpoint = {
                X = referenceCoordinate
                Y = 0.0<mm>
                Z = TrueBeamGeometry.xJawReferenceZ
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

    let private yJaw side =
        let line = apertureLine side

        let referenceZ =
            positiveRadiusIntersection TrueBeamGeometry.yJawReferenceRadius line

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

    /// The final rigid poses of the two X jaws and two Y jaws.
    let placements = [ xJaw Negative; xJaw Positive; yJaw Negative; yJaw Positive ]