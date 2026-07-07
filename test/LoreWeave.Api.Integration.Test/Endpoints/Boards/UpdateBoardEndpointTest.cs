using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Api.Dtos.Boards;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

public class UpdateBoardEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/boards";

    private static object ValidUpdatePayload(string name = "Renamed board") => new
    {
        Name = name,
        Configuration = new
        {
            CharacterNodeColor = "#166534",
            FactNodeColor = "#9f1239",
            RelationEdgeColor = "#64748b",
            FactEdgeColor = "#f59e0b",
            PathHighlightColor = "#7c3aed",
            NodeRadius = 20,
            EdgeWidth = 4,
            CurvedEdges = false,
            ShowGrid = false,
            ScalingObjects = false
        }
    };

    private static HttpRequestMessage BuildUpdateRequest(string url, object payload, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("If-Match", version);

        return request;
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Board_GetNoContent_And_ChangesAreVisible()
    {
        // Arrange
        var request = BuildUpdateRequest($"{Endpoint}/{BoardId}", ValidUpdatePayload(), "\"1\"");

        // Act
        var response = await Client.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"{Endpoint}/{BoardId}", CancellationToken.None);
        getResponse.Headers.ETag!.Tag.ShouldBe("\"2\"", "Update must increment the version");

        var board = await getResponse.Content.ReadFromJsonAsync<BoardDto>();
        board!.Name.ShouldBe("Renamed board");
        board.Configuration.CharacterNodeColor.ShouldBe("#166534");
        board.Configuration.NodeRadius.ShouldBe(20);
        board.Configuration.EdgeWidth.ShouldBe(4);
        board.Configuration.CurvedEdges.ShouldBeFalse();
        board.Configuration.ShowGrid.ShouldBeFalse();
        board.Configuration.ScalingObjects.ShouldBeFalse();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Board_With_Wrong_Version_GetPreconditionFailed()
    {
        // Arrange
        var request = BuildUpdateRequest($"{Endpoint}/{BoardId}", ValidUpdatePayload(), "\"7\"");

        // Act
        var response = await Client.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Not_Existing_Board_GetNotFound()
    {
        // Arrange
        var request = BuildUpdateRequest($"{Endpoint}/{Guid.CreateVersion7()}", ValidUpdatePayload(), "\"1\"");

        // Act
        var response = await Client.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Board_With_Invalid_Configuration_GetBadRequest()
    {
        // Arrange
        var payload = new
        {
            Name = "Renamed board",
            Configuration = new
            {
                CharacterNodeColor = "not-a-colour",
                FactNodeColor = "#9f1239",
                RelationEdgeColor = "#64748b",
                FactEdgeColor = "#f59e0b",
                PathHighlightColor = "#7c3aed",
                NodeRadius = 100,
                EdgeWidth = 0,
                CurvedEdges = false,
                ShowGrid = false,
                ScalingObjects = false
            }
        };
        var request = BuildUpdateRequest($"{Endpoint}/{BoardId}", payload, "\"1\"");

        // Act
        var response = await Client.SendAsync(request, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
