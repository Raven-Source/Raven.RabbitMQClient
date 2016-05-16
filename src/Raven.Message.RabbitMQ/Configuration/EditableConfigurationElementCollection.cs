using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ.Configuration
{
    public abstract class EditableConfigurationElementCollection : ConfigurationElementCollection
    {
        public void Add(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }

        public void Remove(object key)
        {
            BaseRemove(key);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
