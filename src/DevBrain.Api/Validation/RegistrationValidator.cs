using System.Text.RegularExpressions;

namespace DevBrain.Api.Validation;

public static class RegistrationValidator
{
    public static (bool IsValid, string ErrorMessage) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Email is required.");

        var emailTrimmed = email.Trim();

        // RFC 5322 simplified
        if (!Regex.IsMatch(emailTrimmed, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            return (false, "Email format is invalid.");

        if (emailTrimmed.Length > 255)
            return (false, "Email must not exceed 255 characters.");

        return (true, string.Empty);
    }

    public static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return (false, "Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(password, @"[0-9]"))
            return (false, "Password must contain at least one digit.");

        return (true, string.Empty);
    }

    public static (bool IsValid, string ErrorMessage) ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "Display name is required.");

        var displayNameTrimmed = displayName.Trim();

        if (displayNameTrimmed.Length < 3)
            return (false, "Display name must be at least 3 characters.");

        if (displayNameTrimmed.Length > 50)
            return (false, "Display name must not exceed 50 characters.");

        if (!Regex.IsMatch(displayNameTrimmed, @"^[a-zA-Z0-9\s\-\.]+$"))
            return (false, "Display name contains invalid characters.");

        return (true, string.Empty);
    }
}
