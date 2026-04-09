using DevBrain.Api.DTOs;
using DevBrain.Api.Mapping;
using DevBrain.Api.Services;
using DevBrain.Api.Validation;
using DevBrain.Domain.Interfaces;

namespace DevBrain.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithName("Auth");

        group.MapPost("/register", PostRegister)
            .WithName("PostRegister")
            .WithDescription("Register a new user");
    }

    private static async Task<IResult> PostRegister(
        RegisterRequestDto request,
        IUserRepository userRepository,
        IPasswordHashService passwordHashService
    )
    {
        // Validate email
        var (emailValid, emailError) = RegistrationValidator.ValidateEmail(request.Email);
        if (!emailValid)
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = emailError
            });

        // Validate password
        var (passwordValid, passwordError) = RegistrationValidator.ValidatePassword(request.Password);
        if (!passwordValid)
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = passwordError
            });

        // Validate displayName
        var (displayNameValid, displayNameError) = RegistrationValidator.ValidateDisplayName(request.DisplayName);
        if (!displayNameValid)
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = displayNameError
            });

        // Check for duplicate email (case-insensitive)
        var existingUser = await userRepository.GetByEmailAsync(request.Email.ToLower());
        if (existingUser != null)
            return Results.Conflict(new
            {
                status = 409,
                title = "Conflict",
                detail = "Email is already registered."
            });

        // Hash password
        var passwordHash = passwordHashService.HashPassword(request.Password);

        // Create user (email will be normalized to lowercase by domain)
        try
        {
            var user = Domain.Entities.User.CreateFromRegistration(
                email: request.Email,
                passwordHash: passwordHash,
                displayName: request.DisplayName
            );

            // Persist user
            await userRepository.AddAsync(user);

            // Return 201 Created with user data
            var response = user.ToResponseDto();
            return Results.Created($"/api/v1/users/{user.Id}", response);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = ex.Message
            });
        }
    }
}
