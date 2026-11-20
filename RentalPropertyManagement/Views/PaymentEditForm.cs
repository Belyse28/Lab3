using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

public class PaymentEditForm : Form
{
    private readonly PaymentController _controller = new();
    private readonly Payment _existing;
    private readonly Dictionary<int, Tenant> _tenants = new();

    private readonly ComboBox _tenant = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _info = new() { AutoSize = false, Height = 24, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray };
    private readonly NumericUpDown _amount = new() { Minimum = 0, Maximum = 1000000000, DecimalPlaces = 2, ThousandsSeparator = true, Increment = 50 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _period = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", ShowUpDown = true };
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _notes = new() { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };

    public PaymentEditForm(Payment existing)
    {
        _existing = existing;
        _date.MaxDate = DateTime.Today;
        _method.Items.AddRange(PaymentMethods.All);
        _method.SelectedIndex = 0;

        foreach (var t in new TenantController().GetAll(""))
        {
            _tenants[t.Id] = t;
            string label = t.FullName + (t.PropertyName.Length > 0 ? " - " + t.PropertyName : " (no property)");
            _tenant.Items.Add(new ComboItem(t.Id, label));
        }

        if (existing != null)
        {
            foreach (ComboItem item in _tenant.Items)
                if (item.Id == existing.TenantId) _tenant.SelectedItem = item;
            _amount.Value = Math.Min(existing.Amount, _amount.Maximum);
            _date.Value = existing.PaymentDate;
            _period.Value = DateTime.ParseExact(existing.Period + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            _method.SelectedItem = existing.Method;
            _notes.Text = existing.Notes;
        }
        else
        {
            _period.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        _tenant.SelectedIndexChanged += (s, e) => TenantChanged();
        TenantChanged(false);

        var ok = UiHelper.MakeButton("Save", UiHelper.Success, 100);
        var cancel = UiHelper.MakeButton("Cancel", UiHelper.Neutral, 100);
        ok.Click += (s, e) => Save();

        UiHelper.LayoutDialog(this, existing == null ? "Record Rent Payment" : "Edit Rent Payment", 540, ok, cancel,
            ("Tenant", _tenant),
            ("Property", _info),
            ("Amount paid", _amount),
            ("Payment date", _date),
            ("Rent month", _period),
            ("Method", _method),
            ("Notes", _notes));
    }

    private void TenantChanged(bool suggestAmount = true)
    {
        var sel = _tenant.SelectedItem as ComboItem;
        if (sel == null || !_tenants.TryGetValue(sel.Id, out var t))
        {
            _info.Text = "Select a tenant";
            return;
        }
        if (t.PropertyName.Length == 0)
        {
            _info.Text = "Not assigned to a property - rent cannot be recorded";
            return;
        }
        _info.Text = t.PropertyName + "  |  monthly rent " + t.MonthlyRent.ToString("N2");
        if (suggestAmount && _existing == null) _amount.Value = Math.Min(t.MonthlyRent, _amount.Maximum);
    }

    private void Save()
    {
        try
        {
            var p = new Payment
            {
                Id = _existing?.Id ?? 0,
                TenantId = (_tenant.SelectedItem as ComboItem)?.Id ?? 0,
                Amount = _amount.Value,
                PaymentDate = _date.Value.Date,
                Method = _method.Text,
                Period = _period.Value.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Notes = _notes.Text
            };
            if (_existing == null) _controller.Add(p); else _controller.Update(p);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { UiHelper.Handle(ex); }
    }
}
