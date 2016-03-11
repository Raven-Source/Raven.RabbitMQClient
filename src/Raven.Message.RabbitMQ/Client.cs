using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 客户端入口
    /// </summary>
    public class Client
    {
        public Producer Producer { get; set; }

        public Consumer Consumer { get; set; }

        public BrokerConfiguration BrokerConfig { get; set; }

        public static ILog Log { get; set; }

        internal ClientConfiguration Config { get; set; }

        static Dictionary<string, Client> _instances = new Dictionary<string, Client>();

        public static Client GetInstance(string brokerName)
        {
            return _instances[brokerName];
        }

        public static void Init()
        {
            string logType = ClientConfiguration.Instance.LogType;
            Log = Activator.CreateInstance(Type.GetType(logType)) as ILog;
            if (ClientConfiguration.Instance.Brokers == null)
            {
                Log.LogError("no broker configed", null, null);
                return;
            }
            foreach (BrokerConfiguration brokerConfig in ClientConfiguration.Instance.Brokers)
            {
                Client instance = new Client(brokerConfig);
                _instances.Add(brokerConfig.Name, instance);
            }
            Log.LogDebug("Client init complete", null);
        }

        internal Client(BrokerConfiguration brokerConfig)
        {
            BrokerConfig = brokerConfig;
            ChannelManager channelManager = new ChannelManager(Log, brokerConfig);
            Producer = Factory.CreateProducer(Log, brokerConfig);
            Consumer = Factory.CreateConsumer(Log, brokerConfig);
        }
    }
}
