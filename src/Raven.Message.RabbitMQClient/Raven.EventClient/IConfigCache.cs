using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.EventClient.Configs;

namespace Raven.EventClient
{
    /// <summary>
    /// 配置项缓存容器
    /// </summary>
    public interface IConfigCache
    {
        /// <summary>
        /// 根据配置ID获取事件消费端配置
        /// </summary>
        /// <param name="configId">配置ID<example>eventid_bussinessid</example></param>
        /// <returns></returns>
        EventConfig GetConsumeConfig(string configId);
        /// <summary>
        /// 获取事件的发布端配置
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        EventConfig GetProducerConfig(int eventId);
    }
}
