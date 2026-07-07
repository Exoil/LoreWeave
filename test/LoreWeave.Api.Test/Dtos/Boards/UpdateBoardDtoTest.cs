using System;

using LoreWeave.Api.Dtos.Boards;

using Shouldly;

namespace LoreWeave.Api.Test.Dtos.Boards;

public class UpdateBoardDtoTest
{
    private static readonly BoardConfigurationDto Configuration = new(
        "#166534",
        "#9f1239",
        "#64748b",
        "#f59e0b",
        "#7c3aed",
        20,
        4,
        false,
        false,
        true);

    [Theory]
    [InlineData("\"3\"", (ushort)3)]
    [InlineData("7", (ushort)7)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToCommand_MapsAllPropertiesAndParsesVersion(string version, ushort expectedVersion)
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateBoardDto = new UpdateBoardDto("Curse of Strahd — Act II", Configuration);

        // Act
        var command = updateBoardDto.ToCommand(id, version);

        // Assert
        command.Id.ShouldBe(id, "Id should be taken from the route parameter");
        command.Name.ShouldBe(updateBoardDto.Name, "Name should be mapped from the dto");
        command.Configuration.NodeRadius.ShouldBe(Configuration.NodeRadius, "Configuration should be mapped from the dto");
        command.Version.ShouldBe(expectedVersion, "Version should be parsed from the If-Match header");
    }
}
