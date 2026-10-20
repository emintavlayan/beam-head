namespace BeamHead.Domain

/// Defines the physical field-light pyramid independently of jaw placements.
[<RequireQualifiedAccess>]
module FieldLight =
    /// Creates the four requested field corners in the physical isocentre plane.
    let corners field : IsocentreFramePoint array =
        let halfX = JawField.xSize field / 2.0
        let halfY = JawField.ySize field / 2.0
        [| -halfX, -halfY; halfX, -halfY; halfX, halfY; -halfX, halfY |]
        |> Array.map (fun (x, y) -> { X = x; Y = y; Z = 0.0<mm> })
