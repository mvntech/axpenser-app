using Axpenser.Api.Auth;
using Axpenser.Application.Auth.Dtos;
using Axpenser.Infrastructure.Auth;
using Axpenser.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Axpenser.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly IJwtTokenService _jwt;

    public AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, IJwtTokenService jwt)
    {
        _users = users;
        _signIn = signIn;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new AppUser { UserName = req.Email, Email = req.Email };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await IssueJwtCookieAsync(user);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok) return Unauthorized();

        await IssueJwtCookieAsync(user);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id)) return Unauthorized();

        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return Unauthorized();

        return new UserProfileDto(user.Id, user.Email ?? "");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        CookieTokenWriter.ClearAccessTokenCookie(Response);
        return Ok();
    }

    // Google / GitHub OAuth

    [HttpGet("external/google")]
    public IActionResult Google([FromQuery] string returnUrl = "http://localhost:4200/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
        };
        return Challenge(props, "Google");
    }

    [HttpGet("external/google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl = "http://localhost:4200/")
    {
        var principal = User;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Redirect($"{returnUrl}?oauth=failed");

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email };
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded) return Redirect($"{returnUrl}?oauth=failed");
        }

        await IssueJwtCookieAsync(user);
        return Redirect(returnUrl);
    }

    [HttpGet("external/github")]
    public IActionResult GitHub([FromQuery] string returnUrl = "http://localhost:4200/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback), new { returnUrl })
        };
        return Challenge(props, "GitHub");
    }

    [HttpGet("external/github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string returnUrl = "http://localhost:4200/")
    {
        var principal = User;
        var email = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"{returnUrl}?oauth=email_required");

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email };
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded) return Redirect($"{returnUrl}?oauth=failed");
        }

        await IssueJwtCookieAsync(user);
        return Redirect(returnUrl);
    }

    private async Task IssueJwtCookieAsync(AppUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var token = _jwt.CreateAccessToken(user, roles);
        CookieTokenWriter.WriteAccessTokenCookie(Response, token);
    }
}
