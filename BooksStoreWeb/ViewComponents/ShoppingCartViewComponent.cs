using Microsoft.AspNetCore.Mvc;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Utility;
using System.Security.Claims;

namespace BooksStoreWeb.ViewComponents
{
    public class ShoppingCartViewComponent:ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity as ClaimsIdentity != null)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
                    {
                        var SC = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.StoreUserId == claim.Value);
                        if (SC != null)
                            HttpContext.Session.SetInt32(SD.SessionCart, SC.Count());
                    }
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
                else
                {
                    HttpContext.Session.Clear();
                    return View(0);
                }
            }
            else
            {
                return View(0);
            }
        }

    }
}
