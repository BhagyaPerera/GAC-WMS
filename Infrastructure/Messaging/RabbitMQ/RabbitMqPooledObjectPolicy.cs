using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Ardalis.GuardClauses;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMqModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
    {
        private readonly IConnection _connection;

        public RabbitMqModelPooledObjectPolicy(
          IOptions<RabbitMqConfiguration> rabbitMqOptions,
          ILogger<RabbitMqModelPooledObjectPolicy> logger)
        {
            _connection = GetConnection(rabbitMqOptions.Value);
        }

        private IConnection GetConnection(RabbitMqConfiguration settings)
        {
            Guard.Against.Null(settings, nameof(settings));

            var factory = new ConnectionFactory()
            {
                HostName = settings.Hostname,
                UserName = settings.UserName,
                Password = settings.Password,
                Port = settings.Port,
                VirtualHost = settings.VirtualHost,
            };
            factory.AutomaticRecoveryEnabled = true;

            Policy reTryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(10, i => TimeSpan.FromSeconds(2));

            var connection = reTryPolicy.Execute<IConnection>(() =>
            {
                return factory.CreateConnection();
            });

            return connection;
        }

        public IModel Create()
        {
            return _connection.CreateModel();
        }

        public bool Return(IModel obj)
        {
            if (obj.IsOpen)
            {
                return true;
            }
            else
            {
                obj?.Dispose();
                return false;
            }
        }
    }
}
