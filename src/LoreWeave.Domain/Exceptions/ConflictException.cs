namespace LoreWeave.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base("Conflict error", "ConflictException", message)
    {
    }
}