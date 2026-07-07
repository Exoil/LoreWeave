using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Api.Dtos.Boards;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

public class CreateBoardEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/boards";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Board_Get_CreatedStatusCode_And_DefaultConfiguration()
    {
        // Arrange
        var requestPayload = new
        {
            Name = "Curse of Strahd"
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await Client.GetAsync($"{Endpoint}/{id}", CancellationToken.None);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var board = await getResponse.Content.ReadFromJsonAsync<BoardDto>();
        board!.Id.ShouldBe(id);
        board.Name.ShouldBe(requestPayload.Name);
        board.Configuration.CharacterNodeColor.ShouldBe("#4466cc", "Server must assign the default configuration");
        board.Configuration.FactNodeColor.ShouldBe("#d97706");
        board.Configuration.RelationEdgeColor.ShouldBe("#aaaaaa");
        board.Configuration.FactEdgeColor.ShouldBe("#d9a066");
        board.Configuration.PathHighlightColor.ShouldBe("#a855f7");
        board.Configuration.NodeRadius.ShouldBe(16);
        board.Configuration.EdgeWidth.ShouldBe(3);
        board.Configuration.CurvedEdges.ShouldBeTrue();
        board.Configuration.ShowGrid.ShouldBeTrue();
        board.Configuration.ScalingObjects.ShouldBeTrue();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Two_Boards_With_Same_Name_Both_Created()
    {
        // Arrange — Foundry auto-creates boards named after the world, so
        // duplicate names must be allowed (uniqueness is by id only).
        var requestPayload = new
        {
            Name = "Duplicated world"
        };

        // Act
        var firstResponse = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);
        var secondResponse = await Client.PostAsJsonAsync(Endpoint, requestPayload, CancellationToken.None);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var firstId = await firstResponse.Content.ReadFromJsonAsync<Guid>();
        var secondId = await secondResponse.Content.ReadFromJsonAsync<Guid>();
        firstId.ShouldNotBe(secondId, "Each board must get its own id");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Create_Board_With_Invalid_Name_GetBadRequest(int nameLength)
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
}
