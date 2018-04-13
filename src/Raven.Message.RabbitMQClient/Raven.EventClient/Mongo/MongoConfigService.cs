using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.EventClient.Configs;

namespace Raven.EventClient.Mongo
{
    public class MongoConfigService:IConfigService
    {
        readonly Dictionary<int,EventConfig> _eventConfigs=new Dictionary<int, EventConfig>(1)
        {
            { 1,new EventConfig
            {
                EventId = 1,
                Exchange = "event_exchange",
                RouteKey = "paysuccess",
            }}
        };
        readonly Dictionary<string,EventConfig> _queueConfigs=new Dictionary<string, EventConfig>
        {
            {
                "1_sendmsg",new EventConfig{
                    Id="1_sendmsg",
                    EventId = 1,
                    Exchange = "event_exchange",
                    RouteKey = "paysuccess",
                    QueueName = "1_sendmsg",
                }
            },
            {
                "1_completeorder",new EventConfig{
                    Id = "1_completeorder",
                    EventId = 1,
                    Exchange = "event_exchange",
                    RouteKey = "paysuccess",
                    QueueName = "1_completeorder",
                }
            }
        };
        public EventConfig GetByEventId(int eventId)
        {
            if (_eventConfigs.TryGetValue(eventId, out var config))
                return config;
            return null;
        }

        public EventConfig GetByConfigId(string configId)
        {
            if (_queueConfigs.TryGetValue(configId, out var config))
                return config;
            return null;
        }
    }
}
