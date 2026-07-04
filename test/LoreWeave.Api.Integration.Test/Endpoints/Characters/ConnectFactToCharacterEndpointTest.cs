using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver;

using LoreWeave.Domain.Extensions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class ConnectFactToCharacterEndpointTest : IntegrationTestBase
{
    public const string CharacterEndpoint = "/v1/characters";

    private static string ConnectEndpoint(Guid characterId, Guid factId) =>
        $"{CharacterEndpoint}/{characterId}/facts/{factId}";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Connect_Existing_Fact_To_Existing_Character_NoContentStatusCode()
    {
        // Arrange
        var factOwnerId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(factOwnerId);
        var characterId = await CreateCharacterAsync("Companion");

        // Act
        var response = await Client.PutAsync(ConnectEndpoint(characterId, factId), null, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CountConnectionsAsync(characterId, factId)).ShouldBe(1);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Connect_Same_Fact_Twice_KeepsSingleConnection()
    {
        // Arrange
        var factOwnerId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(factOwnerId);
        var characterId = await CreateCharacterAsync("Companion");

        // Act
        var firstResponse = await Client.PutAsync(ConnectEndpoint(characterId, factId), null, CancellationToken.None);
        var secondResponse = await Client.PutAsync(ConnectEndpoint(characterId, factId), null, CancellationToken.None);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CountConnectionsAsync(characterId, factId)).ShouldBe(1);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Connect_Fact_To_NotExisting_Character_NotFoundStatusCode()
    {
        // Arrange
        var factOwnerId = await CreateCharacterAsync("Hero");
        var factId = await CreateFactAsync(factOwnerId);

        // Act
        var response = await Client.PutAsync(
            ConnectEndpoint(Guid.CreateVersion7(), factId), null, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Connect_NotExisting_Fact_To_Character_NotFoundStatusCode()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Hero");

        // Act
        var response = await Client.PutAsync(
            ConnectEndpoint(characterId, Guid.CreateVersion7()), null, CancellationToken.None);

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