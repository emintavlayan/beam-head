# BeamHead Development Doctrine

## Purpose

BeamHead is a geometry tool for radiotherapy beam heads.

Its core responsibility is:

**machine geometry + user-defined positions -> validated geometry -> 3D representation -> STL**

Beam transport, particle energy, dose calculation, and phase-space generation are outside the current scope.

TrueBeam is the first supported machine model. Do not introduce abstractions for future machines unless they simplify a requirement that exists today.

## Architecture

Use lightweight domain-driven design.

Prefer:

- small, well-named modules
- small domain types
- pure functions
- immutable data
- explicit data flow
- composition over inheritance
- side effects at application boundaries

Avoid:

- speculative abstractions
- unnecessary interfaces
- unnecessary classes
- generic frameworks for hypothetical future requirements
- primitive values when a small domain type materially improves correctness

Keep the domain independent of UI, JSCAD, persistence, and other infrastructure.

## F#

Use idiomatic functional F#.

Expected failures use `Result<'T, string>`.

Use `FsToolkit.ErrorHandling` and computation expressions where they improve composition.

Every public type and function must have a concise XML documentation comment describing its purpose, for example:

```fsharp
/// Represents a physical jaw position.
type JawPosition = ...

/// Creates a validated jaw position.
let create value = ...
```

Prefer functions with one clear responsibility.

## SAFE Stack

Use the standard SAFE Stack architecture and its MVU approach.

Do not replace working SAFE template infrastructure without a concrete reason.

Keep domain logic out of views and update functions.

## Geometry

Geometry definitions represent physical machine geometry and should remain independent of rendering technology.

Define millimetres as an F# unit of measure:

```fsharp
[<Measure>]
type mm
```

All physical geometry dimensions and positions in the domain must use `float<mm>`. Do not scatter raw `float` values for physical lengths. Remove the unit and convert to a plain numeric value only at an external boundary such as JSCAD/JavaScript interop or serialization.

JSCAD v2 is the current CAD and visualisation technology.

Translation from BeamHead domain geometry to JSCAD belongs at the application/infrastructure boundary.

## Naming

- Product and documentation: `BeamHead`
- Repository and local folder: `beam-head`
- F# solution, projects and namespaces: `BeamHead` / `BeamHead.*`
- Files, modules, types and functions follow normal F# naming conventions.

Use names from the physical radiotherapy domain where possible.

## Development

Work in small vertical slices.

For each slice:

1. model the smallest required domain concept
2. implement pure behaviour
3. test domain behaviour
4. connect it to the application
5. verify the complete user-facing path

Do not build functionality merely because it may be useful later.

## Testing

Use xUnit for automated tests.

Prefer conventional `[<Fact>]` tests with descriptive test names. Tests should be easy to read for developers familiar with the wider .NET ecosystem, including those who do not regularly use F#.

Example:

```fsharp
[<Fact>]
let ``valid jaw position is accepted`` () =
    let result = JawPosition.create 10.0

    Assert.True(Result.isOk result)
```

Prefer:

- `[<Fact>]` for individual behaviours
- descriptive backtick test names
- simple Arrange/Act/Assert-style structure where useful
- standard xUnit `Assert` functions
- tests focused on observable domain behaviour

Avoid introducing F#-specific testing DSLs or assertion libraries unless they provide a clear benefit.

Keep test code straightforward even when the production implementation uses more advanced functional composition.
