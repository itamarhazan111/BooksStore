using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using StoreWeb.DataAccess.Repository.IRepository;
using StoreWeb.Models;
using StoreWeb.Models.ViewModels;
using StoreWeb.Utility;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Security.Claims;

namespace BooksStoreWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
    public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM? OrderVM { get; set; }


        public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Details(int orderId)
        {
            OrderVM OrderVM = new()
            {
                OrderHeader = await _unitOfWork.OrderHeader.GetAsync(x => x.Id == orderId, includeProperties: "StoreUser"),
                OrderDetails = await _unitOfWork.OrderDetail.GetAllAsync(x => x.OrderHeaderId == orderId, includeProperties: "Product")
            };
           return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public async Task<IActionResult> UpdateOrderDetails() 
        {
            if (OrderVM?.OrderHeader != null)
            {
                OrderHeader? orderHeaderFromDb = await _unitOfWork.OrderHeader.GetByIdAsync(OrderVM.OrderHeader.Id);
                if (orderHeaderFromDb != null)
                {
                    orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
                    orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
                    orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
                    orderHeaderFromDb.City = OrderVM.OrderHeader.City;
                    orderHeaderFromDb.State = OrderVM.OrderHeader.State;
                    orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
                    if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
                    {
                        orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
                    }
                    if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
                    {
                        orderHeaderFromDb.Carrier = OrderVM.OrderHeader.TrackingNumber;
                    }
                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeaderFromDb);
                    TempData["Success"] = "Order Details Updated Successfully.";

                    return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
                }
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> StartProcessing()
        {
            if (OrderVM?.OrderHeader != null) {
                await _unitOfWork.OrderHeader.UpdateStatusAsync(OrderVM.OrderHeader.Id, SD.StatusInProcess);
                TempData["Success"] = "Order Details Updated Successfully.";
                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> ShipOrder()
        {
            if (OrderVM?.OrderHeader != null)
            {
                var orderHeader = await _unitOfWork.OrderHeader.GetByIdAsync(OrderVM.OrderHeader.Id);
                if (orderHeader != null)
                {
                    orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
                    orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
                    orderHeader.OrderStatus = SD.StatusShipped;
                    orderHeader.ShippingDate = DateTime.Now;
                    if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
                    {
                        orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                    }
                    await _unitOfWork.OrderHeader.UpdateAsync(orderHeader);

                    TempData["Success"] = "Order Shipped Successfully.";
                    return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
                }
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> CancelOrder()
        {
            if (OrderVM?.OrderHeader != null)
            {
                var orderHeader = await _unitOfWork.OrderHeader.GetByIdAsync(OrderVM.OrderHeader.Id);
                if (orderHeader != null)
                {
                    if (orderHeader != null && orderHeader.PaymentStatus == SD.PaymentStatusApproved)
                    {
                        var options = new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeader.PaymentIntentId
                        };

                        var service = new RefundService();
                        Refund refund = service.Create(options);

                        await _unitOfWork.OrderHeader.UpdateStatusAsync(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
                    }
                    else
                    {
                        await _unitOfWork.OrderHeader.UpdateStatusAsync(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
                    }

                    TempData["Success"] = "Order Cancelled Successfully.";
                    return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
                }
               
            }
            return RedirectToAction(nameof(Index));
        }
        [ActionName("Details")]
        [HttpPost]
        public async Task<IActionResult> Details_PAY_NOW()
        {
            if (OrderVM != null && OrderVM.OrderHeader != null)
            {
                OrderVM.OrderHeader = await _unitOfWork.OrderHeader
                    .GetAsync(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
                OrderVM.OrderDetails = await _unitOfWork.OrderDetail
                    .GetAllAsync(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

                //stripe logic
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader?.Id}",
                    CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader?.Id}",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in OrderVM.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product?.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }


                var service = new SessionService();
                Session session = service.Create(options);
                await _unitOfWork.OrderHeader.UpdateStripePaymentIdAsync(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader? orderHeader = await _unitOfWork.OrderHeader.GetAsync(u => u.Id == orderHeaderId);
            if (orderHeader?.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    await _unitOfWork.OrderHeader.UpdateStripePaymentIdAsync(orderHeaderId, session.Id, session.PaymentIntentId);
                    await _unitOfWork.OrderHeader.UpdateStatusAsync(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);

                }


            }


            return View(orderHeaderId);
        }





        #region API CALLS
        [HttpGet]
        public async Task<IActionResult> GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = (List<OrderHeader>)await _unitOfWork.OrderHeader.GetAllAsync(includeProperties: "StoreUser");
            }
            else
            {
                if (User.Identity as ClaimsIdentity != null)
                {
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    objOrderHeaders = (List<OrderHeader>)await _unitOfWork.OrderHeader.GetAllAsync(x => x.StoreUserId == userId, includeProperties: "StoreUser");
                }
                else
                {
                    objOrderHeaders=new List<OrderHeader>();
                }
            }
            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }


            return Json(new { data = objOrderHeaders });
        }
        #endregion
    }
}
