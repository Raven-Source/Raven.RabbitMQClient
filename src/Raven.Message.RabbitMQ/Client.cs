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
        public Producer Producer { get; private set; }

        public Consumer Consumer { get; private set; }

        public BrokerConfiguration BrokerConfig { get; private set; }

        public IConnection Connection
        {
            get { return Channel.Connection; }
        }

        public static ILog Log { get; set; }

        internal ClientConfiguration Config { get; set; }

        internal ChannelManager Channel { get; set; }

        static Dictionary<string, Client> _instances = new Dictionary<string, Client>();
        static bool _inited = false;
        static IBrokerWatcher _watcher = null;

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

        public static void Init(IBrokerWatcher brokerWatcher)
        {
            Init(ClientConfiguration.Instance, brokerWatcher);
        }

        public static void Init(string configFile, string section = "ravenRabbitMQ", IBrokerWatcher brokerWatcher = null)
        {
            Init(ClientConfiguration.LoadFrom(configFile, section), brokerWatcher);
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="rabbitMqClientConfig"></param>
        public static void Init(ClientConfiguration rabbitMqClientConfig)
        {
            Init(rabbitMqClientConfig, null);
        }

        public static void Init(ClientConfiguration rabbitMqClientConfig, IBrokerWatcher brokerWatcher)
        {
            if (_inited)
                return;
            lock (_instances)
            {
                if (_inited)
                    return;
                if (rabbitMqClientConfig == null)
                    rabbitMqClientConfig = ClientConfiguration.Instance;
                LoadConfig(rabbitMqClientConfig, brokerWatcher);
                if (brokerWatcher != null)
                {
                    _watcher = brokerWatcher;
                    brokerWatcher.BrokerUriChanged += BrokerWatcher_BrokerUriChanged;
                }
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
            Log.LogDebug("reload complete", null);
        }

        public static void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.BrokerUriChanged -= BrokerWatcher_BrokerUriChanged;
                _watcher = null;
            }
            Dictionary<string, Client> copy = _instances;
            _instances = null;
            foreach (string clientId in copy.Keys)
            {
                Client client = copy[clientId];
                client.DisposeClient();
            }
        }

        private static void LoadConfig(ClientConfiguration config, IBrokerWatcher brokerWatcher)
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
                if (brokerWatcher != null)
                {
                    string uri = brokerWatcher.GetBrokerUri(brokerConfig.Name);
                    brokerConfig.SetUri(uri);
                }
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
                    Factory.ResetBroker(brokerConfig.Name);
                    Client newClient = CreateClient(brokerConfig);
                    oldClient.Consumer.Recover(newClient.Consumer);
                    oldClient.PrepareToDispose();
                }
                else
                {
                    CreateClient(brokerConfig);
                }
            }
        }

        private static void BrokerWatcher_BrokerUriChanged(object sender, BrokerChangeEventArg e)
        {
            if (e == null || string.IsNullOrEmpty(e.BrokerName) || string.IsNullOrEmpty(e.BrokerUri))
            {
                Log.LogError("broker uri change event arg empty", null, null);
                return;
            }
            if (_instances.ContainsKey(e.BrokerName))
            {
                Client oldClient = _instances[e.BrokerName];
                BrokerConfiguration brokerConfig = oldClient.BrokerConfig;
                brokerConfig.SetUri(e.BrokerUri);
                Factory.ResetBroker(e.BrokerName);
                Client newClient = CreateClient(brokerConfig);
                oldClient.Consumer.Recover(newClient.Consumer);
                oldClient.PrepareToDispose();
            }
            Log.LogDebug(string.Format("broker uri changed, {0} {1}", e.BrokerName, e.BrokerUri), null);
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
            Consumer = Factory.CreateConsumer(Producer, Log, brokerConfig);
        }

        ~Client()
        {
            DisposeClient();
            Log.LogDebug("client disposed by destructor", null);
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

        internal void PrepareToDispose()
        {
            Producer = null;
            Consumer = null;
        }
    }
}
