using System.Security.Claims;
using Panthera.Domain.Entities;
using Panthera.Application.Contracts.Responses;
using Panthera.Application.Contracts;
using Panthera.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Panthera.Application.Contracts.Requests;

namespace Panthera.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<User> userManager;
    private readonly IConfiguration config;
    private readonly IJwtGenerator jwtService;

    public AuthService(UserManager<User> _userManager, IConfiguration _config, IJwtGenerator _jwtService)
    {
        userManager = _userManager;
        config = _config;
        jwtService = _jwtService;
    }

    public async Task<IdentityResponse<LoginResponse>> LoginAsync(AuthRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            return new IdentityResponse<LoginResponse>
            {
                Errors = new List<IdentityError>() {
                    new IdentityError
                    {
                        Code = "UserNotFound",
                        Description = "User could not be found with the email provided"
                    }
                },
                Message = "User not found",
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, model.Password);

        if (!isPasswordValid)
        {
            return new IdentityResponse<LoginResponse>
            {
                Errors = new List<IdentityError>() {
                    new IdentityError
                    {
                        Code = "InvalidPassword",
                        Description = "The Password provided was incorrect"
                    }
                },
                Message = "Invalid Password",
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var token = jwtService.GenerateToken(user);

        return new IdentityResponse<LoginResponse>
        {
            Errors = new List<IdentityError>(),
            Message = "Successfully logged you in",
            StatusCode = StatusCodes.Status200OK,
            Data = new LoginResponse(user, token)
        };
    }

    public async Task<IdentityResponse<int>> RegisterAsync(AuthRequest model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is not null)
        {
            return new IdentityResponse<int>
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "The user already exists",
                Errors = new List<IdentityError>() {
                    new IdentityError {
                        Code = "UserExists",
                        Description = "The user with that email already exists"
                    }
                },
                Data = 0,
            };
        }

        var newUser = new User
        {
            UserName = model.Email,
            Email = model.Email,
        };

        var result = await userManager.CreateAsync(newUser, model.Password);

        if (!result.Succeeded)
        {
            return new IdentityResponse<int>
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "Could not create the user",
                Data = 0,
                Errors = result.Errors
            };
        }

        return new IdentityResponse<int>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "User successfully created",
            Errors = result.Errors,
            Data = 1,
        };
    }

    public async Task<User> GetUserAsync(ClaimsPrincipal principal)
    {
        return await userManager.GetUserAsync(principal);
    }
}