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

    public async Task<Result<AuthenticationResponseDTO>> LoginWithGoogle(string googleToken)
    {
        throw new NotImplementedException();
    }





}