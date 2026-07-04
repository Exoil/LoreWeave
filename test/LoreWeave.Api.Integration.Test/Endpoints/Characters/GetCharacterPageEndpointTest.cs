using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using LoreWeave.Application.Models;

using Shouldly;

namespace LoreWeave.Api.Integration.Test.Endpoints.Characters;

public class GetCharacterPageEndpointTest : IntegrationTestBase
{
    public const string Endpoint = "/v1/characters";

    public const string KnowEndpoint = "/v1/characters/knows";

    private static string FactEndpoint(Guid characterId) => $"{Endpoint}/{characterId}/facts";

    [Theory]
    [InlineData(1, 10, "Name", "Asc")]
    [InlineData(1, 10, "Name", "Desc")]
    public async Task GetCharacterPage_GetOk(
        int pageNumber,
        int pageSize,
        string sortType,
        string sortOrder)
    {
        // Arrange
        await _neo4JContainerRunner.ResetAsync();
        var expectedNames = Enumerable.Range(0, pageSize).Select(i => $"Test{i}").ToList();

        if (sortOrder == "Desc")
        {
            expectedNames.Reverse();
        }

        var dataToCreateCharacter = new List<object>();

        foreach (var name in expectedNames)
        {
            dataToCreateCharacter.Add(new
            {
                Name = name
            });
        }

        var characterIds = new List<Guid>();

        foreach (var dataToCreate in dataToCreateCharacter)
        {
            var response = await Client.PostAsJsonAsync(Endpoint, dataToCreate, CancellationToken.None);
            var characterId = await response.Content.ReadFromJsonAsync<Guid>();
            characterIds.Add(characterId);
        }

        var idFrom = Guid.Empty;
        var idTo = Guid.Empty;
        for (var i = 0; i < characterIds.Count; i++)
        {
            idFrom = characterIds[i];

            if (i == characterIds.Count - 1)
            {
                idTo = characterIds[0];
            }
            else
            {
                idTo = characterIds[(i + 1)];
            }
            var createRelationRequest = new
            {
                FromCharacterId = idFrom,
                ToCharacterId = idTo,
                Description = "Test",
                IsStrongRelation = true
            };

            await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);
        }

        var factIds = new List<Guid>();
        foreach (var characterId in characterIds)
        {
            var createFactRequest = new
            {
                Title = "TestFact",
                Content = "TestContent"
            };

            var factResponse = await Client.PutAsJsonAsync(
                FactEndpoint(characterId), createFactRequest, CancellationToken.None);
            factIds.Add(await factResponse.Content.ReadFromJsonAsync<Guid>());
        }

        var endpoint =
            $"{Endpoint}?pageNumber={pageNumber}&pageSize={pageSize}&sortType={sortType}&sortOrder={sortOrder}";

        // Act
        var reponse = await Client.GetAsync(endpoint);

        // Assert
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await reponse.Content.ReadFromJsonAsync<IEnumerable<CharacterPayloadWithRelations>>();
        var list = content!.ToList();

        list.Select(c => c.Name).ShouldBe(expectedNames);
        list.All(c => characterIds.Contains(c.Id)).ShouldBeTrue();
        list.ShouldAllBe(c => c.KnowCharacters.Count == 1);
        list.ShouldAllBe(c => c.KnowCharacters.First().Description == "Test");
        list.ShouldAllBe(c => c.KnowCharacters.First().IsStrongRelation);
        list.All(c => characterIds.Contains(c.KnowCharacters.First().CharacterId)).ShouldBeTrue();
        list.ShouldAllBe(c => c.Facts.Count == 1);
        list.ShouldAllBe(c => c.Facts.First().Title == "TestFact");
        list.ShouldAllBe(c => c.Facts.First().Content == "TestContent");
        list.All(c => factIds.Contains(c.Facts.First().Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetCharacterPage_WithFacts_ReturnsConnectedFactsPerCharacter()
    {
        // Arrange
        await _neo4JContainerRunner.ResetAsync();

        var withFactsResponse = await Client.PostAsJsonAsync(
            Endpoint, new { Name = "CharacterWithFacts" }, CancellationToken.None);
        var withFactsId = await withFactsResponse.Content.ReadFromJsonAsync<Guid>();

        var withoutFactsResponse = await Client.PostAsJsonAsync(
            Endpoint, new { Name = "CharacterWithoutFacts" }, CancellationToken.None);
        var withoutFactsId = await withoutFactsResponse.Content.ReadFromJsonAsync<Guid>();

        var expectedFacts = new[]
        {
            new { Title = "FactA", Content = "ContentA" },
            new { Title = "FactB", Content = "ContentB" }
        };

        var factIds = new List<Guid>();
        foreach (var fact in expectedFacts)
        {
            var factResponse = await Client.PutAsJsonAsync(
                FactEndpoint(withFactsId), fact, CancellationToken.None);
            factIds.Add(await factResponse.Content.ReadFromJsonAsync<Guid>());
        }

        var endpoint = $"{Endpoint}?pageNumber=1&pageSize=10&sortType=Name&sortOrder=Asc";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<IEnumerable<CharacterPayloadWithRelations>>();
        var list = content!.ToList();

        list.Count.ShouldBe(2);

        var withFacts = list.Single(c => c.Id == withFactsId);
        withFacts.Facts.Count.ShouldBe(2);
        withFacts.Facts.Select(f => f.Id).ShouldBe(factIds, ignoreOrder: true);
        withFacts.Facts.Select(f => f.Title).ShouldBe(expectedFacts.Select(f => f.Title), ignoreOrder: true);
        withFacts.Facts.Select(f => f.Content).ShouldBe(expectedFacts.Select(f => f.Content), ignoreOrder: true);

        var withoutFacts = list.Single(c => c.Id == withoutFactsId);
        withoutFacts.Facts.ShouldBeEmpty();
    }


    [Theory]
    [InlineData(1, 10, "Name", "Desc", "Test1")]
    [InlineData(1, 10, "Name", "Asc", "test1")]
    public async Task GetCharacterPage_WithNameFilter_GetOk(
        int pageNumber,
        int pageSize,
        string sortType,
        string sortOrder,
        string filterName)
    {
        // Arrange
        await _neo4JContainerRunner.ResetAsync();
        var expectedNames = Enumerable.Range(0, pageSize).Select(i => $"Test{i}").ToList();

        if (sortOrder == "Desc")
        {
            expectedNames.Reverse();
        }

        var dataToCreateCharacter = new List<object>();

        foreach (var name in expectedNames)
        {
            dataToCreateCharacter.Add(new
            {
                Name = name
            });
        }

        var characterIds = new List<Guid>();

        foreach (var dataToCreate in dataToCreateCharacter)
        {
            var response = await Client.PostAsJsonAsync(Endpoint, dataToCreate, CancellationToken.None);
            var characterId = await response.Content.ReadFromJsonAsync<Guid>();
            characterIds.Add(characterId);
        }

        var idFrom = Guid.Empty;
        var idTo = Guid.Empty;
        for (var i = 0; i < characterIds.Count; i++)
        {
            idFrom = characterIds[i];

            if (i == characterIds.Count - 1)
            {
                idTo = characterIds[0];
            }
            else
            {
                idTo = characterIds[(i + 1)];
            }
            var createRelationRequest = new
            {
                FromCharacterId = idFrom,
                ToCharacterId = idTo,
                Description = "Test",
                IsStrongRelation = true
            };

            await Client.PostAsJsonAsync(KnowEndpoint, createRelationRequest, CancellationToken.None);
        }

        var endpoint =
            $"{Endpoint}?pageNumber={pageNumber}&pageSize={pageSize}&sortType={sortType}&sortOrder={sortOrder}&nameFilter={filterName}";

        // Act
        var reponse = await Client.GetAsync(endpoint);

        // Assert
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await reponse.Content.ReadFromJsonAsync<IEnumerable<CharacterPayloadWithRelations>>();
        var list = content!.ToList();

        list.Count.ShouldBe(1);
        list.Any(c => c.Name.Equals(filterName, StringComparison.CurrentCultureIgnoreCase)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, 10, "Name", "Ascc")]
    [InlineData(1, 10, "Name", "Descc")]
    [InlineData(0, 10, "Name", "Desc")]
    [InlineData(1, 0, "Name", "Desc")]
    public async Task GetCharacterPage_GetBadRequest(int pageNumber, int pageSize, string sortType, string sortOrder)
    {
        // Arrange
        var endpoint =
            $"{Endpoint}?pageNumber={pageNumber}&pageSize={pageSize}&sortType={sortType}&sortOrder={sortOrder}";

        // Act
        var reponse = await Client.GetAsync(endpoint);

        // Assert
        reponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
