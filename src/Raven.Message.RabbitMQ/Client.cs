using RabbitMQ.Client;
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
        public Producer Producer { get; }

        public Consumer Consumer { get; }

        public BrokerConfiguration BrokerConfig { get; }

        public IConnection Connection
        {
            get { return Channel.Connection; }
        }

        public static ILog Log { get; set; }

        internal ClientConfiguration Config { get; set; }

        internal ChannelManager Channel { get; set; }

        static Dictionary<string, Client> _instances = new Dictionary<string, Client>();
        static bool _inited = false;

        public static Client GetInstance(string brokerName)
        {
            if (_instances != null)
                return _instances[brokerName];
            return null;
        }

        public static void Init()
        {
            Init(ClientConfiguration.Instance);
        }

        public static void Init(string configFile, string section = "ravenRabbitMQ")
        {
            Init(ClientConfiguration.LoadFrom(configFile, section));
        }

        public static void Init(ClientConfiguration rabbitMqClientConfig)
        {
            if (_inited)
                return;
            lock (_instances)
            {
                if (_inited)
                    return;
                if (rabbitMqClientConfig == null)
                    rabbitMqClientConfig = ClientConfiguration.Instance;
                if (rabbitMqClientConfig == null)
                    throw new ArgumentNullException("rabbitMqClientConfig");
                string logType = rabbitMqClientConfig.LogType;
                Log = Activator.CreateInstance(Type.GetType(logType)) as ILog;
                if (rabbitMqClientConfig.Brokers == null)
                {
                    Log.LogError("no broker configed", null, null);
                    return;
                }
                foreach (BrokerConfiguration brokerConfig in rabbitMqClientConfig.Brokers)
                {
                    Client instance = new Client(brokerConfig);
                    _instances.Add(brokerConfig.Name, instance);
                }
                _inited = true;
                Log.LogDebug("Client init complete", null);
            }
        }

        public static void Dispose()
        {
            Dictionary<string, Client> copy = _instances;
            _instances = null;
            foreach (string clientId in copy.Keys)
            {
                Client client = copy[clientId];
                client.DisposeClient();
            }
        }

        internal Client(BrokerConfiguration brokerConfig)
        {
            BrokerConfig = brokerConfig;
            Channel = Factory.CreateChannel(Log, brokerConfig);
            Producer = Factory.CreateProducer(Log, brokerConfig);
            Consumer = Factory.CreateConsumer(this, Log, brokerConfig);
        }

        internal void DisposeClient()
        {
            Channel.Release();
        }
    }
}
