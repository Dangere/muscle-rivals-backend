using System.ComponentModel.DataAnnotations;
using MuscleRivalsBackend.Utilities;


namespace MuscleRivalsBackend.Models.DTOs.Users;


public record UpdateUserProfileDTO(string? Username, string? FirstName, string? LastName, UserPreferencesDTO? Preferences);