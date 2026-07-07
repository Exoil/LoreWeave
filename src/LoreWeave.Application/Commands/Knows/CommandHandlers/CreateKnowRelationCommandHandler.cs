using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Knows.CommandHandlers;

public class CreateKnowRelationCommandHandler : IAsyncRequestHandler<CreateKnowRelationCommand, Result<Guid, Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public CreateKnowRelationCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        IKnowRelationWriter knowRelationWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _knowRelationWriter = knowRelationWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Guid, Exception>> InvokeAsync(
        CreateKnowRelationCommand request,
        CancellationToken cancellationToken = new())
    {
        var id = Guid.CreateVersion7();

        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var createKnowRelation = new CreateKnowRelation(
                id,
                request.FromCharacterId,
                request.ToCharacterId,
                request.Description,
                request.IsStrongRelation);

            var fromCharacterExists = await _existsCharacter.CharacterExistsAsync(
                transaction,
                request.BoardId,
                createKnowRelation.FromCharacterId);

            if (!fromCharacterExists.Exists)
            {
                _logger.Error("Create know relation fails for not existing character: {Id}",
                    createKnowRelation.FromCharacterId);
                return UnprocessableContentException.CreateKnowRelationFailsForNotExistingCharacter(createKnowRelation
                    .FromCharacterId);
            }

            var toCharacterExists = await _existsCharacter.CharacterExistsAsync(
                transaction,
                request.BoardId,
                createKnowRelation.ToCharacterId);

            if (!toCharacterExists.Exists)
            {
                _logger.Error("Create know relation fails for not existing character: {Id}", createKnowRelation.ToCharacterId);
                return UnprocessableContentException.CreateKnowRelationFailsForNotExistingCharacter(createKnowRelation
                    .ToCharacterId);
            }

            await _knowRelationWriter.CreateKnowRelationAsync(
                transaction,
                createKnowRelation);
            await transaction.CommitAsync();
            _logger.Information("Know relation created: {FromCharacterId} knows {ToCharacterId}",
                createKnowRelation.FromCharacterId,
                createKnowRelation.ToCharacterId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(
                exception,
                "Error creating know relation: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);

            return exception;
        }

        return id;
    }
}
