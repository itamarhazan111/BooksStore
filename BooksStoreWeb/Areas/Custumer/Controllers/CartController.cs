using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using StoreWeb.DataAccess.Repository;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using StoreWeb.Models.ViewModels;
using StoreWeb.Utility;
using Stripe.Checkout;
using System.Security.Claims;

namespace BooksStoreWeb.Areas.Custumer.Controllers
{
    [Area("Custumer")]
    [Authorize]
    public class CartController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        public readonly IEmailSender _emailSender;

        [BindProperty]
        public ShoppingCartVM? ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                ShoppingCartVM = new()
                {
                    ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(x => x.StoreUserId == userId, includeProperties: "Product"),
                    OrderHeader = new()
                };
                foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
                }
            }
               
          return View(ShoppingCartVM);
        }
        public async Task<IActionResult> Summary()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                ShoppingCartVM = new()
                {
                    ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(x => x.StoreUserId == userId, includeProperties: "Product"),
                    OrderHeader = new()
                };
                if (ShoppingCartVM.OrderHeader.StoreUser != null)
                {
                    ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.StoreUser.PostalCode;
                }
                foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
                }
            }

            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST()
        {

            var userId = GetUserId();
            if (userId != null)
            {

                    if (ShoppingCartVM != null)
                    {

					    ShoppingCartVM.ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(x => x.StoreUserId == userId, includeProperties: "Product");
                        ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
                        ShoppingCartVM.OrderHeader.StoreUserId = userId;
                        StoreUser? storeUser = await _unitOfWork.StoreUser.GetAsync(x => x.Id == userId);

                        foreach (ShoppingCart cart in ShoppingCartVM.ShoppingCartList)
                        {
                            cart.Price = GetPriceBasedOnQuantity(cart);
                            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
                        }
                        if (storeUser != null)
                        {
                            if (storeUser.CompanyId.GetValueOrDefault() == 0)
                            {
                                //REGULAR CUSTUMER
                                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                            }
                            else
                            {
                                //COMPANY CUSTUMER
                                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                            }
                        }
					if (!ModelState.IsValid)
					{
						var shoppingCart = ShoppingCartVM;
						return View(shoppingCart);
					}
					await _unitOfWork.OrderHeader.AddAsync(ShoppingCartVM.OrderHeader);
                        foreach (var cart in ShoppingCartVM.ShoppingCartList)
                        {
                            OrderDetail orderDetail = new()
                            {
                                ProductId = cart.ProductId,
                                OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                                Price = cart.Price,
                                Count = cart.Count,
                            };
                            await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                        }
                        if (storeUser != null)
                        {
                            if (storeUser.CompanyId.GetValueOrDefault() == 0)
                            {
                                var DOMAIN =Request.Scheme+"://"+Request.Host.Value+"/";
                                var options = new SessionCreateOptions
                                {
                                    SuccessUrl = DOMAIN + $"custumer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                                    CancelUrl = DOMAIN + "custumer/cart/index",
                                    LineItems = new List<SessionLineItemOptions>(),
                                    Mode = "payment",
                                };
                                foreach (var item in ShoppingCartVM.ShoppingCartList)
                                {
                                    var sessionLineItem = new SessionLineItemOptions
                                    {
                                        PriceData = new SessionLineItemPriceDataOptions
                                        {
                                            UnitAmount = (long)(item.Price * 100),
                                            Currency = "ILS",
                                            ProductData = new SessionLineItemPriceDataProductDataOptions
                                            {
                                                Name = item.Product?.Title,

                                            }
                                        },
                                        Quantity = item.Count
                                    };
                                    options.LineItems.Add(sessionLineItem);
                                }
                                var service = new SessionService();
                                Session session = service.Create(options);
                                await _unitOfWork.OrderHeader.UpdateStripePaymentIdAsync(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                                Response.Headers.Add("Location", session.Url);
                                return new StatusCodeResult(303);
							}
                        }

					}
				
				}
			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM?.OrderHeader.Id });

		}
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            OrderHeader? orderHeader=await _unitOfWork.OrderHeader.GetAsync(u=>u.Id== id,includeProperties:"StoreUser");
            if (orderHeader != null)
            {
                if(orderHeader.PaymentStatus!=SD.PaymentStatusDelayedPayment)
                {
                    //custumer
                    var service = new SessionService();
                    Session session = await service.GetAsync(orderHeader.SessionId);
                    if(session != null)
                    {
                        if (session.PaymentStatus.ToLower() == "paid")
                        {
                            await _unitOfWork.OrderHeader.UpdateStripePaymentIdAsync(id,session.Id,session.PaymentIntentId);
                            await _unitOfWork.OrderHeader.UpdateStatusAsync(id, SD.StatusApproved, SD.PaymentStatusApproved);
                        }
                        HttpContext.Session.Clear();
                    }
                }
                if (orderHeader.StoreUser != null && orderHeader.StoreUser.Email != null)
                {
                    await _emailSender.SendEmailAsync(orderHeader.StoreUser.Email, "new order-book store", $"<p>NEW ORDER CREATED - {orderHeader.Id}</p>");
                }
                List<ShoppingCart> shoppingCarts =(List<ShoppingCart>)await _unitOfWork.ShoppingCart.GetAllAsync(u=>u.StoreUserId==orderHeader.StoreUserId);
                await _unitOfWork.ShoppingCart.RemoveRangeAsync(shoppingCarts);
			}
            return View(id);
        }
		public async Task<IActionResult> Plus(int cartId)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var cartFromDb = await _unitOfWork.ShoppingCart.GetAsync(x => x.Id == cartId&& x.StoreUserId==userId);
                    if (cartFromDb != null)
                    {
                        cartFromDb.Count += 1;
                        await _unitOfWork.ShoppingCart.UpdateAsync(cartFromDb);
                    }
            }
            return RedirectToAction(nameof(Index));

        }
        public async Task<IActionResult> Minus(int cartId)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var cartFromDb = await _unitOfWork.ShoppingCart.GetAsync(x => x.Id == cartId,tracked:true);
                if (cartFromDb != null)
                {
                    if (cartFromDb.Count <= 1)
                    {
                        var SC = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.StoreUserId == cartFromDb.StoreUserId);
                        if (SC != null)
                            HttpContext.Session.SetInt32(SD.SessionCart, SC.Count() - 1);
                        await _unitOfWork.ShoppingCart.RemoveAsync(cartFromDb);
                    }
                    else
                    {
                        cartFromDb.Count -= 1;
                        await _unitOfWork.ShoppingCart.UpdateAsync(cartFromDb);
                    }
                }
            }
            return RedirectToAction(nameof(Index));

        }
        public async Task<IActionResult> Remove(int cartId)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var cartFromDb = await _unitOfWork.ShoppingCart.GetAsync(x => x.Id == cartId,tracked:true);
                if (cartFromDb != null)
                {
                    var SC = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.StoreUserId == cartFromDb.StoreUserId);
                    if (SC != null)
                        HttpContext.Session.SetInt32(SD.SessionCart, SC.Count() - 1);
                    await _unitOfWork.ShoppingCart.RemoveAsync(cartFromDb);

                }
            }
            return RedirectToAction(nameof(Index));

        }
        private string? GetUserId()
        {
            if (User.Identity as ClaimsIdentity != null)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    var userId = claim.Value;
                    return userId;
                }
            }
            return null;
        }
        private double GetPriceBasedOnQuantity(ShoppingCart cart)
        {
            if (cart.Product != null) {
                if (cart.Count <= 50)
                {
                    return cart.Product.Price;
                } else if (cart.Count <= 100)
                {
                    return cart.Product.Price50;
                }
                else
                {
                    return cart.Product.Price100;
                }
            }
            return 0;

        }
    }
}
