using UsersService.Entities;
using UsersService.Exceptions;

namespace UsersService.Services;

internal static class UserRequestValidation
{
    public static void ValidateName(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > 100)
        {
            throw new ValidationException($"{fieldName} must not exceed 100 characters.");
        }
    }

    public static void ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ValidationException("Phone is required.");
        }

        var trimmedPhone = phone.Trim();

        if (trimmedPhone.Length > 32)
        {
            throw new ValidationException("Phone must not exceed 32 characters.");
        }

        var digitStartIndex = trimmedPhone.StartsWith('+') ? 1 : 0;
        if (digitStartIndex == trimmedPhone.Length || trimmedPhone[digitStartIndex..].Any(character => !char.IsDigit(character)))
        {
            throw new ValidationException("Phone must contain only digits and may start with '+'.");
        }
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length < 8)
        {
            throw new ValidationException("Password must contain at least 8 characters.");
        }

        if (password.Length > 128)
        {
            throw new ValidationException("Password must not exceed 128 characters.");
        }
    }

    public static void ValidateLoginPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }
    }

    public static void ValidateRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ValidationException("Role is invalid.");
        }
    }

    public static void ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("Refresh token is required.");
        }
    }
}
