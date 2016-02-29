using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.MessageQueue
{
    public interface ILoger
    {
        void LogError(string message, Exception ex);
    }
}
