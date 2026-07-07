using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Api.Dtos.Boards;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

public class GetBoardByIdEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/boards";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetBoard_GetOk_WithEtag()
    {
        // Act
        var response = await Client.GetAsync($"{Endpoint}/{BoardId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        const int expectedVersion = 1;
        response.Headers.ETag!.Tag.ShouldBe($"\"{expectedVersion}\"");

        var board = await response.Content.ReadFromJsonAsync<BoardDto>();
        board!.Id.ShouldBe(BoardId);
        board.Name.ShouldBe("Test board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetBoard_GetNotFound()
    {
        // Arrange
        var unknownBoardId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetAsync($"{Endpoint}/{unknownBoardId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
