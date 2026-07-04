using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Facts;

public class UpdateFactEndpointTest : IntegrationTestBase
{
    public const string CharacterEndpoint = "/v1/characters";
    public const string FactEndpoint = "/v1/facts";

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Existing_Fact_NoContentStatusCode()
    {
        // Arrange
        var factId = await CreateFactAsync();
        var updateFactRequest = new
        {
            Title = "The Mended Crown",
            Content = "Reforged in the fires of the south."
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{FactEndpoint}/{factId}", updateFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await AssertFact(factId, updateFactRequest.Title, updateFactRequest.Content, "\"2\"");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Fact_With_Old_Version_PreconditionFailedStatusCode()
    {
        // Arrange
        var factId = await CreateFactAsync();
        var updateFactRequest = new
        {
            Title = "The Mended Crown",
            Content = "Reforged in the fires of the south."
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"9\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{FactEndpoint}/{factId}", updateFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_NotExisting_Fact_NotFoundStatusCode()
    {
        // Arrange
        var updateFactRequest = new
        {
            Title = "The Mended Crown",
            Content = "Reforged in the fires of the south."
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{FactEndpoint}/{Guid.CreateVersion7()}", updateFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(101)]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task Update_Fact_Title_Is_Too_Long_BadRequestStatusCode(int titleLength)
    {
        // Arrange
        var factId = await CreateFactAsync();
        var updateFactRequest = new
        {
            Title = new string('*', titleLength),
            Content = "Reforged in the fires of the south."
        };
        Client.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{FactEndpoint}/{factId}", updateFactRequest, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateFactAsync()
    {
        var createCharacterResponse =
            await Client.PostAsJsonAsync(CharacterEndpoint, new { Name = "Hero" }, CancellationToken.None);
        var characterId = await createCharacterResponse.Content.ReadFromJsonAsync<Guid>();

        var createFactResponse = await Client.PutAsJsonAsync(
            $"{CharacterEndpoint}/{characterId}/facts",
            new { Title = "The Broken Crown", Content = "A relic lost in the northern wastes." },
            CancellationToken.None);

        return await createFactResponse.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task AssertFact(Guid factId, string title, string content, string etag)
    {
        Client.DefaultRequestHeaders.IfMatch.Clear();
        var response = await Client.GetAsync($"{FactEndpoint}/{factId}", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag.Tag.ShouldBe(etag);

        var fact = await response.Content.ReadFromJsonAsync<FactResponse>();
        fact.ShouldNotBeNull();
        fact.Title.ShouldBe(title);
        fact.Content.ShouldBe(content);
    }

    private sealed record FactResponse(Guid Id, string Title, string Content);
}