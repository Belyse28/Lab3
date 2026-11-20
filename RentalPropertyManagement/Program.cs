using RentalPropertyManagement.Data;
using RentalPropertyManagement.Views;

namespace RentalPropertyManagement;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            Database.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("The database could not be opened or created:\n\n" + ex.Message,
                "Rental Property Management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Login -> main window. Logging out returns to the login screen; closing the window exits.
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) break;

            using var main = new MainForm();
            main.ShowDialog();
            Session.End();
            if (!main.LoggedOut) break;
        }
    }
}
