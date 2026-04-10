using DevBrain.Api.DTOs;
using DevBrain.Api.Mapping;
using DevBrain.Api.Services;
using DevBrain.Api.Validation;
using DevBrain.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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

        group.MapPost("/login", PostLogin)
            .WithName("PostLogin")
            .WithDescription("Authenticate user and get JWT token");
    }

    private static async Task<IResult> PostRegister(
        RegisterRequestDto request,
        IUserRepository userRepository,
        IPasswordHashService passwordHashService,
        ILogger logger
    )
    {
        logger.LogInformation("Register called: Email={Email}, DisplayName={DisplayName}", 
            request.Email.Split('@')[0] + "@...", request.DisplayName);
        
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
        {
            logger.LogWarning("Register failed: duplicate email {EmailPrefix}", 
                request.Email.Split('@')[0] + "@...");
            return Results.Conflict(new
            {
                status = 409,
                title = "Conflict",
                detail = "Email is already registered."
            });
        }

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

            logger.LogInformation("Register successful: UserId={UserId}, Email={EmailPrefix}", 
                user.Id, request.Email.Split('@')[0] + "@...");

            // Return 201 Created with user data
            var response = user.ToResponseDto();
            return Results.Created($"/api/v1/users/{user.Id}", response);
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            logger.LogWarning("Register failed with DomainException: {Message}", ex.Message);
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = ex.Message
            });
        }
    }

    private static async Task<IResult> PostLogin(
        LoginRequestDto request,
        IUserRepository userRepository,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        ILogger logger
    )
    {
        logger.LogInformation("Login attempted: Email={EmailPrefix}", 
            request.Email.Split('@')[0] + "@...");
        
        // Validate email
        var (emailValid, emailError) = RegistrationValidator.ValidateEmail(request.Email);
        if (!emailValid)
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = emailError
            });

        // Validate password not empty
        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new
            {
                status = 400,
                title = "Bad Request",
                detail = "Email and password are required"
            });

        // Find user by email (case-insensitive)
        var user = await userRepository.GetByEmailAsync(request.Email.ToLower());
        if (user == null)
        {
            logger.LogWarning("Login failed: user not found {EmailPrefix}", 
                request.Email.Split('@')[0] + "@...");
            return Results.Unauthorized();
        }

        // Verify password
        var passwordMatch = passwordHashService.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordMatch)
        {
            logger.LogWarning("Login failed: authentication failed for {EmailPrefix}", 
                request.Email.Split('@')[0] + "@...");
            return Results.Unauthorized();
        }

        // Generate JWT token
        var token = jwtTokenService.GenerateToken(user.Id, user.Email);
        logger.LogInformation("Login successful: UserId={UserId}, Email={EmailPrefix}", 
            user.Id, request.Email.Split('@')[0] + "@...");

        // Return 200 OK with token and user data
        var response = new LoginResponseDto
        {
            Token = token,
            User = user.ToResponseDto()
        };

        return Results.Ok(response);
    }
}

