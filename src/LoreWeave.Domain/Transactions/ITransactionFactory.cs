namespace LoreWeave.Domain.Transactions;

public interface ITransactionFactory
{
    Task<ITransaction> CreateAsync();
}
