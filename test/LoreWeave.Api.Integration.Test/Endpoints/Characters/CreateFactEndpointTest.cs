using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class CreateFactEndpointTest : IntegrationTestBase
{
    public const string CharacterEndpoint = "/v1/characters";

    private static string FactEndpoint(Guid characterId) => $"/v1/characters/{characterId}/facts";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Fact_For_Existing_Character_CreatedStatusCode()
    {
        // Arrange
        var characterId = await CreateCharacterAsync();

        var createFactRequest = new
        {
            Title = "The Broken Crown",
            Content = "A relic lost in the northern wastes."
        };

        // Act
        var response = await Client.PutAsJsonAsync(FactEndpoint(characterId), createFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Fact_For_NotExisting_Character_NotFoundStatusCode()
    {
        // Arrange
        var createFactRequest = new
        {
            Title = "The Broken Crown",
            Content = "A relic lost in the northern wastes."
        };

        // Act
        var response = await Client.PutAsJsonAsync(
            FactEndpoint(Guid.CreateVersion7()), createFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(101)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Fact_Title_Is_Too_Long_BadRequest(int titleLength)
    {
        // Arrange
        var characterId = await CreateCharacterAsync();

        var createFactRequest = new
        {
            Title = new string('*', titleLength),
            Content = "A relic lost in the northern wastes."
        };

        // Act
        var response = await Client.PutAsJsonAsync(FactEndpoint(characterId), createFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateCharacterAsync()
    {
        var createCharacterResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "Hero" }, CancellationToken.None);

        return await createCharacterResponse.Content.ReadFromJsonAsync<Guid>();
    }
}