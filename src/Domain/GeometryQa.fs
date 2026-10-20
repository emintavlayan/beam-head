namespace BeamHead.Domain

open FsToolkit.ErrorHandling

/// Represents a measured geometric requirement with an explicit numerical tolerance.
type GeometryMeasurement = {
    Component: string
    Requirement: string
    Actual: float
    Expected: float
    Tolerance: float
}

/// Represents geometry measurements and the effective rounded-tip shadow at isocentre.
type GeometryQaReport = {
    Measurements: GeometryMeasurement list
    MlcShadowHalfWidth: float<mm>
    MlcFieldClearance: float<mm>
}

/// Checks resolved physical solid vertices against an independent geometric specification.
[<RequireQualifiedAccess>]
module GeometryQa =
    /// The numerical construction tolerance in millimetres, separate from clinical acceptance.
    let tolerance = 0.000001

    /// Subtracts two coordinate vectors.
    let private subtract a b = List.map2 (-) a b

    /// Calculates the scalar product of two coordinate vectors.
    let private dot a b = List.map2 (*) a b |> List.sum

    /// Calculates the vector product of two coordinate vectors.
    let private cross (a: float list) (b: float list) =
        [ a[1] * b[2] - a[2] * b[1]; a[2] * b[0] - a[0] * b[2]; a[0] * b[1] - a[1] * b[0] ]

    /// Calculates the Euclidean length of a coordinate vector.
    let private length a = sqrt (dot a a)

    /// Converts a physical point into millimetre coordinates.
    let private coordinates (point: IsocentreFramePoint) = [ float point.X; float point.Y; float point.Z ]

    /// Creates a measured requirement with a numerical construction tolerance.
    let private measurement componentName requirement expected actual = {
        Component = componentName
        Requirement = requirement
        Actual = actual
        Expected = expected
        Tolerance = tolerance
    }

    /// Checks whether a finite measured value meets its declared tolerance.
    let meetsRequirement value =
        not (System.Double.IsNaN value.Actual || System.Double.IsInfinity value.Actual)
        && abs (value.Actual - value.Expected) <= value.Tolerance

    /// Checks whether all declared requirements pass and the report is nonempty.
    let passes report =
        not (List.isEmpty report.Measurements)
        && List.forall meetsRequirement report.Measurements

    /// Checks finite downstream polygons before any projection or plane calculation.
    let private validateSurface componentName (surface: IsocentreFramePoint list list) =
        /// Rejects nonfinite coordinates before geometric arithmetic.
        let validPoint point =
            coordinates point
            |> List.forall (fun value -> not (System.Double.IsNaN value || System.Double.IsInfinity value))
        if List.isEmpty surface || surface |> List.exists (fun face -> face.Length < 3) then
            Error $"{componentName}: each solid needs polygon faces with at least three vertices."
        elif surface |> List.collect id |> List.exists (fun point -> not (validPoint point) || point.Z <= -1000.0<mm>) then
            Error $"{componentName}: vertices must be finite and downstream from the source."
        elif surface |> List.exists (fun face ->
            let points = List.map coordinates face
            length (cross (subtract points[1] points[0]) (subtract points[2] points[0])) < 1e-12) then
            Error $"{componentName}: a polygon has a degenerate defining plane."
        else Ok surface

    /// Calculates the span of all vertices along a specified unit direction.
    let private span direction vertices =
        let values = vertices |> List.map (coordinates >> dot direction)
        List.max values - List.min values

    /// Checks every polygon for planarity and every directed edge for an opposing mate.
    let private surfaceMeasurements componentName surface =
        let planarResidual =
            surface |> List.collect (fun face ->
                let points = List.map coordinates face
                let normal = cross (subtract points[1] points[0]) (subtract points[2] points[0])
                points |> List.map (fun point -> abs (dot normal (subtract point points[0])) / length normal))
            |> List.max
        let edges = surface |> List.collect (fun face -> List.zip face (List.tail face @ [List.head face]))
        let unmatched = edges |> List.filter (fun (a, b) ->
            edges |> List.filter (fun (c, d) ->
                length (subtract (coordinates a) (coordinates d)) <= tolerance
                && length (subtract (coordinates b) (coordinates c)) <= tolerance)
            |> List.length |> (<>) 1)
        let polygonViolation =
            surface |> List.collect (fun face ->
                let points = List.map coordinates face
                let normal = cross (subtract points[1] points[0]) (subtract points[2] points[0])
                List.zip points (List.tail points @ [List.head points])
                |> List.collect (fun (a, b) ->
                    let edgeNormal = cross (subtract b a) normal
                    let magnitude = length edgeNormal
                    points |> List.map (fun point -> if magnitude < 1e-12 then System.Double.PositiveInfinity else max 0.0 (dot edgeNormal (subtract point a) / magnitude))))
            |> List.max
        let vertices = surface |> List.collect id |> List.map coordinates
        let halfSpaceViolation =
            surface |> List.collect (fun face ->
                let points = List.map coordinates face
                let normal = cross (subtract points[1] points[0]) (subtract points[2] points[0])
                vertices |> List.map (fun point -> max 0.0 (dot normal (subtract point points[0]) / length normal)))
            |> List.max
        [ measurement componentName "Face planarity" 0.0 planarResidual
          measurement componentName "Unpaired or multiply paired edges" 0.0 (float unmatched.Length)
          measurement componentName "Convex polygon edge violation" 0.0 polygonViolation
          measurement componentName "Outward convex half-space violation" 0.0 halfSpaceViolation ]

    /// Measures an actual jaw face against the corresponding field-light pyramid plane.
    let private jawMeasurements field (axis, side, surface: IsocentreFramePoint list list) =
        let componentName = $"{axis} {side} jaw"
        let sign = if side = Positive then 1.0 else -1.0
        let edge = sign * float (JawField.size axis field) / 2.0
        let slope = edge / 1000.0
        let normal, tangent, crossAxis =
            match axis with
            | X -> [1.0; 0.0; -slope], [slope; 0.0; 1.0], [0.0; 1.0; 0.0]
            | Y -> [0.0; 1.0; -slope], [0.0; slope; 1.0], [1.0; 0.0; 0.0]
        let normal = normal |> List.map (fun value -> value / sqrt (1.0 + slope * slope))
        let tangent = tangent |> List.map (fun value -> value / sqrt (1.0 + slope * slope))
        /// Reads the physical closing-axis coordinate.
        let closing (point: IsocentreFramePoint) = if axis = X then float point.X else float point.Y
        let aperture = surface |> List.minBy (List.averageBy (fun point -> sign * closing point))
        let vertices = List.collect id surface
        let source = [0.0; 0.0; -1000.0]
        let planeResidual = aperture |> List.map (fun point -> abs (dot normal (subtract (coordinates point) source))) |> List.max
        let projectedError = aperture |> List.map (fun point -> abs (1000.0 * closing point / (float point.Z + 1000.0) - edge)) |> List.max
        let points = List.map coordinates aperture
        let actualNormal = cross (subtract points[1] points[0]) (subtract points[2] points[0])
        let sourceDistance = abs (dot actualNormal (subtract source points[0])) / length actualNormal
        let midpoint = aperture |> List.map coordinates |> List.transpose |> List.map List.average
        let reference = if axis = X then midpoint[2] + 1000.0 else sqrt (midpoint[1] ** 2.0 + (midpoint[2] + 1000.0) ** 2.0)
        [ measurement componentName "Aperture face to field-light plane" 0.0 planeResidual
          measurement componentName "Aperture plane to source" 0.0 sourceDistance
          measurement componentName "Projected edge error at isocentre" 0.0 projectedError
          measurement componentName "Thickness along aperture face" 78.0 (span tangent vertices)
          measurement componentName "Body depth normal to aperture face" 110.0 (span normal vertices)
          measurement componentName "Cross-axis extent" (if axis = X then 203.0 else 156.25) (span crossAxis vertices)
          measurement componentName "Reference distance from source" (if axis = X then 406.0 else 319.0) reference
          measurement componentName "Cross-axis centring" 0.0 (dot crossAxis midpoint)
          measurement componentName "Cuboid face count" 6.0 (float surface.Length) ]
        @ surfaceMeasurements componentName surface

    /// Measures MLC physical bounds, actual midplane tips and divergent envelope vertices.
    let private bankMeasurements (side, surface: IsocentreFramePoint list list) =
        let componentName = $"{side} MLC"
        let sign = if side = PositiveBank then 1.0 else -1.0
        let vertices = List.collect id surface
        let atMidplane = vertices |> List.filter (fun point -> abs (float point.Z + 491.0) <= tolerance)
        result {
            let! tip =
                if List.isEmpty atMidplane then Error $"{componentName}: no tip vertices at the specified MLC midplane."
                else Ok (atMidplane |> List.minBy (fun point -> sign * float point.X))
            let envelopeError = vertices |> List.map (fun point -> abs (abs (float point.Y) * 1000.0 / (float point.Z + 1000.0) - 205.0)) |> List.max
            let mirrorError = vertices |> List.map (fun point ->
                vertices |> List.map (fun other ->
                    length [float (point.X - other.X); float point.Y * 509.0 / (float point.Z + 1000.0) - float other.Y * 509.0 / (float other.Z + 1000.0); float point.Z + float other.Z + 982.0]) |> List.min) |> List.max
            return
                [ measurement componentName "Midplane tip at isocentre" 201.0 (sign * float tip.X * 1000.0 / 509.0)
                  measurement componentName "Upstream distance from source" 475.5 (vertices |> List.minBy _.Z |> fun point -> float point.Z + 1000.0)
                  measurement componentName "Downstream distance from source" 542.5 (vertices |> List.maxBy _.Z |> fun point -> float point.Z + 1000.0)
                  measurement componentName "Axial thickness" 67.0 (span [0.0; 0.0; 1.0] vertices)
                  measurement componentName "Physical body depth" 76.35 (span [1.0; 0.0; 0.0] vertices)
                  measurement componentName "Envelope projected half-span error" 0.0 envelopeError
                  measurement componentName "Profile symmetry about midplane" 0.0 mirrorError ]
                @ surfaceMeasurements componentName surface
        }

    /// Creates unique unit directions for convex-solid separation calculations.
    let private uniqueDirections directions =
        directions
        |> List.filter (fun direction -> length direction > 1e-12)
        |> List.map (fun direction ->
            let magnitude = length direction
            let sign = if List.find (fun value -> abs value > 1e-12) direction < 0.0 then -1.0 else 1.0
            direction |> List.map (fun value -> System.Math.Round(sign * value / magnitude, 10)))
        |> List.distinct

    /// Reads the face normals and edge directions of a resolved solid.
    let private separatingDirections surface =
        let faces = surface |> List.map (List.map coordinates)
        let normals = faces |> List.map (fun face -> cross (subtract face[1] face[0]) (subtract face[2] face[0])) |> uniqueDirections
        let edges = faces |> List.collect (fun face -> List.zip face (List.tail face @ [List.head face]) |> List.map (fun (a, b) -> subtract b a)) |> uniqueDirections
        normals, edges

    /// Measures overlap using every face normal and edge-cross-edge separating axis for convex solids.
    let private pairMeasurement (firstName, first) (secondName, second) =
        let firstNormals, firstEdges = separatingDirections first
        let secondNormals, secondEdges = separatingDirections second
        let axes = firstNormals @ secondNormals @ (firstEdges |> List.collect (fun edge -> secondEdges |> List.map (cross edge))) |> uniqueDirections
        let firstVertices = first |> List.collect id |> List.map coordinates |> List.distinct
        let secondVertices = second |> List.collect id |> List.map coordinates |> List.distinct
        let gap = axes |> List.map (fun axis ->
            let firstValues = firstVertices |> List.map (dot axis)
            let secondValues = secondVertices |> List.map (dot axis)
            max (List.min secondValues - List.max firstValues) (List.min firstValues - List.max secondValues)) |> List.max
        measurement $"{firstName} / {secondName}" "Convex solid overlap" 0.0 (max 0.0 -gap)

    /// Calculates material path length along a source ray through an outward convex polygon solid.
    let rayPathLength (target: IsocentreFramePoint) (surface: IsocentreFramePoint list list) : Result<float<mm>, string> =
        result {
            let! validSurface = validateSurface "Ray path" surface
            let source = [0.0; 0.0; -1000.0]
            let direction = subtract (coordinates target) source
            do! if coordinates target |> List.forall (fun value -> not (System.Double.IsNaN value || System.Double.IsInfinity value)) then Ok () else Error "Ray target must have finite coordinates."
            do! if length direction > tolerance then Ok () else Error "Ray target must differ from the source."
            let constraints = validSurface |> List.map (fun face ->
                let points = List.map coordinates face
                let normal = cross (subtract points[1] points[0]) (subtract points[2] points[0])
                let magnitude = length normal
                dot normal (subtract source points[0]) / magnitude, dot normal direction / magnitude)
            let outsideParallel = constraints |> List.exists (fun (offset, rate) -> abs rate < 1e-12 && offset > tolerance)
            let entry = constraints |> List.filter (fun (_, rate) -> rate < -1e-12) |> List.map (fun (offset, rate) -> -offset / rate) |> fun values -> List.max (0.0 :: values)
            let exits = constraints |> List.filter (fun (_, rate) -> rate > 1e-12) |> List.map (fun (offset, rate) -> -offset / rate)
            let exit = if List.isEmpty exits then entry else List.min exits
            return (if outsideParallel then 0.0 else max 0.0 (exit - entry) * length direction) * 1.0<mm>
        }

    /// Checks the actual debug pyramid vertices and planes against the requested source and field.
    let inspectFieldLight field (surface: IsocentreFramePoint list list) =
        let halfX = float (JawField.xSize field) / 2.0
        let halfY = float (JawField.ySize field) / 2.0
        let expected = [[0.0; 0.0; -1000.0]; [-halfX; -halfY; 0.0]; [halfX; -halfY; 0.0]; [halfX; halfY; 0.0]; [-halfX; halfY; 0.0]]
        let vertices = surface |> List.collect id |> List.map coordinates
        if vertices.IsEmpty || vertices |> List.exists (List.exists (fun value -> System.Double.IsNaN value || System.Double.IsInfinity value)) then
            Error "Field light: pyramid vertices must be present and finite."
        elif surface |> List.exists (fun face -> face.Length < 3) then
            Error "Field light: pyramid faces must have at least three vertices."
        elif surface |> List.exists (fun face ->
            let points = List.map coordinates face
            length (cross (subtract points[1] points[0]) (subtract points[2] points[0])) < 1e-12) then
            Error "Field light: a face has a degenerate defining plane."
        else
            let vertexError =
                (vertices |> List.map (fun point -> expected |> List.map (subtract point >> length) |> List.min))
                @ (expected |> List.map (fun point -> vertices |> List.map (subtract point >> length) |> List.min))
                |> List.max
            Ok ([ measurement "Field light" "Pyramid vertex error" 0.0 vertexError
                  measurement "Field light" "Pyramid face count" 5.0 (float surface.Length) ]
                @ surfaceMeasurements "Field light" surface)

    /// Checks the four actual jaw solids independently of the static MLC banks.
    let inspectJaws field jaws =
        result {
            do! if List.sort (List.map (fun (axis, side, _) -> axis, side) jaws) = List.sort [X, Negative; X, Positive; Y, Negative; Y, Positive] then Ok () else Error "QA requires exactly one jaw on each side of each axis."
            for axis, side, surface in jaws do
                let! _ = validateSurface $"{axis} {side} jaw" surface
                ()
            return jaws |> List.collect (jawMeasurements field)
        }

    /// Inspects resolved pre-export solids without using the jaw or MLC construction helpers.
    let inspect field jaws banks : Result<GeometryQaReport, string> =
        result {
            let! jawResults = inspectJaws field jaws
            do! if List.sort (List.map fst banks) = List.sort [NegativeBank; PositiveBank] then Ok () else Error "QA requires exactly two opposing MLC banks."
            for side, surface in banks do
                let! _ = validateSurface $"{side} MLC" surface
                ()
            let! bankResults = banks |> List.map bankMeasurements |> List.sequenceResultM
            let components = (jaws |> List.map (fun (axis, side, surface) -> $"{axis} {side} jaw", surface)) @ (banks |> List.map (fun (side, surface) -> $"{side} MLC", surface))
            let overlaps = components |> List.indexed |> List.collect (fun (index, solid) -> components |> List.skip (index + 1) |> List.map (pairMeasurement solid))
            let! centralPaths = components |> List.map (fun (name, surface) -> rayPathLength IsocentreFrame.isocentre surface |> Result.map (float >> measurement name "Central axis material path" 0.0)) |> List.sequenceResultM
            let negativeVertices = banks |> List.find (fun (side, _) -> side = NegativeBank) |> snd |> List.collect id |> List.map coordinates
            let positiveVertices = banks |> List.find (fun (side, _) -> side = PositiveBank) |> snd |> List.collect id |> List.map coordinates
            let mirrorError = positiveVertices |> List.map (fun point -> negativeVertices |> List.map (fun other -> length [point[0] + other[0]; point[1] - other[1]; point[2] - other[2]]) |> List.min) |> List.max
            let symmetry = measurement "MLC banks" "Opposing bank reflection error" 0.0 mirrorError
            let shadow = banks |> List.map (fun (side, surface) ->
                let sign = if side = PositiveBank then 1.0 else -1.0
                surface |> List.collect id |> List.map (fun point -> sign * float point.X * 1000.0 / (float point.Z + 1000.0)) |> List.min) |> List.min
            return {
                Measurements = jawResults @ List.concat bankResults @ overlaps @ centralPaths @ [symmetry]
                MlcShadowHalfWidth = shadow * 1.0<mm>
                MlcFieldClearance = shadow * 1.0<mm> - JawField.xSize field / 2.0
            }
        }
