using System;

using LoreWeave.Api.Dtos.Facts;
using LoreWeave.Api.Dtos.Maps;
using LoreWeave.Application.Models;

using Shouldly;

namespace LoreWeave.Api.Test.Dtos.Maps;

public class FactDtoMapperTest
{
    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToFactDto_MapsAllProperties()
    {
        // Arrange
        var factPayload = new FactPayload(Guid.NewGuid(), "Title", "Content", 3);

        // Act
        var factDto = factPayload.ToFactDto();

        // Assert
        factDto.ShouldBeOfType<FactDto>("Mapper should produce a FactDto");
        factDto.Id.ShouldBe(factPayload.Id, "Id should be mapped from the payload");
        factDto.Title.ShouldBe(factPayload.Title, "Title should be mapped from the payload");
        factDto.Content.ShouldBe(factPayload.Content, "Content should be mapped from the payload");
    }
}