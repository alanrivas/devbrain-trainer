using DevBrain.Domain.Entities;
using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Tests;

public class UserTests
{
    // --- Helpers ---

    private static readonly Guid TestUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static User CreateValid() => User.Create(
        supabaseId: TestUserId.ToString(),
        email: "alan@example.com",
        displayName: "Alan"
    );

    // --- Creación válida ---

    [Fact]
    public void Create_GivenValidArguments_ShouldReturnUser()
    {
        var user = CreateValid();

        Assert.NotNull(user);
        Assert.Equal(TestUserId, user.Id);
        Assert.Equal("alan@example.com", user.Email);
        Assert.Equal("Alan", user.DisplayName);
    }

    [Fact]
    public void Create_GivenValidArguments_ShouldAssignCreatedAt()
    {
        var user = CreateValid();

        Assert.NotEqual(default, user.CreatedAt);
    }

    // --- Validación de Id ---

    [Fact]
    public void Create_GivenEmptyId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create("", "alan@example.com", "Alan"));
    }

    // --- Validación de Email ---

    [Fact]
    public void Create_GivenEmptyEmail_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create(TestUserId.ToString(), "", "Alan"));
    }

    [Fact]
    public void Create_GivenEmailWithoutAtSign_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create(TestUserId.ToString(), "notanemail", "Alan"));
    }

    // --- Validación de DisplayName ---

    [Fact]
    public void Create_GivenEmptyDisplayName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create(TestUserId.ToString(), "alan@example.com", ""));
    }

    [Fact]
    public void Create_GivenDisplayNameWithOneCharacter_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create(TestUserId.ToString(), "alan@example.com", "A"));
    }

    [Fact]
    public void Create_GivenDisplayNameWith51Characters_ShouldThrowDomainException()
    {
        var longName = new string('A', 51);

        Assert.Throws<DomainException>(() => User.Create(TestUserId.ToString(), "alan@example.com", longName));
    }

    // --- UpdateDisplayName ---

    [Fact]
    public void UpdateDisplayName_GivenValidName_ShouldUpdateDisplayName()
    {
        var user = CreateValid();

        user.UpdateDisplayName("NewName");

        Assert.Equal("NewName", user.DisplayName);
    }

    [Fact]
    public void UpdateDisplayName_GivenEmptyName_ShouldThrowDomainException()
    {
        var user = CreateValid();

        Assert.Throws<DomainException>(() => user.UpdateDisplayName(""));
    }

    [Fact]
    public void UpdateDisplayName_GivenNameWithOneCharacter_ShouldThrowDomainException()
    {
        var user = CreateValid();

        Assert.Throws<DomainException>(() => user.UpdateDisplayName("A"));
    }
}
