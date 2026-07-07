using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Application.Models;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class GetCharacterByIdEndpointTest : IntegrationTestBase
{
    private string Endpoint => $"/v1/boards/{BoardId}/characters";

    [Fact]
    public async Task GetCharacter_GetOk()
    {
        // Arrange
        var requestPayload = new
        {
            Name = "Test"
        };

        var response = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);
        var characterId = await response.Content.ReadFromJsonAsync<Guid>();

        // Act
        var responseGet = await Client.GetAsync($"{Endpoint}/{characterId}", CancellationToken.None);

        // Assert
        responseGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        const int expectedVersion = 1;
        responseGet.Headers.ETag!.Tag.ShouldBe($"\"{expectedVersion}\"");

        var payload = await responseGet.Content.ReadFromJsonAsync<CharacterPayload>();

        payload.Id.ShouldBe(characterId);
        payload.Name.ShouldBe(requestPayload.Name);
    }

    [Fact]
    public async Task GetCharacter_GetNotFound()
    {
        // Arrange
        var characterId = Guid.CreateVersion7();

        // Act
        var responseGet = await Client.GetAsync($"{Endpoint}/{characterId}", CancellationToken.None);

        // Assert
        responseGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
