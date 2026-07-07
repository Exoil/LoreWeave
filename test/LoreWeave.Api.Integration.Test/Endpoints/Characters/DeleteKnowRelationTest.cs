using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class DeleteKnowRelationTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";

    private string KnowEndpoint => $"/v1/boards/{BoardId}/characters/knows";

    private Uri GetDeleteKnowRelationEndpoint(Guid from, Guid to) =>
        new($"{KnowEndpoint}/{from}/to/{to}", UriKind.Relative);

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_KnowRelation_NotContentCode()
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
            Description = "Test"
        };

        await Client
            .PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);

        // Act
        var response = await Client.DeleteAsync(
            GetDeleteKnowRelationEndpoint(fromCharacterId, toCharacterId),
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Delete_KnowRelation_WhenIds_Are_Same_BadRequestCode()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var response = await Client.DeleteAsync(
            GetDeleteKnowRelationEndpoint(
                id,
                id),
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
