using System;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// RabbitMq客户端
    /// </summary>
    public interface IRabbitClient:IDisposable
    {
        bool Send<T>(T message, string queue, SendOption option = null);
        bool Publish<T>(T message, string queue, string exchange);
        bool Subscribe<T>(string queue, string exchange,string routeKey,Func<T,bool> onRecived);
        bool Recived<T>(string queue,  Func<T, bool> onRecived);
        bool Recived<T, TReply>(string queue, Func<T, TReply> onRecived);
    }
}
