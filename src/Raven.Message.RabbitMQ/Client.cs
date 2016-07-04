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
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rabbitMqClientConfig"></param>
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
                LoadConfig(rabbitMqClientConfig);
                _inited = true;
                Log.LogDebug("Client init complete", null);
            }
        }
        /// <summary>
        /// 重新加载配置
        /// </summary>
        /// <param name="config"></param>
        public static void Reload(ClientConfiguration config)
        {
            if (config == null)
            {
                Log.LogError("reload config is null", null, null);
                return;
            }
            if (!_inited)
                return;
            ReloadConfig(config);
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

        private static void LoadConfig(ClientConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            ClientConfiguration.Instance = config;
            string logType = config.LogType;
            Log = Activator.CreateInstance(Type.GetType(logType)) as ILog;
            if (config.Brokers == null)
            {
                Log.LogError("no broker configed", null, null);
                return;
            }
            foreach (BrokerConfiguration brokerConfig in config.Brokers)
            {
                CreateClient(brokerConfig);
            }
        }

        private static void ReloadConfig(ClientConfiguration config)
        {
            ClientConfiguration.Instance = config;
            string logType = config.LogType;
            Log = Activator.CreateInstance(Type.GetType(logType)) as ILog;
            if (config.Brokers == null)
            {
                Log.LogError("no broker configed", null, null);
                return;
            }
            foreach (BrokerConfiguration brokerConfig in config.Brokers)
            {
                if (_instances.ContainsKey(brokerConfig.Name))
                {
                    Client oldClient = _instances[brokerConfig.Name];
                    Client newClient = CreateClient(brokerConfig);
                    oldClient.Consumer.Recover(newClient.Consumer);
                }
                else
                {
                    CreateClient(brokerConfig);
                }
            }
        }

        private static Client CreateClient(BrokerConfiguration brokerConfig)
        {
            Client client = new Client(brokerConfig);
            if (_instances.ContainsKey(brokerConfig.Name))
            {
                _instances[brokerConfig.Name] = client;
            }
            else
            {
                _instances.Add(brokerConfig.Name, client);
            }
            return client;
        }

        internal Client(BrokerConfiguration brokerConfig)
        {
            BrokerConfig = brokerConfig;
            Channel = Factory.CreateChannel(Log, brokerConfig);
            Producer = Factory.CreateProducer(Log, brokerConfig);
            Consumer = Factory.CreateConsumer(this, Log, brokerConfig);
        }

        ~Client()
        {
            DisposeClient();
        }

        private bool _disposed = false;
        internal void DisposeClient()
        {
            if (_disposed)
                return;
            lock (this)
            {
                if (_disposed)
                    return;
                Channel.Release();
                _disposed = true;
            }
        }
    }
}
