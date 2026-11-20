using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

/// <summary>Administrator screen: create, view, update, activate/deactivate user accounts.</summary>
public class UsersView : UserControl
{
    private readonly UserController _controller = new();
    private readonly DataGridView _grid = UiHelper.MakeGrid();

    public UsersView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        var toolbar = UiHelper.MakeToolbar();
        var add = UiHelper.MakeButton("Create User", UiHelper.Success, 110);
        add.Click += (s, e) => AddUser();
        var edit = UiHelper.MakeButton("Edit", UiHelper.Accent, 70);
        edit.Click += (s, e) => EditSelected();
        var toggle = UiHelper.MakeButton("Activate / Deactivate", UiHelper.Warning, 170);
        toggle.Click += (s, e) => ToggleSelected();
        toolbar.Controls.AddRange(new Control[] { add, edit, toggle });

        _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
        UiHelper.SetColumns(_grid,
            ("Username", "Username", null),
            ("FullName", "Full Name", null),
            ("Role", "Role", null),
            ("TenantName", "Linked Tenant", null),
            ("StatusText", "Status", null));
        UiHelper.ColorStatus(_grid, "StatusText");

        Controls.Add(_grid);
        Controls.Add(toolbar);
        LoadData();
    }

    private void LoadData() => UiHelper.Run(() => _grid.DataSource = _controller.GetAll());

    private User Selected() => _grid.CurrentRow?.DataBoundItem as User;

    private void AddUser()
    {
        UiHelper.Run(() =>
        {
            using var dlg = new UserEditForm(null);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void EditSelected()
    {
        var u = Selected();
        if (u == null) { UiHelper.Info("Please select a user first."); return; }
        UiHelper.Run(() =>
        {
            using var dlg = new UserEditForm(u);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void ToggleSelected()
    {
        var u = Selected();
        if (u == null) { UiHelper.Info("Please select a user first."); return; }
        string action = u.IsActive ? "deactivate" : "activate";
        if (!UiHelper.Confirm("Do you want to " + action + " the account '" + u.Username + "'?")) return;
        UiHelper.Run(() =>
        {
            _controller.SetActive(u.Id, !u.IsActive);
            LoadData();
        });
    }
}
