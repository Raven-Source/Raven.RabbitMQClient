using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 连接管理者
    /// </summary>
    internal class ChannelManager
    {
        ILog Log { get; set; }

        BrokerConfiguration BrokerConfig { get; set; }

        ConcurrentQueue<IModel> _channelQueue = new ConcurrentQueue<IModel>();
        ConnectionFactory _factory;
        IConnection _connection;
        int _state;

        const int State_Init = 1;
        const int State_Release = 2;

        internal event EventHandler<ShutdownEventArgs> ConnectionShutdown;

        internal ChannelManager(ILog log, BrokerConfiguration brokerConfig)
        {
            Log = log;
            BrokerConfig = brokerConfig;
            Init();
        }

        void Init()
        {
            if (_factory == null)
            {
                _factory = new ConnectionFactory() { HostName = BrokerConfig.Host };
                if (!string.IsNullOrEmpty(BrokerConfig.UserName))
                {
                    _factory.UserName = BrokerConfig.UserName;
                    _factory.Password = BrokerConfig.Password;
                }
                if (BrokerConfig.Port != null)
                {
                    _factory.Port = BrokerConfig.Port.Value;
                }
                _factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(5); //当连接断开后每隔5秒检测一次
                _factory.AutomaticRecoveryEnabled = true;//自动恢复连接
            }
            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _state = State_Init;
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (ConnectionShutdown != null)
                ConnectionShutdown(sender, e);
        }

        void Release()
        {
            _connection.ConnectionShutdown -= OnConnectionShutdown;
            _connection = null;
            _channelQueue = new ConcurrentQueue<IModel>();
            _state = State_Release;
        }

        internal IModel GetChannel()
        {
            if (_state == State_Release)
            {
                Log.LogError("channels released", null, null);
                return null;
            }
            IModel channel;
            _channelQueue.TryDequeue(out channel);
            if (channel == null)
            {
                channel = _connection.CreateModel();
            }
            return channel;
        }

        internal void ReturnChannel(IModel channel)
        {
            if (channel == null)
                return;
            try
            {
                _channelQueue.Enqueue(channel);
            }
            catch { }
        }
    }
}
