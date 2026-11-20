using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

public class UserEditForm : Form
{
    private readonly UserController _controller = new();
    private readonly User _existing;

    private readonly TextBox _username = new();
    private readonly TextBox _fullName = new();
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _tenant = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly CheckBox _active = new() { Text = "Account is active", Checked = true };

    public UserEditForm(User existing)
    {
        _existing = existing;
        _role.Items.AddRange(Roles.All);
        _role.SelectedItem = Roles.Landlord;

        _tenant.Items.Add(new ComboItem(0, "(None)"));
        foreach (var t in new TenantController().GetAll(""))
            _tenant.Items.Add(new ComboItem(t.Id, t.FullName));
        _tenant.SelectedIndex = 0;

        if (existing != null)
        {
            _username.Text = existing.Username;
            _username.Enabled = false;
            _fullName.Text = existing.FullName;
            _role.SelectedItem = existing.Role;
            _active.Checked = existing.IsActive;
            foreach (ComboItem item in _tenant.Items)
                if (item.Id == (existing.TenantId ?? 0)) _tenant.SelectedItem = item;
        }

        _role.SelectedIndexChanged += (s, e) => _tenant.Enabled = _role.Text == Roles.Tenant;
        _tenant.Enabled = _role.Text == Roles.Tenant;

        var ok = UiHelper.MakeButton("Save", UiHelper.Success, 100);
        var cancel = UiHelper.MakeButton("Cancel", UiHelper.Neutral, 100);
        ok.Click += (s, e) => Save();

        UiHelper.LayoutDialog(this, existing == null ? "Create User Account" : "Edit User Account", 520, ok, cancel,
            ("Username", _username),
            ("Full name", _fullName),
            ("Role", _role),
            ("Linked tenant", _tenant),
            (existing == null ? "Password" : "New password", _password),
            ("Status", _active));
    }

    private void Save()
    {
        try
        {
            int tenantId = (_tenant.SelectedItem as ComboItem)?.Id ?? 0;
            var u = new User
            {
                Id = _existing?.Id ?? 0,
                Username = _username.Text,
                FullName = _fullName.Text,
                Role = _role.Text,
                IsActive = _active.Checked,
                TenantId = tenantId > 0 ? tenantId : null
            };
            if (_existing == null) _controller.Add(u, _password.Text);
            else _controller.Update(u, _password.Text); // blank = keep current password
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { UiHelper.Handle(ex); }
    }
}
