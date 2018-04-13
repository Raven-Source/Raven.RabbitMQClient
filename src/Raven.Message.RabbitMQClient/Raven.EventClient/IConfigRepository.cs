using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.EventClient.Configs;

namespace Raven.EventClient
{
    public interface IConfigService
    {
        /// <summary>
        /// 获取事件配置
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        EventConfig GetByEventId(int eventId);
        /// <summary>
        /// 获取业务事件配置
        /// </summary>
        /// <param name="configId"></param>
        /// <returns></returns>
        EventConfig GetByConfigId(string configId);
    }
}
