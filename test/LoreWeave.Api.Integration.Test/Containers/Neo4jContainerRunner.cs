using System;
using System.IO;
using System.Threading.Tasks;

using Neo4j.Driver;

using Testcontainers.Neo4j;

using LoreWeave.Infrastructure.Migrations;

namespace LoreWeave.Api.Integration.Test.Containers;

public class Neo4jContainerRunner : IAsyncDisposable
{
    private readonly Neo4jContainer _container;
    private string image = "neo4j:latest";

    private bool _isInitialized;

    public Neo4jContainerRunner() =>
        _container = new Neo4jBuilder(image)
            .WithCleanUp(true)
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _container.StartAsync();

        await using var driver = CreateDriver();
        var migrationRunner = new Neo4jMigrationRunner(driver, MigrationsDirectory);
        await migrationRunner.RunAsync();

        _isInitialized = true;
    }

    private static string MigrationsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "migrations");

    public async Task ResetAsync()
    {
        await using var driver = CreateDriver();
        await using var session = driver.AsyncSession();
        await session.RunAsync(new Query("MATCH (n) DETACH DELETE n"));
    }

    public IDriver CreateDriver() =>
        GraphDatabase.Driver(
            ConnectionString.Replace("neo4j","bolt"),
            AuthTokens.None);
}
