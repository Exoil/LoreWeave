using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;

using Neo4j.Driver;

using LoreWeave.Api.Integration.Test.Containers;

namespace LoreWeave.Api.Integration.Test;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected Neo4jContainerRunner _neo4JContainerRunner = default!;
    protected HttpClient Client = default!;
    protected Guid BoardId;
    protected ApiWebApplicationFactory Factory = default!;
    protected FakeTimeProvider TimeProvider = default!;

    protected IntegrationTestBase() => _neo4JContainerRunner = new Neo4jContainerRunner();

    public virtual async Task InitializeAsync()
    {
        await _neo4JContainerRunner.InitializeAsync();
        Factory = new ApiWebApplicationFactory(_neo4JContainerRunner);
        Client = Factory.CreateClient();
        TimeProvider = Factory.TimeProvider;
        BoardId = await CreateBoardAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await _neo4JContainerRunner.ResetAsync();
        await Factory.DisposeAsync();
    }

    protected Task<IDriver> GetDriverAsync() => Task.FromResult(_neo4JContainerRunner.CreateDriver());

    protected async Task<Guid> CreateBoardAsync(string name = "Test board")
    {
        var response = await Client.PostAsJsonAsync(
            "/v1/boards",
            new
            {
                Name = name
            },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    protected void SetCurrentTime(DateTimeOffset time) =>
        TimeProvider.SetUtcNow(time);

    protected void AdvanceTime(TimeSpan timeSpan) =>
        TimeProvider.Advance(timeSpan);
}
