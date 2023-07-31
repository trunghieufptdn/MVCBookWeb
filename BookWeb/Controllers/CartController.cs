using System.Security.Claims;
using BookWeb.Contast;
using BookWeb.Data;
using BookWeb.Models;
using BookWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    
    [BindProperty] public ShoppingCartVM ShoppingCartVm { get; set; }

    public CartController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    //GET
    public IActionResult Index()
    {  // lấy ID của người đang đăng nhập hiện tại
        var claimIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            //tạo shopping vm
            // lấy dữ liệu cart của user đang đăng nhập
        ShoppingCartVm = new ShoppingCartVM()
        {
            OderHeader  = new OrderHeader(),
            ListCarts = _db.ShoppingCarts.Where(u => u.UserId == claim.Value).Include(x => x.Product)
    
        };
        //set giá trị total bằng 0
        ShoppingCartVm.OderHeader.Total = 0;
        //
        ShoppingCartVm.OderHeader.User = _db.Users.FirstOrDefault(x => x.Id == claim.Value);
        foreach (var list in ShoppingCartVm.ListCarts)
        {
            ShoppingCartVm.OderHeader.Total += (list.Price * list.Count);
            
        }
        
        return View(ShoppingCartVm);
    }

    public IActionResult Plus(int CartId)
    {
        var cart = _db.ShoppingCarts.Include(x => x.Product).FirstOrDefault(x => x.Id == CartId);
        cart.Count += 1;
        cart.Price = cart.Product.Price * cart.Count;
        _db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int CartId)
    {
        var cart = _db.ShoppingCarts.Include(x => x.Product).FirstOrDefault(x => x.Id == CartId);
        if (cart.Count == 1)
        {
            var cnt = _db.ShoppingCarts.Where(u => u.UserId == cart.UserId).ToList().Count;
            _db.ShoppingCarts.Remove(cart);
            _db.SaveChanges();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt -1);
            
        }
        else
        {
            cart.Count -= 1;
            cart.Price = cart.Product.Price * cart.Count;
            _db.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int CartId)
    {
        var cart = _db.ShoppingCarts.Include(x => x.Product).FirstOrDefault(x => x.Id == CartId);
        var cnt = _db.ShoppingCarts.Where(u => u.UserId == cart.UserId).ToList().Count;
        _db.ShoppingCarts.Remove(cart);
        _db.SaveChanges();
        HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Summary()
    {
        var claimIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm = new ShoppingCartVM()
        {
            OderHeader = new OrderHeader(),
            ListCarts = _db.ShoppingCarts.Where(u => u.UserId == claim.Value).Include(u => u.Product)
        };

        ShoppingCartVm.OderHeader.User = _db.Users.FirstOrDefault(u => u.Id == claim.Value);

        foreach (var list in ShoppingCartVm.ListCarts)
        {
            ShoppingCartVm.OderHeader.Total += (list.Product.Price * list.Count);
            
        }

        ShoppingCartVm.OderHeader.Address = ShoppingCartVm.OderHeader.User.Address;
        return View(ShoppingCartVm);
    }

    [HttpPost]
    [ActionName("Summary")]
    [ValidateAntiForgeryToken]
    public IActionResult SummaryPost()
    {
        var claimIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm.OderHeader.User = _db.Users.FirstOrDefault(u => u.Id == claim.Value);
        ShoppingCartVm.ListCarts = _db.ShoppingCarts.Where(u => u.UserId == claim.Value).Include(u => u.Product);
        ShoppingCartVm.OderHeader.UserId = claim.Value;
        ShoppingCartVm.OderHeader.Name = ShoppingCartVm.OderHeader.User.FullName;
        ShoppingCartVm.OderHeader.PhoneNumber = ShoppingCartVm.OderHeader.User.PhoneNumber;
        ShoppingCartVm.OderHeader.Address = ShoppingCartVm.OderHeader.User.Address;
        _db.OrderHeaders.Add(ShoppingCartVm.OderHeader);
        _db.SaveChanges();

        foreach (var item in ShoppingCartVm.ListCarts)
        {
            item.Price = item.Product.Price;
            // update quantity of the products
            var productDb = _db.Products.Find(item.ProductId);
            if (productDb.Quantity >= item.Count)
            {
                productDb.Quantity -= item.Count;
                
            }
            else
            {
                item.Count = productDb.Quantity;
                productDb.Quantity = 0;
            }

            _db.Products.Update(productDb);

            OrderDetails orderDetails = new OrderDetails()
            {
                ProductId = item.ProductId,
                OrderHeaderId = ShoppingCartVm.OderHeader.Id,
                Price = item.Price,
                Quantity = item.Count
            };

            ShoppingCartVm.OderHeader.Total += orderDetails.Quantity * orderDetails.Price;
            _db.OrderDetails.Add(orderDetails);

        }
        
        _db.ShoppingCarts.RemoveRange(ShoppingCartVm.ListCarts);
        _db.SaveChanges();
        HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);
        return RedirectToAction("OderConfirmation", "Cart", new { id = ShoppingCartVm.OderHeader.Id });

    }

    public IActionResult OderConfirmation(int id)
    {
        return View(id);
    }
}