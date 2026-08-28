namespace AccountsService.Exceptions;

public sealed class NotFoundException(string message) : Exception(message)
{
}
