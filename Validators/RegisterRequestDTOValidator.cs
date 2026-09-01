using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MuscleRivalsBackend.Models.DTOs.Auth;

namespace MuscleRivalsBackend.Validators;

public class RegisterRequestDTOValidator : AbstractValidator<RegisterRequestDTO>
{
    /// <summary>
    /// Validating email and password and first and last names to not be empty
    /// Validating passwords to be between 8 and 32 characters
    /// First and last name should be standard letters and between 3 to 32
    /// Usernames should be between 3 and 20 and english number and letters only with no spaces
    /// </summary>

    public RegisterRequestDTOValidator()
    {
        RuleFor(x => x.FirstName).Length(3, 32).Matches(@"^[\p{L}\p{M}]+(?:[\s'-][\p{L}\p{M}]+)*$");
        RuleFor(x => x.LastName).Length(3, 32).Matches(@"^[\p{L}\p{M}]+(?:[\s'-][\p{L}\p{M}]+)*$");
        RuleFor(x => x.Password).Length(8, 32).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().Matches(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"); ;
        RuleFor(x => x.Username).NotEmpty().Length(3, 20).Matches(@"^[A-Za-z0-9]+$");

    }
}