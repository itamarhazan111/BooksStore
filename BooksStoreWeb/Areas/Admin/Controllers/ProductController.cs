using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using StoreWeb.Models.ViewModels;
using StoreWeb.Utility;
using System.Drawing;
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
				Product? product =await _unitOfWork.Product.GetAsync(u => u.Id == id,includeProperties:"ProductImages");
                if (product is not null)
                {
                    productVM.Product = product;
                }
			return View(productVM);
			}

        }
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM,List<IFormFile> formFiles)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    await _unitOfWork.Product.AddAsync(productVM.Product);
                }
                else
                {
                    await _unitOfWork.Product.UpdateAsync(productVM.Product);
                }
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (formFiles != null)
                {
                    foreach (IFormFile file in formFiles)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath=@"images\products\product-"+productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath,productPath);
                        if(!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        ProductImage image = new()
                        {
                            ImageUrl=@"\"+productPath+@"\"+fileName,
                            ProductId=productVM.Product.Id,
                        };
                        if (productVM.Product.ProductImages == null)
                        {
                            productVM.Product.ProductImages=new List<ProductImage>();
                        }
                        productVM.Product.ProductImages.Add(image);
                        
                    }
                    await _unitOfWork.Product.UpdateAsync(productVM.Product);


                }
                TempData["success"] = "Product created/updated successfully";
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
            //if (!string.IsNullOrEmpty(productToBeDeleted.ImageUrl))
            //{
            //    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
            //    if (System.IO.File.Exists(oldPath))
            //    {
            //        System.IO.File.Delete(oldPath);
            //    }
            //}

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);
            if (Directory.Exists(finalPath))
            {
                string[] filePaths=Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }
            await _unitOfWork.Product.RemoveAsync(productToBeDeleted);
            return Json(new { success = true, message = "deleted successful" });
        }
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var imageToBeDeleted= await _unitOfWork.ProductImage.GetByIdAsync(imageId);
            int productId = imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    
                }
                await _unitOfWork.ProductImage.RemoveAsync(imageToBeDeleted);
                TempData["success"] = "Product deleted successfully";
            }
            return RedirectToAction(nameof(Upsert),new {id= productId });
        }
        #endregion
    }
}

