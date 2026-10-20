module JscadChecks

open BeamHead.Domain

/// Fails a geometry test with a specific geometric requirement.
let private require condition message =
    if not condition then
        failwith message

/// Extracts a successful geometric calculation or reports its error.
let private valid value =
    match value with
    | Ok result -> result
    | Error error -> failwith error

/// Creates physical solids through the same production boundary used by the app.
let private geometry field =
    let jaws =
        TrueBeam.jaws field
        |> List.map IsocentreFrame.fromSourceJawPlacement
        |> JscadGeometry.createJaws

    let banks =
        TrueBeam.mlcBanks
        |> List.map IsocentreFrame.fromSourceMlcBankPlacement
        |> JscadGeometry.createMlcBanks

    jaws, banks

/// Reads actual solid faces while retaining the known component identities.
let private surfaces jaws banks =
    let jawLabels = [ X, Negative; X, Positive; Y, Negative; Y, Positive ]

    let jawSurfaces =
        List.zip jawLabels (Array.toList jaws)
        |> List.map (fun ((axis, side), solid) -> axis, side, JscadGeometry.readSurface solid)

    let bankSurfaces =
        List.zip [ NegativeBank; PositiveBank ] (Array.toList banks)
        |> List.map (fun (side, solid) -> side, JscadGeometry.readSurface solid)

    jawSurfaces, bankSurfaces

/// Requires every measured property to pass and prints individual failures otherwise.
let private requirePass report =
    let failures =
        report.Measurements |> List.filter (GeometryQa.meetsRequirement >> not)

    require
        (GeometryQa.passes report)
        (failures
         |> List.map (fun check -> $"{check.Component}: {check.Requirement}: {check.Actual}, expected {check.Expected}")
         |> String.concat "\n")

/// Lists representative square and rectangular fields used by both runners.
let representativeFields = [
    5, 5
    100, 100
    200, 200
    300, 300
    360, 360
    400, 400
    5, 400
    400, 5
    360, 200
    200, 360
    123, 287
]

/// Checks one representative field through the production constructors.
let checkRepresentativeField (x, y) =
    let field = JawField.create (float x * 1.0<mm>) (float y * 1.0<mm>) |> valid
    let jaws, banks = geometry field
    JscadGeometry.inspectBeamHead field jaws banks |> valid |> requirePass

/// Checks all representative fields.
let private selectedFields () =
    representativeFields |> List.iter checkRepresentativeField

/// Checks one supported jaw setting using the actual resolved solid surfaces.
let checkJawSetting size =
    let field =
        JawField.create (float size * 1.0<mm>) (float (405 - size) * 1.0<mm>) |> valid

    let jaws, banks = geometry field
    let jawSurfaces, _ = surfaces jaws banks
    let checks = GeometryQa.inspectJaws field jawSurfaces |> valid
    require (List.forall GeometryQa.meetsRequirement checks) $"Jaw setting {size} failed geometric checks."

/// Checks all supported settings using the same per-setting function as the browser.
let private allJawSettings () = [ 5..400 ] |> List.iter checkJawSetting

/// Maps every vertex of the selected jaw while retaining all other components.
let private changeJaw
    axis
    side
    (change: IsocentreFramePoint -> IsocentreFramePoint)
    (jaws: (JawAxis * JawSide * IsocentreFramePoint list list) list)
    =
    jaws
    |> List.map (fun (actualAxis, actualSide, surface) ->
        actualAxis,
        actualSide,
        if actualAxis = axis && actualSide = side then
            List.map (List.map change) surface
        else
            surface)

/// Requires a specific requirement to fail after introducing a deliberate error.
let private requireFailure requirement field jaws banks =
    let report = GeometryQa.inspect field jaws banks |> valid
    require (not (GeometryQa.passes report)) $"Mutation escaped QA: {requirement}"

    require
        (report.Measurements
         |> List.exists (fun check -> check.Requirement = requirement && not (GeometryQa.meetsRequirement check)))
        $"Expected failure was absent: {requirement}"

/// Checks sensitivity to translations, wrong divergence, swapped axes, scale and display-frame errors.
let private jawMutations () =
    let field = JawField.create 360.0<mm> 200.0<mm> |> valid
    let jawSolids, bankSolids = geometry field
    let jaws, banks = surfaces jawSolids bankSolids

    let shifted =
        changeJaw X Positive (fun point -> { point with X = point.X + 1.0<mm> }) jaws

    requireFailure "Aperture face to field-light plane" field shifted banks

    let tilted =
        changeJaw
            Y
            Negative
            (fun point -> {
                point with
                    Y = point.Y + 0.01 * (point.Z + 687.0<mm>)
            })
            jaws

    requireFailure "Aperture plane to source" field tilted banks

    let scaled =
        changeJaw
            X
            Positive
            (fun point -> {
                point with
                    X = point.X * 10.0
                    Y = point.Y * 10.0
                    Z = point.Z * 0.1
            })
            jaws

    requireFailure "Body depth normal to aperture face" field scaled banks
    let mirrored = changeJaw X Positive (fun point -> { point with Z = -point.Z }) jaws
    requireFailure "Projected edge error at isocentre" field mirrored banks

    let swapped =
        jaws
        |> List.map (fun (axis, side, surface) -> (if axis = X then Y else X), side, surface)

    requireFailure "Cross-axis extent" field swapped banks

/// Checks that missing faces, reversed winding and duplicate components cannot pass QA.
let private topologyMutations () =
    let field = JawField.fullSize
    let jawSolids, bankSolids = geometry field
    let jaws, banks = surfaces jawSolids bankSolids

    let opened =
        jaws
        |> List.map (fun (axis, side, surface) ->
            axis,
            side,
            if axis = X && side = Positive then
                List.tail surface
            else
                surface)

    requireFailure "Unpaired or multiply paired edges" field opened banks

    let reversed =
        jaws
        |> List.map (fun (axis, side, surface) ->
            axis,
            side,
            if axis = Y && side = Positive then
                List.rev (List.head surface) :: List.tail surface
            else
                surface)

    requireFailure "Unpaired or multiply paired edges" field reversed banks
    require (GeometryQa.inspect field (List.tail jaws) banks |> Result.isError) "Missing jaw escaped QA."
    require (GeometryQa.inspect field (List.head jaws :: jaws) banks |> Result.isError) "Duplicate jaw escaped QA."
    require (GeometryQa.inspect field jaws [ List.head banks ] |> Result.isError) "Missing bank escaped QA."

/// Checks malformed vertices and degenerate polygons return explicit validation errors.
let private malformedGeometry () =
    let field = JawField.fullSize
    let jawSolids, bankSolids = geometry field
    let jaws, banks = surfaces jawSolids bankSolids

    for value in [ System.Double.NaN; System.Double.PositiveInfinity ] do
        let invalid =
            changeJaw X Positive (fun point -> { point with X = value * 1.0<mm> }) jaws

        require (GeometryQa.inspect field invalid banks |> Result.isError) "Nonfinite vertex escaped QA."

    let upstream =
        changeJaw X Positive (fun point -> { point with Z = -1000.0<mm> }) jaws

    require (GeometryQa.inspect field upstream banks |> Result.isError) "Source-plane vertex escaped QA."
    let collapsed = changeJaw X Positive (fun _ -> IsocentreFrame.isocentre) jaws
    require (GeometryQa.inspect field collapsed banks |> Result.isError) "Degenerate polygon escaped QA."

/// Checks physical MLC placement independently of the jaw field and detects the old tip setting.
let private mlcMutations () =
    let field = JawField.fullSize
    let jawSolids, bankSolids = geometry field
    let jaws, banks = surfaces jawSolids bankSolids

    let oldSetting =
        banks
        |> List.map (fun (side, surface) ->
            let shift = (if side = PositiveBank then 1.0 else -1.0) * 1.3234<mm>
            side, surface |> List.map (List.map (fun point -> { point with X = point.X + shift })))

    requireFailure "Midplane tip at isocentre" field jaws oldSetting

    let wrongSpan =
        banks
        |> List.map (fun (side, surface) ->
            side, surface |> List.map (List.map (fun point -> { point with Y = point.Y * 0.99 })))

    requireFailure "Envelope projected half-span error" field jaws wrongSpan

    let reversed =
        banks
        |> List.map (fun (side, surface) -> (if side = PositiveBank then NegativeBank else PositiveBank), surface)

    requireFailure "Midplane tip at isocentre" field jaws reversed

/// Checks overlap and material-path failures using deliberately misplaced production bank surfaces.
let private collisionAndPathMutations () =
    let field = JawField.fullSize
    let jawSolids, bankSolids = geometry field
    let jaws, banks = surfaces jawSolids bankSolids

    let negativeSurface =
        banks |> List.find (fun (side, _) -> side = NegativeBank) |> snd

    let coincident = [ NegativeBank, negativeSurface; PositiveBank, negativeSurface ]
    requireFailure "Convex solid overlap" field jaws coincident

    let blocking =
        banks
        |> List.map (fun (side, surface) ->
            side,
            if side = PositiveBank then
                surface
                |> List.map (List.map (fun point -> { point with X = point.X - 130.0<mm> }))
            else
                surface)

    requireFailure "Central axis material path" field jaws blocking

/// Checks that changed field-light vertices and reversed complete winding cannot pass.
let private fieldLightMutations () =
    let surface = JscadGeometry.fieldLightSurface JawField.fullSize

    let shifted =
        surface
        |> List.map (List.map (fun point -> { point with X = point.X + 1.0<mm> }))

    let shiftedChecks = GeometryQa.inspectFieldLight JawField.fullSize shifted |> valid

    require
        (shiftedChecks |> List.exists (GeometryQa.meetsRequirement >> not))
        "Displaced field-light pyramid escaped QA."

    let reversedChecks =
        GeometryQa.inspectFieldLight JawField.fullSize (surface |> List.map List.rev)
        |> valid

    require
        (reversedChecks
         |> List.exists (fun check ->
             check.Requirement = "Outward convex half-space violation"
             && not (GeometryQa.meetsRequirement check)))
        "Inward pyramid winding escaped QA."

    let displayMirror =
        surface |> List.map (List.map (fun point -> { point with Z = -point.Z }))

    let mirroredChecks =
        GeometryQa.inspectFieldLight JawField.fullSize displayMirror |> valid

    require
        (mirroredChecks
         |> List.exists (fun check ->
             check.Requirement = "Pyramid vertex error"
             && not (GeometryQa.meetsRequirement check)))
        "Display coordinates escaped the physical guide checks."

/// Checks that the rounded-tip silhouette is measured separately from its midplane setting.
let private roundedTipShadow () =
    let jaws, banks = geometry JawField.fullSize
    let report = JscadGeometry.inspectBeamHead JawField.fullSize jaws banks |> valid

    require
        (report.MlcShadowHalfWidth > 197.0<mm> && report.MlcShadowHalfWidth < 199.0<mm>)
        "Rounded-tip shadow was confused with the 201 mm midplane tip setting."

    require (report.MlcFieldClearance < 0.0<mm>) "The 400 mm field must report its rounded-tip geometric intrusion."
    let smaller = JawField.create 360.0<mm> 360.0<mm> |> valid
    let smallerJaws, _ = geometry smaller
    let smallerReport = JscadGeometry.inspectBeamHead smaller smallerJaws banks |> valid
    require (smallerReport.MlcFieldClearance > 0.0<mm>) "The 360 mm field should clear the rounded MLC silhouette."

    printfn
        "Rounded MLC shadow half-width: %.6f mm; 400 mm field clearance: %.6f mm"
        (float report.MlcShadowHalfWidth)
        (float report.MlcFieldClearance)

/// Shares the geometry groups between the Node runner and browser.
let groups = [
    "Representative field solids", selectedFields
    "All 396 settings per jaw axis", allJawSettings
    "Jaw pose and transform mutations", jawMutations
    "Topology mutations", topologyMutations
    "Malformed surfaces", malformedGeometry
    "MLC placement mutations", mlcMutations
    "Collision and ray-path mutations", collisionAndPathMutations
    "Field-light pyramid mutations", fieldLightMutations
    "Rounded-tip silhouette", roundedTipShadow
]