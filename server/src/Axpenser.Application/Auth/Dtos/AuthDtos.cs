namespace Axpenser.Application.Auth.Dtos
{
    public record RegisterRequest(string Name, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record UserProfileDto(Guid Id, string Email, string Name);
}
