using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Api.Dtos.Boards;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

public class GetBoardsEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/boards";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetBoards_ReturnsAllBoards()
    {
        // Arrange — IntegrationTestBase already created "Test board"
        var strahdId = await CreateBoardAsync("Curse of Strahd");
        var waterdeepId = await CreateBoardAsync("Waterdeep");

        // Act
        var response = await Client.GetAsync(Endpoint, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var boards = await response.Content.ReadFromJsonAsync<BoardDto[]>();
        boards!.Length.ShouldBe(3, "The base board and the two created boards should be returned");
        boards.Select(board => board.Id).ShouldContain(strahdId);
        boards.Select(board => board.Id).ShouldContain(waterdeepId);
        boards.Select(board => board.Id).ShouldContain(BoardId);
        boards.ShouldAllBe(board => board.Configuration != null, "Every board carries its configuration");
    }
}
