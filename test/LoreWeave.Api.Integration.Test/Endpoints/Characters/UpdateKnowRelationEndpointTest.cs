using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver;

using LoreWeave.Domain.Extensions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class UpdateKnowRelationEndpointTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";

    private string KnowEndpoint => $"/v1/boards/{BoardId}/characters/knows";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_KnowRelation_NoContent_And_Persisted()
    {
        // Arrange
        var (fromId, toId) = await CreateRelationAsync("Original", true);

        var updateRequest = new
        {
            Description = "Updated description",
            IsStrongRelation = false
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{KnowEndpoint}/{fromId}/to/{toId}", updateRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var (description, isStrong, version) = await GetRelationAsync(fromId, toId);
        description.ShouldBe("Updated description");
        isStrong.ShouldBeFalse();
        version.ShouldBe(2);
    }

    [Theory]
    [InlineData(257)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_KnowRelation_Description_Is_Too_Long_BadRequest(int descriptionLength)
    {
        // Arrange
        var (fromId, toId) = await CreateRelationAsync("Original", true);

        var updateRequest = new
        {
            Description = new string('*', descriptionLength),
            IsStrongRelation = true
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{KnowEndpoint}/{fromId}/to/{toId}", updateRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_KnowRelation_With_Old_Version_PreconditionFailed()
    {
        // Arrange
        var (fromId, toId) = await CreateRelationAsync("Original", true);

        var updateRequest = new
        {
            Description = "Updated description",
            IsStrongRelation = false
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"99\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{KnowEndpoint}/{fromId}/to/{toId}", updateRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_NotExisting_KnowRelation_NotFound()
    {
        // Arrange — both characters exist but no relation between them
        var fromResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "From" }, CancellationToken.None);
        var toResponse = await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "To" }, CancellationToken.None);
        var fromId = await fromResponse.Content.ReadFromJsonAsync<Guid>();
        var toId = await toResponse.Content.ReadFromJsonAsync<Guid>();

        var updateRequest = new
        {
            Description = "Updated description",
            IsStrongRelation = false
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{KnowEndpoint}/{fromId}/to/{toId}", updateRequest, CancellationToken.None);

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

    private async Task<(string Description, bool IsStrong, int Version)> GetRelationAsync(Guid fromId, Guid toId)
    {
        await using var driver = await GetDriverAsync();
        await using var session = driver.AsyncSession();
        await using var transaction = await session.BeginTransactionAsync();

        const string queryString = @"
            MATCH (:Character {Id: $FromCharacterId})-[r:KNOWS]->(:Character {Id: $ToCharacterId})
            RETURN r.Description AS Description, r.IsStrong AS IsStrong, r.Version AS Version";
        var query = new Query(queryString, new
        {
            FromCharacterId = fromId.ToDatabaseId(),
            ToCharacterId = toId.ToDatabaseId()
        });

        var cursor = await transaction.RunAsync(query);
        var record = await cursor.SingleAsync();

        return (record["Description"].As<string>(), record["IsStrong"].As<bool>(), record["Version"].As<int>());
    }
}