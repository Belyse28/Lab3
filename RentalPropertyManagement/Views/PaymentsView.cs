using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

/// <summary>
/// Payment history (filterable per tenant) and outstanding balances.
/// Managers can record/edit/delete payments; tenants see only their own, read-only.
/// </summary>
public class PaymentsView : UserControl
{
    private readonly PaymentController _controller = new();
    private readonly DataGridView _history = UiHelper.MakeGrid();
    private readonly DataGridView _balances = UiHelper.MakeGrid();
    private readonly ComboBox _tenantFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private bool _loadingFilter;

    public PaymentsView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        var toolbar = UiHelper.MakeToolbar();

        if (Session.CanManage)
        {
            toolbar.Controls.Add(UiHelper.MakeLabel("Tenant:"));
            toolbar.Controls.Add(_tenantFilter);
            LoadTenantFilter();
            _tenantFilter.SelectedIndexChanged += (s, e) => { if (!_loadingFilter) LoadData(); };

            var record = UiHelper.MakeButton("Record Payment", UiHelper.Success, 130);
            record.Margin = new Padding(20, 0, 8, 0);
            record.Click += (s, e) => RecordPayment();
            var edit = UiHelper.MakeButton("Edit", UiHelper.Accent, 70);
            edit.Click += (s, e) => EditSelected();
            var delete = UiHelper.MakeButton("Delete", UiHelper.Danger, 80);
            delete.Click += (s, e) => DeleteSelected();
            toolbar.Controls.AddRange(new Control[] { record, edit, delete });
            _history.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
        }
        else
        {
            toolbar.Controls.Add(UiHelper.MakeLabel("Your rent payments and balance"));
        }

        var refresh = UiHelper.MakeButton("Refresh", UiHelper.Neutral, 80);
        refresh.Click += (s, e) => { if (Session.CanManage) LoadTenantFilter(); LoadData(); };
        toolbar.Controls.Add(refresh);

        UiHelper.SetColumns(_history,
            ("PaymentDate", "Date", "yyyy-MM-dd"),
            ("TenantName", "Tenant", null),
            ("PropertyName", "Property", null),
            ("Period", "Rent Month", null),
            ("Amount", "Amount", "N2"),
            ("Method", "Method", null),
            ("Status", "Status", null),
            ("Notes", "Notes", null));
        UiHelper.ColorStatus(_history, "Status");

        UiHelper.SetColumns(_balances,
            ("TenantName", "Tenant", null),
            ("PropertyName", "Property", null),
            ("MonthlyRent", "Monthly Rent", "N2"),
            ("MonthsDue", "Months Due", null),
            ("TotalCharges", "Total Charges", "N2"),
            ("TotalPaid", "Total Paid", "N2"),
            ("Outstanding", "Outstanding", "N2"),
            ("Status", "Status", null));
        UiHelper.ColorStatus(_balances, "Status");

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var historyTab = new TabPage("Payment History") { BackColor = Color.White };
        historyTab.Controls.Add(_history);
        var balanceTab = new TabPage("Outstanding Rent") { BackColor = Color.White };
        balanceTab.Controls.Add(_balances);
        tabs.TabPages.Add(historyTab);
        tabs.TabPages.Add(balanceTab);

        Controls.Add(tabs);
        Controls.Add(toolbar);
        LoadData();
    }

    private void LoadTenantFilter()
    {
        UiHelper.Run(() =>
        {
            _loadingFilter = true;
            int previous = (_tenantFilter.SelectedItem as ComboItem)?.Id ?? 0;
            _tenantFilter.Items.Clear();
            _tenantFilter.Items.Add(new ComboItem(0, "All tenants"));
            foreach (var t in new TenantController().GetAll(""))
                _tenantFilter.Items.Add(new ComboItem(t.Id, t.FullName));
            _tenantFilter.SelectedIndex = 0;
            foreach (ComboItem item in _tenantFilter.Items)
                if (item.Id == previous) _tenantFilter.SelectedItem = item;
            _loadingFilter = false;
        });
    }

    private void LoadData()
    {
        UiHelper.Run(() =>
        {
            int tenantId = (_tenantFilter.SelectedItem as ComboItem)?.Id ?? 0;
            _history.DataSource = _controller.GetAll(tenantId);

            var balances = _controller.GetBalances();
            if (tenantId > 0) balances = balances.Where(b => b.TenantId == tenantId).ToList();
            _balances.DataSource = balances;
        });
    }

    private Payment Selected() => _history.CurrentRow?.DataBoundItem as Payment;

    private void RecordPayment()
    {
        UiHelper.Run(() =>
        {
            using var dlg = new PaymentEditForm(null);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void EditSelected()
    {
        var p = Selected();
        if (p == null) { UiHelper.Info("Please select a payment first."); return; }
        UiHelper.Run(() =>
        {
            using var dlg = new PaymentEditForm(p);
            if (dlg.ShowDialog(this) == DialogResult.OK) LoadData();
        });
    }

    private void DeleteSelected()
    {
        var p = Selected();
        if (p == null) { UiHelper.Info("Please select a payment first."); return; }
        if (!UiHelper.Confirm("Delete the payment of " + p.Amount.ToString("N2") + " by " + p.TenantName + "?")) return;
        UiHelper.Run(() =>
        {
            _controller.Delete(p.Id);
            LoadData();
        });
    }
}
