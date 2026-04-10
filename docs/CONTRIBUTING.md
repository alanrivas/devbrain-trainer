# Contributing to DevBrain Trainer

Guía para contribuidores y desarrolladores.

---

## Git Workflow

### Branch Naming

```
main                    — Production-ready (latest stable)
  ├── feature/xxx      — New feature
  ├── fix/xxx          — Bug fix
  ├── chore/xxx        — Refactor, docs, cleanup
  └── test/xxx         — Test infrastructure
```

**Examples**:
- `feature/endpoint-logging`
- `fix/accuracy-calculation`
- `chore/reorganize-tests`

### Commit Message Format

```
<type>: <subject> — <context>

<body (optional)>

Fixes #123
```

**Types**:
- `feat:` — New feature
- `fix:` — Bug fix
- `chore:` — Refactor, docs, cleanup
- `test:` — Test infrastructure
- `ci:` — CI/CD changes
- `docs:` — Documentation only

**Examples**:
```
feat: add endpoint logging infrastructure — Phase 3.3 (212/212 tests)
fix: accuracy calculation now returns 0-100% instead of 0-1
chore: reorganize project structure — move scripts and docs
```

### Pull Requests

1. **Create feature branch**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes following SDD+TDD**
   - See «Methodology» below

3. **Push & open PR**
   ```bash
   git push -u origin feature/my-feature
   ```

4. **PR Title Format**
   ```
   [Phase X.Y] Feature name — X tests added/modified
   ```
   
   Example:
   ```
   [Phase 3.3] Endpoint logging integration — 10 tests added
   ```

5. **PR Checklist**
   - [ ] All tests passing (`dotnet test`)
   - [ ] `context.md` updated with new status
   - [ ] Spec created (if applicable)
   - [ ] No temporary files (.log, test_*.txt)

---

## Development Methodology: SDD + TDD

**Every feature MUST follow this workflow:**

```
1. Spec (.spec.md)  →  2. Tests (xUnit)  →  3. Implement  →  4. Update context.md  →  5. Commit & Push
```

### Step 1: Write the Spec

Create a `.spec.md` file describing the feature **without** implementation details:

**Location**:
- `specs/domain/{name}.spec.md` — Domain entities, services
- `specs/api/{endpoint}.spec.md` — API endpoints
- `specs/infrastructure/{feature}.spec.md` — Repositories, services

**Template** (`specs/api/post-auth-login.spec.md`):
```markdown
# POST /auth/login — User Authentication

## Purpose
Users authenticate with email/password to receive a JWT token.

## Request
- **Body**: `{ "email": "user@example.com", "password": "secret123" }`
- **Headers**: None required

## Response (200 OK)
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "Alan",
  "token": "eyJhbGc...",
  "expiresAt": "2026-04-11T12:00:00Z"
}
```

## Error Cases
| Condition | Status | Response |
|-----------|--------|----------|
| Email not found | 404 | `{ "error": "User not found" }` |
| Invalid password | 401 | `{ "error": "Invalid credentials" }` |

## Invariantes
- Never log passwords
- Token expires in 24 hours
- Use PBKDF2 hashing
```

### Step 2: Write Tests

Create test file in `tests/{project}/{entity}Tests.cs`:

```csharp
using Xunit;

public class LoginTests
{
    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        // Arrange
        var request = new { email = "user@example.com", password = "secret123" };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsAsync<LoginResponse>();
        Assert.NotNull(body.Token);
    }
}
```

**Run tests** (should FAIL before implementation):
```bash
dotnet test tests/DevBrain.Api.Tests/ --filter "LoginTests"
```

### Step 3: Implement

Write code to make tests pass (green phase). Follow code conventions:
- **C#**: nullable enabled, records for immutables
- **Naming**: English in code, Spanish in commits/docs/specs
- **Structure**: Dependency injection, sealed classes, property validation

### Step 4: Update context.md

Add status to `context.md`:
```markdown
- [x] `post-auth-login.spec.md` — User authentication
- [x] Login tests (8 tests in Api.Tests)
- [x] Implementation in AuthEndpoints.cs
```

### Step 5: Commit & Push

```bash
git add .
git commit -m "feat: user authentication endpoint — Phase 2.1 (215/215 tests)"
git push origin feature/auth-login
```

---

## Code Conventions

### C# Style

```csharp
// ✅ Use records for immutable domain entities
public record User(
    Guid Id,
    string Email,
    string PasswordHash,
    string DisplayName
);

// ✅ Sealed classes for implementations
public sealed class EFUserRepository : IUserRepository
{
    public async Task AddAsync(User user) { ... }
}

// ✅ Property validation in domain layer
public record Challenge(string Title)
{
    public Challenge(string title) : this(
        !string.IsNullOrWhiteSpace(title) 
            ? title 
            : throw new DomainException("Title required")
    ) { }
}

// ❌ Don't: Magic strings, public setters, inheritance chains
// ❌ Don't: Implement features without specs
```

### File Organization

```
src/DevBrain.Api/
├── Endpoints/
│   ├── AuthEndpoints.cs       ← POST /auth/login, POST /auth/register
│   ├── ChallengeEndpoints.cs  ← GET /challenges, POST /attempt
│   └── UserEndpoints.cs       ← GET /users/me/stats
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── Mapping/
│   └── MappingExtensions.cs
└── Program.cs
```

---

## Testing Patterns

### Test Naming: `{Behavior}_Given{Condition}_Should{Result}`

```csharp
[Fact]
public void CalculateAccuracy_GivenMixedAttempts_ShouldReturn50Percent()
{
    // Arrange
    var attempts = new[] { correct: true, false, true, false };
    
    // Act
    var accuracy = CalculateAccuracy(attempts);
    
    // Assert
    Assert.Equal(50f, accuracy);
}
```

### Integration Tests with TestContainers

```csharp
public class PostgresIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainer _postgres = new PostgresBuilder().Build();
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Setup database
    }
    
    public async Task DisposeAsync()
    {
        await _postgres.StopAsync();
    }
    
    [Fact]
    public async Task SaveUser_ToRealDatabase_Should_Succeed()
    {
        // Test against real PostgreSQL
    }
}
```

---

## Build & Test Commands

```bash
# Build solution
dotnet build -c Debug

# Run all tests
dotnet test

# Run specific project
dotnet test tests/DevBrain.Domain.Tests/

# Run specific test class
dotnet test --filter "LoginTests"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura

# Run API locally
dotnet run --project src/DevBrain.Api/ --configuration Debug

# Format code
dotnet format
```

---

## PR Review Checklist

**Before approving, verify:**

- [ ] Spec exists (`.spec.md`) — describes contract, not implementation
- [ ] Tests follow naming convention: `{Behavior}_Given{Condition}_Should{Result}`
- [ ] All tests passing: `dotnet test` shows 100% green
- [ ] No temporary files committed (logs, test_*.txt, *.log)
- [ ] `context.md` updated with new feature status
- [ ] Code follows C# conventions (records, sealed, DI)
- [ ] Comments in Spanish (if needed), code in English
- [ ] Commit message follows format: `<type>: <subject> — <context>`

---

## Common Issues & Solutions

### Tests fail locally but pass in CI

- **Cause**: Different environment (OS, .NET version)
- **Fix**: Run `dotnet clean && dotnet build -c Debug && dotnet test`

### Integration tests hang

- **Cause**: TestContainers not stopping, port conflicts
- **Fix**: Check `docker ps`, kill stale containers: `docker rm -f $(docker ps -q)`

### Migration issues with PostgreSQL

- **Cause**: Old migrations not applied
- **Fix**: See [`docs/POSTGRES_SETUP.md`](./POSTGRES_SETUP.md)

---

## Questions?

- Check [`DEVELOPMENT.md`](./DEVELOPMENT.md) for methodology details
- Check [`context.md`](../context.md) for current roadmap
- Check [`STACK.md`](../STACK.md) for technology details
