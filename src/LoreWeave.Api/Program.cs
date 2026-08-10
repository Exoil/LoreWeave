using System.Diagnostics.CodeAnalysis;

using LoreWeave.Api.Endpoints;
using LoreWeave.Api.IoC;
using LoreWeave.Application.IoC;
using LoreWeave.Infrastructure.IoC;

using Steeltoe.Configuration.Placeholder;

namespace LoreWeave.Api;

public class Program
{
    // S1118 wants a static class, but the integration tests need Program as a
    // type argument for WebApplicationFactory<Program>, which static types
    // cannot be. A protected constructor satisfies the rule instead.
    protected Program()
    {
    }

    public static void Main(string[] args)
    {
        var app = BuildApp(args);
        app.Run();
    }

    // Exposed builder to support tests if needed
    [SuppressMessage("Major Vulnerability", "S5122:Restrict this CORS policy to trusted origins",
        Justification =
            "Deliberately open to every origin: the API is consumed by the local dev frontend and by "
            + "deployed clients whose origins are not known up front. Credentials are never allowed, "
            + "so this cannot be used to ride on a user's cookies.")]
    private static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddPlaceholderResolver();

        // Services
        builder.Services.RegisterGraphDb(builder.Configuration);
        builder.Services.RegisterHandlers();
        builder.Host.ConfigureLogger(builder.Configuration);
        builder.Services.RegisterResultsResolvers();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("ETag", "Date", "Location")
            );
#pragma warning restore S5122
        });

        var app = builder.Build();

        // Middleware and endpoints
        // Outside Development only: over plain http the redirect turns the CORS
        // preflight OPTIONS into a 307, which browsers reject before the real
        // request is ever sent.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.MapUtilityEndpoints();
        app.MapBoardEndpoints();
        app.MapCharacterEndpoints();
        app.MapFactsEndpoints();

        return app;
    }
}
