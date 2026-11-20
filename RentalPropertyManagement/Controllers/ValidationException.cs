namespace RentalPropertyManagement.Controllers;

/// <summary>Raised for problems the user can fix (bad input, missing permission, rule violations).</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
