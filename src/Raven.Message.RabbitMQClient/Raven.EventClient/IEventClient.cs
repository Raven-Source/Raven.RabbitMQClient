using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.EventClient
{
    /// <summary>
    /// 事件客户端
    /// </summary>
    public interface IEventClient:IDisposable
    {
        /// <summary>
        /// 发布事件消息
        /// </summary>
        /// <typeparam name="T">数据结构</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="data">消息体</param>
        /// <returns></returns>
        bool Publish<T>(int eventId, T data);
        /// <summary>
        /// 监听事件消息
        /// </summary>
        /// <typeparam name="T">数据结构</typeparam>
        /// <param name="configId">配置id <example>事件ID_业务ID</example></param>
        /// <param name="onRecived">监听到消息时处理事件</param>
        /// <returns></returns>
        bool Subscribe<T>(string configId, Func<T, bool> onRecived);
    }
}
