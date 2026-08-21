namespace BeamHead.Domain.Tests

open BeamHead.Domain
open Xunit

module ProofCubeTests =
    [<Fact>]
    let ``proof cube is 100 millimetres on every axis`` () =
        let dimensions = ProofCube.dimensions

        Assert.Equal(100.0, dimensions.Width)
        Assert.Equal(100.0, dimensions.Depth)
        Assert.Equal(100.0, dimensions.Height)