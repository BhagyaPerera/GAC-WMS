using Ardalis.Specification;
using Core.Entities.SalesOrderAggregate;
using SharedKernal.Interfaces;

namespace Core.Specification
{
    public class GetPartnerOrderByOrderNoSpec : Specification<SalesOrder>, IAggregateRoot
    {
        public GetPartnerOrderByOrderNoSpec(string orderNo)
        {
            Query.Include(a => a.SalesOrderLines)
                .Where(a => a.OrderNo == orderNo);
        }

    }
}
