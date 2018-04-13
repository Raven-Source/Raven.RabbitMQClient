using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.EventClient.Configs;

namespace Raven.EventClient
{
    /// <summary>
    /// 本地缓存配置项
    /// </summary>
    public class LocalCache:IConfigCache
    {
        private readonly IConfigService _configService;
        readonly ConcurrentDictionary<string,EventConfig> _queueConfigs=new ConcurrentDictionary<string, EventConfig>();
        readonly ConcurrentDictionary<int,EventConfig> _eventConfigs=new ConcurrentDictionary<int, EventConfig>();

        public LocalCache(IConfigService configService)
        {
            _configService = configService;
        }

        public EventConfig GetConsumeConfig(string configId)
        {
            if (_queueConfigs.TryGetValue(configId, out var config))
            {
                if (config.ExpiredTime < DateTime.Now)
                    return config;
                _queueConfigs.TryRemove(configId, out var rm);
            }

            config = _configService.GetByConfigId(configId);
            if(config==null)
                throw new Exception($"config not found by id:{configId}");
            _queueConfigs.TryAdd(config.Id, config);
            return config;
        }

        public EventConfig GetProducerConfig(int eventId)
        {
            if (_eventConfigs.TryGetValue(eventId, out var config))
            {
                if (config.ExpiredTime < DateTime.Now)
                    return config;
                _eventConfigs.TryRemove(eventId, out var rm);
            }

            config = _configService.GetByEventId(eventId);
            if (config == null)
                throw new Exception($"config not found by eventid:{eventId}");
            _eventConfigs.TryAdd(config.EventId, config);
            return config;
        }
    }
}
