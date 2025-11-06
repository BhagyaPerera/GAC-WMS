namespace API.Background
{
    public class FailedMessagesScheduler : BackgroundService
    {
        private readonly ILogger<FailedMessagesScheduler> _logger;

        public FailedMessagesScheduler(ILogger<FailedMessagesScheduler> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Here you could:
                // - query DB for failed orders
                // - republish events to RabbitMQ
                _logger.LogInformation("Running scheduled job at {time}", DateTimeOffset.Now);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

}
