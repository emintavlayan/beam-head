namespace BeamHead.Domain

/// Represents physical lengths measured in millimetres.
[<Measure>]
type mm

/// Describes the physical dimensions of an axis-aligned cuboid.
type CuboidDimensions = {
    Width: float<mm>
    Depth: float<mm>
    Height: float<mm>
}

/// Provides the fixed geometry used by the first BeamHead proof of concept.
[<RequireQualifiedAccess>]
module ProofCube =
    /// The dimensions of the 100 x 100 x 100 mm proof cube.
    let dimensions = {
        Width = 100.0<mm>
        Depth = 100.0<mm>
        Height = 100.0<mm>
    }