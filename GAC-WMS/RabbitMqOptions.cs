namespace Infrastructure.Messaging.RabbitMQ
{
    public class RabbitMqOptions
    {
        public string HostName { get; set; }
        public int Port { get; set; } = 5672;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; } = "/";
        // Add other RabbitMQ configuration properties as needed
    }
}