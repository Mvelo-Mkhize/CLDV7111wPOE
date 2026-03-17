namespace CLDV7111wPOE.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string inputPassword, string storedHash);
    }
}