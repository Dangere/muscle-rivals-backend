using System.ComponentModel.DataAnnotations;
using MuscleRivalsBackend.Models.DTOs.Users;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Models.DTOs.Auth;

public record RegisterRequestDTO([Required, EmailAddress] string Email, [Required] string Password, [Required] string Username, [Required] string FirstName, [Required] string LastName, UserPreferencesDTO? UserPreferences);