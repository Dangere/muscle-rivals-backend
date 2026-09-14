using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MuscleRivalsBackend.Data;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Mappers;
using MuscleRivalsBackend.Models.DTOs.Auth;
using MuscleRivalsBackend.Models.Entities;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Services;

public class AuthService(MuscleRivalsDBContext dbContext, TokenService tokenService, UserMapper userMapper, IConfiguration config, ILogger<AuthService> logger)

{
    private readonly MuscleRivalsDBContext _dbContext = dbContext;
    private readonly TokenService _tokenService = tokenService;
    private readonly UserMapper _userMapper = userMapper;
    private readonly IConfiguration _config = config;
    private readonly ILogger<AuthService> _logger = logger;


    public async Task<Result<AuthenticationResponseDTO>> LoginWithEmailAndPassword(LoginRequestDTO loginRequest)
    {
        UserEntity? user = await _dbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, loginRequest.Email) && !u.IsDeleted);

        if (user is null)
        {
            return Result<AuthenticationResponseDTO>.Error("Invalid credentials", ErrorCodes.INVALID_CREDENTIALS);
        }

        if (!Hashing.ValidatePassword(loginRequest.Password, user.Hash, user.Salt))
        {
            return Result<AuthenticationResponseDTO>.Error("Invalid credentials", ErrorCodes.INVALID_CREDENTIALS);
        }


        // Generate refresh token, and store it hashed
        RefreshTokenEntity refreshTokenEntity = _tokenService.GenerateRefreshToken(user.Id, user.Salt, out string refreshToken);
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();


        // Generate access token
        string accessToken = _tokenService.GenerateAccessToken(user);

        AuthenticationResponseDTO authenticationResponse = new(new(AccessToken: accessToken, RefreshToken: refreshToken), _userMapper.UserToUserDTO(user), user.IsVerified);

        return Result<AuthenticationResponseDTO>.Success(authenticationResponse);
    }

    public async Task<Result<AuthenticationResponseDTO>> RegisterWithEmailAndPassword(RegisterRequestDTO registerRequest)
    {
        // Looks for a user with the same email or username, also searches deleted users 
        UserEntity? user = await _dbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, registerRequest.Email) || EF.Functions.ILike(u.Username, registerRequest.Username));

        if (user is not null)
        {
            if (user.Email.Equals(registerRequest.Email, StringComparison.CurrentCultureIgnoreCase))
            {
                return Result<AuthenticationResponseDTO>.Error("Email already in use", ErrorCodes.EMAIL_ALREADY_IN_USE);
            }

            return Result<AuthenticationResponseDTO>.Error("Username already exists", ErrorCodes.USERNAME_ALREADY_IN_USE);
        }


        string salt = Hashing.GenerateSalt();
        string passwordHash = Hashing.HashPassword(registerRequest.Password, salt);

        UserEntity newUser = UserEntity.CreateUser(registerRequest.Email, registerRequest.Username, registerRequest.FirstName, registerRequest.LastName, passwordHash, salt, UserRoles.User, false);


        // Save new user
        await _dbContext.Users.AddAsync(newUser);
        await _dbContext.SaveChangesAsync();

        // Generate refresh token, and store it hashed
        RefreshTokenEntity refreshTokenEntity = _tokenService.GenerateRefreshToken(newUser.Id, salt, out string refreshToken);
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Generate access token
        string accessToken = _tokenService.GenerateAccessToken(newUser);

        AuthenticationResponseDTO authenticationResponse = new(new(AccessToken: accessToken, RefreshToken: refreshToken), _userMapper.UserToUserDTO(newUser), newUser.IsVerified);

        return Result<AuthenticationResponseDTO>.Success(authenticationResponse);

    }

    // public async Task<Result<AuthenticationResponseDTO>> LoginWithGoogle(string googleToken)
    // {
    //     throw new NotImplementedException();
    // }

    public async Task<Result<TokensDTO>> RefreshToken(string expiredAccessToken, string refreshToken)
    {
        ClaimsPrincipal claimsFromExpiredToken;

        try
        {
            claimsFromExpiredToken = _tokenService.ExtractPrincipalFromToken(expiredAccessToken, false);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Token validation failed.");
            return Result<TokensDTO>.Error("Invalid refresh token.", ErrorCodes.INVALID_TOKEN, StatusCodes.Status401Unauthorized);


        }

        if (claimsFromExpiredToken.FindFirst(ClaimTypes.NameIdentifier) == null)
            return Result<TokensDTO>.Error("Invalid refresh token.", ErrorCodes.INVALID_TOKEN, StatusCodes.Status401Unauthorized);


        int userId = int.Parse(claimsFromExpiredToken.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        string? salt = await _dbContext.Users.Where(u => u.Id == userId && !u.IsDeleted).Select(u => u.Salt).FirstOrDefaultAsync();
        if (salt == null)
            return Result<TokensDTO>.Error("Invalid refresh token.", ErrorCodes.INVALID_TOKEN, StatusCodes.Status401Unauthorized);


        // Recreate hashed refresh token
        string hashedToken = Hashing.HashToken(refreshToken, salt);
        // Validate refresh token
        RefreshTokenEntity? existingRefreshToken = await _dbContext.RefreshTokens.AsTracking().Where(t => t.HashedToken == hashedToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync();

        // If the token is not found, it means its expired or didn't exist in th first place 
        if (existingRefreshToken is null)
            return Result<TokensDTO>.Error("Invalid refresh token.", ErrorCodes.INVALID_TOKEN, StatusCodes.Status401Unauthorized);


        // We mark the refresh token as revoked as we gonna replace it
        existingRefreshToken.IsRevoked = true;

        // Generate refresh token, and store it hashed
        RefreshTokenEntity refreshTokenEntity = _tokenService.GenerateRefreshToken(userId, salt, out string newRefreshToken);
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Generate access token
        UserEntity user = await _dbContext.Users.FirstAsync(u => u.Id == userId);
        string accessToken = _tokenService.GenerateAccessToken(user);

        return Result<TokensDTO>.Success(new TokensDTO(accessToken, newRefreshToken));



    }
}