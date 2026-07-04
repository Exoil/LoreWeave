using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neo4j.Driver;

using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;
using LoreWeave.Infrastructure.Repositories.Characters;
using LoreWeave.Infrastructure.Repositories.Facts;
using LoreWeave.Infrastructure.Repositories.Knows;
using LoreWeave.Infrastructure.Transactions;

namespace LoreWeave.Infrastructure.IoC;

public static class RegisterDatabase
{
    private const string _configurationPathToGraphDbConnectionString = "GraphDb:ConnectionString";

    private const string _configurationPathToGraphDbUsername = "GraphDb:Username";

    private const string _configurationPathToGraphDbPassword = "GraphDb:Password";

    public static void RegisterGraphDb(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton(
            GraphDatabase.Driver(
                configuration[_configurationPathToGraphDbConnectionString],
                AuthTokens.Basic(configuration[_configurationPathToGraphDbUsername],
                    configuration[_configurationPathToGraphDbPassword]),
                config => config.WithEncryptionLevel(EncryptionLevel.None)));

        serviceCollection
            .AddScoped<IAsyncSession>(serviceProvider =>
                serviceProvider.GetRequiredService<IDriver>().AsyncSession())
            .AddScoped<ITransactionFactory, Neo4jTransactionFactory>();

        serviceCollection
            .AddScoped<CharacterRepository>()
            .AddScoped<IExistsCharacter>(serviceProvider => serviceProvider.GetRequiredService<CharacterRepository>())
            .AddScoped<ICharacterReader>(serviceProvider => serviceProvider.GetRequiredService<CharacterRepository>())
            .AddScoped<ICharacterWriter>(serviceProvider => serviceProvider.GetRequiredService<CharacterRepository>());

        serviceCollection
            .AddScoped<FactRepository>()
            .AddScoped<IExistsFact>(serviceProvider => serviceProvider.GetRequiredService<FactRepository>())
            .AddScoped<IFactReader>(serviceProvider => serviceProvider.GetRequiredService<FactRepository>())
            .AddScoped<IFactWriter>(serviceProvider => serviceProvider.GetRequiredService<FactRepository>())
            .AddScoped<IFactConnection>(serviceProvider => serviceProvider.GetRequiredService<FactRepository>());

        serviceCollection
            .AddScoped<KnowRelationRepository>()
            .AddScoped<IExistsKnowRelation>(serviceProvider
                => serviceProvider.GetRequiredService<KnowRelationRepository>())
            .AddScoped<IKnowRelationReader>(serviceProvider
                => serviceProvider.GetRequiredService<KnowRelationRepository>())
            .AddScoped<IKnowRelationWriter>(serviceProvider
                => serviceProvider.GetRequiredService<KnowRelationRepository>());
    }
}
