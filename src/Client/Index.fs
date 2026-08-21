module Index

open BeamHead.Domain

/// Represents messages handled by the current BeamHead client.
type Msg = NoOp

/// Creates the initial BeamHead client model.
let init () = ()

/// Applies a client message to the current model.
let update (_: Msg) (model: unit) = model

open Feliz

let private proofCube = JscadGeometry.createCuboid ProofCube.dimensions

/// Renders the current BeamHead client.
let view (_: unit) (_: Msg -> unit) =
    Html.section [
        prop.className "h-screen w-screen relative overflow-hidden"
        prop.children [
            Html.meta [
                prop.name "viewport"
                prop.content "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"
            ]

            Html.div [
                prop.className
                    "absolute inset-0 bg-cover bg-center bg-fixed bg-no-repeat
                bg-white/20 backdrop-blur-sm"
                prop.style [ style.backgroundImageUrl "https://unsplash.it/1200/900?random" ]
            ]

            Html.div [
                prop.className "relative z-10 h-full w-full"
                prop.children [
                    Html.div [
                        prop.className "flex flex-col items-center justify-center h-full"
                        prop.children [
                            Html.div [
                                prop.className
                                    "bg-white/20 backdrop-blur-lg p-4 sm:p-8 rounded-xl shadow-lg border border-white/30 mx-4 sm:mx-0 max-w-full sm:max-w-2xl"
                                prop.children [
                                    Html.h1 [
                                        prop.className "text-center text-3xl sm:text-5xl font-bold mb-3 p-2 sm:p-4"
                                        prop.text "BeamHead"
                                    ]
                                    Html.p [
                                        prop.className "mb-3 text-center text-sm text-slate-700"
                                        prop.text "100 x 100 x 100 mm proof cube"
                                    ]
                                    Html.div [
                                        prop.className "w-full sm:w-[42rem]"
                                        prop.children [ CubeViewer.View proofCube ]
                                    ]
                                    Html.div [
                                        prop.className "flex justify-center py-4"
                                        prop.children [
                                            Html.button [
                                                prop.className
                                                    "rounded bg-teal-600 px-6 py-2 font-bold text-white outline-none hover:bg-teal-700 focus:ring-2 ring-teal-300"
                                                prop.onClick (fun _ ->
                                                    JscadGeometry.downloadStl "beam-head-cube-100mm.stl" proofCube)
                                                prop.text "Export STL"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]