namespace Core.Dtos
{
    public record ProductDto(
        string? ProductCode,
        string? Title,
        string? Description,
        decimal? Width,
        decimal? Height,
        decimal? Length,
        decimal? Weight
    );
}

