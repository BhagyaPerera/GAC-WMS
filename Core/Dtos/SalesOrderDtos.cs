namespace Core.Dtos
{
    public record SalesOrderLineDto(string ProductCode, decimal Quantity);

    public record CreateSalesOrderDto(
        string OrderId,
        string Name,
        DateTime ProcessingDate,
        string CustomerId,
        string ShipmentAddress,
        List<SalesOrderLineDto> Lines
    );
}
