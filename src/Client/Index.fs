module Index

open BeamHead.Domain
open Feliz

/// Represents the static TrueBeam workbench state.
type Model = unit

/// Represents messages handled by the static TrueBeam workbench.
type Msg = | NoOp

/// Creates the initial BeamHead client model.
let init () = ()

/// Applies a client message to the current model.
let update (_: Msg) (model: Model) = model

let private trueBeamJaws = JscadGeometry.createJaws TrueBeamJaws.placements

let private staticFieldSize (label: string) =
    Html.label [
        prop.className "form-control w-full"
        prop.children [
            Html.div [
                prop.className "label"
                prop.children [ Html.span [ prop.className "label-text font-medium"; prop.text label ] ]
            ]
            Html.div [
                prop.className "input input-bordered flex items-center gap-2 bg-base-200"
                prop.children [
                    Html.span [ prop.className "grow"; prop.text "400" ]
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "mm" ]
                ]
            ]
        ]
    ]

let private controlsCard =
    Html.aside [
        prop.className "card border border-base-300 bg-base-100 shadow-md"
        prop.children [
            Html.div [
                prop.className "card-body gap-5 p-5"
                prop.children [
                    Html.h2 [ prop.className "card-title text-lg"; prop.text "Geometry" ]

                    Html.label [
                        prop.className "form-control w-full"
                        prop.children [
                            Html.div [
                                prop.className "label"
                                prop.children [
                                    Html.span [ prop.className "label-text font-medium"; prop.text "Machine model" ]
                                ]
                            ]
                            Html.div [
                                prop.className "select select-bordered flex w-full items-center bg-base-200"
                                prop.text "TrueBeam"
                            ]
                        ]
                    ]

                    Html.div [ prop.className "divider my-0"; prop.text "Static jaws" ]

                    Html.div [
                        prop.className "space-y-2"
                        prop.children [ staticFieldSize "X field size"; staticFieldSize "Y field size" ]
                    ]

                    Html.p [
                        prop.className "text-sm text-base-content/70"
                        prop.text "Fixed 400 x 400 mm field at isocentre"
                    ]

                    Html.button [
                        prop.className "btn btn-primary mt-1 w-full"
                        prop.onClick (fun _ -> JscadGeometry.downloadStl "truebeam-jaws-400x400mm.stl" trueBeamJaws)
                        prop.text "Export four-jaw STL"
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
                        prop.text "TrueBeam X and Y jaws - static 400 x 400 mm field"
                    ]
                ]
            ]
            Html.div [ prop.className "p-3"; prop.children [ GeometryViewer.View trueBeamJaws ] ]
        ]
    ]

/// Renders the current BeamHead client.
let view (_: Model) (_: Msg -> unit) =
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
                        prop.children [ controlsCard; viewerCard ]
                    ]
                ]
            ]
        ]
    ]