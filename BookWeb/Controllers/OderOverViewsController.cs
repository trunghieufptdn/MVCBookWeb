using BookWeb.Contast;
using BookWeb.Data;
using BookWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookWeb.Controllers;

[Authorize(Roles = SD.StoreOwner_Role)]
public class OderOverViewsController : Controller
{
    private readonly ApplicationDbContext _db;
   [BindProperty]
    public OderDetailsVM OderVM { get; set; }

    public OderOverViewsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var oderHeaderList = _db.OrderHeaders.Include(u => u.User).ToList();
        return View(oderHeaderList);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        OderVM = new OderDetailsVM
        {
            OrderHeader = _db.OrderHeaders.Where(u => u.Id == id).Include(u => u.User).FirstOrDefault(),
            OderDetails = _db.OrderDetails.Where(o => o.OrderHeaderId == id).Include(u => u.Product)
        };
        return View(OderVM);
    }

}