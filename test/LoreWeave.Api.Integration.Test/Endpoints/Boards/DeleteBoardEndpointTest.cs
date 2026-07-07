using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

public class DeleteBoardEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/boards";

    private string CharactersEndpoint => $"/v1/boards/{BoardId}/characters";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_Board_Cascades_To_Characters_Relations_And_Facts()
    {
        // Arrange — board with two related characters, a fact and an orphaned fact
        var fromCharacterId = await CreateCharacterAsync("Strahd");
        var toCharacterId = await CreateCharacterAsync("Ireena");
        await CreateKnowRelationAsync(fromCharacterId, toCharacterId);
        var factId = await CreateFactAsync(fromCharacterId);
        await DisconnectFactAsync(fromCharacterId, factId);

        // Act
        var response = await Client.DeleteAsync($"{Endpoint}/{BoardId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"{Endpoint}/{BoardId}", CancellationToken.None);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Deleted board must be gone");

        var remainingNodes = await CountBoardNodesAsync();
        remainingNodes.ShouldBe(0, "All characters and facts of the board (including orphaned facts) must be deleted");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_Not_Existing_Board_GetNotFound()
    {
        // Arrange
        var unknownBoardId = Guid.CreateVersion7();

        // Act
        var response = await Client.DeleteAsync($"{Endpoint}/{unknownBoardId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_Board_Does_Not_Touch_Other_Boards()
    {
        // Arrange
        var survivorBoardId = await CreateBoardAsync("Survivor board");
        var survivorCharacterId = await CreateCharacterAsync("Survivor", survivorBoardId);
        await CreateCharacterAsync("Victim");

        // Act
        var response = await Client.DeleteAsync($"{Endpoint}/{BoardId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var survivorResponse = await Client.GetAsync(
            $"/v1/boards/{survivorBoardId}/characters/{survivorCharacterId}",
            CancellationToken.None);
        survivorResponse.StatusCode.ShouldBe(HttpStatusCode.OK, "Characters of other boards must survive the cascade");
    }

    private async Task<Guid> CreateCharacterAsync(string name, Guid? boardId = null)
    {
        var endpoint = boardId is null ? CharactersEndpoint : $"/v1/boards/{boardId}/characters";
        var response = await Client.PostAsJsonAsync(
            endpoint,
            new
            {
                Name = name
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task CreateKnowRelationAsync(Guid fromCharacterId, Guid toCharacterId)
    {
        var response = await Client.PostAsJsonAsync(
            $"{CharactersEndpoint}/knows",
            new
            {
                FromCharacterId = fromCharacterId,
                ToCharacterId = toCharacterId,
                Description = "Knows",
                IsStrongRelation = true
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateFactAsync(Guid characterId)
    {
        var response = await Client.PutAsJsonAsync(
            $"{CharactersEndpoint}/{characterId}/facts",
            new
            {
                Title = "Fact",
                Content = "Content"
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task DisconnectFactAsync(Guid characterId, Guid factId)
    {
        var response = await Client.DeleteAsync(
            $"{CharactersEndpoint}/{characterId}/facts/{factId}",
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> CountBoardNodesAsync()
    {
        await using var driver = await GetDriverAsync();
        await using var session = driver.AsyncSession();

        var cursor = await session.RunAsync(
            "MATCH (n) WHERE (n:Character OR n:Fact) AND n.BoardId = $BoardId RETURN count(n) AS Count",
            new { BoardId = BoardId.ToString().ToLowerInvariant() });
        var record = await cursor.SingleAsync();

        return record["Count"].As<int>();
    }
}
