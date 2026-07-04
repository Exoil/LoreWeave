using System;

using LoreWeave.Api.Dtos.Facts;

using Shouldly;

namespace LoreWeave.Api.Test.Dtos.Facts;

public class UpdateFactDtoTest
{
    [Theory]
    [InlineData("\"3\"", (ushort)3)]
    [InlineData("7", (ushort)7)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToCommand_MapsAllPropertiesAndParsesVersion(string version, ushort expectedVersion)
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateFactDto = new UpdateFactDto("Title", "Content");

        // Act
        var command = updateFactDto.ToCommand(id, version);

        // Assert
        command.Id.ShouldBe(id, "Id should be taken from the route parameter");
        command.Title.ShouldBe(updateFactDto.Title, "Title should be mapped from the dto");
        command.Content.ShouldBe(updateFactDto.Content, "Content should be mapped from the dto");
        command.Version.ShouldBe(expectedVersion, "Version should be parsed from the If-Match header");
    }
}