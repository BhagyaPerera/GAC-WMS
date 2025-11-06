using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernal
{
    public class BaseIntegrationEvent : IApplicationEvent
    {
        public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;
        public string EventType { set; get; }

        public BaseIntegrationEvent(string eventName)
        {
            EventType = eventName;
        }

        public BaseIntegrationEvent()
        { } // Json Serialization Need this

        public string GetIntegrationName()
        {
            return EventType;
        }
    }
}
