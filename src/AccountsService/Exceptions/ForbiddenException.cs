namespace AccountsService.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message)
{
}
