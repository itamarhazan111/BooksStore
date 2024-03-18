using Microsoft.EntityFrameworkCore;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StoreDbContext _db;
        public ICategoryRepository Category { get; private set; }

        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }
        public IStoreUserRepository StoreUser { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IOrderDetailRepository OrderDetail { get; private set; }

        public UnitOfWork(StoreDbContext db)
        {
            _db = db;
            Category=new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            ShoppingCart = new ShoppingCartRepository(_db);
            StoreUser = new StoreUserRepository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
        }



        //public async Task<bool> SaveAsync() => await _db.SaveChangesAsync() > 0;//todo
    }
}
