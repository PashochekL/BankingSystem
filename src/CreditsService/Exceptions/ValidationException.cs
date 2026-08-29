namespace CreditsService.Exceptions;

public sealed class ValidationException(string message) : Exception(message)
{
}
