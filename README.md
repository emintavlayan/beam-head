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

## Status

Early development.