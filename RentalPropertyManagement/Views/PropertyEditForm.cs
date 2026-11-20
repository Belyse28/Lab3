using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;
using RentalPropertyManagement.Models;

namespace RentalPropertyManagement.Views;

public class PropertyEditForm : Form
{
    private readonly PropertyController _controller = new();
    private readonly Property _existing;

    private readonly TextBox _name = new();
    private readonly TextBox _location = new();
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _rooms = new() { Minimum = 1, Maximum = 100, Value = 1 };
    private readonly NumericUpDown _rent = new() { Minimum = 0, Maximum = 1000000000, DecimalPlaces = 2, ThousandsSeparator = true, Increment = 50 };
    private readonly TextBox _description = new() { Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };

    public PropertyEditForm(Property existing)
    {
        _existing = existing;
        _type.Items.AddRange(PropertyTypes.All);

        if (existing != null)
        {
            _name.Text = existing.Name;
            _location.Text = existing.Location;
            _type.SelectedItem = existing.Type;
            _rooms.Value = Math.Min(Math.Max(existing.Rooms, 1), 100);
            _rent.Value = Math.Min(existing.MonthlyRent, _rent.Maximum);
            _description.Text = existing.Description;
        }
        else
        {
            _type.SelectedIndex = 0;
        }

        var ok = UiHelper.MakeButton("Save", UiHelper.Success, 100);
        var cancel = UiHelper.MakeButton("Cancel", UiHelper.Neutral, 100);
        ok.Click += (s, e) => Save();

        var rows = new List<(string, Control)>
        {
            ("Name / Number", _name),
            ("Location", _location),
            ("Type", _type),
            ("Rooms", _rooms),
            ("Monthly rent", _rent),
            ("Description", _description)
        };
        if (existing != null)
            rows.Add(("Status", new Label { Text = existing.Status + " (set automatically from tenant assignment)", AutoSize = false, Height = 24, TextAlign = ContentAlignment.MiddleLeft }));

        UiHelper.LayoutDialog(this, existing == null ? "Add Property" : "Edit Property", 520, ok, cancel, rows.ToArray());
    }

    private void Save()
    {
        try
        {
            var p = new Property
            {
                Id = _existing?.Id ?? 0,
                Name = _name.Text,
                Location = _location.Text,
                Type = _type.Text,
                Rooms = (int)_rooms.Value,
                MonthlyRent = _rent.Value,
                Description = _description.Text
            };
            if (_existing == null) _controller.Add(p); else _controller.Update(p);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { UiHelper.Handle(ex); }
    }
}
