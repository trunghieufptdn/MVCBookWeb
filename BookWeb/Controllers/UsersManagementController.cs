using System.Security.Claims;
using BookWeb.Contast;
using BookWeb.Data;
using BookWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Index = Microsoft.EntityFrameworkCore.Metadata.Internal.Index;

namespace BookWeb.Controllers;

[Authorize(Roles = SD.Admin_Role)]
public class UsersManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersManagementController(ApplicationDbContext db, UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _db = db;
        _roleManager = roleManager;
    }

    //Get
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // lấy id của người đăng nhập hiện tại
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        //dùng để tránh trường hợp xóa nhầm role của mình
        var userList = _db.Users.Where(u => u.Id != claims.Value);

        foreach (var user in userList)
        {
            var roleTemp = await _userManager.GetRolesAsync(user);
            user.Role = roleTemp.FirstOrDefault();
        }

        return View(userList.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();

        }

        await _userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LockUnLock(string id)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        var userNeedToLock = _db.Users.Where(u => u.Id == id).First();
        if (userNeedToLock.Id == claims.Value)
        {
            //hien ra loi ban dang khoa tai khoan cua chinh minh
        }

        if (userNeedToLock.LockoutEnd != null && userNeedToLock.LockoutEnd > DateTime.Now)
        {
            userNeedToLock.LockoutEnd = DateTime.Now;
        }
        else
        {
            userNeedToLock.LockoutEnd = DateTime.Now.AddYears(1);
        }

        _db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Update(String id)
    {
        if (id != null)
        {
            //khởi tạo vm
            UserVM userVm = new UserVM();
            //get user data
            var user = _db.Users.Find(id);
            // gắn dữ liệu cho obj user in UserVm
            userVm.User = user;
            //lấy role hiện tại của user được chọn
            var roleTemp = await _userManager.GetRolesAsync(user);
            // gắn role hiện taại vào biến role
            userVm.Rolelist = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem()
            {
                Text = i,
                Value = i

            });
            return View(userVm);
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Update(UserVM userVm)
    {
        //validate dữ liệu
        if (userVm.User.Id != String.Empty
            && userVm.User.PhoneNumber != null
            && userVm.User.FullName != string.Empty
            && userVm.User.Role != String.Empty)
        {
            // kiểm tra user cần update
            var user = _db.Users.Find(userVm.User.Id);
            //update dữ liệu cho obj user
            user.FullName = userVm.User.FullName;
            user.PhoneNumber = userVm.User.PhoneNumber;
            user.Address = userVm.User.Address;

            // timf ra role hieenj tai cua user;
            var oldRole = await _userManager.GetRolesAsync(user);
            //remove user ra khoi role cu
            await _userManager.RemoveFromRoleAsync(user, oldRole.First());
            // add user vao role moi
            await _userManager.AddToRoleAsync(user, userVm.Role);

            //update va luu thong tin
            _db.Users.Update(user);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        //truong hop mot trong cac trường trên bị lo
        userVm.Rolelist = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem()
        {
            Text = i,
            Value = i
        });
        return View(userVm);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string id)
    {
        var user = _db.Users.Find(id);

        if (user == null)
        {
            return View();
        }

        ConfirmEmailVM confirmEmailVm = new ConfirmEmailVM()
        {
            Email = user.Email
        };
        return View(confirmEmailVm);

    }
  

    [HttpPost]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailVM confirmEmailVm)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(confirmEmailVm.Email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction("ResetPassword", "UsersManagement",
                new { token = token, email = user.Email });
        }
        

        return View(confirmEmailVm);

    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token, string email)
    {
        if (token == null || email == null)
        {
            ModelState.AddModelError("", "Invalid password reset token");
        }

        ResetPasswordViewModel resetPasswordViewModel = new ResetPasswordViewModel()
        {
            Email = email,
            Token = token
        };
        return View(resetPasswordViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
    {
        if (ModelState.IsValid)

        {
            var user = await _userManager.FindByEmailAsync(resetPasswordViewModel.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, resetPasswordViewModel.Token,
                    resetPasswordViewModel.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        return View(resetPasswordViewModel);
    }
}