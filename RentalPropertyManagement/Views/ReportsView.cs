using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;

namespace RentalPropertyManagement.Views;

/// <summary>Property, tenant and payment reports that can be viewed on screen or exported to CSV (opens in Excel).</summary>
public class ReportsView : UserControl
{
    private readonly ReportController _controller = new();
    private readonly DataGridView _grid = UiHelper.MakeGrid();
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly Label _summary = new() { AutoSize = true, Margin = new Padding(16, 8, 0, 0) };
    private DataTable _current;

    public ReportsView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;

        _type.Items.AddRange(new object[] { "Property Report", "Tenant Report", "Payment Report" });
        _type.SelectedIndex = 0;
        _from.Value = new DateTime(DateTime.Today.Year, 1, 1);
        _to.Value = DateTime.Today;
        _type.SelectedIndexChanged += (s, e) =>
        {
            bool dates = _type.SelectedIndex == 2;
            _from.Enabled = dates;
            _to.Enabled = dates;
        };
        _from.Enabled = false;
        _to.Enabled = false;

        var generate = UiHelper.MakeButton("Generate", UiHelper.Accent, 90);
        generate.Margin = new Padding(16, 0, 8, 0);
        generate.Click += (s, e) => Generate();
        var export = UiHelper.MakeButton("Export CSV", UiHelper.Success, 100);
        export.Click += (s, e) => Export();

        var toolbar = UiHelper.MakeToolbar();
        toolbar.Controls.Add(UiHelper.MakeLabel("Report:"));
        toolbar.Controls.Add(_type);
        toolbar.Controls.Add(UiHelper.MakeLabel("   From:"));
        toolbar.Controls.Add(_from);
        toolbar.Controls.Add(UiHelper.MakeLabel("   To:"));
        toolbar.Controls.Add(_to);
        toolbar.Controls.Add(generate);
        toolbar.Controls.Add(export);
        toolbar.Controls.Add(_summary);

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Generate();
    }

    private void Generate()
    {
        UiHelper.Run(() =>
        {
            switch (_type.SelectedIndex)
            {
                case 1: _current = _controller.TenantReport(); break;
                case 2: _current = _controller.PaymentReport(_from.Value, _to.Value); break;
                default: _current = _controller.PropertyReport(); break;
            }

            _grid.DataSource = null;
            _grid.Columns.Clear();
            _grid.AutoGenerateColumns = true;
            _grid.DataSource = _current;
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                if (_current.Columns[col.DataPropertyName]?.DataType == typeof(decimal))
                    col.DefaultCellStyle.Format = "N2";
            }

            string total = "";
            if (_type.SelectedIndex == 2)
            {
                decimal sum = _current.Rows.Cast<DataRow>().Sum(r => (decimal)r["Amount"]);
                total = "   |   Total: " + sum.ToString("N2");
            }
            _summary.Text = _current.Rows.Count + " record(s)" + total;
        });
    }

    private void Export()
    {
        if (_current == null || _current.Rows.Count == 0)
        {
            UiHelper.Info("There is nothing to export. Generate a report with at least one record first.");
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = _type.Text.Replace(" ", "") + "_" + DateTime.Today.ToString("yyyyMMdd") + ".csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        UiHelper.Run(() =>
        {
            _controller.ExportCsv(_current, dlg.FileName);
            UiHelper.Info("Report exported to:\n" + dlg.FileName);
        });
    }
}
