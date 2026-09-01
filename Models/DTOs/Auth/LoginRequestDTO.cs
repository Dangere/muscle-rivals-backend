using System.ComponentModel.DataAnnotations;
using MuscleRivalsBackend.Utilities;


namespace MuscleRivalsBackend.Models.DTOs.Auth;


public record LoginRequestDTO([Required, EmailAddress] string Email, [Required] string Password);