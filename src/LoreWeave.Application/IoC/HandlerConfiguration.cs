
using MessagePipe;

using Microsoft.Extensions.DependencyInjection;

using LoreWeave.Application.Commands;
using LoreWeave.Application.Commands.CommandHandlers;
using LoreWeave.Application.Filters;
using LoreWeave.Application.Models;
using LoreWeave.Application.Queries;
using LoreWeave.Application.Queries.QueryHandlers;

namespace LoreWeave.Application.IoC;

public static class HandlerConfiguration
{
    public static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        var messagePipeBuilder = services
            .AddMessagePipe(options =>
            {
                options.InstanceLifetime = InstanceLifetime.Scoped;
                options.RequestHandlerLifetime = InstanceLifetime.Scoped;
                options.AddGlobalAsyncRequestHandlerFilter(typeof(LogFilter<,>), 0);
            })
            .AddAsyncRequestHandler<CreateCharacterCommandHandler>()
            .AddAsyncRequestHandler<UpdateCharacterCommandHandler>()
            .AddAsyncRequestHandler<DeleteCharacterCommandHandler>()
            .AddAsyncRequestHandler<GetCharacterByIdQueryHandler>()
            .AddAsyncRequestHandler<GetCharacterPageQueryHandler>()
            .AddAsyncRequestHandler<CreateKnowRelationCommandHandler>()
            .AddAsyncRequestHandler<UpdateKnowRelationCommandHandler>()
            .AddAsyncRequestHandler<DeleteKnowRelationCommandHandler>()
            .AddAsyncRequestHandler<FindRelationBetweenCharacterQueryHandler>()
            .AddAsyncRequestHandler<GetKnowRelationQueryHandler>()
            .AddAsyncRequestHandler<CreateFactCommandHandler>();

        return services;
    }
}
