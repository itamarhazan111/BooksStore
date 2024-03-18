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
    public class CompanyController : Controller
    {        
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            List<Company> Companys = (List<Company>)await _unitOfWork.Company.GetAllAsync();
            return View(Companys);
        }
        public async Task<IActionResult> Upsert(int? id)
        {

            if (id == null || id == 0)
            {
				return View(new Company());
            }
            else
            {
				Company? Company =await _unitOfWork.Company.GetAsync(u => u.Id == id);
			    return View(Company);
			}

        }
        [HttpPost]
        public async Task<IActionResult> Upsert(Company CompanyObj)
        {
            if (ModelState.IsValid)
            {
                if (CompanyObj.Id == 0)
                {
					await _unitOfWork.Company.AddAsync(CompanyObj);
					TempData["success"] = "Company created successfully";
				}
                else
                {
					await _unitOfWork.Company.UpdateAsync(CompanyObj);
					TempData["success"] = "Company updated successfully";
				}



                return RedirectToAction("Index");
            }
            else
            {
                return View(CompanyObj);
            }
            
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletPOST(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Company? companyFromDb = await _unitOfWork.Company.GetByIdAsync((int)id);
            if (companyFromDb == null)
            {
                return NotFound();
            }
            await _unitOfWork.Company.RemoveAsync(companyFromDb);
            TempData["success"] = "Company deleted successfully";
            return RedirectToAction("Index");
        }
        #region API CALLS
        public async Task<IActionResult> GetAll()
        {
            List<Company> Companys = (List<Company>)await _unitOfWork.Company.GetAllAsync();
            return Json(new { data = Companys });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var CompanyToBeDeleted=await _unitOfWork.Company.GetAsync(x=>x.Id == id);
            if(CompanyToBeDeleted == null)
            {
                return Json(new { success = false, massage = "error while deleting" });
            }
            await _unitOfWork.Company.RemoveAsync(CompanyToBeDeleted);
            return Json(new { success = true, message = "deleted successful" });
        }
        #endregion
    }
}

