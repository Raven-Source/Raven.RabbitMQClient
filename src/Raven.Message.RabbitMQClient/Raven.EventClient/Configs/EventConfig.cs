using System;

namespace Raven.EventClient.Configs
{
    /// <summary>
    /// 事件配置
    /// </summary>
    public class EventConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 事件ID
        /// </summary>
        public int EventId { get; set; }
        /// <summary>
        /// 交换机名称
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// 绑定路由
        /// </summary>
        public string RouteKey { get; set; }
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; }
        public DateTime ExpiredTime { get; set; }

        public EventConfig()
        {
            ExpiredTime = DateTime.Now.AddMinutes(10);
        }
    }
}
