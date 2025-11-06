
using Core.Events.ApplicationEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernal;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMqHostedService : IHostedService
    {
        private const int MaxRetryCount = 5; // Maximum number of retries
        private const int MaxParallelHandlers = 5;
        private const ushort PrefetchCount = 10;

        private readonly DefaultObjectPool<IModel> _objectPool;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqHostedService> _logger;
        private readonly IConfiguration _configuration;

        private IModel SalesOrderchannel { get; set; }
        private IModel PurchaseOrderchannel { get; set; }


        private readonly SemaphoreSlim _concurrencyLimiter = new(MaxParallelHandlers);

        public RabbitMqHostedService(IPooledObjectPolicy<IModel> objectPolicy,
            ILogger<RabbitMqHostedService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration.GetSection("Messaging:RabbitMQ:Config");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            NewSalesOrderEventHandler();
            NewPurchaseOrderEventHandler();
            return Task.CompletedTask;
        }

        #region NewSalesOrder
        private void NewSalesOrderEventHandler()
        {
            SalesOrderchannel = _objectPool.Get();
            SalesOrderchannel.ExchangeDeclare(_configuration["Exchange"], ExchangeType.Direct, true, false);
            SalesOrderchannel.QueueDeclare(_configuration["Queues:NewSalesOrder"], true, false, false, arguments: null);
            SalesOrderchannel.QueueBind(_configuration["Queues:NewSalesOrder"], _configuration["Exchange"], _configuration["Routes:NewSalesOrder"], null);
            SalesOrderchannel.BasicQos(0, 1, true);

            var consumer = new EventingBasicConsumer(SalesOrderchannel);
            consumer.Received += OnNewSalesOrderReceived;

            SalesOrderchannel.BasicConsume(queue: _configuration["Queues:NewSalesOrder"],
                                 autoAck: false,
                                 consumer: consumer);
        }

        private void OnNewSalesOrderReceived(object model, BasicDeliverEventArgs args)
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation(" [x] Received {0}", message);

            try
            {
                var receivedEvent = JsonSerializer.Deserialize<BaseIntegrationEvent>(message);
                if (receivedEvent != null && receivedEvent.EventType == nameof(SalesOrderCreateEvent))
                {
                    var newSalesOrderEvent = JsonSerializer.Deserialize<SalesOrderCreateEvent>(message);
                    newSalesOrderEvent.SalesOrderLines.ToList().ForEach(line => newSalesOrderEvent.SalesOrder.AddSalesOrderLine(line));

                    if (newSalesOrderEvent != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var _handler = scope.ServiceProvider.GetRequiredService<IApplicationEventHandler<SalesOrderCreateEvent>>();
                            if (_handler.Handle(newSalesOrderEvent).Result)
                            {
                                SalesOrderchannel.BasicAck(args.DeliveryTag, false);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogError(message);
                SalesOrderchannel.BasicAck(args.DeliveryTag, false);
            }
        }
        #endregion


        #region NewPurchaseOrder
        private void NewPurchaseOrderEventHandler()
        {
            SalesOrderchannel = _objectPool.Get();
            SalesOrderchannel.ExchangeDeclare(_configuration["Exchange"], ExchangeType.Direct, true, false);
            SalesOrderchannel.QueueDeclare(_configuration["Queues:NewPurchaseOrder"], true, false, false, arguments: null);
            SalesOrderchannel.QueueBind(_configuration["Queues:NewPurchaseOrder"], _configuration["Exchange"], _configuration["Routes:NewPurchaseOrder"], null);
            SalesOrderchannel.BasicQos(0, 1, true);

            var consumer = new EventingBasicConsumer(PurchaseOrderchannel);
            consumer.Received += OnNewPurchaseOrderReceived;

            SalesOrderchannel.BasicConsume(queue: _configuration["Queues:NewPurchaseOrder"],
                                 autoAck: false,
                                 consumer: consumer);
        }

        private void OnNewPurchaseOrderReceived(object model, BasicDeliverEventArgs args)
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation(" [x] Received {0}", message);

            try
            {
                var receivedEvent = JsonSerializer.Deserialize<BaseIntegrationEvent>(message);
                if (receivedEvent != null && receivedEvent.EventType == nameof(PurchaseOrderCreateEvent))
                {
                    var newPurchaseOrderEvent = JsonSerializer.Deserialize<PurchaseOrderCreateEvent>(message);
                    newPurchaseOrderEvent.PurchaseOrderLines.ToList().ForEach(line => newPurchaseOrderEvent.PurchaseOrder.AddPurchaseOrderLine(line));

                    if (newPurchaseOrderEvent != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var _handler = scope.ServiceProvider.GetRequiredService<IApplicationEventHandler<PurchaseOrderCreateEvent>>();
                            if (_handler.Handle(newPurchaseOrderEvent).Result)
                            {
                                PurchaseOrderchannel.BasicAck(args.DeliveryTag, false);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogError(message);
                SalesOrderchannel.BasicAck(args.DeliveryTag, false);
            }
        }
        #endregion

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _objectPool.Return(SalesOrderchannel);
            _objectPool.Return(PurchaseOrderchannel);
            return Task.CompletedTask;
        }



    }

}
