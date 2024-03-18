using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T:class 
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter=null, string? includeProperties = null);
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null,bool tracked=false);
        Task<bool> AddAsync(T entity);
        //Task<bool> UpdateAsync(T entity);
        Task<bool> RemoveAsync(T entity);
        Task <bool> RemoveRangeAsync(IEnumerable<T> entity);
        Task<bool> SaveAsync();
        IQueryable<T> GetQueryable();

    }
}
