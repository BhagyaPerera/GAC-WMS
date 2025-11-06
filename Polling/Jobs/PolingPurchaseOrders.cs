using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Quartz;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.FileIntegration;

namespace Polling.Jobs
{
    public class PolingPurchaseOrders : IJob
    {
        private readonly ILogger<PolingPurchaseOrders> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public PolingPurchaseOrders(IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<PolingPurchaseOrders> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var purchaseOrderService = scope.ServiceProvider.GetRequiredService<FileIntegrationService>();

                    _logger.LogInformation("Polling Purchase Orders started");
                    //await purchaseOrderService.PullSalesOrderProcess();
                    _logger.LogInformation("Polling PurchaseOrder ended");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.InnerException?.Message);
                    _logger.LogError(e.Message);
                    return;
                }
            }
        }
    }
    
}
