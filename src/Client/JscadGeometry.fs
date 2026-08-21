module JscadGeometry

open BeamHead.Domain
open Fable.Core

[<Import("createCuboid", "./BeamHeadJscad.js")>]
let private createJscadCuboid (width: float) (depth: float) (height: float) : obj = jsNative

[<Import("downloadStl", "./BeamHeadJscad.js")>]
let private downloadJscadStl (fileName: string) (geometry: obj) : unit = jsNative

/// Translates technology-independent cuboid dimensions into JSCAD geometry.
let createCuboid (dimensions: CuboidDimensions) =
    createJscadCuboid dimensions.Width dimensions.Depth dimensions.Height

/// Downloads a JSCAD geometry as a binary STL file.
let downloadStl fileName geometry = downloadJscadStl fileName geometry