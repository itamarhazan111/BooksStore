using StoreWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
       Task<bool> UpdateAsync(Product product);

    }
}
