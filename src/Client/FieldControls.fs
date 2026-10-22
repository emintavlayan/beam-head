module FieldControls

open System
open System.Globalization
open BeamHead.Domain
open FsToolkit.ErrorHandling

/// Represents editable field controls and their last valid geometry.
type Model = {
    Field: JawField
    SquareField: bool
    XInput: string
    YInput: string
    XError: string option
    YError: string option
}

/// The commissioning presets expressed as full field widths at isocentre.
let presets = [ 100.0<mm>; 200.0<mm>; 300.0<mm>; 360.0<mm>; 400.0<mm> ]

/// Formats a supported field dimension in centimetres with one decimal place.
let formatCentimetres (size: float<mm>) =
    let wholeMillimetres = int (System.Math.Round(float size))
    let centimetres = wholeMillimetres / 10
    let millimetres = abs wholeMillimetres % 10
    $"{centimetres}.{millimetres}"

/// Creates square field controls at the full-size initial field.
let init () = {
    Field = JawField.fullSize
    SquareField = true
    XInput = formatCentimetres (JawField.xSize JawField.fullSize)
    YInput = formatCentimetres (JawField.ySize JawField.fullSize)
    XError = None
    YError = None
}

/// Parses a centimetre input without using the machine's decimal separator.
let private parseCentimetres (text: string) : Result<float<mm>, string> =
    match Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture) with
    | true, value when not (Double.IsNaN value || Double.IsInfinity value) -> Ok (value * 10.0<mm>)
    | _ -> Error "Enter a field size between 0.5 and 40.0 cm."

/// Applies a requested dimension while respecting the square-field constraint.
let private changeSize axis size model =
    match axis with
    | X -> JawField.create size (if model.SquareField then size else JawField.ySize model.Field)
    | Y -> JawField.create (JawField.xSize model.Field) size

/// Updates an editable dimension while retaining the last valid geometry on errors.
let editSize axis text model =
    if axis = Y && model.SquareField then
        model
    else
        let updated = result {
            let! size = parseCentimetres text
            return! changeSize axis size model
        }
        match axis, updated with
        | X, Ok field when model.SquareField -> {
            model with
                Field = field
                XInput = text
                YInput = formatCentimetres (JawField.ySize field)
                XError = None
                YError = None
          }
        | X, Ok field -> { model with Field = field; XInput = text; XError = None }
        | Y, Ok field -> { model with Field = field; YInput = text; YError = None }
        | X, Error error -> { model with XInput = text; XError = Some error }
        | Y, Error error -> { model with YInput = text; YError = Some error }

/// Formats a valid input when the user leaves its numeric control.
let commitSize axis model =
    match axis with
    | X when model.XError.IsNone -> { model with XInput = formatCentimetres (JawField.xSize model.Field) }
    | Y when model.YError.IsNone -> { model with YInput = formatCentimetres (JawField.ySize model.Field) }
    | _ -> model

/// Applies a slider value and formats its matching numeric input.
let setSliderSize axis text model =
    model |> editSize axis text |> commitSize axis

/// Applies a commissioning preset to one enabled field axis.
let selectPreset axis size model =
    model |> editSize axis (formatCentimetres size) |> commitSize axis

/// Links Y to the current X dimension or releases Y for independent editing.
let setSquareField enabled model =
    if enabled then
        let field = JawField.squareFromX model.Field
        {
            Field = field
            SquareField = true
            XInput = formatCentimetres (JawField.xSize field)
            YInput = formatCentimetres (JawField.ySize field)
            XError = None
            YError = None
        }
    else
        { model with SquareField = false }

/// Checks whether all editable dimensions are valid for STL export.
let canExport model = model.XError.IsNone && model.YError.IsNone

/// Names an STL from its actual jaw dimensions and fixed MLC delivery position.
let exportFileName field =
    sprintf "truebeam-%dx%dmm-mlc%dmm.stl"
        (int (float (JawField.xSize field)))
        (int (float (JawField.ySize field)))
        (int (float TrueBeamMlc.deliveryTipSettingAtIsocentre))
