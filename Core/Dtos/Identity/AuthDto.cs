namespace Core.Dtos.Identity
{
    public record RegisterDto(string UserId,string FullName, string Email, string Password, string Role);
    public record LoginDto(string Email, string Password);
}
