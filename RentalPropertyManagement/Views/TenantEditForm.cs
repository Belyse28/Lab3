using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

public class TenantEditForm : Form
{
    private readonly TenantController _controller = new();
    private readonly Tenant _existing;

    private readonly TextBox _fullName = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _email = new();
    private readonly TextBox _idNumber = new();
    private readonly ComboBox _property = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _leaseStart = new() { Format = DateTimePickerFormat.Short };

    public TenantEditForm(Tenant existing)
    {
        _existing = existing;

        _property.Items.Add(new ComboItem(0, "(No property)"));
        foreach (var p in new PropertyController().GetAvailable(existing?.PropertyId ?? 0))
            _property.Items.Add(new ComboItem(p.Id, p.Name + " - " + p.Location + " (" + p.MonthlyRent.ToString("N2") + ")"));
        _property.SelectedIndex = 0;

        if (existing != null)
        {
            _fullName.Text = existing.FullName;
            _phone.Text = existing.Phone;
            _email.Text = existing.Email;
            _idNumber.Text = existing.IdNumber;
            _leaseStart.Value = existing.LeaseStartDate;
            foreach (ComboItem item in _property.Items)
                if (item.Id == (existing.PropertyId ?? 0)) _property.SelectedItem = item;
        }

        var ok = UiHelper.MakeButton("Save", UiHelper.Success, 100);
        var cancel = UiHelper.MakeButton("Cancel", UiHelper.Neutral, 100);
        ok.Click += (s, e) => Save();

        UiHelper.LayoutDialog(this, existing == null ? "Register Tenant" : "Edit Tenant", 520, ok, cancel,
            ("Full name", _fullName),
            ("Phone", _phone),
            ("Email", _email),
            ("ID number", _idNumber),
            ("Property", _property),
            ("Lease start", _leaseStart));
    }

    private void Save()
    {
        try
        {
            int propertyId = (_property.SelectedItem as ComboItem)?.Id ?? 0;
            var t = new Tenant
            {
                Id = _existing?.Id ?? 0,
                FullName = _fullName.Text,
                Phone = _phone.Text,
                Email = _email.Text,
                IdNumber = _idNumber.Text,
                PropertyId = propertyId > 0 ? propertyId : null,
                LeaseStartDate = _leaseStart.Value.Date
            };
            if (_existing == null) _controller.Add(t); else _controller.Update(t);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { UiHelper.Handle(ex); }
    }
}
