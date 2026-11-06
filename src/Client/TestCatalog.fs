module TestCatalog

open BeamHead.Domain
open BeamHead.Domain.Tests
open FsToolkit.ErrorHandling
open TestSession

/// Formats one diagnostic number without relying on Fable format specifiers.
let private formatFixed decimals value =
    let scale = pown 10 decimals
    let scaled = int (System.Math.Round(value * float scale))
    let absolute = abs scaled
    let fraction = string (absolute % scale)
    let padding = String.replicate (decimals - fraction.Length) "0"
    let sign = if scaled < 0 then "-" else ""
    $"{sign}{absolute / scale}.{padding}{fraction}"

/// Holds the exact physical solids measured by a run and later used for export.
type Scene = {
    Field: JawField
    Jaws: obj array
    Banks: obj array
}

/// Describes one actual executable work step.
type WorkStep = {
    CheckId: string
    Execute: unit -> Result<Evidence, string>
}

/// Binds the visible checks and their execution to a captured scene.
type Workflow = {
    Definitions: CheckDefinition list
    Steps: WorkStep array
    Scene: Scene
}

/// Represents successful assertion-only evidence.
let private success = {
    Detail = None
    Failure = None
    Geometry = None
}

/// Executes a shared test body and converts assertion failures to an explicit error.
let private assertion check () =
    try
        check ()
        Ok success
    with error ->
        Error error.Message

/// Registers an existing test body without copying its assertions.
let private shared id chapter group statement check =
    {
        Id = id
        Chapter = chapter
        Group = group
        Statement = statement
        Steps = 1
    },
    [
        {
            CheckId = id
            Execute = assertion check
        }
    ]

/// Registers the 55 Domain and 15 Geometry checker test bodies also used by the native suites.
let private sharedChecks = [
    shared
        "domain-1"
        Domain
        "Jaws and coordinate frames"
        "Source plane and isocentre use the global Z convention"
        TrueBeamJawTests.``source plane and isocentre use the global Z convention``
    shared
        "domain-2"
        Domain
        "Jaws and coordinate frames"
        "Source transforms to minus 1000 millimetres in isocentre frame"
        TrueBeamJawTests.``source transforms to minus 1000 millimetres in isocentre frame``
    shared
        "domain-3"
        Domain
        "Jaws and coordinate frames"
        "Isocentre transforms to zero in isocentre frame"
        TrueBeamJawTests.``isocentre transforms to zero in isocentre frame``
    shared
        "domain-4"
        Domain
        "Jaws and coordinate frames"
        "Frame transformation leaves X and Y coordinates unchanged"
        TrueBeamJawTests.``frame transformation leaves X and Y coordinates unchanged``
    shared
        "domain-5"
        Domain
        "Jaws and coordinate frames"
        "Viewer display mirrors Z without changing X or Y"
        TrueBeamJawTests.``viewer display mirrors Z without changing X or Y``
    shared
        "domain-6"
        Domain
        "Jaws and coordinate frames"
        "Viewer display source is derived at positive 1000 millimetres for BEV"
        TrueBeamJawTests.``viewer display source is derived at positive 1000 millimetres for BEV``
    shared
        "domain-7"
        Domain
        "Jaws and coordinate frames"
        "Nominal field guide half width is derived from the 400 millimetre clinical field"
        TrueBeamJawTests.``nominal field guide half width is derived from the 400 millimetre clinical field``
    shared
        "domain-8"
        Domain
        "Jaws and coordinate frames"
        "Jaw aperture rays pass through the source centre"
        TrueBeamJawTests.``jaw aperture rays pass through the source centre``
    shared
        "domain-9"
        Domain
        "Jaws and coordinate frames"
        "400 millimetre field has plus and minus 200 millimetre jaw edges"
        TrueBeamJawTests.``400 millimetre field has plus and minus 200 millimetre jaw edges``
    shared
        "domain-10"
        Domain
        "Jaws and coordinate frames"
        "Jaw physical thickness is exactly 78 millimetres"
        TrueBeamJawTests.``jaw physical thickness is exactly 78 millimetres``
    shared
        "domain-11"
        Domain
        "Jaws and coordinate frames"
        "Lower X jaws use the simplified axis-specific body extents"
        TrueBeamJawTests.``lower X jaws use the simplified axis-specific body extents``
    shared
        "domain-12"
        Domain
        "Jaws and coordinate frames"
        "Upper Y jaws use the simplified axis-specific body extents"
        TrueBeamJawTests.``upper Y jaws use the simplified axis-specific body extents``
    shared
        "domain-13"
        Domain
        "Jaws and coordinate frames"
        "X jaw reference Z is exactly 406 millimetres"
        TrueBeamJawTests.``X jaw reference Z is exactly 406 millimetres``
    shared
        "domain-14"
        Domain
        "Jaws and coordinate frames"
        "X jaw reference transforms to minus 594 millimetres"
        TrueBeamJawTests.``X jaw reference transforms to minus 594 millimetres``
    shared
        "domain-15"
        Domain
        "Jaws and coordinate frames"
        "X jaw unrotated axial references are 367 and 445 millimetres"
        TrueBeamJawTests.``X jaw unrotated axial references are 367 and 445 millimetres``
    shared
        "domain-16"
        Domain
        "Jaws and coordinate frames"
        "Y jaw aperture face midpoint radius is exactly 319 millimetres"
        TrueBeamJawTests.``Y jaw aperture face midpoint radius is exactly 319 millimetres``
    shared
        "domain-17"
        Domain
        "Jaws and coordinate frames"
        "Y jaw midpoint radius and half thickness give supported upstream trajectory radius"
        TrueBeamJawTests.``Y jaw midpoint radius and half thickness give supported upstream trajectory radius``
    shared
        "domain-18"
        Domain
        "Jaws and coordinate frames"
        "Y jaw reference transforms to isocentre frame"
        TrueBeamJawTests.``Y jaw reference transforms to isocentre frame``
    shared
        "domain-19"
        Domain
        "Jaws and coordinate frames"
        "Jaw frame transformation preserves orientation and body dimensions"
        TrueBeamJawTests.``jaw frame transformation preserves orientation and body dimensions``
    shared
        "domain-20"
        Domain
        "Jaws and coordinate frames"
        "Positive and negative jaw placements are symmetric"
        TrueBeamJawTests.``positive and negative jaw placements are symmetric``
    shared
        "domain-21"
        Domain
        "Jaws and coordinate frames"
        "Positive jaw reference positions and tilt are calculated from divergence"
        TrueBeamJawTests.``positive jaw reference positions and tilt are calculated from divergence``
    shared
        "domain-22"
        Domain
        "Jaws and coordinate frames"
        "Aperture faces project to source centre and selected isocentre edges"
        TrueBeamJawTests.``aperture faces project to source centre and selected isocentre edges``
    shared
        "domain-23"
        Domain
        "Jaws and coordinate frames"
        "Domain jaw geometry retains millimetre units through the rendering boundary input"
        TrueBeamJawTests.``domain jaw geometry retains millimetre units through the rendering boundary input``
    shared
        "domain-24"
        Domain
        "MLC parameters and placement"
        "MLC midplane is 509 millimetres from source"
        TrueBeamMlcTests.``MLC midplane is 509 millimetres from source``
    shared
        "domain-25"
        Domain
        "MLC parameters and placement"
        "Delivery tip setting is 201 millimetres at isocentre"
        TrueBeamMlcTests.``delivery tip setting is 201 millimetres at isocentre``
    shared
        "domain-26"
        Domain
        "MLC parameters and placement"
        "Nominal leaf coverage and simplified bank envelope remain separate"
        TrueBeamMlcTests.``nominal leaf coverage and simplified bank envelope remain separate``
    shared
        "domain-27"
        Domain
        "MLC parameters and placement"
        "Physical tip reference is derived from the rounded profile tangent"
        TrueBeamMlcTests.``physical tip reference is derived from rounded profile tangent``
    shared
        "domain-28"
        Domain
        "MLC parameters and placement"
        "Bank span follows source projection through the MLC thickness"
        TrueBeamMlcTests.``bank span follows source projection through the MLC thickness``
    shared
        "domain-29"
        Domain
        "MLC parameters and placement"
        "Bank span increases monotonically from upstream to downstream"
        TrueBeamMlcTests.``bank span increases monotonically from upstream to downstream``
    shared
        "domain-30"
        Domain
        "MLC parameters and placement"
        "MLC placements transform to minus 491 millimetres in isocentre frame"
        TrueBeamMlcTests.``MLC placements transform to minus 491 millimetres in isocentre frame``
    shared
        "domain-31"
        Domain
        "MLC parameters and placement"
        "Supported MLC thickness is 67 millimetres with 33 point 5 millimetre half thickness"
        TrueBeamMlcTests.``supported MLC thickness is 67 millimetres with 33 point 5 millimetre half thickness``
    shared
        "domain-32"
        Domain
        "MLC parameters and placement"
        "Declared rounded tip radius and outer angle are retained"
        TrueBeamMlcTests.``declared rounded tip radius and outer angle are retained``
    shared
        "domain-33"
        Domain
        "MLC parameters and placement"
        "Reconstructed profile contains the local tip reference and all supplied points"
        TrueBeamMlcTests.``reconstructed profile contains the local tip reference and all supplied points``
    shared
        "domain-34"
        Domain
        "MLC parameters and placement"
        "Simplified body depth is derived from the projected published leaf length"
        TrueBeamMlcTests.``simplified body depth is derived from the projected published leaf length``
    shared
        "domain-35"
        Domain
        "MLC parameters and placement"
        "Profile rear points use the body depth approximation and half thickness"
        TrueBeamMlcTests.``profile rear points use the body depth approximation and half thickness``
    shared
        "domain-36"
        Domain
        "MLC parameters and placement"
        "Bank envelope uses a source-projected Y half-span at every X-Z profile point"
        TrueBeamMlcTests.``bank envelope uses a source-projected Y half-span at every X-Z profile point``
    shared
        "domain-37"
        Domain
        "MLC parameters and placement"
        "Positive and negative MLC bank placements are symmetric"
        TrueBeamMlcTests.``positive and negative MLC bank placements are symmetric``
    shared
        "domain-38"
        Domain
        "MLC parameters and placement"
        "MLC bank references match the tangent-compensated physical placement"
        TrueBeamMlcTests.``MLC bank references match the tangent compensated physical placement``
    shared
        "domain-39"
        Domain
        "MLC parameters and placement"
        "MLC rounded profiles are tangent at plus and minus 201 millimetres"
        TrueBeamMlcTests.``MLC rounded profiles are tangent at plus and minus 201 millimetres``
    shared
        "domain-40"
        Domain
        "MLC parameters and placement"
        "Rounded-tip setting is one millimetre outside the maximum jaw field"
        TrueBeamMlcTests.``rounded tip setting is one millimetre outside the maximum jaw field``
    shared
        "domain-41"
        Domain
        "MLC parameters and placement"
        "MLC frame transformation changes only Z"
        TrueBeamMlcTests.``MLC frame transformation changes only Z``
    shared
        "domain-42"
        Domain
        "MLC parameters and placement"
        "Domain MLC geometry retains millimetre units through the rendering boundary input"
        TrueBeamMlcTests.``domain MLC geometry retains millimetre units through the rendering boundary input``
    shared
        "domain-43"
        Domain
        "Field dimensions"
        "Field dimensions support 5 to 400 millimetres"
        JawFieldTests.``field dimensions support 5 to 400 millimetres``
    shared
        "domain-44"
        Domain
        "Field dimensions"
        "Invalid or fractional millimetre dimensions return errors"
        JawFieldTests.``invalid or fractional millimetre dimensions return errors``
    shared
        "domain-45"
        Domain
        "Field dimensions"
        "Floating point slider values normalize to exact millimetres"
        JawFieldTests.``floating point slider values normalize to exact millimetres``
    shared
        "domain-46"
        Domain
        "Field dimensions"
        "Selected jaw faces project to the correct independent field edges"
        JawFieldTests.``selected jaw faces project to the correct independent field edges``
    shared
        "domain-47"
        Domain
        "Field dimensions"
        "Changing one field axis leaves the other jaw pair unchanged"
        JawFieldTests.``changing one field axis leaves the other jaw pair unchanged``
    shared
        "domain-48"
        Domain
        "Field controls"
        "Square field mirrors X into Y at the minimum size"
        FieldControlsTests.``square field mirrors X into Y at the minimum size``
    shared
        "domain-49"
        Domain
        "Field controls"
        "Y edits are ignored while square field is checked"
        FieldControlsTests.``Y edits are ignored while square field is checked``
    shared
        "domain-50"
        Domain
        "Field controls"
        "Square checkbox copies X into Y after rectangular editing"
        FieldControlsTests.``square checkbox copies X into Y after rectangular editing``
    shared
        "domain-51"
        Domain
        "Field controls"
        "Invalid input keeps last valid field and disables export until corrected"
        FieldControlsTests.``invalid input keeps last valid field and disables export until corrected``
    shared
        "domain-52"
        Domain
        "Field controls"
        "Presets produce matching field dimensions and STL filenames"
        FieldControlsTests.``presets produce matching field dimensions and STL filenames``
    shared
        "domain-53"
        Domain
        "Field controls"
        "Rectangular filename uses both selected dimensions"
        FieldControlsTests.``rectangular filename uses both selected dimensions``
    shared
        "domain-54"
        Domain
        "Field controls"
        "Correcting X does not hide an invalid independent Y input"
        FieldControlsTests.``correcting X does not hide an invalid independent Y input``
    shared
        "domain-55"
        Domain
        "Field controls"
        "Numeric input formats to one decimal on commit"
        FieldControlsTests.``numeric input formats to one decimal on commit``
    shared
        "geometrychecker-56"
        GeometryChecker
        "Independent measuring algorithms"
        "Measurement tolerance includes its boundary"
        GeometryValidationTests.``measurement tolerance includes its boundary``
    shared
        "geometrychecker-57"
        GeometryChecker
        "Independent measuring algorithms"
        "Nonfinite measurements fail"
        GeometryValidationTests.``nonfinite measurements fail``
    shared
        "geometrychecker-58"
        GeometryChecker
        "Independent measuring algorithms"
        "Empty QA reports cannot pass"
        GeometryValidationTests.``empty QA reports cannot pass``
    shared
        "geometrychecker-59"
        GeometryChecker
        "Independent measuring algorithms"
        "Independent field light pyramid meets all geometric requirements"
        GeometryValidationTests.``independent field light pyramid meets all geometric requirements``
    shared
        "geometrychecker-60"
        GeometryChecker
        "Independent measuring algorithms"
        "Displaced field light fails vertex agreement"
        GeometryValidationTests.``displaced field light fails vertex agreement``
    shared
        "geometrychecker-61"
        GeometryChecker
        "Independent measuring algorithms"
        "Inward pyramid faces fail half space validation"
        GeometryValidationTests.``inward pyramid faces fail half space validation``
    shared
        "geometrychecker-62"
        GeometryChecker
        "Independent measuring algorithms"
        "Missing pyramid face fails closure"
        GeometryValidationTests.``missing pyramid face fails closure``
    shared
        "geometrychecker-63"
        GeometryChecker
        "Independent measuring algorithms"
        "Invalid field light surfaces return errors"
        GeometryValidationTests.``invalid field light surfaces return errors``
    shared
        "geometrychecker-64"
        GeometryChecker
        "Independent measuring algorithms"
        "Missing physical components return errors"
        GeometryValidationTests.``missing physical components return errors``
    shared
        "geometrychecker-65"
        GeometryChecker
        "Independent measuring algorithms"
        "Field light corners preserve independent x and y dimensions"
        GeometryValidationTests.``field light corners preserve independent x and y dimensions``
    shared
        "geometrychecker-66"
        GeometryChecker
        "Independent measuring algorithms"
        "Central ray measures known cube thickness"
        GeometryValidationTests.``central ray measures known cube thickness``
    shared
        "geometrychecker-67"
        GeometryChecker
        "Independent measuring algorithms"
        "Oblique material path includes geometric ray length"
        GeometryValidationTests.``oblique material path includes geometric ray length``
    shared
        "geometrychecker-68"
        GeometryChecker
        "Independent measuring algorithms"
        "Unobstructed ray has zero material path"
        GeometryValidationTests.``unobstructed ray has zero material path``
    shared
        "geometrychecker-69"
        GeometryChecker
        "Independent measuring algorithms"
        "Source cannot be its own ray target"
        GeometryValidationTests.``source cannot be its own ray target``
    shared
        "geometrychecker-70"
        GeometryChecker
        "Independent measuring algorithms"
        "Crossed polygon edges fail convex face validation"
        GeometryValidationTests.``crossed polygon edges fail convex face validation``
]

/// Describes the production geometry checks in the same order as the Node suite.
let private productionDefinitions = [
    {
        Id = "current-geometry"
        Chapter = JscadGeometry
        Group = "Selected field"
        Statement = "The selected beam head meets every geometric requirement"
        Steps = 1
    }
    {
        Id = "representative-fields"
        Chapter = JscadGeometry
        Group = "Supported fields"
        Statement = "Representative square and rectangular fields produce correct solids"
        Steps = 11
    }
    {
        Id = "jaw-sweep"
        Chapter = JscadGeometry
        Group = "Supported fields"
        Statement = "Every supported jaw setting produces the correct aperture"
        Steps = 396
    }
    {
        Id = "jaw-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Incorrect jaw positions, angles, axes and scale are detected"
        Steps = 1
    }
    {
        Id = "topology-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Missing surfaces, reversed faces and missing components are detected"
        Steps = 1
    }
    {
        Id = "malformed-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Invalid coordinates and collapsed faces are rejected"
        Steps = 1
    }
    {
        Id = "mlc-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Incorrect MLC settings and bank envelopes are detected"
        Steps = 1
    }
    {
        Id = "collision-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Overlaps and central-axis obstructions are detected"
        Steps = 1
    }
    {
        Id = "light-defects"
        Chapter = JscadGeometry
        Group = "Error detection"
        Statement = "Incorrect field-light position and orientation are detected"
        Steps = 1
    }
    {
        Id = "rounded-tangent"
        Chapter = JscadGeometry
        Group = "MLC field edge"
        Statement = "A 40.2 cm field-light pyramid touches both rounded MLC surfaces"
        Steps = 1
    }
]

/// Lists all visible requirements without executing any test or CAD construction.
let definitions = List.map fst sharedChecks @ productionDefinitions

/// Measures the captured solids and preserves both passing and failing measurement evidence.
let private inspect selected () = result {
    let! report = JscadGeometry.inspectBeamHead selected.Field selected.Jaws selected.Banks
    let failed = report.Measurements |> List.filter (GeometryQa.meetsRequirement >> not)

    let failure =
        if List.isEmpty failed then
            None
        else
            failed
            |> List.map (fun item ->
                $"{item.Component}: {item.Requirement}: {formatFixed 6 item.Actual}; expected {formatFixed 6 item.Expected}.")
            |> String.concat "\n"
            |> Some

    return {
        Detail = Some $"{report.Measurements.Length} measured requirements; construction tolerance 0.000001 mm."
        Failure = failure
        Geometry = Some report
    }
}

/// Captures the selected scene and schedules actual tests with separate steps for long sweeps.
let prepare field =
    let selected = {
        Field = field
        Jaws =
            TrueBeam.jaws field
            |> List.map IsocentreFrame.fromSourceJawPlacement
            |> JscadGeometry.createJaws
        Banks =
            TrueBeam.mlcBanks
            |> List.map IsocentreFrame.fromSourceMlcBankPlacement
            |> JscadGeometry.createMlcBanks
    }

    let groups = JscadChecks.groups |> List.map snd |> List.toArray

    let production = [
        yield {
            CheckId = "current-geometry"
            Execute = inspect selected
        }
        for dimensions in JscadChecks.representativeFields do
            yield {
                CheckId = "representative-fields"
                Execute = assertion (fun () -> JscadChecks.checkRepresentativeField dimensions)
            }
        for size in 5..400 do
            yield {
                CheckId = "jaw-sweep"
                Execute = assertion (fun () -> JscadChecks.checkJawSetting size)
            }
        for id, index in
            [
                "jaw-defects", 2
                "topology-defects", 3
                "malformed-defects", 4
                "mlc-defects", 5
                "collision-defects", 6
                "light-defects", 7
                "rounded-tangent", 8
            ] do
            yield {
                CheckId = id
                Execute = assertion groups[index]
            }
    ]

    {
        Definitions = definitions
        Steps = (List.collect snd sharedChecks @ production) |> List.toArray
        Scene = selected
    }
