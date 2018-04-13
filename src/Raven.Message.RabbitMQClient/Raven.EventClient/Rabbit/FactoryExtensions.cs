using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Message.RabbitMQ.Abstract;
using Raven.Message.RabbitMQ.Configuration;

namespace Raven.EventClient.Rabbit
{
    public static class FactoryExtensions
    {
        /// <summary>
        /// 创建基于rabbitmq的事件客户端,进程结束时需要释放
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="config">rabbit配置</param>
        /// <param name="configService">事件配置获取服务</param>
        /// <param name="log">日志工具</param>
        /// <returns></returns>
        public static IEventClient CreateRabbit(this EventClientFactory factory, ClientConfiguration config,IConfigService configService,ILog log=null)
        {
            var cache = factory.CreateLocalCache(configService);
            return new RabbitEventClient(config,cache,log);
        }
    }
}
