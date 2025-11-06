using Core.Events.ApplicationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IMessagePublisher
    {
        public void Publish(SalesOrderCreateEvent eventToPublish);

        public void Publish(PurchaseOrderCreateEvent eventToPublish);
    }
}
