namespace AccountsService.Exceptions;

public sealed class ValidationException(string message) : Exception(message)
{
}
