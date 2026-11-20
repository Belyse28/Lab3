using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;

namespace RentalPropertyManagement.Views;

public class LoginForm : Form
{
    private readonly AuthController _auth = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };

    public LoginForm()
    {
        var login = UiHelper.MakeButton("Login", UiHelper.Accent, 110);
        var exit = UiHelper.MakeButton("Exit", UiHelper.Neutral, 110);
        login.Click += (s, e) => DoLogin();

        UiHelper.LayoutDialog(this, "Rental Property Management - Login", 400, login, exit,
            ("Username", _username),
            ("Password", _password));
    }

    private void DoLogin()
    {
        try
        {
            _auth.Login(_username.Text, _password.Text);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            UiHelper.Handle(ex);
            _password.Clear();
            _password.Focus();
        }
    }
}
