module Server

open Saturn

/// Hosts the compiled BeamHead client through the SAFE server.
let app = application {
    use_static "public"
    use_gzip
}

/// Starts the BeamHead web server.
[<EntryPoint>]
let main _ =
    run app
    0