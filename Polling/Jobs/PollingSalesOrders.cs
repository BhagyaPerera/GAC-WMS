using Infrastructure.FileIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;

namespace Polling.Jobs
{
    public class PollingSalesOrders : IJob
    {
        private readonly ILogger<PollingSalesOrders> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PollingSalesOrders(IServiceProvider serviceProvider,
            ILogger<PollingSalesOrders> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var salesOrderService = scope.ServiceProvider.GetRequiredService<FileIntegrationService>();

                    _logger.LogInformation("Polling Sales Orders started");
                    await salesOrderService.PullSalesOrderProcess();
                    _logger.LogInformation("Polling Sales ended");
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
