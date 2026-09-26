using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MuscleRivalsBackend.Attributes;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Services;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Controllers;

[AuthorizeRoles(UserRoles.User)]
[ApiController]
[Route("api/[controller]")]
public class MatchmakingController(MatchmakingService matchmakingService) : ControllerBase
{
    private readonly MatchmakingService _matchmakingService = matchmakingService;

    [HttpPost("queue")]
    public async Task<IActionResult> Queue()
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "fail", out int id))
            return Unauthorized("User ID not found in token");

        Result<string> result = await _matchmakingService.EnterQueue(id);

        if (!result.IsSuccess) return this.ErrorResponse(result);

        return Ok();

    }

}