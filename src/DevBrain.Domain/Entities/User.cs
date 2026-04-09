using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Entities;

public sealed class User
{
    public string Id { get; }
    public string Email { get; }
    public string DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private User(string id, string email, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

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

        return new User(
            id: supabaseId.Trim(),
            email: email.Trim(),
            displayName: displayName.Trim(),
            createdAt: DateTimeOffset.UtcNow
        );
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
