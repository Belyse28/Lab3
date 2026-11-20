using System.Drawing;
using System.Windows.Forms;
using RentalPropertyManagement.Controllers;

namespace RentalPropertyManagement.Views;

/// <summary>Item for combo boxes: an id plus the text shown to the user.</summary>
public class ComboItem
{
    public int Id { get; }
    public string Text { get; }
    public ComboItem(int id, string text) { Id = id; Text = text; }
    public override string ToString() => Text;
}

/// <summary>Shared look-and-feel and small UI utilities used by every view.</summary>
public static class UiHelper
{
    public static readonly Color Primary = Color.FromArgb(31, 56, 100);
    public static readonly Color Accent = Color.FromArgb(46, 117, 182);
    public static readonly Color Light = Color.FromArgb(242, 245, 250);
    public static readonly Color Danger = Color.FromArgb(192, 57, 43);
    public static readonly Color Success = Color.FromArgb(39, 142, 80);
    public static readonly Color Warning = Color.FromArgb(200, 120, 0);
    public static readonly Color Neutral = Color.FromArgb(110, 120, 135);

    public static Button MakeButton(string text, Color back, int width = 110)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Label MakeLabel(string text) =>
        new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 6, 0) };

    public static FlowLayoutPanel MakeToolbar() =>
        new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(10, 10, 10, 0),
            WrapContents = false,
            BackColor = Color.White
        };

    public static DataGridView MakeGrid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 36
        };
        g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
        g.AlternatingRowsDefaultCellStyle.BackColor = Light;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(205, 224, 245);
        g.DefaultCellStyle.SelectionForeColor = Color.Black;
        return g;
    }

    /// <summary>Defines the visible columns: property name, header text, optional number/date format.</summary>
    public static void SetColumns(DataGridView grid, params (string Prop, string Header, string Format)[] cols)
    {
        grid.AutoGenerateColumns = false;
        grid.Columns.Clear();
        foreach (var c in cols)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = c.Prop,
                HeaderText = c.Header,
                Name = c.Prop,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            if (!string.IsNullOrEmpty(c.Format)) col.DefaultCellStyle.Format = c.Format;
            grid.Columns.Add(col);
        }
    }

    /// <summary>Colours status text (Paid = green, Outstanding = red, ...) in the given column.</summary>
    public static void ColorStatus(DataGridView grid, string columnName)
    {
        grid.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (grid.Columns[e.ColumnIndex].Name != columnName) return;
            switch (e.Value.ToString())
            {
                case "Available":
                case "Paid":
                case "Active":
                    e.CellStyle.ForeColor = Success; break;
                case "Outstanding":
                case "Inactive":
                    e.CellStyle.ForeColor = Danger; break;
                case "Partially Paid":
                    e.CellStyle.ForeColor = Warning; break;
                case "Occupied":
                    e.CellStyle.ForeColor = Accent; break;
            }
        };
    }

    // ---- messages ----

    public static void Info(string message) =>
        MessageBox.Show(message, "Rental Property Management", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Warn(string message) =>
        MessageBox.Show(message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Error(string message) =>
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Confirm(string message) =>
        MessageBox.Show(message, "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Shows friendly messages for expected problems and a generic one for unexpected failures.</summary>
    public static void Handle(Exception ex)
    {
        if (ex is ValidationException) Warn(ex.Message);
        else Error("Something went wrong and the operation could not be completed.\n\nDetails: " + ex.Message);
    }

    /// <summary>Runs an action and reports any error to the user instead of crashing.</summary>
    public static void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { Handle(ex); }
    }

    /// <summary>Lays out a simple "label : control" dialog with OK / Cancel buttons.</summary>
    public static void LayoutDialog(Form form, string title, int clientWidth, Button ok, Button cancel,
        params (string Label, Control Ctrl)[] rows)
    {
        form.Text = title;
        form.Font = new Font("Segoe UI", 9.5f);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ShowInTaskbar = false;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(16, 12, 16, 0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int height = 12;
        foreach (var row in rows)
        {
            int rowHeight = Math.Max(40, row.Ctrl.Height + 12);
            height += rowHeight;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            table.Controls.Add(new Label
            {
                Text = row.Label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });

            if (row.Ctrl is TextBox tb && tb.Multiline) row.Ctrl.Dock = DockStyle.Fill;
            else row.Ctrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            table.Controls.Add(row.Ctrl);
        }

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 10, 12, 0)
        };
        cancel.DialogResult = DialogResult.Cancel;
        bar.Controls.Add(cancel);
        bar.Controls.Add(ok);

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Controls.Add(table); // Fill control first, then the docked button bar
        form.Controls.Add(bar);
        form.ClientSize = new Size(clientWidth, height + 56);
    }
}
