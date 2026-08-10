using MessagePipe;

using Microsoft.Extensions.DependencyInjection;

using LoreWeave.Application.Commands.Boards.CommandHandlers;
using LoreWeave.Application.Commands.Characters.CommandHandlers;
using LoreWeave.Application.Commands.Facts.CommandHandlers;
using LoreWeave.Application.Commands.Knows.CommandHandlers;
using LoreWeave.Application.Filters;
using LoreWeave.Application.Queries.Boards.QueryHandlers;
using LoreWeave.Application.Queries.Characters.QueryHandlers;
using LoreWeave.Application.Queries.Facts.QueryHandlers;
using LoreWeave.Application.Queries.Knows.QueryHandlers;

namespace LoreWeave.Application.IoC;

public static class HandlerConfiguration
{
    public static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        services
            .AddMessagePipe(options =>
            {
                options.InstanceLifetime = InstanceLifetime.Scoped;
                options.RequestHandlerLifetime = InstanceLifetime.Scoped;
                options.AddGlobalAsyncRequestHandlerFilter(typeof(LogFilter<,>), 0);
            })
            .AddAsyncRequestHandler<CreateBoardCommandHandler>()
            .AddAsyncRequestHandler<UpdateBoardCommandHandler>()
            .AddAsyncRequestHandler<DeleteBoardCommandHandler>()
            .AddAsyncRequestHandler<GetBoardByIdQueryHandler>()
            .AddAsyncRequestHandler<GetBoardsQueryHandler>()
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
            .AddAsyncRequestHandler<CreateFactCommandHandler>()
            .AddAsyncRequestHandler<UpdateFactCommandHandler>()
            .AddAsyncRequestHandler<DeleteFactCommandHandler>()
            .AddAsyncRequestHandler<ConnectFactToCharacterCommandHandler>()
            .AddAsyncRequestHandler<DisconnectFactFromCharacterCommandHandler>()
            .AddAsyncRequestHandler<GetFactByIdQueryHandler>();

        return services;
    }
}
