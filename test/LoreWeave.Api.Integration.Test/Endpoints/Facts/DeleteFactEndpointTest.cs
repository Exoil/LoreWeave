using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Facts;

public class DeleteFactEndpointTest : IntegrationTestBase
{
    public const string CharacterEndpoint = "/v1/characters";
    public const string FactEndpoint = "/v1/facts";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_Existing_Fact_NoContentStatusCode()
    {
        // Arrange
        var factId = await CreateFactAsync();

        // Act
        var response = await Client.DeleteAsync($"{FactEndpoint}/{factId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"{FactEndpoint}/{factId}", CancellationToken.None);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_NotExisting_Fact_NotFoundStatusCode()
    {
        // Act
        var response = await Client.DeleteAsync($"{FactEndpoint}/{Guid.CreateVersion7()}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateFactAsync()
    {
        var createCharacterResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "Hero" }, CancellationToken.None);
        var characterId = await createCharacterResponse.Content.ReadFromJsonAsync<Guid>();

        var createFactResponse = await Client.PutAsJsonAsync(
            $"{CharacterEndpoint}/{characterId}/facts",
            new { Title = "The Broken Crown", Content = "A relic lost in the northern wastes." },
            CancellationToken.None);

        return await createFactResponse.Content.ReadFromJsonAsync<Guid>();
    }
}