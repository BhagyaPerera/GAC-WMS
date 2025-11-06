using Core.Interfaces;
using Core.Services;
using Core.Services.Identity;
using Infrastructure.FileIntegration;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polling.Jobs;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Quartz.Simpl;
using Quartz.Spi;
using System;

var builder = Host.CreateDefaultBuilder(args);

Host.CreateDefaultBuilder(args)
.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("appsettings.development.json", optional: false, reloadOnChange: true);
    config.AddEnvironmentVariables();
});

builder.ConfigureServices((hostContext, services) =>
{


    var configuration = hostContext.Configuration;

    // Add Quartz services
    services.AddQuartz(q =>
    {
        // PollingSalesOrders job every 5 minutes
        var salesOrdersJobKey = new JobKey("PollingSalesOrdersJob");
        q.AddJob<PollingSalesOrders>(opts => opts.WithIdentity(salesOrdersJobKey));
        q.AddTrigger(opts => opts
            .ForJob(salesOrdersJobKey)
            .WithIdentity("PollingSalesOrdersJob-trigger")
            .WithCronSchedule("0 */2 * * * ?")); // Every 2 minutes

        // PollingPurchaseOrders job every 5 minutes
        var purchaseOrdersJobKey = new JobKey("PollingPurchaseOrdersJob");
        q.AddJob<PolingPurchaseOrders>(opts => opts.WithIdentity(purchaseOrdersJobKey));
        q.AddTrigger(opts => opts
            .ForJob(purchaseOrdersJobKey)
            .WithIdentity("PollingPurchaseOrdersJob-trigger")
            .WithCronSchedule("0 */5 * * * ?")); // Every 5 minutes
    });

    // Add Quartz hosted service
    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    // Application & Domain Services
    services.AddScoped<PurchaseOrdersService>();
    services.AddScoped<SalesOrdersService>();
    services.AddScoped<CustomerService>();
    services.AddScoped<ProductService>();
    services.AddScoped<AuthService>();

    // Add File Integration service
    services.AddScoped<IFileIntegrationService, FileIntegrationService>();
    services.AddScoped<FileIntegrationService>();

    // Register RabbitMqPublisher for DI
    services.AddScoped<Infrastructure.Messaging.RabbitMQ.RabbitMqPublisher>();
    services.AddSingleton<Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<RabbitMQ.Client.IModel>, Infrastructure.Messaging.RabbitMQ.RabbitMqModelPooledObjectPolicy>();

    // Add Database Context
    services.AddDbContext<IntegrationDbContext>();

    services.AddScoped(typeof(SharedKernal.Interfaces.IRepository<>), typeof(Infrastructure.Persistence.ApiEfRepository<>));
    // Register IReadRepository<T> using ApiEfRepository<T>
    services.AddScoped(typeof(SharedKernal.Interfaces.IReadRepository<>), typeof(Infrastructure.Persistence.ApiEfRepository<>));

    // Register IMemoryCache for caching support
    services.AddMemoryCache();
});

var host = builder.Build();
await host.RunAsync();