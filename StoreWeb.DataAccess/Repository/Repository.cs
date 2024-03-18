 using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly StoreDbContext _db;
        private readonly DbSet<T> _set;
        public Repository(StoreDbContext db)
        {
            _db = db;
            _set=_db.Set<T>();
            _db.Products.Include(x => x.Category).Include(x=>x.CategoryId);
        }
        public async Task<bool> AddAsync(T entity)
        {
            await _set.AddAsync(entity);
            return await SaveAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter=null,string? includeProperties = null) {
            var query = GetQueryable();
            if(filter != null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach(var property in includeProperties.Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries))
                {
                    query=query.Include(property);
                }
            }

			return await query.ToListAsync(); 
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null,bool tracked=false)
        {
            IQueryable<T> query;
            if (tracked)
            {
                query = GetQueryable();
            }
            else
            {
                query = GetQueryable().AsNoTracking();
            }
            query = query.Where(filter);

			if (!string.IsNullOrEmpty(includeProperties))
			{
				foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(property);
				}
			}
			return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

        public async Task<bool> RemoveAsync(T entity)
        {
            _set.Remove(entity);
            return await SaveAsync();
        }

        public async Task<bool> RemoveRangeAsync(IEnumerable<T> entity)
        {
            _set.RemoveRange(entity);
            return await SaveAsync();
        }

        public async Task<bool> SaveAsync()=>await _db.SaveChangesAsync()>0;
        public IQueryable<T> GetQueryable() => _set;
    }
}
