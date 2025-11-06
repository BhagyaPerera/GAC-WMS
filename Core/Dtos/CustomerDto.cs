namespace Core.Dtos
{
    public record CustomerDto(
    string? CustomerNo,
    string? Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Country,
    string? PostalCode,
    string? PhoneNumber,
    string? Email
    );

}
