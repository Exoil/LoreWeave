using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver;

using LoreWeave.Domain.Extensions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class DisconnectFactFromCharacterEndpointTest : IntegrationTestBase
{
    public const string CharacterEndpoint = "/v1/characters";
    public const string FactEndpoint = "/v1/facts";

    private static string ConnectionEndpoint(Guid characterId, Guid factId) =>
        $"{CharacterEndpoint}/{characterId}/facts/{factId}";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Disconnect_Connected_Fact_NoContentStatusCodeAndKeepsFact()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(characterId);

        // Act
        var response = await Client.DeleteAsync(ConnectionEndpoint(characterId, factId), CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CountConnectionsAsync(characterId, factId)).ShouldBe(0);

        var getFactResponse = await Client.GetAsync($"{FactEndpoint}/{factId}", CancellationToken.None);
        getFactResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Disconnect_NotConnected_Fact_NotFoundStatusCode()
    {
        // Arrange
        var factOwnerId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(factOwnerId);
        var characterId = await CreateCharacterAsync("Companion");

        // Act
        var response = await Client.DeleteAsync(ConnectionEndpoint(characterId, factId), CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Disconnect_Fact_From_NotExisting_Character_NotFoundStatusCode()
    {
        // Arrange
        var factOwnerId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(factOwnerId);

        // Act
        var response = await Client.DeleteAsync(
            ConnectionEndpoint(Guid.CreateVersion7(), factId), CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Disconnect_NotExisting_Fact_NotFoundStatusCode()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Hero");

        // Act
        var response = await Client.DeleteAsync(
            ConnectionEndpoint(characterId, Guid.CreateVersion7()), CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateCharacterAsync(string name)
    {
        var createCharacterResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = name }, CancellationToken.None);

        return await createCharacterResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateFactAsync(Guid characterId)
    {
        var createFactResponse = await Client.PutAsJsonAsync(
            $"{CharacterEndpoint}/{characterId}/facts",
            new { Title = "The Broken Crown", Content = "A relic lost in the northern wastes." },
            CancellationToken.None);

        return await createFactResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<int> CountConnectionsAsync(Guid characterId, Guid factId)
    {
        await using var driver = await GetDriverAsync();
        await using var session = driver.AsyncSession();
        await using var transaction = await session.BeginTransactionAsync();

        var query = new Query(
            @"MATCH (ch:Character {Id: $CharacterId})-[r:HAS_FACT]->(f:Fact {Id: $FactId})
              RETURN count(r) AS ConnectionCount",
            new
            {
                CharacterId = characterId.ToDatabaseId(),
                FactId = factId.ToDatabaseId()
            });

        var cursorResult = await transaction.RunAsync(query);
        var record = await cursorResult.SingleAsync();

        return record["ConnectionCount"].As<int>();
    }
}