namespace UsersService.Exceptions;

public sealed class ValidationException(string message) : Exception(message)
{
}
