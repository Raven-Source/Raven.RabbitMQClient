using System;

namespace Raven.Message.RabbitMQ.Abstract
{
    /// <summary>
    /// 日志
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="errorMessage">异常</param>
        /// <param name="ex"></param>
        /// <param name="dataObj">消息对象</param>
        void LogError(string errorMessage, Exception ex, object dataObj);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="info"></param>
        /// <param name="dataObj"></param>
        void LogDebug(string info, object dataObj);
    }
}
