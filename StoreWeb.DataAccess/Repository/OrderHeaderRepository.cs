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
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly StoreDbContext _db;
        public OrderHeaderRepository(StoreDbContext db):base(db) 
        {
            _db = db;
        }

        public async Task<bool> UpdateAsync(OrderHeader orderHeader)
        {
            _db.OrderHeaders.Update(orderHeader);
            return await SaveAsync();
        }

		public async Task UpdateStatusAsync(int id, string orderStatus, string? paymentStatus = null)
		{
			var orderFromDb=_db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if (orderFromDb != null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
            await SaveAsync();
		}

		public async Task UpdateStripePaymentIdAsync(int id, string sessionId, string paymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
			if (orderFromDb != null)
			{
				if (!string.IsNullOrEmpty(sessionId))
				{
					orderFromDb.SessionId = sessionId;
				}
				if (!string.IsNullOrEmpty(paymentIntentId))
				{
					orderFromDb.PaymentIntentId = paymentIntentId;
					orderFromDb.PaymentDate = DateTime.Now;
				}
			}
			await SaveAsync();
		}
	}
}
