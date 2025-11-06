using Ardalis.Specification;
using Core.Entities.ProductAggregate;
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
    public class GetProductByProductCodeSpec : Specification<Product>, IAggregateRoot
    {
        public GetProductByProductCodeSpec(string productCode)
        {
            Query.Where(a => a.ProductCode == productCode);
        }

    }
}
