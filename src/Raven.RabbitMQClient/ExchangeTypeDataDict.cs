using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.MessageQueue.WithRabbitMQ
{
    public static class ExchangeTypeDataDict
    {
        /// <summary>
        /// exchangeType对应数值表
        /// </summary>
        public readonly static IDictionary<ExchangeType, string> ExchangeTypeDict = new Dictionary<ExchangeType, string>()
        {
            {ExchangeType.Default, string.Empty},
            {ExchangeType.Fanout, RabbitMQ.Client.ExchangeType.Fanout},
            {ExchangeType.Direct, RabbitMQ.Client.ExchangeType.Direct},
            {ExchangeType.Topic, RabbitMQ.Client.ExchangeType.Topic},
            {ExchangeType.Headers, RabbitMQ.Client.ExchangeType.Headers}
        };
    }
}
