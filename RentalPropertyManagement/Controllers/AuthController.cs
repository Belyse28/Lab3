using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class AuthController
{
    public User Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new ValidationException("Please enter both your username and password.");

        User user = new UserController().GetByUsername(username.Trim());

        // Same message for unknown user / wrong password so accounts cannot be guessed.
        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
            throw new ValidationException("Invalid username or password.");

        if (!user.IsActive)
            throw new ValidationException("This account has been deactivated. Please contact the administrator.");

        Session.Start(user);
        return user;
    }

    public void Logout() => Session.End();
}
