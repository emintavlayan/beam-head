module Index

open BeamHead.Domain
open Feliz

/// Represents editable workbench values that have not yet been applied to geometry.
type Model = {
    XFieldSizeInput: string
    YFieldSizeInput: string
}

/// Represents messages handled by the BeamHead workbench.
type Msg =
    | XFieldSizeChanged of string
    | YFieldSizeChanged of string

/// Creates the initial BeamHead client model.
let init () = {
    XFieldSizeInput = "100"
    YFieldSizeInput = "100"
}

/// Applies a client message to the current model.
let update (msg: Msg) (model: Model) =
    match msg with
    | XFieldSizeChanged value -> { model with XFieldSizeInput = value }
    | YFieldSizeChanged value -> { model with YFieldSizeInput = value }

let private proofCube = JscadGeometry.createCuboid ProofCube.dimensions

let private fieldSizeInput (label: string) (value: string) (message: string -> Msg) (dispatch: Msg -> unit) =
    Html.label [
        prop.className "form-control w-full"
        prop.children [
            Html.div [
                prop.className "label"
                prop.children [ Html.span [ prop.className "label-text font-medium"; prop.text label ] ]
            ]
            Html.label [
                prop.className "input input-bordered flex items-center gap-2"
                prop.children [
                    Html.input [
                        prop.className "min-w-0 grow"
                        prop.type'.number
                        prop.value value
                        prop.onChange (message >> dispatch)
                    ]
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "mm" ]
                ]
            ]
        ]
    ]

let private controlsCard (model: Model) dispatch =
    Html.aside [
        prop.className "card border border-base-300 bg-base-100 shadow-md"
        prop.children [
            Html.div [
                prop.className "card-body gap-5 p-5"
                prop.children [
                    Html.h2 [ prop.className "card-title text-lg"; prop.text "Geometry controls" ]

                    Html.label [
                        prop.className "form-control w-full"
                        prop.children [
                            Html.div [
                                prop.className "label"
                                prop.children [
                                    Html.span [ prop.className "label-text font-medium"; prop.text "Machine model" ]
                                ]
                            ]
                            Html.select [
                                prop.className "select select-bordered w-full"
                                prop.value "TrueBeam"
                                prop.onChange (fun (_: string) -> ())
                                prop.children [ Html.option [ prop.value "TrueBeam"; prop.text "TrueBeam" ] ]
                            ]
                        ]
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Jaws" ]

                    Html.div [
                        prop.className "space-y-2"
                        prop.children [
                            fieldSizeInput "X field size" model.XFieldSizeInput XFieldSizeChanged dispatch
                            fieldSizeInput "Y field size" model.YFieldSizeInput YFieldSizeChanged dispatch
                        ]
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "MLC" ]

                    Html.div [
                        prop.className "rounded-box bg-base-200 p-4"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center justify-between gap-3"
                                prop.children [
                                    Html.span [ prop.className "font-semibold"; prop.text "Retracted" ]
                                    Html.span [ prop.className "badge badge-outline"; prop.text "MLC" ]
                                ]
                            ]
                            Html.p [
                                prop.className "mt-1 text-sm text-base-content/70"
                                prop.text "Simplified two-bank geometry"
                            ]
                        ]
                    ]

                    Html.button [
                        prop.className "btn btn-primary mt-1 w-full"
                        prop.onClick (fun _ -> JscadGeometry.downloadStl "beam-head-cube-100mm.stl" proofCube)
                        prop.text "Export STL"
                    ]
                ]
            ]
        ]
    ]

let private viewerCard =
    Html.section [
        prop.className "card overflow-hidden border border-base-300 bg-base-100 shadow-md"
        prop.children [
            Html.div [
                prop.className "border-b border-base-300 px-5 py-4"
                prop.children [
                    Html.h2 [ prop.className "text-lg font-semibold"; prop.text "3D preview" ]
                    Html.p [
                        prop.className "text-sm text-base-content/60"
                        prop.text "100 x 100 x 100 mm proof cube"
                    ]
                ]
            ]
            Html.div [ prop.className "p-3"; prop.children [ CubeViewer.View proofCube ] ]
        ]
    ]

/// Renders the current BeamHead client.
let view (model: Model) (dispatch: Msg -> unit) =
    Html.main [
        prop.className "min-h-screen w-full bg-base-200 text-base-content"
        prop.children [
            Html.section [
                prop.className "mx-auto w-[96vw] max-w-[1600px] p-4"
                prop.children [
                    Html.div [
                        prop.className "navbar rounded-box border border-base-300 bg-base-100 px-4 shadow-md"
                        prop.children [
                            Html.div [
                                prop.className "navbar-start"
                                prop.children [
                                    Html.h1 [ prop.className "text-xl font-bold tracking-tight"; prop.text "BeamHead" ]
                                ]
                            ]
                        ]
                    ]

                    Html.div [
                        prop.className "mt-4 grid items-start gap-4 lg:grid-cols-[20rem_minmax(0,1fr)]"
                        prop.children [ controlsCard model dispatch; viewerCard ]
                    ]
                ]
            ]
        ]
    ]