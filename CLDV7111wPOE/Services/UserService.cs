using CLDV7111wPOE.Data;
using CLDV7111wPOE.Models;
using Microsoft.EntityFrameworkCore;

namespace CLDV7111wPOE.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<UserService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, string firstName, string lastName)
        {
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                FirstName = firstName ?? "",
                LastName = lastName ?? ""
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created user {username} with hashed password: {user.PasswordHash.Substring(0, 10)}...");
            return user;
        }

        public async Task<Employee> CreateEmployeeAsync(string username, string email, string password, string firstName, string lastName)
        {
            var employee = new Employee
            {
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                FirstName = firstName ?? "",
                LastName = lastName ?? ""
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created employee {username} ({firstName} {lastName}) with hashed password: {employee.PasswordHash.Substring(0, 10)}...");
            return employee;
        }

        public async Task<Administrator> CreateAdminAsync(string username, string email, string password, string firstName, string lastName)
        {
            var admin = new Administrator
            {
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                FirstName = firstName ?? "",
                LastName = lastName ?? ""
            };

            _context.Administrators.Add(admin);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created admin {username} ({firstName} {lastName}) with hashed password: {admin.PasswordHash.Substring(0, 10)}...");
            return admin;
        }

        public async Task<User> UpdateUserAsync(int userId, string username, string email, string firstName, string lastName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found");

            user.Username = username;
            user.Email = email;
            user.FirstName = firstName ?? "";
            user.LastName = lastName ?? "";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated user {username} information");
            return user;
        }

        public async Task<Employee> UpdateEmployeeAsync(int employeeId, string username, string email, string firstName, string lastName, string? newPassword = null)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                throw new ArgumentException($"Employee with ID {employeeId} not found");

            employee.Username = username;
            employee.Email = email;
            employee.FirstName = firstName ?? "";
            employee.LastName = lastName ?? "";

            if (!string.IsNullOrEmpty(newPassword))
            {
                employee.PasswordHash = _passwordHasher.HashPassword(newPassword);
                _logger.LogInformation($"Updated password for employee {username}");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated employee {username} information");
            return employee;
        }

        public async Task<Administrator> UpdateAdminAsync(int adminId, string username, string email, string firstName, string lastName, string? newPassword = null)
        {
            var admin = await _context.Administrators.FindAsync(adminId);
            if (admin == null)
                throw new ArgumentException($"Admin with ID {adminId} not found");

            admin.Username = username;
            admin.Email = email;
            admin.FirstName = firstName ?? "";
            admin.LastName = lastName ?? "";

            if (!string.IsNullOrEmpty(newPassword))
            {
                admin.PasswordHash = _passwordHasher.HashPassword(newPassword);
                _logger.LogInformation($"Updated password for admin {username}");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated admin {username} information");
            return admin;
        }

        public async Task<User> UpdateUserPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated password for user {user.Username}");
            return user;
        }

        public async Task<Employee> UpdateEmployeePasswordAsync(int employeeId, string newPassword)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                throw new ArgumentException($"Employee with ID {employeeId} not found");

            employee.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated password for employee {employee.Username}");
            return employee;
        }

        public async Task<Administrator> UpdateAdminPasswordAsync(int adminId, string newPassword)
        {
            var admin = await _context.Administrators.FindAsync(adminId);
            if (admin == null)
                throw new ArgumentException($"Admin with ID {adminId} not found");

            admin.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated password for admin {admin.Username}");
            return admin;
        }

        public bool ValidatePassword(string inputPassword, string storedHash)
        {
            return _passwordHasher.VerifyPassword(inputPassword, storedHash);
        }
    }
}