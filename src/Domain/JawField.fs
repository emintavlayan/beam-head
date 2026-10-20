namespace BeamHead.Domain

open System
open FsToolkit.ErrorHandling

/// Represents validated X and Y field dimensions projected at isocentre.
type JawField = private { XSize: float<mm>; YSize: float<mm> }

/// Creates and reads supported jaw field dimensions.
[<RequireQualifiedAccess>]
module JawField =
    /// The smallest supported field dimension at isocentre.
    let minimumSize = 5.0<mm>

    /// The largest supported field dimension at isocentre.
    let maximumSize = 400.0<mm>

    /// The initial full-size square jaw field.
    let fullSize = { XSize = maximumSize; YSize = maximumSize }

    /// Checks a field dimension and normalizes floating-point input to millimetre steps.
    let private validateSize axis (size: float<mm>) =
        let value = float size
        if Double.IsNaN value || Double.IsInfinity value || size < minimumSize || size > maximumSize then
            Error $"{axis} field size must be between 0.5 and 40.0 cm."
        elif abs (value - Math.Round value) > 0.000001 then
            Error $"{axis} field size must use 0.1 cm steps."
        else
            Ok (Math.Round value * 1.0<mm>)

    /// Creates a field with independently validated X and Y dimensions.
    let create xSize ySize : Result<JawField, string> =
        result {
            let! x = validateSize "X" xSize
            let! y = validateSize "Y" ySize
            return { XSize = x; YSize = y }
        }

    /// Reads the X field dimension at isocentre.
    let xSize field = field.XSize

    /// Reads the Y field dimension at isocentre.
    let ySize field = field.YSize

    /// Reads the field dimension for the requested jaw axis.
    let size axis field =
        match axis with
        | X -> field.XSize
        | Y -> field.YSize

    /// Links Y to the current validated X field dimension.
    let squareFromX field = { field with YSize = field.XSize }
