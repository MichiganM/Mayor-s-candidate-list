using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MayorElection.Domain.Enums;

namespace MayorElection.WinForms.Ui;

internal static class Ui
{
    public static readonly Color Accent = Color.FromArgb(15, 118, 110);
    public static Button Button(string text) => new()
    {
        Text = text, AutoSize = true, MinimumSize = new Size(110, 36),
        FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White,
        Margin = new Padding(4), Padding = new Padding(8, 3, 8, 3)
    };
    public static TextBox Input(bool password = false) => new()
    {
        Dock = DockStyle.Fill, UseSystemPasswordChar = password, MaxLength = password ? 128 : 100
    };
    public static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    public static TableLayoutPanel Fields() => new()
    {
        Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Padding = new Padding(8),
        ColumnStyles = { new ColumnStyle(SizeType.Absolute, 165), new ColumnStyle(SizeType.Percent, 100) }
    };
    public static void Field(TableLayoutPanel table, string caption, Control control)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = new Padding(4, 10, 4, 10) }, 0, row);
        control.Margin = new Padding(4, 7, 20, 7);
        table.Controls.Add(control, 1, row);
    }
    public static FlowLayoutPanel Actions(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8), WrapContents = true };
        panel.Controls.AddRange(controls);
        return panel;
    }
    public static string Status(CandidateStatus status) => typeof(CandidateStatus).GetMember(status.ToString())
        .FirstOrDefault()?.GetCustomAttribute<DisplayAttribute>()?.Name ?? status.ToString();
    public static string Template(CandidateTemplate template) => template == CandidateTemplate.SelfNominee
        ? "Самовыдвиженец" : "Партийный кандидат";
    public static void Statuses(ComboBox combo, bool all = false)
    {
        if (all) combo.Items.Add(new Choice<CandidateStatus>(null, "Все статусы"));
        foreach (var value in Enum.GetValues<CandidateStatus>()) combo.Items.Add(new Choice<CandidateStatus>(value, Status(value)));
        combo.SelectedIndex = 0;
    }
    public static void Templates(ComboBox combo, bool all = false)
    {
        if (all) combo.Items.Add(new Choice<CandidateTemplate>(null, "Все шаблоны"));
        foreach (var value in Enum.GetValues<CandidateTemplate>()) combo.Items.Add(new Choice<CandidateTemplate>(value, Template(value)));
        combo.SelectedIndex = 0;
    }
    public static T? Selected<T>(ComboBox combo) where T : struct => (combo.SelectedItem as Choice<T>)?.Value;
    public static void Select<T>(ComboBox combo, T value) where T : struct
    {
        foreach (var item in combo.Items.OfType<Choice<T>>())
            if (EqualityComparer<T?>.Default.Equals(item.Value, value)) { combo.SelectedItem = item; return; }
    }
    private sealed record Choice<T>(T? Value, string Label) where T : struct
    {
        public override string ToString() => Label;
    }
}
