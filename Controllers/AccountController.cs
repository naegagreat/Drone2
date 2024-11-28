using Drone2.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Drone2.Models
{

    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser>? signInManager;
        private readonly UserManager<AppUser>? userManager;


        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }
         [HttpPost]
    public IActionResult GuestLogin()
    {
        
        HttpContext.Session.SetString("UserRole", "Guest");
        return RedirectToAction("Index", "Booth"); // พาผู้ใช้ไปหน้า Details
    }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    // เพิ่ม Role ใน Claims (ผ่าน Identity)
                    var user = await userManager.FindByNameAsync(model.Username);
                    var roles = await userManager.GetRolesAsync(user);

                    if (roles.Contains("Admin"))
                    {
                        HttpContext.Session.SetString("UserRole", "Admin");
                    }
                    else
                    {
                        HttpContext.Session.SetString("UserRole", "User");
                    }

                    return RedirectToAction("Index", "Booth");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new()
                {
                    Name = model.Name,
                    UserName = model.Email,
                    Email = model.Email,
                    Address = model.Address
                };
                var result = await userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {

                    await signInManager.SignInAsync(user, false);
                    return RedirectToAction("Login", "Account");


                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");

        }

    }
}
