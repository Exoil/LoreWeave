using System.Text;

namespace LoreWeave.Domain.Exceptions;

public class ValueObjectException : DomainException
{
    // object? (not object) so the dictionary can be handed straight to
    // Results.Problem's extensions parameter without a null-forgiving cast.
    public IDictionary<string, object?> ValidationErrors { get; }

    public ValueObjectException(
        string title,
        string errorCode,
        IList<ValidationMessage> validationMessages)
        : base(title, errorCode, GetValidationMessage(validationMessages)) =>
        ValidationErrors = validationMessages.ToDictionary(x => x.PropertyName, object? (y) => y.Message);

    private static string GetValidationMessage(IList<ValidationMessage> validationMessages)
    {
        var stringBuilder = new StringBuilder("Value object exception occured:");

        foreach (var validationMessage in validationMessages)
        {
            stringBuilder
                .AppendLine()
                .Append(
                    $"Property: {validationMessage.PropertyName}, validation message: {validationMessage.Message}.");
        }

        return stringBuilder.ToString();
    }
}

public record ValidationMessage(string PropertyName, string Message);
