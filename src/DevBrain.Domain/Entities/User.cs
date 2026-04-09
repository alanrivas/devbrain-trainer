using DevBrain.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace DevBrain.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }  // Changed from string (was SupabaseId) to Guid (server-generated)
    public string Email { get; }
    public string PasswordHash { get; }  // Added for registration
    public string DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private User(Guid id, string email, string passwordHash, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Factory for user registration. Validates email, password, and displayName per spec.
    /// Password is expected to be already hashed (bcrypt) by caller.
    /// </summary>
    public static User CreateFromRegistration(string email, string passwordHash, string displayName)
    {
        // Email validation
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        var emailTrimmed = email.Trim().ToLower();  // Normalize to lowercase
        if (!IsValidEmail(emailTrimmed))
            throw new DomainException("Email format is invalid.");

        if (emailTrimmed.Length > 255)
            throw new DomainException("Email must not exceed 255 characters.");

        // Password validation (note: caller should hash, we just validate it exists)
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        // DisplayName validation
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name is required.");

        var displayNameTrimmed = displayName.Trim();
        if (displayNameTrimmed.Length < 3)
            throw new DomainException("Display name must be at least 3 characters.");

        if (displayNameTrimmed.Length > 50)
            throw new DomainException("Display name must not exceed 50 characters.");

        if (!IsValidDisplayName(displayNameTrimmed))
            throw new DomainException("Display name contains invalid characters.");

        return new User(
            id: Guid.NewGuid(),  // Server generates UUID
            email: emailTrimmed,
            passwordHash: passwordHash,
            displayName: displayNameTrimmed,
            createdAt: DateTimeOffset.UtcNow
        );
    }

    /// <summary>
    /// Legacy factory for existing users (may be used for seeding or other scenarios).
    /// Kept for backward compatibility until migration complete.
    /// </summary>
    public static User Create(string supabaseId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(supabaseId))
            throw new DomainException("User Id is required.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("A valid email is required.");

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length < 2)
            throw new DomainException("Display name must be between 2 and 50 characters.");

        if (displayName.Trim().Length > 50)
            throw new DomainException("Display name must be between 2 and 50 characters.");

        // Create dummy hash for legacy users (should not be used for login)
        var dummyHash = "legacy_user_no_password_hash";

        return new User(
            id: Guid.Parse(supabaseId),  // Parse as Guid
            email: email.Trim(),
            passwordHash: dummyHash,
            displayName: displayName.Trim(),
            createdAt: DateTimeOffset.UtcNow
        );
    }

    private static bool IsValidEmail(string email)
    {
        // RFC 5322 simplified: must have @ and .
        return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    }

    private static bool IsValidDisplayName(string displayName)
    {
        // Allow letters, digits, spaces, dash, dot
        return Regex.IsMatch(displayName, @"^[a-zA-Z0-9\s\-\.]+$");
    }

    public void UpdateDisplayName(string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName) || newDisplayName.Trim().Length < 2)
            throw new DomainException("Display name must be between 2 and 50 characters.");

        if (newDisplayName.Trim().Length > 50)
            throw new DomainException("Display name must be between 2 and 50 characters.");

        DisplayName = newDisplayName.Trim();
    }
}
