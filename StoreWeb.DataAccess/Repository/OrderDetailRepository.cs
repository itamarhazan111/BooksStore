using Microsoft.Identity.Client;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly StoreDbContext _db;
        public OrderDetailRepository(StoreDbContext db):base(db) 
        {
            _db = db;
        }

        public async Task<bool> UpdateAsync(OrderDetail orderDetail)
        {
            _db.OrderDetails.Update(orderDetail);
            return await SaveAsync();
        }
    }
}
