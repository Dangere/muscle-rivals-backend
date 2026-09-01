
using System.ComponentModel.DataAnnotations;

namespace MuscleRivalsBackend.Models.DTOs.Auth;


public record TokensDTO([Required] string AccessToken, [Required] string RefreshToken);