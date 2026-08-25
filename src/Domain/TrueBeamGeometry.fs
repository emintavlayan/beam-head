namespace BeamHead.Domain

/// Provides axial reference constants shared by the supported TrueBeam components.
[<RequireQualifiedAccess>]
module TrueBeamGeometry =
    /// The target/source plane Z coordinate and SourceFrame origin.
    let sourcePlaneZ = 0.0<mm>

    /// The downstream isocentre Z coordinate in SourceFrame.
    let isocentreZ = 1000.0<mm>
