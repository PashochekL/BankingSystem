namespace UsersService.Exceptions;

public sealed class UnauthorizedException(string message) : Exception(message)
{
}
