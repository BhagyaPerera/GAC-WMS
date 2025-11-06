using Ardalis.Specification;
using Core.Entities.PurchaseOrderAggregate;
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
    public class GetPurchaseOrderbyOrderNoSpec : Specification<PurchaseOrder>, IAggregateRoot
    {
        public GetPurchaseOrderbyOrderNoSpec(string orderNo)
        {
            Query.Include(a => a.PurchaseOrderLines)
                .Where(a => a.OrderNo == orderNo);
        }

    }
}
