using Azure.Storage.Blobs;
using Core.Entities.CustomerAggregate;
using Core.Entities.Logging;
using Core.Entities.ProductAggregate;
using Core.Entities.SalesOrderAggregate;
using Core.Events;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Specification;
using Infrastructure.FileIntegration.AzureSftp;
using Infrastructure.FileIntegration.Models;
using Infrastructure.Messaging;
using Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernal.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
//using DocumentFormat.OpenXml.Vml;

namespace Infrastructure.FileIntegration
{
    public class FileIntegrationService : IFileIntegrationService
    {
        private readonly ILogger<FileIntegrationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRepository<SalesOrderIncomingLog> _soIncomingLogRepo;
        private readonly IReadRepository<SalesOrder> _salesOrderRepo;
        private readonly IReadRepository<Customer> _customerRepo;
        private readonly IReadRepository<Product> _productRepo;
        private readonly string _salesOrderSchemaPath;
        private readonly BlobServiceClient _serviceClient;
        private readonly AzureSftpBlobConfiguration _azureBlobConfiguration;
        private readonly IRepository<SalesOrderIncomingLog> _salesIncomingLogRepository;
        private readonly RabbitMqPublisher _publisher;

        public FileIntegrationService(
            ILogger<FileIntegrationService> logger,
            IConfiguration configuration,
            IRepository<SalesOrderIncomingLog> salesIncomingLogRepository,
            IOptions<AzureSftpBlobConfiguration> azureBlobConfiguration,
            IReadRepository<SalesOrder> salesOrderRepo,
            IReadRepository<Customer> customerRepo,
            IReadRepository<Product> productRepo,
            RabbitMqPublisher publisher)
        {
            _logger = logger;
            _configuration = configuration;
            _salesOrderSchemaPath = configuration["XMLSchema:SalesOrderSchemaPath"];
            _azureBlobConfiguration = azureBlobConfiguration.Value;
            _serviceClient = new BlobServiceClient(_azureBlobConfiguration.ConnectionString);
            _salesOrderSchemaPath = configuration["XMLSchema:SalesOrderSchemaPath"];
            _salesIncomingLogRepository = salesIncomingLogRepository;
            _salesOrderRepo = salesOrderRepo;
            _customerRepo = customerRepo;
            _productRepo = productRepo;
            _publisher = publisher;

        }
        


        #region Pull and Process Sales Orders (Schema-Based)
        public async Task PullSalesOrderProcess(bool useLocalFile = false, string localFilePath = null)
        {
            List<ADN> salesOrders = new();

            try
            {
                string xmlContent = null;

                // ------------------------------
                // Step 1: Fetch XML (from Blob or Local)
                // ------------------------------
                try
                {
                    if (useLocalFile && !string.IsNullOrEmpty(localFilePath))
                    {
                        BlobContainerClient containerClient = _serviceClient.GetBlobContainerClient("gmc");
                        BlobClient blobClient = containerClient.GetBlobClient(localFilePath);

                        var downloadInfo = await blobClient.DownloadAsync();
                        using var reader = new StreamReader(downloadInfo.Value.Content);
                        xmlContent = await reader.ReadToEndAsync();

                        _logger.LogInformation("Successfully downloaded SalesOrder XML from Blob Storage: {File}", localFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching SalesOrder XML file.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _logger.LogWarning("No SalesOrder XML content found.");
                    return;
                }

                // ------------------------------
                // Step 2: Validate & Deserialize
                // ------------------------------
                var invalidOrders = ValidateXml(xmlContent, "ADN");
                if (invalidOrders.Any())
                {
                    _logger.LogError("SalesOrder XML failed validation: {Errors}", string.Join(", ", invalidOrders));
                    return;
                }

                ADN salesOrder = DeserializeXml<ADN>(xmlContent);
                if (salesOrder == null)
                {
                    _logger.LogError("Failed to deserialize SalesOrder XML.");
                    return;
                }

                salesOrders.Add(salesOrder);
                _logger.LogInformation("Parsed SalesOrder: {OrderNo}", salesOrder.OrderNo);

                // ------------------------------
                // Step 3: Validate business rules
                // ------------------------------
                if (salesOrder.Lines == null || !salesOrder.Lines.Any())
                {
                    _logger.LogError("SalesOrder {OrderNo} has no Lines — skipping.", salesOrder.OrderNo);
                    return;
                }

                bool allPositive = salesOrder.Lines.All(l => l.Quantity > 0);
                bool allNegative = salesOrder.Lines.All(l => l.Quantity < 0);

                if (!(allPositive || allNegative))
                {
                    _logger.LogError("SalesOrder {OrderNo} contains both positive and negative quantities.", salesOrder.OrderNo);
                    return;
                }

                // ------------------------------
                // Step 4: Check existing order
                // ------------------------------
                var existingOrderSpec = new GetPartnerOrderByOrderNoSpec(salesOrder.OrderNo);
                var existingOrder = await _salesOrderRepo.FirstOrDefaultAsync(existingOrderSpec);
                if (existingOrder != null && allPositive)
                {
                    _logger.LogWarning("SalesOrder {OrderNo} already exists in DB.", salesOrder.OrderNo);
                    return;
                }

                var existingCustomerSpec=new GetCustomerByCustomerNoSpec(salesOrder.Customer.CustomerNo);
                var existingCustomer=await _customerRepo.FirstOrDefaultAsync(existingCustomerSpec);

                if (existingCustomer == null)
                {
                    _logger.LogWarning("Customer not found.", salesOrder.Customer.CustomerNo);
                    return;
                }

                var products = await _productRepo.ListAsync();
                List<Product> productList = new List<Product>();
                foreach (var line in salesOrder.Lines)
                {
                    var product = products.FirstOrDefault(p => p.ProductCode == line.Product.ProductCode);
                    if (product == null)
                    {
                        _logger.LogWarning("Product not found: {ProductCode}", line.Product.ProductCode);
                        return;
                    }
                    productList.Add(product);
                }



                // ------------------------------
                // Step 5: Map to PartnerOrder and Publish
                // ------------------------------
                try
                {
                    SalesOrder partnerOrder = MapSalesOrder(salesOrder,existingCustomer, productList);
                    var evt = new SalesOrderCreateEvent(partnerOrder);
                    _publisher.Publish(evt);

                    _logger.LogInformation("Published SalesOrder {OrderNo} for processing.", salesOrder.OrderNo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process SalesOrder {salesOrder.OrderNo}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the SalesOrder processing job.");
            }
        }
        #endregion



        #region utilities


        private SalesOrder MapSalesOrder(ADN order,Customer customer, List<Product> products)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //check for customer


            List<SalesOrderLine> lines = new List<SalesOrderLine>();
            
            // Create SalesOrder object based on schema
            var salesOrder = new SalesOrder
            {
                OrderNo = order.OrderNo ?? string.Empty,
                ProcessingDate = DateTime.UtcNow,
                ShipmentAddress = order.ShipmentAddress,
                Status = order.Status ?? "Created",
                Customer=customer
            };

            var lineNo = 1;
            foreach (var line in order.Lines)
            {
                var product = products.FirstOrDefault(p => p.ProductCode == line.Product.ProductCode);
                if (product != null)
                {
                    SalesOrderLine soLine = new SalesOrderLine
                    {
                        Product = product,
                        Quantity = line.Quantity
                    };
                    soLine.LineNo = lineNo;
                    soLine.Product = product;
                    salesOrder.AddSalesOrderLine(soLine);
                }
            }


            



            return salesOrder;
        }
        private List<string> ValidateXml(string xmlContent, string schemaType)
        {
            var invalidInvoices = new List<string>();

            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string schemaPath;

                switch (schemaType)
                {
                    case "SalesOrder":
                        schemaPath = Path.Combine(baseDirectory, _salesOrderSchemaPath);
                        break;
                    default:
                        throw new ArgumentException("Invalid schema type specified");
                }

                if (!File.Exists(schemaPath))
                {
                    _logger.LogError($"Schema file not found at path: {schemaPath}");
                    throw new FileNotFoundException($"Schema file not found at path: {schemaPath}");
                }

                XmlSchemaSet schema = new XmlSchemaSet();
                schema.Add("", schemaPath);

                XDocument document = XDocument.Parse(xmlContent);
                bool isValid = true;
                document.Validate(schema, (o, e) =>
                {
                    isValid = false;
                    _logger.LogError($"XML Validation Error: {e.Message}");
                });

                if (isValid)
                {
                    var ns = document.Root?.GetDefaultNamespace();
                    var detailsElementName = schemaType == "ASN" ? "asn_detail" : "adn_detail";
                    var details = document.Root?.Elements(ns + detailsElementName);

                    var mandatoryFields = GetMandatoryFieldsForSchema(schemaType);

                    foreach (var detail in details)
                    {
                        var invoice = detailsElementName == "asn_detail"
                            ? detail.Element(ns + "Invoice")?.Value
                            : detail.Element(ns + "PickNo")?.Value;

                        foreach (var field in mandatoryFields)
                        {
                            var element = detail.Element(ns + field);
                            if (element == null || string.IsNullOrWhiteSpace(element.Value))
                            {
                                isValid = false;
                                _logger.LogError($"XML Validation Error: Mandatory field '{field}' is missing or empty in {detailsElementName} section.");
                                invalidInvoices.Add(invoice);
                                break;
                            }
                        }
                    }
                }
                return invalidInvoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during XML validation");
                throw;
            }
        }

        private List<string> GetMandatoryFieldsForSchema(string schemaType)
        {
            Type modelType;
            if (schemaType == "SalesOrder")
            {
                modelType = typeof(ADN);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported schema type: {schemaType}");
            }

            var mandatoryFields = modelType.GetProperties()
                                           .Where(p => p.GetCustomAttributes(typeof(RequiredAttribute), false).Any())
                                           .Select(p => p.Name)
                                           .ToList();

            return mandatoryFields;
        }



        private T DeserializeXml<T>(string xmlContent)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using StringReader reader = new StringReader(xmlContent);
                return (T)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during XML deserialization");
                throw;
            }
        }

        private string SerializeXml<T>(T value)
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                var xmlSettings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                };

                var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });

                using (var stringWriter = new Utf8StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                    {
                        xmlSerializer.Serialize(xmlWriter, value, emptyNamespaces);
                    }
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during XML serialization");
                throw;
            }
        }
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }




        #endregion


        #region Save SalesOrders in SalesOrderIncommingLogs Table
        public async Task<bool> LogIncomingSalesOrders(string adnXmlContent)
        {
            var contentItem = new SalesOrderIncomingLog();
            contentItem.SalesOrderXmlContent = adnXmlContent;
            try
            {
                var dbSaveResult = await _salesIncomingLogRepository.AddAsync(contentItem);
                return dbSaveResult != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log incoming SalesOrder XML.");
                return false;
            }
        }
        #endregion // utilities

    }
}