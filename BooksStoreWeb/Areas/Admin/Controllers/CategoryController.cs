using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using StoreWeb.Utility;

namespace BooksStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            List<Category> categories = (List<Category>)await _unitOfWork.Category.GetAllAsync();
            return View(categories);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (category.Name == category.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name");
            }
            if (ModelState.IsValid)
            {
                await _unitOfWork.Category.AddAsync(category);
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index");
            }
            return View();
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = await _unitOfWork.Category.GetByIdAsync((int)id) as Category;
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Category.UpdateAsync(category);
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("Index");
            }
            return View();
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = await _unitOfWork.Category.GetByIdAsync((int)id) as Category;
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletPOST(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = await _unitOfWork.Category.GetByIdAsync((int)id) as Category;
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            await _unitOfWork.Category.RemoveAsync(categoryFromDb);
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
