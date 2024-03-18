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
    public class StoreUserRepository : Repository<StoreUser>, IStoreUserRepository
    {
        private readonly StoreDbContext _db;
        public StoreUserRepository(StoreDbContext db):base(db) 
        {
            _db = db;
        }

    }
}
