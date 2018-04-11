using RabbitMQ.Client.Events;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// 处理消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="message">消息</param>
    /// <param name="messageKey">消息关键字</param>
    /// <param name="messageId">消息标识</param>
    /// <param name="correlationId">关联标识</param>
    /// <returns>消息处理完成</returns>
    public delegate bool MessageReceived<T>(T message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args);

    /// <summary>
    /// 处理消息并构造响应消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TReply">回复消息类型</typeparam>
    /// <param name="message">消息</param>
    /// <param name="messageKey">消息关键字</param>
    /// <param name="messageId">消息标识</param>
    /// <param name="correlationId">关联标识</param>
    /// <returns>响应消息</returns>
    public delegate TReply MessageReceived<TMessage, TReply>(TMessage message, string messageKey, string messageId, string correlationId, BasicDeliverEventArgs args);
}
