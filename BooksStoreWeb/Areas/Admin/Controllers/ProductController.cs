using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using StoreWeb.Models.ViewModels;
using StoreWeb.Utility;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace BooksStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {        
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            List<Product> products = (List<Product>)await _unitOfWork.Product.GetAllAsync(includeProperties:"Category");
            return View(products);
        }
        public async Task<IActionResult> Upsert(int? id)
        {
            List<Category> categories = (List<Category>)await _unitOfWork.Category.GetAllAsync();
            IEnumerable<SelectListItem> categoryList = categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString(),
            });
            ProductVM productVM = new ProductVM {
				CategoryList = categoryList,
				Product = new Product()  
            };
            if (id == null || id == 0)
            {
				return View(productVM);
            }
            else
            {
				Product? product =await _unitOfWork.Product.GetAsync(u => u.Id == id);
                if (product is not null)
                {
                    productVM.Product = product;
                }
			return View(productVM);
			}

        }
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM,IFormFile? formFile)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (formFile != null)
                {
                    string fileName=Guid.NewGuid().ToString()+Path.GetExtension(formFile.FileName);  
                    string productPath=Path.Combine(wwwRootPath,@"images\product");
                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldPath=Path.Combine(wwwRootPath,productVM.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath); 
                        }
                    }
                    using(var fileStream=new FileStream(Path.Combine(productPath,fileName),FileMode.Create))
                    {
                        await formFile.CopyToAsync(fileStream);
                    }
                    productVM.Product.ImageUrl = @"\images\product\" + fileName;

				}
                if (productVM.Product.Id == 0)
                {
					await _unitOfWork.Product.AddAsync(productVM.Product);
					TempData["success"] = "Product created successfully";
				}
                else
                {
					await _unitOfWork.Product.UpdateAsync(productVM.Product);
					TempData["success"] = "Product updated successfully";
				}



                return RedirectToAction("Index");
            }
            else
            {
				List<Category> categories = (List<Category>)await _unitOfWork.Category.GetAllAsync();
				IEnumerable<SelectListItem> categoryList = categories.Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.Id.ToString(),
				});
                productVM.CategoryList = categoryList;
			}
            return View(productVM);
        }
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product? productFromDb = await _unitOfWork.Product.GetByIdAsync((int)id) as Product;
        //    if (productFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(productFromDb);
        //}
        //[HttpPost]
        //public async Task<IActionResult> Edit(Product product)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _unitOfWork.Product.Update(product);
        //        TempData["success"] = "Product updated successfully";
        //        return RedirectToAction("Index");
        //    }
        //    return View();
        //}
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product? pruductFromDb = await _unitOfWork.Product.GetByIdAsync((int)id) as Product;
        //    if (pruductFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(pruductFromDb);
        //}
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletPOST(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? pruductFromDb = await _unitOfWork.Product.GetByIdAsync((int)id);
            if (pruductFromDb == null)
            {
                return NotFound();
            }
            await _unitOfWork.Product.RemoveAsync(pruductFromDb);
            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }
        #region API CALLS
        public async Task<IActionResult> GetAll()
        {
            List<Product> products = (List<Product>)await _unitOfWork.Product.GetAllAsync(includeProperties: "Category");
            return Json(new { data = products });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var productToBeDeleted=await _unitOfWork.Product.GetAsync(x=>x.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new { success = false, massage = "error while deleting" });
            }
            if (!string.IsNullOrEmpty(productToBeDeleted.ImageUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }
            await _unitOfWork.Product.RemoveAsync(productToBeDeleted);
            return Json(new { success = true, message = "deleted successful" });
        }
        #endregion
    }
}

