# Development Guide — DevBrain Trainer

Guía para desarrolladores que quieren contribuir o agregar features nuevas.

---

## Metodología: SDD + TDD

Cada feature DEBE seguir este flujo:

```
1. Spec (.spec.md)  →  2. Tests (xUnit)  →  3. Implementación  →  4. Update context.md  →  5. Commit & Push
```

**Nunca implementar sin spec previa. Nunca commitear sin actualizar context.md.**

---

## Paso 1: Escribir la Spec

Crea un archivo en `specs/` con nombre descriptivo:

- `specs/domain/{entity}.spec.md` — Para dominios (entities, value objects)
- `specs/api/{endpoint}.spec.md` — Para endpoints
- `specs/infrastructure/{feature}.spec.md` — Para repositorios, servicios

**Ejemplo**: `specs/api/post-user-register.spec.md`

```markdown
# POST /auth/register — User Registration

## Contract

### Route
- `POST /auth/register`
- Returns: `201 Created`

### Request Body
- `email` (string, required) — Valid email format
- `password` (string, required) — Min 8 chars
- `displayName` (string, required) — 3-50 chars

### Response (201)
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "User Name",
  "createdAt": "2026-04-09T12:00:00Z"
}
```

### Error Responses
- `400` — Validation error (invalid email, weak password)
- `409` — Email already exists
- `422` — Invalid request format

## Comportamientos

1. `BeCreated_GivenValidRequest_Should201AndReturnUserDto`
2. `BeRejected_GivenInvalidEmail_Should400`
3. `BeRejected_GivenWeakPassword_Should400`
4. `BeRejected_GivenDuplicateEmail_Should409`

## Invariantes

- Email siempre se almacena en minúscula
- Contraseña se hashea con bcrypt (nunca plaintext)
- UserId es UUID generado por server, no por cliente
```

---

## Paso 2: Escribir Tests

Crea archivo en `tests/` con sufijo `Tests.cs`:

```csharp
using Xunit;
using DevBrain.Api.DTOs;
using System.Net;

namespace DevBrain.Api.Tests;

public class PostUserRegisterEndpointTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PostRegister_ValidRequest_Should201AndReturnUserDto()
    {
        var request = new RegisterRequestDto(
            Email: "test@example.com",
            Password: "SecurePass123!",
            DisplayName: "Test User"
        );

        var response = await _client.PostAsync(
            "/api/v1/auth/register",
            new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            )
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadAsAsync<UserResponseDto>();
        Assert.NotNull(result.UserId);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task PostRegister_InvalidEmail_Should400()
    {
        var request = new RegisterRequestDto(
            Email: "invalid-email",
            Password: "SecurePass123!",
            DisplayName: "Test User"
        );

        var response = await _client.PostAsync(
            "/api/v1/auth/register",
            new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

Patrón para naming tests:
```
{MethodUnderTest}_{Description}_Should{ExpectedResult}
```

---

## Paso 3: Implementar

### A. Crear DTOs

`src/DevBrain.Api/DTOs/RegisterRequestDto.cs`:
```csharp
namespace DevBrain.Api.DTOs;

public record RegisterRequestDto(string Email, string Password, string DisplayName);
```

### B. Crear endpoint handler

`src/DevBrain.Api/Endpoints/AuthEndpoints.cs`:
```csharp
namespace DevBrain.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", Register)
            .WithName("RegisterUser")
            .WithOpenApi();
    }

    private static async Task<IResult> Register(
        RegisterRequestDto request,
        IUserRepository userRepository
    )
    {
        // Validations
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            return Results.BadRequest("Invalid email format");

        if (request.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters");

        // Domain logic
        var user = User.Create(request.Email.ToLower(), request.Password, request.DisplayName);
        await userRepository.AddAsync(user);

        // Return response
        return Results.Created($"/api/v1/users/{user.Id}", user.ToResponseDto());
    }

    private static bool IsValidEmail(string email) => 
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
```

### C. Register in Program.cs

`src/DevBrain.Api/Program.cs`:
```csharp
// After var app = builder.Build();

app.MapAuthEndpoints();  // Add this line
```

---

## Paso 4: Run Tests

```bash
# All tests (should all pass)
dotnet test

# Specific test file
dotnet test tests/DevBrain.Api.Tests/PostUserRegisterEndpointTests.cs

# With verbose output
dotnet test --logger "console;verbosity=detailed"
```

If tests fail, fix your implementation until all green (🟢).

---

## Paso 5: Update context.md

Edit `context.md` — "Último paso completado" section:

```markdown
## Último paso completado
> **POST /auth/register endpoint — User registration** ✅
>
> - DTOs: `RegisterRequestDto`, `UserResponseDto`
> - Endpoint handler with email/password validation
> - Domain User entity with bcrypt hashing
> - Tests: 4 tests passing (valid request, invalid email, weak password, duplicate email)
> - All 95+ tests passing
>
> Próximo paso: **POST /user/profile** — Get user profile endpoint
```

And update the checklist:
```markdown
- [x] Endpoint POST /auth/register (X tests en verde)
- [ ] Endpoint POST /user/profile
```

---

## Paso 6: Commit & Push

```bash
# Stage changes
git add -A

# Commit with clear message
git commit -m "feat: Implement POST /auth/register endpoint

- Add RegisterRequestDto and UserResponseDto DTOs
- Create AuthEndpoints handler with email/password validation
- Add User.Create() domain factory with bcrypt
- 4 integration tests (valid/invalid requests)
- All 99+ tests passing"

# Push to GitHub
git push
```

**Commit message format**:
- `feat:` — New feature
- `fix:` — Bug fix
- `docs:` — Documentation
- `test:` — Tests only
- `refactor:` — Code restructuring
- `chore:` — Dependencies, build config

---

## File Organization Rules

### Domain Layer (`src/DevBrain.Domain/`)
- **Entities**: Pure C# classes with no EF dependencies
- **Interfaces**: Abstract dependencies, no implementation
- **Exceptions**: Custom exceptions inheriting `DomainException`
- **Enums**: Strongly-typed options

**Example**:
```csharp
// Domain — NO EF Core imports!
public sealed class User
{
    public Guid Id { get; }
    public string Email { get; }  // Computed from UserId (Supabase SupabaseId)
    
    public static User Create(string email, string password, string displayName)
    {
        // Validation & hashing logic here
    }
}
```

### Infrastructure Layer (`src/DevBrain.Infrastructure/`)
- **DbContext**: EF Core configuration, migrations
- **Repositories**: IRepository implementations using EF
- **Migrations**: Auto-generated by EF (for PostgreSQL later)

**Example**:
```csharp
// Infrastructure — EF Core models & repositories
public class EFUserRepository : IUserRepository
{
    private readonly DevBrainDbContext _db;
    
    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
}
```

### API Layer (`src/DevBrain.Api/`)
- **Endpoints**: Route handlers, request validation, HTTP concerns
- **DTOs**: Request/Response schemas (no domain logic)
- **Mapping**: Extension methods for entity → DTO conversion

**Example**:
```csharp
// API — HTTP concerns
public record RegisterRequestDto(string Email, string Password, string DisplayName);

public static class UserMapper
{
    public static UserResponseDto ToResponseDto(this User user) => new(
        UserId: user.Id,
        Email: user.Email,
        DisplayName: user.DisplayName
    );
}
```

### Tests (`tests/`)
- **Domain.Tests**: xUnit tests for domain entities
- **Infrastructure.Tests**: xUnit tests for repositories & DbContext
- **Api.Tests**: xUnit integration tests for endpoints

---

## Common Patterns

### Validation in Endpoint
```csharp
if (string.IsNullOrWhiteSpace(request.Email))
    return Results.BadRequest(new ProblemDetails {
        Status = 400,
        Title = "Validation Error",
        Detail = "Email is required"
    });
```

### Domain Factory with Validation
```csharp
public static Attempt Create(Guid challengeId, string userId, string userAnswer, int elapsedSecs, Challenge challenge)
{
    if (challengeId == Guid.Empty)
        throw new DomainException("ChallengeId is required.");
    
    // More validation...
    
    return new Attempt(
        id: Guid.NewGuid(),
        challengeId,
        userId,
        userAnswer,
        isCorrect: challenge.IsCorrectAnswer(userAnswer),
        elapsedSecs,
        occurredAt: DateTimeOffset.UtcNow
    );
}
```

### DTO Mapping
```csharp
public static class ChallengeMapper
{
    public static ChallengeResponseDto ToResponseDto(this Challenge challenge) => new(
        Id: challenge.Id,
        Title: challenge.Title,
        Description: challenge.Description,
        Category: challenge.Category.ToString(),
        Difficulty: challenge.Difficulty.ToString(),
        TimeLimitSecs: challenge.TimeLimitSecs
    );
}
```

---

## Testing Tips

### Test Naming
Use this format consistently:
```csharp
[Fact]
public async Task {Action}_{GivenCondition}_Should{ExpectedResult}()
```

Examples:
- `AddAsync_GivenValidChallenge_ShouldPersist`
- `IsCorrectAnswer_GivenCaseInsensitive_ShouldMatch`
- `PostAttempt_CorrectAnswer_Should201WithIsCorrectTrue`

### Test Setup with IAsyncLifetime
```csharp
public class MyTests : IAsyncLifetime
{
    private HttpClient _client = null!;
    
    public async Task InitializeAsync()
    {
        // Setup before each test
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup after each test
    }
}
```

### Assertions
```csharp
Assert.NotNull(result);
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.False(condition);
Assert.Throws<ExceptionType>(() => { /* code that should throw */ });
```

---

## Debugging

### Run specific test
```bash
dotnet test --filter "ClassName=PostAttemptEndpointTests"
```

### Run with verbose output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Debug in VS Code
Set breakpoint in test file, then run via Test Explorer.

---

## Questions?

- Check existing specs in `specs/` for examples
- Check existing endpoint implementations in `src/DevBrain.Api/Endpoints/`
- Check test examples in `tests/DevBrain.Api.Tests/`
- Review `context.md` for roadmap and completed features

---

**Happy coding! 🚀**
