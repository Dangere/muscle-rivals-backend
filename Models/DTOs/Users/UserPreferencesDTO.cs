using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Models.DTOs.Users;

public record UserPreferencesDTO(bool? DarkMode, string? LanguageCode);