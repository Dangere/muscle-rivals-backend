using Microsoft.AspNetCore.Mvc;
using MuscleRivalsBackend.Attributes;
using MuscleRivalsBackend.Enums;

namespace MuscleRivalsBackend.Controllers;

[AuthorizeRoles(UserRoles.User)]
[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{

    [HttpPost("queue")]
    public async Task<IActionResult> Queue()
    {

        // Result<AuthenticationResponseDTO> result = await _authService.LoginWithEmailAndPassword(loginRequest);

        // if (!result.IsSuccess) return this.ErrorResponse(result);

        return Ok();

    }

}