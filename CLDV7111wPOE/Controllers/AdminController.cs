using CLDV7111wPOE.Data;
using CLDV7111wPOE.Models;
using CLDV7111wPOE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CLDV7111wPOE.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public async Task<IActionResult> ManageCustomers()
        {
            if (!IsAdmin()) return Forbid();

            var customers = await _context.Users.ToListAsync();
            return View(customers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (!IsAdmin()) return Forbid();

            var customer = await _context.Users.FindAsync(id);
            if (customer != null)
            {
                _context.Users.Remove(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Customer {customer.Username} deleted by admin");
                TempData["SuccessMessage"] = $"Customer {customer.Username} deleted successfully!";
            }

            return RedirectToAction(nameof(ManageCustomers));
        }

        public async Task<IActionResult> ManageEmployees()
        {
            if (!IsAdmin()) return Forbid();

            var employees = await _context.Employees.ToListAsync();
            return View(employees);
        }

        public IActionResult AddEmployee()
        {
            if (!IsAdmin()) return Forbid();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(Employee employee, string password, string confirmPassword)
        {
            if (!IsAdmin()) return Forbid();

            _logger.LogInformation("=== ADD EMPLOYEE ATTEMPT ===");
            _logger.LogInformation($"FirstName: '{employee?.FirstName}'");
            _logger.LogInformation($"LastName: '{employee?.LastName}'");
            _logger.LogInformation($"Username: '{employee?.Username}'");
            _logger.LogInformation($"Email: '{employee?.Email}'");
            _logger.LogInformation($"Password length: {(password?.Length ?? 0)}");
            _logger.LogInformation($"ConfirmPassword length: {(confirmPassword?.Length ?? 0)}");
            _logger.LogInformation($"ModelState.IsValid before validation: {ModelState.IsValid}");

            if (password != confirmPassword)
            {
                _logger.LogWarning("Passwords do not match");
                ModelState.AddModelError("", "Passwords do not match.");
                return View(employee);
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                _logger.LogWarning($"Password invalid: length={(password?.Length ?? 0)}");
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                return View(employee);
            }

            _logger.LogInformation("Hashing password...");
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                employee.PasswordHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                _logger.LogInformation($"Password hashed: {employee.PasswordHash.Substring(0, 10)}...");
            }

            if (ModelState.ContainsKey("PasswordHash"))
            {
                _logger.LogInformation("Removing PasswordHash validation error");
                ModelState.Remove("PasswordHash");
            }

            _logger.LogInformation($"ModelState.IsValid after removing PasswordHash error: {ModelState.IsValid}");
            _logger.LogInformation("ModelState errors count: {count}", ModelState.ErrorCount);

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        _logger.LogWarning($"  - {key}: {error.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid, proceeding with database operations");

                try
                {
                    _logger.LogInformation("Checking for existing username...");
                    var existingUsername = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == employee.Username);

                    if (existingUsername != null)
                    {
                        _logger.LogWarning($"Username '{employee.Username}' already exists");
                        ModelState.AddModelError("Username", "Username already taken.");
                        return View(employee);
                    }
                    _logger.LogInformation("Username is available");

                    _logger.LogInformation("Checking for existing email...");
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == employee.Email);

                    if (existingEmail != null)
                    {
                        _logger.LogWarning($"Email '{employee.Email}' already exists");
                        ModelState.AddModelError("Email", "Email already registered.");
                        return View(employee);
                    }
                    _logger.LogInformation("Email is available");

                    _logger.LogInformation("Adding employee to context...");
                    _logger.LogInformation($"Employee object state before add: {_context.Entry(employee).State}");

                    _context.Employees.Add(employee);

                    _logger.LogInformation($"Employee object state after add: {_context.Entry(employee).State}");
                    _logger.LogInformation("Calling SaveChangesAsync...");

                    var saveResult = await _context.SaveChangesAsync();

                    _logger.LogInformation($"SaveChangesAsync returned: {saveResult} rows affected");

                    if (saveResult > 0)
                    {
                        _logger.LogInformation($"Employee saved successfully with ID: {employee.EmployeeId}");

                        TempData["SuccessMessage"] = $"Employee {employee.FirstName} {employee.LastName} added successfully!";
                        return RedirectToAction(nameof(ManageEmployees));
                    }
                    else
                    {
                        _logger.LogError("SaveChangesAsync returned 0 rows affected - no data was saved!");
                        ModelState.AddModelError("", "No data was saved to the database. Please try again.");
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "DATABASE UPDATE ERROR in AddEmployee");
                    _logger.LogError($"Inner exception: {dbEx.InnerException?.Message}");

                    if (dbEx.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Database error: {dbEx.InnerException.Message}");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Database error: {dbEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GENERAL EXCEPTION in AddEmployee");
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid, cannot proceed with save");
            }

            _logger.LogInformation("Returning to AddEmployee view due to errors");
            return View(employee);
        }

        public async Task<IActionResult> EditEmployee(int id)
        {
            if (!IsAdmin()) return Forbid();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, Employee employee, string newPassword)
        {
            if (!IsAdmin()) return Forbid();
            if (id != employee.EmployeeId) return NotFound();

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("", "Password must be at least 6 characters long.");
                    return View(employee);
                }

                using (var sha256 = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(newPassword);
                    var hash = sha256.ComputeHash(bytes);
                    employee.PasswordHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                _logger.LogInformation($"Password updated for employee {employee.Username}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUsername = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == employee.Username && e.EmployeeId != id);

                    if (existingUsername != null)
                    {
                        ModelState.AddModelError("Username", "Username already taken by another employee.");
                        return View(employee);
                    }

                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == employee.Email && e.EmployeeId != id);

                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email already registered by another employee.");
                        return View(employee);
                    }

                    var existing = await _context.Employees.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Username = employee.Username;
                    existing.Email = employee.Email;
                    existing.FirstName = employee.FirstName ?? "";
                    existing.LastName = employee.LastName ?? "";

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        existing.PasswordHash = employee.PasswordHash;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Employee {employee.Username} updated successfully");

                    TempData["SuccessMessage"] = "Employee updated successfully!";
                    return RedirectToAction(nameof(ManageEmployees));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating employee");
                    ModelState.AddModelError("", "An error occurred while updating the employee.");
                }
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (!IsAdmin()) return Forbid();

            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Employee {employee.Username} deleted by admin");
                TempData["SuccessMessage"] = "Employee deleted successfully!";
            }

            return RedirectToAction(nameof(ManageEmployees));
        }

        [HttpGet]
        public async Task<IActionResult> TestDirectSave()
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var testEmployee = new Employee
                {
                    Username = "testuser_" + DateTime.Now.Ticks,
                    Email = "test@" + DateTime.Now.Ticks + ".com",
                    PasswordHash = "testhash",
                    FirstName = "Test",
                    LastName = "User"
                };

                _context.Employees.Add(testEmployee);
                var saveResult = await _context.SaveChangesAsync();

                return Content($"Save result: {saveResult} rows affected. Employee ID: {testEmployee.EmployeeId}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckDatabase()
        {
            if (!IsAdmin()) return Forbid();

            var results = new List<string>();

            try
            {
                var count = await _context.Employees.CountAsync();
                results.Add($"Current employee count: {count}");

                if (count > 0)
                {
                    var employees = await _context.Employees.ToListAsync();
                    results.Add("Employees in database:");
                    foreach (var emp in employees)
                    {
                        results.Add($"  - ID: {emp.EmployeeId}, Name: {emp.FirstName} {emp.LastName}, Username: {emp.Username}, Email: {emp.Email}");
                    }
                }

                return Content(string.Join("\n", results));
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ForceRefreshAndCheck()
        {
            if (!IsAdmin()) return Forbid();

            var results = new List<string>();

            try
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(_context.Database.GetConnectionString())
                    .Options;

                using (var freshContext = new ApplicationDbContext(options))
                {
                    var employees = await freshContext.Employees.ToListAsync();
                    results.Add($"Total employees in fresh context: {employees.Count}");

                    foreach (var emp in employees)
                    {
                        results.Add($"  - ID: {emp.EmployeeId}, Name: {emp.FirstName} {emp.LastName}, Username: {emp.Username}, Email: {emp.Email}");
                    }
                }

                return Content(string.Join("\n", results));
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }
}