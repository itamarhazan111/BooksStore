using StoreWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository:IRepository<OrderHeader>
    {
       Task<bool> UpdateAsync(OrderHeader orderHeader);
       Task UpdateStatusAsync(int id,string orderStatus,string? paymentStatus=null );
	   Task UpdateStripePaymentIdAsync(int id, string sessionId, string paymentIntentId);

	}
}
