using StoreWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using StoreWeb.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using StoreWeb.Utility;
using Microsoft.AspNetCore.Http;

namespace BooksStoreWeb.Areas.Custumer.Controllers
{
    [Area("Custumer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;    
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity as ClaimsIdentity != null)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    var SC = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.StoreUserId == claim.Value);
                    if (SC != null)
                        HttpContext.Session.SetInt32(SD.SessionCart, SC.Count());
                }
            }
            IEnumerable<Product> products =await _unitOfWork.Product.GetAllAsync(includeProperties: "Category,ProductImages");
            return View(products);
        }
        public async Task<IActionResult> Details(int?id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            ShoppingCart shoppingCart = new()
            {
                Product = await _unitOfWork.Product.GetAsync(u => u.Id == id, includeProperties: "Category,ProductImages"),
                Count=1,
                ProductId=(int)id

            };
            return View(shoppingCart);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            if (User.Identity as ClaimsIdentity != null)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    var userId = claim.Value;
                    shoppingCart.StoreUserId = userId;
                    ShoppingCart? cartFromDb=await _unitOfWork.ShoppingCart.GetAsync(u=>u.StoreUserId == userId&&u.ProductId==shoppingCart.ProductId);
                    if(cartFromDb != null)
                    {
                        cartFromDb.Count += shoppingCart.Count;
                        await _unitOfWork.ShoppingCart.UpdateAsync(cartFromDb);
                    }
                    else
                    {
                        await _unitOfWork.ShoppingCart.AddAsync(shoppingCart);
                        var SC=await _unitOfWork.ShoppingCart.GetAllAsync(u => u.StoreUserId == userId);
                        if(SC!= null) 
                            HttpContext.Session.SetInt32(SD.SessionCart, SC.Count());
                    }
                    TempData["success"] = "cart update seccessfuly";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}