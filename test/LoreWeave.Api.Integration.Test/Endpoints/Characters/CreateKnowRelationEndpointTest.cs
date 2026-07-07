using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Domain.Extensions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class CreateKnowRelationEndpointTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";

    private string KnowEndpoint => $"/v1/boards/{BoardId}/characters/knows";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_KnowRelation_CreatedStatusCode()
    {
        // Arrange
        var requestCreateCharacterFrom = new
        {
            Name = "From"
        };

        var requestCreateCharacterTo = new
        {
            Name = "To"
        };

        var fromCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterFrom, CancellationToken.None);
        var toCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterTo, CancellationToken.None);

        var fromCharacterId = await fromCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();
        var toCharacterId = await toCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var createRelationRequest = new
        {
            FromCharacterId = fromCharacterId,
            ToCharacterId = toCharacterId,
            Description = "Test",
            IsStrongRelation = true
        };

        // Act
        var response = await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_KnowRelation_For_Same_CharacterId_BadRequestStatusCode()
    {
        // Arrange
        var requestCreateCharacterFrom = new
        {
            Name = "From"
        };

        var fromCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterFrom, CancellationToken.None);

        var fromCharacterId = await fromCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var createRelationRequest = new
        {
            FromCharacterId = fromCharacterId,
            ToCharacterId = fromCharacterId,
            Description = "Test",
            IsStrongRelation = true
        };

        // Act
        var response = await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_KnowRelation_For_NotExisting_ToCharacter_UnprocessableStatusCode()
    {
        // Arrange
        var requestCreateCharacterFrom = new
        {
            Name = "From"
        };

        var fromCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterFrom, CancellationToken.None);

        var fromCharacterId = await fromCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var createRelationRequest = new
        {
            FromCharacterId = fromCharacterId,
            ToCharacterId = Guid.CreateVersion7(),
            Description = "Test",
            IsStrongRelation = true
        };

        // Act
        var response = await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_KnowRelation_For_NotExisting_FromCharacter_UnprocessableStatusCode()
    {
        // Arrange
        var requestCreateCharacterTo = new
        {
            Name = "To"
        };

        var toCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterTo, CancellationToken.None);

        var toCharacterId = await toCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var createRelationRequest = new
        {
            FromCharacterId = Guid.CreateVersion7(),
            ToCharacterId = toCharacterId,
            Description = "Test",
            IsStrongRelation = true
        };

        // Act
        var response = await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData(257)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_KnowRelation_Description_Is_Too_Long_BadRequest(int descriptionLenght)
    {
        // Arrange
        var requestCreateCharacterFrom = new
        {
            Name = "From"
        };

        var requestCreateCharacterTo = new
        {
            Name = "To"
        };

        var fromCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterFrom, CancellationToken.None);
        var toCharacterCreateResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, requestCreateCharacterTo, CancellationToken.None);

        var fromCharacterId = await fromCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();
        var toCharacterId = await toCharacterCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var createRelationRequest = new
        {
            FromCharacterId = fromCharacterId,
            ToCharacterId = toCharacterId,
            Description = new string('*', descriptionLenght),
            IsStrongRelation = true
        };

        // Act
        var response = await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
