using System.Collections.Generic;
using Raven.Serializer;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class ClientConfiguration
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public SerializerType SerializerType { get; set; }
        public Dictionary<string,QueueConfiguration> QueueConfigs { get; set; }
        public Dictionary<string,ExchangeConfiguration> ExchangeConfigurations { get; set; }

        public ClientConfiguration()
        {
            SerializerType = SerializerType.MessagePack;
            QueueConfigs=new Dictionary<string, QueueConfiguration>();
            ExchangeConfigurations=new Dictionary<string, ExchangeConfiguration>();
        }
    }
}
