namespace Core.Dtos
{

        public record PurchaseOrderLineDto(string ProductCode, decimal Quantity);

        public record CreatePurchaseOrderDto(
            string OrderId,
            DateTime ProcessingDate,
            string CustomerId,
            List<PurchaseOrderLineDto> Lines
        );
    
}
