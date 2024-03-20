using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using StoreWeb.DataAccess.Data;
using StoreWeb.DataAccess.Repository;
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
    public class UserController : Controller { 
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
 
        private readonly IUnitOfWork _unitOfWork;
        public UserController(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }
        public async Task<IActionResult> RoleManagment(string userId)
        {
            var companyList = await _unitOfWork.Company.GetAllAsync();

            RoleManagmentVM RoleVM = new RoleManagmentVM()
            {
                StoreUser = await _unitOfWork.StoreUser.GetAsync(u => u.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = companyList.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };
            if (RoleVM.StoreUser != null)
            {
                var user =await _unitOfWork.StoreUser.GetAsync(u => u.Id == userId);
                if (user!=null)
                RoleVM.StoreUser.Role = _userManager.GetRolesAsync(user)
                        .GetAwaiter().GetResult().FirstOrDefault();
            }
            return View(RoleVM);
        }
        [HttpPost]
        public async Task<IActionResult> RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            if (roleManagmentVM.StoreUser == null)
            {
                // Handle case where StoreUser is missing (e.g., validation error message)
                return BadRequest("StoreUser information is missing.");
            }

            try
            {
                // Get existing user details
                var existingUser = await _unitOfWork.StoreUser.GetAsync(u => u.Id == roleManagmentVM.StoreUser.Id);

                if (existingUser == null)
                {
                    // Handle case where user doesn't exist (e.g., log error, display user-friendly message)
                    return NotFound("User not found.");
                }

                // Update role if necessary
                if (existingUser.Role != roleManagmentVM.StoreUser.Role)
                {
                    // Update role and company ID (if applicable)
                    existingUser.Role = roleManagmentVM.StoreUser.Role;
                    if (roleManagmentVM.StoreUser.Role == SD.Role_Company)
                    {
                        existingUser.CompanyId = roleManagmentVM.StoreUser.CompanyId;
                    }
                    else if (existingUser.Role == SD.Role_Company)
                    {
                        existingUser.CompanyId = null;
                    }

                    // Update user in database
                    await _unitOfWork.StoreUser.UpdateAsync(existingUser);

                    // Update user roles using UserManager
                    await _userManager.RemoveFromRoleAsync(existingUser, existingUser.Role); // Assuming existingUser.Role is populated correctly
                    await _userManager.AddToRoleAsync(existingUser, roleManagmentVM.StoreUser.Role);
                }
                else // Update company ID if roles haven't changed (special case)
                {
                    if (existingUser.Role == SD.Role_Company && existingUser.CompanyId != roleManagmentVM.StoreUser.CompanyId)
                    {
                        existingUser.CompanyId = roleManagmentVM.StoreUser.CompanyId;
                        await _unitOfWork.StoreUser.UpdateAsync(existingUser);
                    }
                }

                // Success (redirect to appropriate action, e.g., Index)
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle exceptions gracefully (log, display error message)
                return StatusCode(500, ex.Message);
            }
        }

        #region API CALLS
        public async Task<IActionResult> GetAll()
        {
            var storeUsers =await _unitOfWork.StoreUser.GetAllAsync(includeProperties: "Company");
            foreach (StoreUser storeUser in storeUsers)
            {
                storeUser.Role = _userManager.GetRolesAsync(storeUser).GetAwaiter().GetResult().FirstOrDefault();

                if (storeUser.Company == null)
                {
                    storeUser.Company = new() { Name = "" };
                }
            }
            return Json(new { data = storeUsers });
        }
        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] string id)
        {

            var objFromDb = await _unitOfWork.StoreUser.GetAsync(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            await _unitOfWork.StoreUser.UpdateAsync(objFromDb);
            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}

