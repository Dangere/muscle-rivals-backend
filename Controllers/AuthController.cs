using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MuscleRivalsBackend.Models.DTOs.Auth;
using MuscleRivalsBackend.Services;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth-policy")]

public class AuthController(AuthService authServer) : ControllerBase
{
    private readonly AuthService _authService = authServer;

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> LoginWithEmailAndPassword([FromBody] LoginRequestDTO loginRequest)
    {

        Result<AuthenticationResponseDTO> result = await _authService.LoginWithEmailAndPassword(loginRequest);

        if (!result.IsSuccess) return this.ErrorResponse(result);

        return Ok(result.Data);

    }
    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> RegisterWithEmailAndPassword([FromBody] RegisterRequestDTO registerRequest)
    {
        Result<AuthenticationResponseDTO> result = await _authService.RegisterWithEmailAndPassword(registerRequest);

        if (!result.IsSuccess) return this.ErrorResponse(result);

        return Ok(result.Data);

    }
}
