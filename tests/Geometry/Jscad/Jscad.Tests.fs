module BeamHead.Geometry.Tests

/// Runs the same production geometry groups used by the browser workbench.
[<EntryPoint>]
let main _ =
    for name, check in JscadChecks.groups do
        check ()
        printfn "PASS: %s" name

    printfn
        "Geometry tests passed: %d groups; 11 representative fields and all 396 settings per jaw axis."
        JscadChecks.groups.Length

    0