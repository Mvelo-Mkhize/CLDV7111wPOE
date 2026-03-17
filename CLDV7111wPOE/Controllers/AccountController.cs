using CLDV7111wPOE.Data;
using CLDV7111wPOE.Models;
using CLDV7111wPOE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLDV7111wPOE.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountController> logger,
            IUserService userService,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _userService = userService;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _logger.LogInformation($"Login attempt - Role: {model.Role}, Username/Email: {model.UsernameOrEmail}");

            bool isValid = false;
            int userId = 0;
            string userName = "";

            switch (model.Role)
            {
                case "Customer":
                    var customer = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.UsernameOrEmail || u.Email == model.UsernameOrEmail);

                    if (customer != null)
                    {
                        isValid = _userService.ValidatePassword(model.Password, customer.PasswordHash);
                        if (isValid)
                        {
                            userId = customer.UserId;
                            userName = customer.Username;
                        }
                    }
                    break;

                case "Employee":
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == model.UsernameOrEmail || e.Email == model.UsernameOrEmail);

                    if (employee != null)
                    {
                        isValid = _userService.ValidatePassword(model.Password, employee.PasswordHash);
                        if (isValid)
                        {
                            userId = employee.EmployeeId;
                            userName = employee.Username;
                        }
                    }
                    break;

                case "Admin":
                    var admin = await _context.Administrators
                        .FirstOrDefaultAsync(a => a.Username == model.UsernameOrEmail || a.Email == model.UsernameOrEmail);

                    if (admin != null)
                    {
                        isValid = _userService.ValidatePassword(model.Password, admin.PasswordHash);
                        if (isValid)
                        {
                            userId = admin.AdminId;
                            userName = admin.Username;
                        }
                    }
                    break;
            }

            if (isValid)
            {
                _httpContextAccessor.HttpContext.Session.SetInt32("UserId", userId);
                _httpContextAccessor.HttpContext.Session.SetString("UserRole", model.Role);
                _httpContextAccessor.HttpContext.Session.SetString("UserName", userName);

                _logger.LogInformation($"Successful login for {model.Role}: {userName}");

                if (model.Role == "Admin")
                    return RedirectToAction("ManageCustomers", "Admin");
                else if (model.Role == "Employee")
                    return RedirectToAction("Index", "BookingRequests");
                else
                    return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username/email or password.");
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

            if (existingUser != null)
            {
                if (existingUser.Username == model.Username)
                    ModelState.AddModelError("Username", "Username already taken.");
                if (existingUser.Email == model.Email)
                    ModelState.AddModelError("Email", "Email already registered.");

                return View(model);
            }

            var user = await _userService.CreateUserAsync(
                model.Username,
                model.Email,
                model.Password,
                model.FirstName,
                model.LastName
            );

            _httpContextAccessor.HttpContext.Session.SetInt32("UserId", user.UserId);
            _httpContextAccessor.HttpContext.Session.SetString("UserRole", "Customer");
            _httpContextAccessor.HttpContext.Session.SetString("UserName", user.Username);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            _httpContextAccessor.HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> HashExistingPasswords()
        {
            int count = 0;

            var employees = await _context.Employees.ToListAsync();
            foreach (var emp in employees)
            {
                if (emp.PasswordHash != null && emp.PasswordHash.Length < 64)
                {
                    emp.PasswordHash = _passwordHasher.HashPassword(emp.PasswordHash);
                    count++;
                }
            }

            var admins = await _context.Administrators.ToListAsync();
            foreach (var admin in admins)
            {
                if (admin.PasswordHash != null && admin.PasswordHash.Length < 64)
                {
                    admin.PasswordHash = _passwordHasher.HashPassword(admin.PasswordHash);
                    count++;
                }
            }

            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                if (user.PasswordHash != null && user.PasswordHash.Length < 64)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user.PasswordHash);
                    count++;
                }
            }

            await _context.SaveChangesAsync();

            return Content($"Fixed {count} passwords. All passwords are now properly hashed.\n\n" +
                          $"You can now login with your existing passwords.\n" +
                          $"The hashed values are now:\n" +
                          $"'password123' → {_passwordHasher.HashPassword("password123")}\n" +
                          $"'admin123' → {_passwordHasher.HashPassword("admin123")}");
        }
    }
}