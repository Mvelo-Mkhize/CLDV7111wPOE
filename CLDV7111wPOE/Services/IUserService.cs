using CLDV7111wPOE.Models;

namespace CLDV7111wPOE.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(string username, string email, string password, string firstName, string lastName);
        Task<Employee> CreateEmployeeAsync(string username, string email, string password, string firstName, string lastName);
        Task<Administrator> CreateAdminAsync(string username, string email, string password, string firstName, string lastName);
        Task<User> UpdateUserAsync(int userId, string username, string email, string firstName, string lastName);
        Task<Employee> UpdateEmployeeAsync(int employeeId, string username, string email, string firstName, string lastName, string? newPassword = null);
        Task<Administrator> UpdateAdminAsync(int adminId, string username, string email, string firstName, string lastName, string? newPassword = null);
        Task<User> UpdateUserPasswordAsync(int userId, string newPassword);
        Task<Employee> UpdateEmployeePasswordAsync(int employeeId, string newPassword);
        Task<Administrator> UpdateAdminPasswordAsync(int adminId, string newPassword);
        bool ValidatePassword(string inputPassword, string storedHash);
    }
}