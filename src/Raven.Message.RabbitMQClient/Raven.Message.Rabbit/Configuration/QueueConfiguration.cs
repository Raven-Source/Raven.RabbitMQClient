using Raven.Serializer;

namespace Raven.Message.RabbitMQ.Configuration
{
    /// <summary>
    /// 队列配置
    /// </summary>
    public class QueueConfiguration
    {

        public QueueConfiguration(string name, int maxPriority = 0, uint expiration = 0, uint maxLength = 0,
            bool redeclareWhenFailed = true, string bindToExchange = "",
            string bindMessageKeyPattern = "", string deadMessageKeyPattern = "", SerializerType? serializerType = null,
            string replyQueue = "", bool needAck = true,
            ushort maxWorker = 100, bool durable = true, bool autoDelete = false, string deadExchange = "",ProducerConfiguration producerConfig=null)
        {
            Name = name;
            MaxWorker = maxWorker;
            MaxLength = maxLength;
            MaxPriority = maxPriority;
            Expiration = expiration;
            RedeclareWhenFailed = redeclareWhenFailed;
            BindToExchange = bindToExchange;
            BindMessageKeyPattern = bindMessageKeyPattern;
            DeadMessageKeyPattern = deadMessageKeyPattern;
            SerializerType = serializerType;
            ReplyQueue = replyQueue;
            NeedAck = needAck;
            Durable = durable;
            AutoDelete = autoDelete;
            DeadExchange = deadExchange;
            ProducerConfig = producerConfig;
        }
        public string Name { get; set; }
        public int MaxPriority { get; set; }
        public uint Expiration { get; set; }
        public uint MaxLength { get; set; }
        public bool RedeclareWhenFailed { get; set; }
        public string BindToExchange { get; set; }
        public string BindMessageKeyPattern { get; set; }
        public string DeadMessageKeyPattern { get; set; }
        public SerializerType? SerializerType { get; set; }
        public string ReplyQueue { get; set; }
        public bool NeedAck { get; set; }
        public ushort MaxWorker { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public string DeadExchange { get; set; }
        public ProducerConfiguration ProducerConfig { get; set; }
    }
}
