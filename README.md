# BeamHead

BeamHead is a geometry tool for radiotherapy beam heads.

It converts machine-specific geometry and user-defined jaw and MLC positions into validated 3D geometry that can be exported for Monte Carlo workflows.

The first supported machine model is the Varian TrueBeam.

## Current scope

- TrueBeam beam-head geometry
- Jaws
- Millennium 120 MLC
- Geometry validation
- STL export

Beam transport, energy modelling, dose calculation, and phase-space generation are outside the scope of BeamHead.

## Technology

BeamHead is written in F# using the SAFE Stack.

It uses a battle-tested MVU (Model-View-Update) architecture for the UI, with SAFE Stack providing a stable full-stack foundation.

For CAD rendering, BeamHead uses JSCAD v2 to generate and visualise 3D geometry.

The codebase follows a lightweight domain-driven design approach in F#, with a strong emphasis on functional core design, small domain types, pure functions, and explicit validation. Side effects are kept at the application boundaries.

## Field controls

- X and Y field widths are defined at isocentre in centimetres.
- Supported dimensions: 0.5–40.0 cm, in 0.1 cm steps.
- Each axis has a slider, an editable numeric input and 10/20/30/36/40 cm presets.
- Square field is enabled initially. It links Y to X and disables the Y controls.
- Clearing Square field enables independent rectangular-field editing.
- Invalid numeric input keeps the last valid preview and disables STL export until corrected.
- The preview, debug field guide and STL export use the same selected dimensions.
- Millennium 120 leaf tips remain fixed at ±201 mm at isocentre.
- STL filenames include both jaw dimensions, for example `truebeam-360x200mm-mlc201mm.stl`.

## Status

Early development.