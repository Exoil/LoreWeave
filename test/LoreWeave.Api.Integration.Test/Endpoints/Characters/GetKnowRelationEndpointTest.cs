using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class GetKnowRelationEndpointTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";

    private string KnowEndpoint => $"/v1/boards/{BoardId}/characters/knows";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetKnowRelation_Ok_VersionInHeader_NotInBody()
    {
        // Arrange
        var (fromId, toId) = await CreateRelationAsync("Test description", isStrongRelation: true);

        // Act
        var response = await Client.GetAsync($"{KnowEndpoint}/{fromId}/to/{toId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        const int expectedVersion = 1;
        response.Headers.ETag!.Tag.ShouldBe($"\"{expectedVersion}\"");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("fromCharacterId").GetGuid().ShouldBe(fromId);
        body.GetProperty("toCharacterId").GetGuid().ShouldBe(toId);
        body.GetProperty("description").GetString().ShouldBe("Test description");
        body.GetProperty("isStrongRelation").GetBoolean().ShouldBeTrue();
        body.TryGetProperty("version", out _).ShouldBeFalse("Version must not be present in the response body");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetKnowRelation_NotExisting_NotFound()
    {
        // Arrange — both characters exist but no relation between them
        var fromResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "From" }, CancellationToken.None);
        var toResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "To" }, CancellationToken.None);
        var fromId = await fromResponse.Content.ReadFromJsonAsync<Guid>();
        var toId = await toResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.GetAsync($"{KnowEndpoint}/{fromId}/to/{toId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(Guid FromId, Guid ToId)> CreateRelationAsync(string description, bool isStrongRelation)
    {
        var fromResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "From" }, CancellationToken.None);
        var toResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "To" }, CancellationToken.None);

        var fromCharacterId = await fromResponse.Content.ReadFromJsonAsync<Guid>();
        var toCharacterId = await toResponse.Content.ReadFromJsonAsync<Guid>();

        var createRequest = new
        {
            FromCharacterId = fromCharacterId,
            ToCharacterId = toCharacterId,
            Description = description,
            IsStrongRelation = isStrongRelation
        };

        var createResponse = await Client.PostAsJsonAsync(KnowEndpoint, createRequest, CancellationToken.None);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (fromCharacterId, toCharacterId);
    }
}
