using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MuscleRivalsBackend.Models.DTOs.Auth;

namespace MuscleRivalsBackend.Validators;

public class LoginRequestDTOValidator : AbstractValidator<LoginRequestDTO>
{
    /// <summary>
    /// Validating email and passwords to not be empty
    /// Validating them to be in proper format
    /// Validating passwords to be between 8 and 32 characters
    /// </summary>
    public LoginRequestDTOValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().Matches(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        RuleFor(x => x.Password).Length(8, 32).NotEmpty();
    }

}