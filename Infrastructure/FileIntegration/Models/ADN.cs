using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Infrastructure.FileIntegration.Models
{
    [XmlRoot(ElementName = "ADN", Namespace = "http://schemas.gacwms.com/SalesOrder")]
    public class ADN
    {
        [XmlElement(ElementName = "Id")]
        public string Id { get; set; } = string.Empty;

        [XmlElement(ElementName = "OrderNo")]
        public string OrderNo { get; set; } = string.Empty;

        [XmlElement(ElementName = "ProcessingDate")]
        public DateTime ProcessingDate { get; set; }

        [XmlElement(ElementName = "Customer")]
        public CustomerModel Customer { get; set; } = new();

        [XmlElement(ElementName = "ShipmentAddress")]
        public string ShipmentAddress { get; set; } = string.Empty;

        [XmlElement(ElementName = "Status")]
        public string Status { get; set; } = "Created";

        [XmlArray(ElementName = "Lines")]
        [XmlArrayItem(ElementName = "Line")]
        public List<SalesOrderLineModel> Lines { get; set; } = new();
    }

    // --------------------------
    // Nested Types
    // --------------------------

    public class SalesOrderLineModel
    {
        [XmlElement(ElementName = "Id")]
        public string Id { get; set; } = string.Empty;

        [XmlElement(ElementName = "SalesOrderId")]
        public string SalesOrderId { get; set; } = string.Empty;

        [XmlElement(ElementName = "Product")]
        public ProductModel Product { get; set; } = new();

        [XmlElement(ElementName = "Quantity")]
        public decimal Quantity { get; set; }
    }

    public class ProductModel
    {
        [XmlElement(ElementName = "ProductCode")]
        public string ProductCode { get; set; } = string.Empty;

        [XmlElement(ElementName = "Title")]
        public string? Title { get; set; }

        [XmlElement(ElementName = "Description")]
        public string? Description { get; set; }

        [XmlElement(ElementName = "Length")]
        public decimal? Length { get; set; }

        [XmlElement(ElementName = "Width")]
        public decimal? Width { get; set; }

        [XmlElement(ElementName = "Height")]
        public decimal? Height { get; set; }

        [XmlElement(ElementName = "Weight")]
        public decimal? Weight { get; set; }
    }

    public class CustomerModel
    {
        [XmlElement(ElementName = "CustomerNo")]
        public string CustomerNo { get; set; } = string.Empty;

        [XmlElement(ElementName = "Name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement(ElementName = "AddressLine1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [XmlElement(ElementName = "AddressLine2")]
        public string AddressLine2 { get; set; } = string.Empty;

        [XmlElement(ElementName = "City")]
        public string City { get; set; } = string.Empty;

        [XmlElement(ElementName = "Country")]
        public string Country { get; set; } = string.Empty;

        [XmlElement(ElementName = "PostalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [XmlElement(ElementName = "PhoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [XmlElement(ElementName = "Email")]
        public string Email { get; set; } = string.Empty;

        [XmlElement(ElementName = "IsActive")]
        public bool IsActive { get; set; } = true;

        [XmlElement(ElementName = "CreatedAtUtc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [XmlElement(ElementName = "UpdatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
