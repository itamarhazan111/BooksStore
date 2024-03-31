using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>,IProductRepository
    {
        private readonly StoreDbContext _db;
        public ProductRepository(StoreDbContext db):base(db) 
        {
            _db = db;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var objFromDb=await _db.Products.FirstOrDefaultAsync(x => x.Id == product.Id);
            if (objFromDb != null)
            {
                objFromDb.Title = product.Title;
                objFromDb.Description = product.Description;
                objFromDb.ISBN = product.ISBN;
                objFromDb.ListPrice = product.ListPrice;
                objFromDb.Price = product.Price;
                objFromDb.Price50= product.Price50;
                objFromDb.Price100= product.Price100;
                objFromDb.CategoryId = product.CategoryId;
                objFromDb.Author = product.Author;
                objFromDb.ProductImages = product.ProductImages;
                //if(product.ImageUrl != null)
                //{
                //    objFromDb.ImageUrl = product.ImageUrl;
                //}


            }
            //_db.Products.Update(product);
            return await SaveAsync();
        }
    }
}
