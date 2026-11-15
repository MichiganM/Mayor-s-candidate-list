namespace MayorElection.WinForms;

/// <summary>Temporary entry screen until the authentication UI is implemented.</summary>
public sealed class ScaffoldForm : Form
{
    public ScaffoldForm()
    {
        Text = "Выборы мэра — каркас приложения";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 180);
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "MayorElection\nЭтап 1: решение и зависимости настроены.\nФорма входа будет добавлена на этапе 4."
        });
    }
}
