namespace BeamHead.Domain

/// Provides the source-origin construction frame with positive Z downstream toward the patient.
[<RequireQualifiedAccess>]
module SourceFrame =
    /// The source/target CAX origin in the source frame.
    let source: SourceFramePoint = {
        X = 0.0<mm>
        Y = 0.0<mm>
        Z = TrueBeamGeometry.sourcePlaneZ
    }

    /// The isocentre point in the source frame.
    let isocentre: SourceFramePoint = {
        X = 0.0<mm>
        Y = 0.0<mm>
        Z = TrueBeamGeometry.isocentreZ
    }

/// Provides the isocentre-origin presentation/export frame with positive Z downstream toward the patient.
[<RequireQualifiedAccess>]
module IsocentreFrame =
    /// Converts a source-frame point to the isocentre frame without changing physical geometry.
    let fromSourcePoint (point: SourceFramePoint) : IsocentreFramePoint = {
        X = point.X
        Y = point.Y
        Z = point.Z - TrueBeamGeometry.isocentreZ
    }

    /// Converts a source-frame jaw pose to the isocentre frame.
    let fromSourceJawPlacement (placement: SourceFrameJawPlacement) : IsocentreFrameJawPlacement = {
        Axis = placement.Axis
        Side = placement.Side
        ApertureFaceMidpoint = fromSourcePoint placement.ApertureFaceMidpoint
        ApertureFaceAngleRadians = placement.ApertureFaceAngleRadians
        BodyDimensions = placement.BodyDimensions
    }

    /// Converts a source-frame MLC bank placement to the isocentre frame.
    let fromSourceMlcBankPlacement (placement: SourceFrameMlcBankPlacement) : IsocentreFrameMlcBankPlacement = {
        Side = placement.Side
        TipReference = fromSourcePoint placement.TipReference
        LocalProfile = placement.LocalProfile
        CrossLeafWidth = placement.CrossLeafWidth
    }

    /// The isocentre origin in the isocentre frame.
    let isocentre = fromSourcePoint SourceFrame.isocentre

    /// The source/target CAX point in the isocentre frame.
    let source = fromSourcePoint SourceFrame.source

/// Provides the non-physical orientation transform used only by the 3D preview.
[<RequireQualifiedAccess>]
module ViewerDisplay =
    /// Mirrors an isocentre-frame point in Z for natural JSCAD presentation.
    let fromIsocentrePoint (point: IsocentreFramePoint) : ViewerDisplayPoint = {
        X = point.X
        Y = point.Y
        Z = -point.Z
    }