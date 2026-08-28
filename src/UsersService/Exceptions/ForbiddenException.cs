namespace UsersService.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message)
{
}
