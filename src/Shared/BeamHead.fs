namespace BeamHead.Domain

/// Describes the physical dimensions of an axis-aligned cuboid in millimetres.
type CuboidDimensions = {
    Width: float
    Depth: float
    Height: float
}

/// Provides the fixed geometry used by the first BeamHead proof of concept.
[<RequireQualifiedAccess>]
module ProofCube =
    /// The dimensions of the 100 x 100 x 100 mm proof cube.
    let dimensions = {
        Width = 100.0
        Depth = 100.0
        Height = 100.0
    }