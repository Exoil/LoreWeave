using System.Diagnostics.CodeAnalysis;

using Neo4j.Driver;

namespace LoreWeave.Infrastructure.Migrations;

/// <summary>
/// Applies ordered, idempotent <c>*.cypher</c> migration files from a directory.
/// Already-applied versions are tracked as <c>(:__Migration {Version})</c> nodes,
/// so running the migrations repeatedly is safe.
/// </summary>
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase",
    Justification = "Neo4j is the vendor's own spelling of the product name; Neo4J would be wrong.")]
public sealed class Neo4jMigrationRunner
{
    private const string MigrationLabel = "__Migration";

    private readonly IDriver _driver;
    private readonly string _migrationsDirectory;

    public Neo4jMigrationRunner(IDriver driver, string migrationsDirectory)
    {
        _driver = driver;
        _migrationsDirectory = migrationsDirectory;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_migrationsDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Migrations directory not found: {_migrationsDirectory}");
        }

        var files = Directory
            .GetFiles(_migrationsDirectory, "*.cypher")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        await using var session = _driver.AsyncSession();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var version = Path.GetFileNameWithoutExtension(file);

            if (await IsAppliedAsync(session, version))
            {
                continue;
            }

            var script = await File.ReadAllTextAsync(file, cancellationToken);

            // Each statement runs in its own auto-commit transaction: Neo4j does
            // not allow schema commands (CREATE CONSTRAINT/INDEX) to be mixed
            // with data writes in a single explicit transaction.
            foreach (var statement in SplitStatements(script))
            {
                var cursor = await session.RunAsync(statement);
                await cursor.ConsumeAsync();
            }

            await MarkAppliedAsync(session, version);
        }
    }

    private static async Task<bool> IsAppliedAsync(IAsyncSession session, string version)
    {
        var cursor = await session.RunAsync(
            $"MATCH (m:{MigrationLabel} {{Version: $Version}}) RETURN count(m) AS Count",
            new { Version = version });

        var record = await cursor.SingleAsync();

        return record["Count"].As<int>() > 0;
    }

    private static async Task MarkAppliedAsync(IAsyncSession session, string version)
    {
        var cursor = await session.RunAsync(
            $"MERGE (m:{MigrationLabel} {{Version: $Version}}) SET m.AppliedAt = datetime()",
            new { Version = version });

        await cursor.ConsumeAsync();
    }

    private static IEnumerable<string> SplitStatements(string script) =>
        script
            .Split(';')
            .Select(statement => StripComments(statement).Trim())
            .Where(statement => statement.Length > 0);

    private static string StripComments(string statement) =>
        string.Join(
            '\n',
            statement
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));
}
