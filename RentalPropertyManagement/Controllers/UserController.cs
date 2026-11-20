using Microsoft.Data.Sqlite;
using RentalPropertyManagement.Data;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Controllers;

public class UserController
{
    private const string SelectSql =
        @"SELECT u.Id, u.Username, u.PasswordHash, u.FullName, u.Role, u.IsActive, u.TenantId, COALESCE(t.FullName,'')
          FROM Users u LEFT JOIN Tenants t ON t.Id = u.TenantId ";

    private static User Map(SqliteDataReader r) => new User
    {
        Id = r.GetInt32(0),
        Username = r.GetString(1),
        PasswordHash = r.GetString(2),
        FullName = r.GetString(3),
        Role = r.GetString(4),
        IsActive = r.GetInt32(5) == 1,
        TenantId = r.IsDBNull(6) ? null : r.GetInt32(6),
        TenantName = r.GetString(7)
    };

    /// <summary>Used by login (before a session exists), so it performs no permission check.</summary>
    public User GetByUsername(string username)
    {
        using var conn = Database.Open();
        using var cmd = Database.Command(conn, SelectSql + "WHERE u.Username = @u", null, ("@u", username));
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public List<User> GetAll()
    {
        Session.RequireAdmin();
        var list = new List<User>();
        using var conn = Database.Open();
        using var cmd = Database.Command(conn, SelectSql + "ORDER BY u.Username");
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public void Add(User user, string password)
    {
        Session.RequireAdmin();
        Validate(user, true);
        ValidatePassword(password, true);

        using var conn = Database.Open();
        EnsureUsernameUnique(conn, user.Username, 0);
        Database.NonQuery(conn,
            "INSERT INTO Users(Username,PasswordHash,FullName,Role,IsActive,TenantId) VALUES(@u,@h,@f,@r,@a,@t)", null,
            ("@u", user.Username.Trim()), ("@h", PasswordHasher.Hash(password)), ("@f", user.FullName.Trim()),
            ("@r", user.Role), ("@a", user.IsActive ? 1 : 0), ("@t", user.Role == Roles.Tenant ? user.TenantId : null));
    }

    /// <param name="newPassword">Leave empty to keep the existing password.</param>
    public void Update(User user, string newPassword)
    {
        Session.RequireAdmin();
        Validate(user, false);
        if (!string.IsNullOrEmpty(newPassword)) ValidatePassword(newPassword, false);

        using var conn = Database.Open();
        EnsureAdminRemains(conn, user.Id, user.Role, user.IsActive);

        Database.NonQuery(conn,
            "UPDATE Users SET FullName=@f, Role=@r, IsActive=@a, TenantId=@t WHERE Id=@id", null,
            ("@f", user.FullName.Trim()), ("@r", user.Role), ("@a", user.IsActive ? 1 : 0),
            ("@t", user.Role == Roles.Tenant ? user.TenantId : null), ("@id", user.Id));

        if (!string.IsNullOrEmpty(newPassword))
            Database.NonQuery(conn, "UPDATE Users SET PasswordHash=@h WHERE Id=@id", null,
                ("@h", PasswordHasher.Hash(newPassword)), ("@id", user.Id));
    }

    public void SetActive(int userId, bool active)
    {
        Session.RequireAdmin();
        using var conn = Database.Open();
        object role = Database.Scalar(conn, "SELECT Role FROM Users WHERE Id=@id", null, ("@id", userId));
        if (role == null) throw new ValidationException("User not found.");
        EnsureAdminRemains(conn, userId, role.ToString(), active);
        Database.NonQuery(conn, "UPDATE Users SET IsActive=@a WHERE Id=@id", null,
            ("@a", active ? 1 : 0), ("@id", userId));
    }

    // ---- rules ----

    private static void Validate(User u, bool isNew)
    {
        if (u == null) throw new ValidationException("No user data supplied.");
        if (isNew)
        {
            string name = (u.Username ?? "").Trim();
            if (name.Length < 3) throw new ValidationException("Username must be at least 3 characters.");
            if (name.Any(char.IsWhiteSpace)) throw new ValidationException("Username cannot contain spaces.");
        }
        if (string.IsNullOrWhiteSpace(u.FullName)) throw new ValidationException("Full name is required.");
        if (!Roles.All.Contains(u.Role)) throw new ValidationException("Please select a valid role.");
        if (u.Role == Roles.Tenant && (u.TenantId == null || u.TenantId <= 0))
            throw new ValidationException("A tenant account must be linked to a tenant record.");
    }

    private static void ValidatePassword(string password, bool required)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (required) throw new ValidationException("A password is required.");
            return;
        }
        if (password.Length < 6) throw new ValidationException("Password must be at least 6 characters.");
    }

    private static void EnsureUsernameUnique(SqliteConnection conn, string username, int exceptId)
    {
        long n = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Users WHERE Username=@u AND Id<>@id", null,
            ("@u", username.Trim()), ("@id", exceptId)));
        if (n > 0) throw new ValidationException("That username is already taken.");
    }

    /// <summary>Prevents locking everyone out: the last active administrator cannot be demoted or deactivated.</summary>
    private static void EnsureAdminRemains(SqliteConnection conn, int userId, string newRole, bool newActive)
    {
        if (newRole == Roles.Admin && newActive) return;

        object currentRole = Database.Scalar(conn, "SELECT Role FROM Users WHERE Id=@id", null, ("@id", userId));
        if (currentRole == null || currentRole.ToString() != Roles.Admin) return;

        long otherAdmins = Convert.ToInt64(Database.Scalar(conn,
            "SELECT COUNT(*) FROM Users WHERE Role='Admin' AND IsActive=1 AND Id<>@id", null, ("@id", userId)));
        if (otherAdmins == 0)
            throw new ValidationException("There must always be at least one active administrator.");
    }
}
