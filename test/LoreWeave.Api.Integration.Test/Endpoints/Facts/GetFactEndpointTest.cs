using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Facts;

public class GetFactEndpointTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";
    private string FactEndpoint => $"/v1/boards/{BoardId}/facts";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Get_Existing_Fact_OkStatusCodeWithEtag()
    {
        // Arrange
        var factId = await CreateFactAsync("The Broken Crown", "A relic lost in the northern wastes.");

        // Act
        var response = await Client.GetAsync($"{FactEndpoint}/{factId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag.Tag.ShouldBe("\"1\"");

        var fact = await response.Content.ReadFromJsonAsync<FactResponse>();
        fact.ShouldNotBeNull();
        fact.Id.ShouldBe(factId);
        fact.Title.ShouldBe("The Broken Crown");
        fact.Content.ShouldBe("A relic lost in the northern wastes.");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Get_NotExisting_Fact_NotFoundStatusCode()
    {
        // Act
        var response = await Client.GetAsync($"{FactEndpoint}/{Guid.CreateVersion7()}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateFactAsync(string title, string content)
    {
        var createCharacterResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "Hero" }, CancellationToken.None);
        var characterId = await createCharacterResponse.Content.ReadFromJsonAsync<Guid>();

        var createFactResponse = await Client.PutAsJsonAsync(
            $"{CharacterEndpoint}/{characterId}/facts",
            new { Title = title, Content = content },
            CancellationToken.None);

        return await createFactResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private sealed record FactResponse(Guid Id, string Title, string Content);
}