using System.ComponentModel.DataAnnotations;
using MuscleRivalsBackend.Models.DTOs.Users;


namespace MuscleRivalsBackend.Models.DTOs.Auth;


public record AuthenticationResponseDTO([Required] TokensDTO Tokens, [Required] UserDTO UserData, [Required] bool IsVerified);