using Microsoft.AspNetCore.Authorization;
using MuscleRivalsBackend.Enums;
namespace MuscleRivalsBackend.Attributes;

public class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(params UserRoles[] roles) : base()
    {
        string[] stringifiedRoles = roles.Select(x => x.ToString()).ToArray();
        Roles = string.Join(",", stringifiedRoles);
    }
}