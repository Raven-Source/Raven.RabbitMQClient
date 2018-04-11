using Raven.Serializer;

namespace Raven.Message.RabbitMQ.Configuration
{
    public class ExchangeConfiguration
    {
        public ExchangeConfiguration(string name,ProducerConfiguration producerConfig=null,SerializerType? serializerType=null,string exchangeType="direct",
            bool durable=true,bool autoDelete=false)
        {
            Name = name;
            ProducerConfig = producerConfig;
            SerializerType = serializerType;
            ExchangeType = exchangeType;
            Durable = durable;
            AutoDelete = autoDelete;

        }
        public SerializerType? SerializerType { get; set; }
        public ProducerConfiguration ProducerConfig { get; set; }
        public string ExchangeType { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public string Name { get; set; }
    }
}
