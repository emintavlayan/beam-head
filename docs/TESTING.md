# Test coverage and geometric validation

## Existing coverage assessment

The original Domain suite contains 55 xUnit tests:

| Area | Tests | What the evidence establishes |
| --- | ---: | --- |
| Jaws and coordinate frames | 23 | Reference coordinates, frame transforms, placement symmetry, nominal dimensions and selected divergence calculations |
| MLC | 19 | Declared parameters, profile data, machine setting, tangent-compensated placement, envelope scaling and transforms |
| Field domain | 5 | Accepted dimensions, invalid input, normalization, representative projected edges and axis independence |
| Field controls | 8 | Linking, independent edits, validation state, presets, formatting and filenames |

These tests remain useful regression tests. Several assert constants or compare results with related construction formulas. They do not, on their own, establish that the JSCAD transforms and resulting surfaces are correct. The test retaining the declared 80 mm tip-radius parameter does not establish that the supplied profile follows that circle.

No instrumented line or branch coverage percentage has been measured. Test counts describe behaviours, not a percentage of geometric correctness.

## Geometry suite

`tests/Geometry/Geometry.Tests.fsproj` contains 15 native xUnit tests for the independent checker: tolerance boundaries, nonfinite values, missing evidence, known pyramid geometry, displaced and inward guides, open and self-crossed surfaces, malformed geometry, component inventory, rectangular guide dimensions and analytic cube ray chords.

`tests/Geometry/Jscad/Geometry.Jscad.Tests.fsproj` runs the production F# placements and production JSCAD constructors through Fable and Node. This boundary needs JavaScript execution; a native .NET test cannot execute JSCAD directly. It uses straightforward assertions, with failures propagated as a nonzero exit status.

The JSCAD suite checks 11 representative square/rectangular fields and sweeps all 396 supported settings on each jaw axis. In the sweep, X increases from 5 to 400 mm and Y decreases from 400 to 5 mm. Every setting on each axis is covered; this is not enumeration of all 156,816 rectangular combinations. The sweep checks jaw geometry; representative fields also run the full static-bank and component-separation checks.

The expected measurements are specified independently in `GeometryQa`; they do not call the jaw placement, aperture-line or MLC projection helpers. JSCAD polygons are read after resolving their transforms, before the viewer-only mirror and before STL serialization.

| Requirement | Method |
| --- | --- |
| Jaw divergence | Every recovered aperture-face vertex lies on its requested source-to-field plane; its actual polygon plane also contains the source |
| Requested aperture | Every aperture-face vertex projects to the requested signed isocentre edge |
| Jaw body | Extents measured along independent field-plane normal, tangent and cross-axis directions: 110 mm body depth, 78 mm thickness, axis-specific cross width |
| Jaw reference | Aperture-face centroid lies at the 406 mm X reference plane or 319 mm Y trajectory radius; cross-axis centre is zero |
| Field-light pyramid | Actual debug-pyramid constructor, specified source and corner vertices, face count, planarity, closure and outward convex half-spaces |
| MLC placement | Each actual rounded profile is tangent to its reported +/-201 mm setting at isocentre; axial limits are 475.5 and 542.5 mm from source |
| MLC body | Axial thickness, body depth, projected Y envelope and profile symmetry after correcting for Y divergence |
| Opposing banks | Resolved bank vertices agree under reflection in X |
| Surface structure | Face planarity and convex polygon winding, exactly one oppositely directed edge mate, and every vertex inside every outward face half-space |
| Component separation | Separating-axis test using face normals and edge-cross-edge axes for each pair of convex components |
| Central axis | Source-ray material path through each component is zero |
| Rounded-tip tangent | Minimum signed X projection of each actual bank is 201 mm at isocentre, 1 mm outside the maximum jaw field |

The checker assumes the supported convex jaw and bank envelopes. Convexity and outward half-spaces are checked; this is not a general-purpose validator for arbitrary concave CAD.

The numerical tolerance is **0.000001 mm** for double-precision, pre-export geometry. It is a construction tolerance, not a clinical acceptance criterion. Integer counts and edge-pair counts must match exactly in practice.

## Detection tests

The suite deliberately introduces jaw translations, incorrect tilt, swapped axes, scale errors, display-frame mirroring, missing faces, reversed winding, missing/duplicate components, nonfinite coordinates, source-plane vertices, collapsed faces, displaced MLC banks, incorrect envelope span, swapped banks, overlapping banks, central-axis obstruction and altered field-light geometry. It requires the relevant check to fail.

## Rounded-tip tangent placement

The reported delivery setting remains +/-201 mm at isocentre. Field-light observation shows that the retracted MLC does not shade the 400 mm jaw field. The machine setting represents the source-ray tangent to the rounded surface, not the physical profile reference at the MLC midplane.

The CAD placement is therefore derived from the reconstructed profile. For every profile point, the model calculates the physical translation required to keep that point outside the 201 mm setting ray and selects the largest requirement. This places the physical midplane reference at 103.90517 mm in the MLC plane, projecting to 204.135894 mm at isocentre. The resulting rounded profile is tangent at 201 mm and clears the 200 mm jaw edge by 1 mm.

## Running checks

From the repository root:

```sh
dotnet test tests/Domain/Domain.Tests.fsproj
dotnet test tests/Geometry/Geometry.Tests.fsproj
dotnet test tests/Client/Client.Tests.fsproj
npm --prefix src/Client run test:geometry
```

Install the repository's .NET tools and client dependencies first if needed (`dotnet tool restore`, `npm --prefix src/Client ci`). The geometry script invokes the repository's Fable tool and then Node. Node uses the same JSCAD dependencies as the client.

`dotnet run -- RunTestsHeadless` runs all three native suites and the JSCAD suite. `dotnet run -- RunGeometryTests` runs the JSCAD suite alone.

## Test dialog

Use **Tests** in the navigation bar for a manual review, or click **Export beam head STL** to verify before download. A large native modal keeps the geometry workbench behind it. The action header and progress remain visible above one vertically scrolling list. The active check scrolls into view. Passing chapters collapse to a green result header; failed chapters remain open with their failure reasons visible.

| Chapter | Visible checks | Execution |
| --- | ---: | --- |
| Domain tests | 55 | The original parameter, placement, transform and control test bodies |
| Geometry checker tests | 15 | The original independent-checker test bodies |
| JSCAD geometry tests | 10 | The selected scene plus the nine shared production-JSCAD groups |

**Run all tests** executes 80 visible checks in 485 actual work steps. The 11 representative fields and 396 jaw settings are scheduled individually, allowing progress to repaint and Stop to work between settings. No timed pass animation is used. The right-hand squares are read-only status indicators: waiting, running, passed, failed, stopped or outdated. Expand a statement for failure details; the selected-scene check includes all 102 measured requirements with expected values and tolerances.

The browser compiles the exact native test source bodies with `BrowserTestSupport.fs`, a small adapter for the assertion operations those tests use. It does not run the .NET/xUnit runner in the browser. Native test projects continue to use real xUnit. `JscadChecks.fs` contains the shared production test functions used by both the browser and Node.

Closing a manually opened dialog, clicking outside it or pressing Escape keeps an active run running. **Stop** cancels it. Native modal behaviour traps focus, makes the background inert and restores focus when closed. Reopening preserves progress and results.

The export button remains available whenever the field input is valid. It opens the dialog and starts the same verification automatically. After a successful run, the three collapsed chapter results remain visible for 2 seconds; the captured solids are then downloaded and the dialog closes for the next field. A failed run remains open and never starts a download.

Results belong to the captured physical field. Changing its accepted dimensions marks results outdated and rejects pending callbacks. Invalid numeric drafts block running and export; restoring the same physical field keeps its evidence. Export requires a complete, passing run and passing measured geometry for the exact field. It serializes the captured solids that were measured. A partial, stopped, failed or outdated run cannot authorize export.

Rounded-tip tangency is included in construction pass/fail. There is no separate intrusion report. A construction pass is evidence for the listed geometry requirements, not a claim of dose agreement.

`tests/Client/Client.Tests.fsproj` adds 14 native xUnit tests for snapshot capture, run identity, callback order, invalidation, cancellation, persistent failures, measured evidence and export permission. These verify the workflow rather than adding another end-user chapter.

## Remaining coverage gaps

- STL serialization, float32 quantization and re-import are outside these pre-export checks.
- Field-light observation establishes the no-shading boundary condition. The exact physical reference correction is inferred from the reconstructed tip profile; the profile's complete shape has not been validated against vendor data or direct mechanical measurement.
- The tests establish listed requirements within explicit floating-point tolerances; they are not a theorem-prover verification of all possible CAD properties.
- Beam transport, material assignment, transmission, scatter, dose and agreement with the physical machine require separate evidence.
- Browser controls and download behaviour have been exercised with a browser harness during development; that harness is not yet a committed automated UI suite.
