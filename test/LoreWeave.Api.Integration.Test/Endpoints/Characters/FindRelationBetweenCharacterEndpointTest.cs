using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Extensions;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class FindRelationBetweenCharacterEndpointTest : IntegrationTestBase
{
    private string CharacterEndpoint => $"/v1/boards/{BoardId}/characters";
    private string KnowEndpoint => $"/v1/boards/{BoardId}/characters/knows";
    private string PathEndpoint => $"/v1/boards/{BoardId}/characters/path";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_DirectRelation_ReturnsOkWithTwoCharacters()
    {
        // Arrange
        var fromId = await CreateCharacterAsync("FromChar");
        var toId = await CreateCharacterAsync("ToChar");
        await CreateKnowRelationAsync(fromId, toId);

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{fromId}/to/{toId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "Should return OK when path exists");
        var payload = await response.Content.ReadFromJsonAsync<RelationPathPayload>();
        payload.ShouldNotBeNull("Response body should not be null");
        payload.CharacterIds.Count.ShouldBe(2, "Direct relation should have 2 characters in path");
        payload.Hops.ShouldBe(1, "Direct relation should have 1 hop");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_MultiHopChain_ReturnsFullPath()
    {
        // Arrange: A -> B -> C
        var charA = await CreateCharacterAsync("CharA");
        var charB = await CreateCharacterAsync("CharB");
        var charC = await CreateCharacterAsync("CharC");
        await CreateKnowRelationAsync(charA, charB);
        await CreateKnowRelationAsync(charB, charC);

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{charA}/to/{charC}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "Should return OK for multi-hop path");
        var payload = await response.Content.ReadFromJsonAsync<RelationPathPayload>();
        payload.ShouldNotBeNull("Response body should not be null");
        payload.CharacterIds.Count.ShouldBe(3, "Chain A->B->C should have 3 characters in path");
        payload.Hops.ShouldBe(2, "Chain A->B->C should have 2 hops");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_NoConnectionBetweenCharacters_ReturnsEmptyPath()
    {
        // Arrange
        var fromId = await CreateCharacterAsync("Isolated1");
        var toId = await CreateCharacterAsync("Isolated2");

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{fromId}/to/{toId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "Should return OK even when no path exists");
        var payload = await response.Content.ReadFromJsonAsync<RelationPathPayload>();
        payload.ShouldNotBeNull("Response body should not be null");
        payload.CharacterIds.Count.ShouldBe(0, "Should return empty path when characters are not connected");
        payload.Hops.ShouldBe(0, "Should return 0 hops when characters are not connected");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_FromCharacterNotFound_ReturnsNotFound()
    {
        // Arrange
        var toId = await CreateCharacterAsync("Existing");
        var nonExistentId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{nonExistentId}/to/{toId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Should return 404 when from character does not exist");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_ToCharacterNotFound_ReturnsNotFound()
    {
        // Arrange
        var fromId = await CreateCharacterAsync("Existing");
        var nonExistentId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{fromId}/to/{nonExistentId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Should return 404 when to character does not exist");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_WithMaxHopsLimit_ReturnsEmptyWhenPathExceedsLimit()
    {
        // Arrange: A -> B -> C (2 hops), but limit to 1
        var charA = await CreateCharacterAsync("CharA");
        var charB = await CreateCharacterAsync("CharB");
        var charC = await CreateCharacterAsync("CharC");
        await CreateKnowRelationAsync(charA, charB);
        await CreateKnowRelationAsync(charB, charC);

        // Act
        var response = await Client.GetAsync(
            $"{PathEndpoint}/{charA}/to/{charC}?maxHops=1",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "Should return OK even when path exceeds max hops");
        var payload = await response.Content.ReadFromJsonAsync<RelationPathPayload>();
        payload.ShouldNotBeNull("Response body should not be null");
        payload.CharacterIds.Count.ShouldBe(0, "Should return empty path when hops exceed maxHops limit");
        payload.Hops.ShouldBe(0, "Should return 0 hops when path exceeds limit");
    }

    private async Task<Guid> CreateCharacterAsync(string name)
    {
        var response = await Client.PostAsJsonAsync(
            CharacterEndpoint,
            new { Name = name },
            CancellationToken.None);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task CreateKnowRelationAsync(Guid fromId, Guid toId)
    {
        await Client.PostAsJsonAsync(
            KnowEndpoint,
            new
            {
                FromCharacterId = fromId,
                ToCharacterId = toId,
                Description = "Knows"
            },
            CancellationToken.None);
    }
}