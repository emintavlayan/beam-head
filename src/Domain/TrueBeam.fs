namespace BeamHead.Domain

/// Composes the supported TrueBeam beam-head components for the current static geometry.
[<RequireQualifiedAccess>]
module TrueBeam =
    /// The final source-frame placements of the four TrueBeam jaws.
    let jaws = TrueBeamJaws.placements

    /// The final source-frame placements of the two simplified Millennium 120 MLC banks.
    let mlcBanks = TrueBeamMlc.placements
