namespace BeamHead.Domain

/// Represents physical lengths measured in millimetres.
[<Measure>]
type mm

/// Represents a point measured from the source/target CAX origin.
type SourceFramePoint = {
    X: float<mm>
    Y: float<mm>
    Z: float<mm>
}

/// Represents a point measured from the isocentre origin.
type IsocentreFramePoint = {
    X: float<mm>
    Y: float<mm>
    Z: float<mm>
}

/// Represents an isocentre-frame point after applying the viewer-only display orientation.
type ViewerDisplayPoint = {
    X: float<mm>
    Y: float<mm>
    Z: float<mm>
}

/// Identifies the axis along which a jaw closes.
type JawAxis =
    | X
    | Y

/// Identifies the side of the beam aperture controlled by a jaw.
type JawSide =
    | Negative
    | Positive

/// Describes the dimensions of a simplified rectangular jaw body.
type JawBodyDimensions = {
    ClosingAxisExtent: float<mm>
    CrossAxisExtent: float<mm>
    Thickness: float<mm>
}

/// Describes a divergent aperture line in one transverse axis.
type DivergentApertureLine = {
    SourcePlaneEdge: float<mm>
    IsocentrePlaneEdge: float<mm>
}

/// Describes a source-frame jaw pose whose reference is the aperture-forming face midpoint at the jaw midplane.
type SourceFrameJawPlacement = {
    Axis: JawAxis
    Side: JawSide
    ApertureFaceMidpoint: SourceFramePoint
    ApertureFaceAngleRadians: float
    BodyDimensions: JawBodyDimensions
}

/// Describes an isocentre-frame jaw pose for presentation and export.
type IsocentreFrameJawPlacement = {
    Axis: JawAxis
    Side: JawSide
    ApertureFaceMidpoint: IsocentreFramePoint
    ApertureFaceAngleRadians: float
    BodyDimensions: JawBodyDimensions
}

/// Identifies one of the opposing Millennium MLC banks.
type MlcBankSide =
    | NegativeBank
    | PositiveBank

/// Represents a point in the local X-Z profile of a simplified MLC bank.
type MlcProfilePoint = { X: float<mm>; Z: float<mm> }

/// Represents one local X-Z profile point with its source-projected Y half-span.
type MlcBankEnvelopePoint = {
    X: float<mm>
    Z: float<mm>
    HalfSpan: float<mm>
}

/// Describes a source-frame MLC bank whose reference is the beam-facing tip centre at the MLC midplane.
type SourceFrameMlcBankPlacement = {
    Side: MlcBankSide
    TipReference: SourceFramePoint
    EnvelopeProfile: MlcBankEnvelopePoint list
}

/// Describes an isocentre-frame MLC bank for presentation and export.
type IsocentreFrameMlcBankPlacement = {
    Side: MlcBankSide
    TipReference: IsocentreFramePoint
    EnvelopeProfile: MlcBankEnvelopePoint list
}

/// Provides calculations for divergent aperture lines.
[<RequireQualifiedAccess>]
module DivergentApertureLine =
    /// Calculates the transverse aperture coordinate at a downstream Z position.
    let coordinateAt (isocentreZ: float<mm>) (z: float<mm>) (line: DivergentApertureLine) =
        line.SourcePlaneEdge
        + (line.IsocentrePlaneEdge - line.SourcePlaneEdge) * z / isocentreZ

    /// Calculates the signed angle of the aperture line from the positive Z axis.
    let angleRadians (isocentreZ: float<mm>) (line: DivergentApertureLine) =
        atan ((line.IsocentrePlaneEdge - line.SourcePlaneEdge) / isocentreZ)