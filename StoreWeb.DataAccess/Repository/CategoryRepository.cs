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
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly StoreDbContext _db;
        public CategoryRepository(StoreDbContext db):base(db) 
        {
            _db = db;
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            _db.Categories.Update(category);
            return await SaveAsync();
        }
    }
}
