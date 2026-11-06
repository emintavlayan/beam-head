namespace BeamHead.Domain

/// Composes the supported TrueBeam beam-head components for the selected field.
[<RequireQualifiedAccess>]
module TrueBeam =
    /// The final source-frame placements of the four TrueBeam jaws.
    let jaws field = TrueBeamJaws.placements field

    /// The final source-frame placements of the two simplified Millennium 120 MLC banks.
    let mlcBanks = TrueBeamMlc.placements
