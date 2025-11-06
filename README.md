🧩GAC-WMS Integration Architecture Document
🏗 Overview
The GAC-WMS Integration Platform is a modular .NET 9–based middleware designed to integrate external ERP systems and the internal GAC Warehouse Management System (WMS).
It supports:
Real-time data ingestion via REST APIs


File-based legacy integration (via SFTP or shared folders)


Event-driven messaging via RabbitMQ


Automated scheduler jobs for XML polling, parsing, and transformation


Robust retry and validation workflows


Centralized JWT-secured authentication



🧱 1. Solution Structure
src/
│
├── Core/                     → Domain + Application Layer
│   ├── Dtos/                 → Data Transfer Objects
│   ├── Entities/             → Domain entities (SalesOrder, PurchaseOrder, Product, Customer)
│   ├── EventHandlers/        → Domain + Integration Event Handlers
│   ├── Events/               → Application Events (e.g., SalesOrderCreated)
│   ├── Interfaces/           → Abstractions (Repositories, MessagePublisher, Services)
│   ├── Services/             → Business logic services (Orders, Products, Customers)
│   ├── Specification/        → Query filter and criteria patterns
│   └── Program.cs            → For test or domain bootstrapping (if needed)
│
├── Infrastructure/           → Infrastructure Layer
│   ├── Persistence/          → EF Core DbContext (IntegrationDbContext), Migrations
│   ├── Messaging/            → RabbitMQ Publisher / Consumer services
│   ├── FileIntegration/      → XML file pollers & parsers (SFTP/Local)
│   ├── Configurations/       → EntityType configurations & app settings bindings
│   └── Identity/             → ASP.NET Identity entities and seeders
│
├── API/                      → Presentation Layer (Web API)
│   ├── Controllers/          → REST endpoints for Orders, Products, Customers, Auth
│   ├── Background/           → Quartz Schedulers and retry services
│   ├── Migrations/           → (Optionally mirrored migrations)
│   ├── appsettings.json      → Connection strings + RabbitMQ + Scheduler configs
│   └── Program.cs            → Entry point for the entire solution
│
├── SharedKernal/             → Shared cross-cutting concerns
│   ├── Interfaces/           → IRepository, IReadRepository, IAggregateRoot
│   ├── BaseEntities/         → BaseEntity, ValueObjects, Domain Events
│   └── Helpers/              → Validation and Utility functions
│
└── GAC-WMS.sln


⚙️ 2. Technology Stack
Category
Technology
Framework
.NET 9 Web API
ORM
Entity Framework Core 9
Database
SQL Server 2022
Messaging
RabbitMQ (AMQP 0-9-1)
Authentication
ASP.NET Identity + JWT Bearer
Scheduler
Quartz.NET 3.7 / BackgroundService
Caching
IMemoryCache + Repository Pattern
Retry Policy
Polly (Transient Resilience)
File Integration
SFTP via Atmoz SFTP container / Local Path Polling
Logging
Microsoft.Extensions.Logging + Serilog (optional)


🧠 3. Design Principles
Principle
Description
Clean Architecture
Separates concerns across Core, Infrastructure, and API layers.
Dependency Inversion
Core defines interfaces, Infrastructure implements them.
SOLID Principles
Each class has a single responsibility and depends on abstractions.
Repository Pattern
Generic repositories handle CRUD logic with EF Core.
Event-Driven Design
Order creation publishes RabbitMQ events for downstream systems.
Retry & Resilience
Polly policies for RabbitMQ and SFTP network faults.
Validation Pipeline
DTO validation using FluentValidation before persistence.


📦 4. Data Persistence Layer
IntegrationDbContext
Located in Infrastructure/Persistence/IntegrationDbContext.cs
Handles entity configurations and relationships for:
SalesOrders, PurchaseOrders, Products, Customers


Repository Pattern
public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

ApiEfRepository<T> implements this interface using EF Core’s DbSet<T>.
Caching and read optimization are implemented via CachedRepository<T> using IMemoryCache.

📨 5. Messaging – RabbitMQ Integration
Configuration
appsettings.json
"Messaging": {
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": 5672,
    "Config": {
      "Exchange": "gac-dev",
      "Queues": {
        "NewSalesOrder": "sales.order",
        "NewPurchaseOrder": "purchase.order"
      }
    }
  }
}

Publisher
RabbitMqPublisher uses object pooling for IModel (channels):
public void Publish(SalesOrderCreateEvent eventToPublish)
{
    var channel = _objectPool.Get();
    channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Direct);
    channel.BasicPublish(exchange: _settings.Exchange,
                         routingKey: "sales.order",
                         basicProperties: null,
                         body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventToPublish)));
}

✅ Uses Polly retry policies to reconnect on transient failures.

⏰ 6. Scheduler – XML File Polling
File-based integration uses either:
Local path (C:\Integration\Inbound)


Or SFTP (/home/integration/inbound)


Jobs are executed by Quartz or Hosted Services every N minutes (per appsettings.json):
"Scheduler": {
  "Jobs": [
    { "Name": "PurchaseOrderJob", "Cron": "0 */5 * * * ?", "Target": "purchaseOrders" },
    { "Name": "SalesOrderJob",    "Cron": "0 */10 * * * ?", "Target": "salesOrders" }
  ]
}

Each job:
reads XML files


Validates against XSD (schema)


Deserializes to DTOs


Persists via EF Core repositories


Publishes RabbitMQ events


Archives processed files



🧾 7. Validation and Retry
Validation
XML schema (XSD) validation for inbound files


DTO validation using FluentValidation


Business-rule validation before publishing to RabbitMQ


Retry
Implemented via Polly:
Policy
 .Handle<BrokerUnreachableException>()
 .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

Ensures transient errors (e.g. RabbitMQ down, SFTP unreachable) auto-retry before failure.

🔒 8. Authentication & Authorization
JWT Bearer tokens for API access


ASP.NET Identity for user management


Default admin created via IdentitySeeder


{
  "email": "u001@gmail.com",
  "password": "User001@123"
}


🧰 9. Background Services
FailedMessagesScheduler
Runs on a timer to retry publishing failed messages


Logs status to the database for audit purposes


FileIntegrationService
Handles file polling logic (SFTP + local path)


Runs continuously via Quartz trigger



🚀 10. Deployment Notes
Component
Host
Port
API
http://localhost:5000
5000
RabbitMQ
http://localhost:15672
5672 (AMQP) / 15672 (HTTP UI)
SQL Server
localhost,1433
1433
SFTP Container
localhost:2222
2222

Docker example for SFTP:
docker run -p 2222:22 \
  -v C:\SFTP\Inbound:/home/integration/inbound \
  -v C:\SFTP\Archive:/home/integration/archive \
  -e SFTP_USERS="integrationuser:password:1001" atmoz/sftp


🧾 11. Key Design Benefits
✅ Decoupled architecture → testable and maintainable
 ✅ Asynchronous integration via RabbitMQ
 ✅ Extensible file integration framework (SFTP/Shared folder)
 ✅ Scheduler-based automation
 ✅ Robust resilience and validation layer
 ✅ Secure authentication with JWT and Identity




















