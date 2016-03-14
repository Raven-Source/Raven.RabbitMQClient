using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// 接收消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="message">消息</param>
    /// <param name="messageKey">消息关键字</param>
    /// <param name="messageId">消息标识</param>
    /// <param name="correlationId">关联标识</param>
    /// <returns></returns>
    public delegate bool MessageReceived<T>(T message, string messageKey, string messageId, string correlationId);

    /// <summary>
    /// 接收消息并构造响应消息
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TReply"></typeparam>
    /// <param name="message"></param>
    /// <param name="messageKey"></param>
    /// <param name="messageId"></param>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    public delegate TReply MessageReceived<TMessage, TReply>(TMessage message, string messageKey, string messageId, string correlationId);
}
