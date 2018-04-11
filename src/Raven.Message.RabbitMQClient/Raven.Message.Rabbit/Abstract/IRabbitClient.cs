using System;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// RabbitMq客户端
    /// </summary>
    public interface IRabbitClient:IDisposable
    {
        void Send<T>(T message, string queue, SendOption option = null);
        void Publish<T>(T message, string queue, string exchange);
        void Subscribe<T>(string queue, string exchange,string routeKey,Func<T,bool> onRecived);
        void Recived<T>(string queue,  Func<T, bool> onRecived);
    }
}
