using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.EventClient
{
    /// <summary>
    /// 事件客户端工厂
    /// </summary>
    public class EventClientFactory
    {
        static EventClientFactory()
        {
            Default=new EventClientFactory();
        }
        /// <summary>
        /// 事件客户端工厂单例
        /// </summary>
        public static EventClientFactory Default { get; }

        public IConfigCache CreateLocalCache(IConfigService service)
        {
            return new LocalCache(service);
        }

    }
}
