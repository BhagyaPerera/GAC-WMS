using Ardalis.Specification;
using Core.Entities.CustomerAggregate;
using Core.Entities.SalesOrderAggregate;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Core.Specification
{
    public class GetCustomerByCustomerNoSpec : Specification<Customer>, IAggregateRoot
    {
        public GetCustomerByCustomerNoSpec(string customerNo)
        {
            Query.Where(a => a.CustomerNo == customerNo);
        }

    }
}
