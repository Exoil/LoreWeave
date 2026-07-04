using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Domain.Extensions;
using LoreWeave.Infrastructure.Repositories.Characters;
using LoreWeave.Infrastructure.Transactions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class CreateCharacterEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/characters";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Character_Get_CreatedStatusCode()
    {
        // Arrange
        var requestPayload = new
        {
            Name = "Test"
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        await AssertCharacter(id, requestPayload.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Character_With_Invalid_Name_GetBadRequest(int nameLength)
    {
        // Arrange
        var requestPayload = new
        {
            Name = new string('*', nameLength)
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
