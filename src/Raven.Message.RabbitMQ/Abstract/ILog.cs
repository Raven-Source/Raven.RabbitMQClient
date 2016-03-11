using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="ex">异常</param>
        /// <param name="dataObj">消息对象</param>
        void LogError(string errorMessage, Exception ex, object dataObj);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="info"></param>
        void LogDebug(string info, object dataObj);
    }
}
