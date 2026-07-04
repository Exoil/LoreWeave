using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Domain.Extensions;
using LoreWeave.Infrastructure.Repositories.Characters;
using LoreWeave.Infrastructure.Transactions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class UpdateCharacterEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/characters";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task UpdateCharacterName_GetNoContent()
    {
        // Arrange
        var dataWithUpdate = new
        {
            Name = "UpdatedName",
            Version = 1
        };
        var dataForCreateCharacter = new
        {
            Name = "Test"
        };

        var response = await Client.PostAsJsonAsync(Endpoint, dataForCreateCharacter, CancellationToken.None);
        var characterId = await response.Content.ReadFromJsonAsync<Guid>();
        const int expectedVersion = 1;
        Client.DefaultRequestHeaders.IfMatch.Add(
            new EntityTagHeaderValue($"\"{expectedVersion}\""));

        // Act
        var responseUpdate =
            await Client.PutAsJsonAsync($"{Endpoint}/{characterId}", dataWithUpdate, CancellationToken.None);

        // Assert
        responseUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await AssertCharacter(characterId, dataWithUpdate.Name);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task UpdateCharacterWithInvalidNameLength_GetBadRequest(int stringLength)
    {
        // Arrange
        var dataWithUpdate = new
        {
            Name = new string('*', stringLength),
            Version = 1
        };
        var dataForCreateCharacter = new
        {
            Name = "Test"
        };

        var response = await Client.PostAsJsonAsync(Endpoint, dataForCreateCharacter, CancellationToken.None);
        var characterId = await response.Content.ReadFromJsonAsync<Guid>();

        // Act
        const int expectedVersion = 1;
        Client.DefaultRequestHeaders.IfMatch.Add(
            new EntityTagHeaderValue($"\"{expectedVersion}\""));

        var responseUpdate =
            await Client.PutAsJsonAsync($"{Endpoint}/{characterId}", dataWithUpdate, CancellationToken.None);

        // Assert
        responseUpdate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task UpdateCharacterWithOldVersion_GetPreconditionFailed()
    {
        // Arrange
        var dataWithUpdate = new
        {
            Name = new string('*', 10),
            Version = 0
        };
        var dataForCreateCharacter = new
        {
            Name = "Test"
        };

        var response = await Client.PostAsJsonAsync(Endpoint, dataForCreateCharacter, CancellationToken.None);
        var characterId = await response.Content.ReadFromJsonAsync<Guid>();
        const int expectedVersion = 0;
        Client.DefaultRequestHeaders.IfMatch.Add(
            new EntityTagHeaderValue($"\"{expectedVersion}\""));

        // Act
        var responseUpdate =
            await Client.PutAsJsonAsync($"{Endpoint}/{characterId}", dataWithUpdate, CancellationToken.None);

        // Assert
        responseUpdate.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task UpdateNotExistingCharacterName()
    {
        // Arrange
        var characterId = Guid.CreateVersion7();
        var dataWithUpdate = new
        {
            Name = "Test",
            Verson = 1
        };

        // Act
        const int expectedVersion = 1;
        Client.DefaultRequestHeaders.IfMatch.Add(
            new EntityTagHeaderValue($"\"{expectedVersion}\""));

        var responseUpdate =
            await Client.PutAsJsonAsync($"{Endpoint}/{characterId}", dataWithUpdate, CancellationToken.None);

        // Assert
        responseUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task AssertCharacter(Guid id, string name)
    {
        await using var driver = await GetDriverAsync();
        await using var session = driver.AsyncSession();
        await using var transaction = await session.BeginTransactionAsync();

        var characterRepository = new CharacterRepository();
        var character = await characterRepository.GetAsync(new Neo4jTransaction(transaction), id);

        character.Name.ShouldBe(name);
    }
}
