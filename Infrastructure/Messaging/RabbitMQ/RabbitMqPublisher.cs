using Ardalis.GuardClauses;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly DefaultObjectPool<IModel> _objectPool;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IPooledObjectPolicy<IModel> objectPolicy, IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount * 2);
            _configuration = configuration.GetSection("Messaging:RabbitMQ:Config");
            _logger = logger;
        }

        public void Publish(SalesOrderCreateEvent eventToPublish)
        {
            Guard.Against.Null(eventToPublish, nameof(eventToPublish));

            var channel = _objectPool.Get();
            object message = (object)eventToPublish;

            try
            {
                string exchangeName = _configuration["Exchange"];
                channel.ExchangeDeclare(exchangeName, "direct", true, false, null);

                var messageString = JsonSerializer.Serialize(message);
                var sendBytes = Encoding.UTF8.GetBytes(messageString);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                  exchange: exchangeName,
                  routingKey: _configuration["Routes:NewSalesOrder"],
                  basicProperties: properties,
                  body: sendBytes);
                _logger.LogInformation($"Sending event: {messageString}");
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }


        public void Publish(PurchaseOrderCreateEvent eventToPublish)
        {
            Guard.Against.Null(eventToPublish, nameof(eventToPublish));

            var channel = _objectPool.Get();
            object message = (object)eventToPublish;

            try
            {
                string exchangeName = _configuration["Exchange"];
                channel.ExchangeDeclare(exchangeName, "direct", true, false, null);

                var messageString = JsonSerializer.Serialize(message);
                var sendBytes = Encoding.UTF8.GetBytes(messageString);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                  exchange: exchangeName,
                  routingKey: _configuration["Routes:NewPurchaseOrder"],
                  basicProperties: properties,
                  body: sendBytes);
                _logger.LogInformation($"Sending event: {messageString}");
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }
    }

}
