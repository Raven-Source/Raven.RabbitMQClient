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

        public ClientConfiguration():this("","")
        {
        }

        public ClientConfiguration(string uri, string name, SerializerType serializerType = SerializerType.NewtonsoftJson)
        {
            Uri = uri;
            Name = name;
            SerializerType = serializerType;
            QueueConfigs = new Dictionary<string, QueueConfiguration>();
            ExchangeConfigurations = new Dictionary<string, ExchangeConfiguration>();

        }
    }
}
