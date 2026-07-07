using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Application.Models;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Boards;

/// <summary>
///     A resource living on board A must return 404 (or an empty page) when
///     addressed through the paths of board B — data must never leak between
///     boards.
/// </summary>
public class BoardIsolationEndpointTest : IntegrationTestBase
{
    private Guid _otherBoardId;

    private string CharactersEndpoint => $"/v1/boards/{BoardId}/characters";

    private string OtherCharactersEndpoint => $"/v1/boards/{_otherBoardId}/characters";

    private string OtherFactsEndpoint => $"/v1/boards/{_otherBoardId}/facts";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _otherBoardId = await CreateBoardAsync("Other board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetCharacterById_Through_Other_Board_GetNotFound()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Strahd");

        // Act
        var response = await Client.GetAsync($"{OtherCharactersEndpoint}/{characterId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Character of board A must not be readable via board B");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetCharacterPage_Through_Other_Board_ReturnsEmptyPage()
    {
        // Arrange
        await CreateCharacterAsync("Strahd");
        const string query = "?pageNumber=1&pageSize=10&sortType=Name&sortOrder=Asc";

        // Act
        var response = await Client.GetAsync($"{OtherCharactersEndpoint}{query}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<CharacterPayloadWithRelations[]>();
        page.ShouldBeEmpty("Characters of board A must not appear in board B pages");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task SearchCharacters_Through_Other_Board_ReturnsEmptyPage()
    {
        // Arrange
        await CreateCharacterAsync("Strahd");
        const string query = "?pageNumber=1&pageSize=10&sortType=Name&sortOrder=Asc&nameFilter=Strahd";

        // Act
        var response = await Client.GetAsync($"{OtherCharactersEndpoint}{query}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<CharacterPayloadWithRelations[]>();
        page.ShouldBeEmpty("Search must not find characters of another board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetCharacterPage_For_Not_Existing_Board_GetNotFound()
    {
        // Arrange
        const string query = "?pageNumber=1&pageSize=10&sortType=Name&sortOrder=Asc";

        // Act
        var response = await Client.GetAsync(
            $"/v1/boards/{Guid.CreateVersion7()}/characters{query}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Paging a not existing board must return 404");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task CreateCharacter_On_Not_Existing_Board_GetNotFound()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            $"/v1/boards/{Guid.CreateVersion7()}/characters",
            new
            {
                Name = "Orphan"
            },
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Creating a character on a not existing board must return 404");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task FindPath_Through_Other_Board_GetNotFound()
    {
        // Arrange
        var fromCharacterId = await CreateCharacterAsync("Strahd");
        var toCharacterId = await CreateCharacterAsync("Ireena");
        await CreateKnowRelationAsync(fromCharacterId, toCharacterId);

        // Act
        var response = await Client.GetAsync(
            $"{OtherCharactersEndpoint}/path/{fromCharacterId}/to/{toCharacterId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Path finding must not see characters of another board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetKnowRelation_Through_Other_Board_GetNotFound()
    {
        // Arrange
        var fromCharacterId = await CreateCharacterAsync("Strahd");
        var toCharacterId = await CreateCharacterAsync("Ireena");
        await CreateKnowRelationAsync(fromCharacterId, toCharacterId);

        // Act
        var response = await Client.GetAsync(
            $"{OtherCharactersEndpoint}/knows/{fromCharacterId}/to/{toCharacterId}",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Relations of board A must not be readable via board B");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task GetFact_Through_Other_Board_GetNotFound()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Strahd");
        var factId = await CreateFactAsync(characterId);

        // Act
        var response = await Client.GetAsync($"{OtherFactsEndpoint}/{factId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Facts of board A must not be readable via board B");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task DeleteCharacter_Through_Other_Board_GetNotFound_And_CharacterSurvives()
    {
        // Arrange
        var characterId = await CreateCharacterAsync("Strahd");

        // Act
        var response = await Client.DeleteAsync($"{OtherCharactersEndpoint}/{characterId}", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "Deleting through another board must return 404");

        var getResponse = await Client.GetAsync($"{CharactersEndpoint}/{characterId}", CancellationToken.None);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK, "The character must still exist on its own board");
    }

    private async Task<Guid> CreateCharacterAsync(string name)
    {
        var response = await Client.PostAsJsonAsync(
            CharactersEndpoint,
            new
            {
                Name = name
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task CreateKnowRelationAsync(Guid fromCharacterId, Guid toCharacterId)
    {
        var response = await Client.PostAsJsonAsync(
            $"{CharactersEndpoint}/knows",
            new
            {
                FromCharacterId = fromCharacterId,
                ToCharacterId = toCharacterId,
                Description = "Knows",
                IsStrongRelation = true
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateFactAsync(Guid characterId)
    {
        var response = await Client.PutAsJsonAsync(
            $"{CharactersEndpoint}/{characterId}/facts",
            new
            {
                Title = "Fact",
                Content = "Content"
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
