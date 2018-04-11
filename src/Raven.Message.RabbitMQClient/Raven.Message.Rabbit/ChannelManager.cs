using RabbitMQ.Client;
using Raven.Message.RabbitMQ.Abstract;
using System;
using System.Collections.Concurrent;

namespace Raven.Message.RabbitMQ
{
    /// <summary>
    /// 连接管理者
    /// </summary>
    internal class ChannelManager:IDisposable
    {
        private readonly ILog _log;
        ConcurrentQueue<IModel> _channelQueue = new ConcurrentQueue<IModel>();
        readonly IConnection _connection;
        private readonly ConnectionFactory _factory;
        int _state;
        const int StateInit = 1;
        const int StateRelease = 2;

        internal IConnection Connection => _connection;

        internal event EventHandler<ShutdownEventArgs> ConnectionShutdown;

        internal ChannelManager(string uri,ILog log )
        {
            _log = log;
            _factory = new ConnectionFactory
            {
                Uri = new Uri(uri),
                //当连接断开后每隔5秒检测一次
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                //自动恢复连接
                AutomaticRecoveryEnabled = true
            };
            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _state = StateInit;
        }


        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            ConnectionShutdown?.Invoke(sender, e);
        }

   

        internal IModel GetChannel()
        {
            if (_state == StateRelease)
            {
                _log?.LogError("channels released", null, null);
                return null;
            }

            _channelQueue.TryDequeue(out var channel);
            return channel ?? _connection.CreateModel();
        }

        internal void ReturnChannel(IModel channel)
        {
            if (channel == null)
                return;
            try
            {
                _channelQueue.Enqueue(channel);
            }
            catch(Exception ex)
            {
                _log?.LogError("channel enqueue:",ex,null);
            }
        }

        public void Dispose()
        {
            try
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.Close();
                _channelQueue = new ConcurrentQueue<IModel>();
                _state = StateRelease;
            }
            catch(Exception ex)
            {
                _log?.LogError("channels released",ex,null);
            }
        }
    }
}
