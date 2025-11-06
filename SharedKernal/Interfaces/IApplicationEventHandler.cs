using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernal.Interfaces
{
    public interface IApplicationEventHandler<T> where T : IApplicationEvent
    {
        public Task<bool> Handle(T applicationEvent);
    }
}
