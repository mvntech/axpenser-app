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
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            return BadRequest(new { errors = new[] { "Email and Password are required." } });

        var user = new AppUser { UserName = req.Email, Email = req.Email, FullName = req.Name };
        var result = await _users.CreateAsync(user, req.Password);
        
        if (!result.Succeeded) 
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

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

        return new UserProfileDto(user.Id, user.Email ?? "", user.FullName ?? "");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        CookieTokenWriter.ClearAccessTokenCookie(Response);
        return Ok();
    }

    // Google / GitHub OAuth

    [HttpGet("external/google")]
    public IActionResult Google([FromQuery] string returnUrl = "https://localhost:4200/")
    {
        var redirectUrl = Url.Action(nameof(GoogleSuccess), "Auth", new { returnUrl }, Request.Scheme);
        var props = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, "Google");
    }

    [HttpGet("external/google/success")]
    public async Task<IActionResult> GoogleSuccess([FromQuery] string returnUrl = "https://localhost:4200/")
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authResult.Succeeded) 
        {
            return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=failed&reason=external_auth_failed");
        }

        var principal = authResult.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue("name") ?? principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(email)) return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=failed");

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, FullName = name };
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded) 
            {
                return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=failed");
            }
        }

        await IssueJwtCookieAsync(user);
        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    [HttpGet("external/github")]
    public IActionResult GitHub([FromQuery] string returnUrl = "https://localhost:4200/")
    {
        var redirectUrl = Url.Action(nameof(GitHubSuccess), "Auth", new { returnUrl }, Request.Scheme);
        var props = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, "GitHub");
    }

    [HttpGet("external/github/success")]
    public async Task<IActionResult> GitHubSuccess([FromQuery] string returnUrl = "https://localhost:4200/")
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authResult.Succeeded)
        {
            return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=failed&reason=external_auth_failed");
        }

        var principal = authResult.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue("name") ?? principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=email_required");

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, FullName = name };
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded) return Redirect($"{GetSafeReturnUrl(returnUrl)}?oauth=failed");
        }

        await IssueJwtCookieAsync(user);
        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    [HttpGet("external/error")]
    public IActionResult ExternalError([FromQuery] string reason)
    {
        return Redirect($"https://localhost:4200/auth/login?oauth=failed&reason={System.Net.WebUtility.UrlEncode(reason)}");
    }

    private string GetSafeReturnUrl(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/"))
            return "https://localhost:4200/";

        return "https://localhost:4200" + returnUrl;
    }

    private async Task IssueJwtCookieAsync(AppUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var token = _jwt.CreateAccessToken(user, roles);
        CookieTokenWriter.WriteAccessTokenCookie(Response, token);
    }
}
