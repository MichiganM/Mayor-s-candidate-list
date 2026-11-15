using FluentValidation;
using Microsoft.Extensions.Logging;

namespace MayorElection.WinForms.Ui;

public abstract class ServiceForm : Form
{
    private readonly ILogger logger;
    private readonly ErrorProvider errors;
    private readonly Dictionary<string, Control> fields = new();
    private bool busy;

    protected ServiceForm(ILogger logger)
    {
        this.logger = logger;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);
        ForeColor = Color.FromArgb(24, 39, 57);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(16);
        errors = new ErrorProvider { ContainerControl = this, BlinkStyle = ErrorBlinkStyle.NeverBlink };
    }

    protected void ValidationField(string name, Control control) => fields[name] = control;

    protected async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        errors.Clear();
        Enabled = false;
        UseWaitCursor = true;
        try { await action(); }
        catch (ValidationException exception)
        {
            foreach (var group in exception.Errors.GroupBy(x => x.PropertyName))
                if (fields.TryGetValue(group.Key, out var control))
                    errors.SetError(control, string.Join(Environment.NewLine, group.Select(x => x.ErrorMessage)));
            MessageBox.Show(this, string.Join(Environment.NewLine, exception.Errors.Select(x => x.ErrorMessage).Distinct()),
                "Проверьте введённые данные", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidOperationException
            or ArgumentException or KeyNotFoundException or InvalidDataException or IOException)
        {
            MessageBox.Show(this, exception.Message, "Операция не выполнена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected UI operation failure");
            MessageBox.Show(this, "Не удалось выполнить операцию. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            busy = false;
            if (!IsDisposed) { Enabled = true; UseWaitCursor = false; }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) errors.Dispose();
        base.Dispose(disposing);
    }
}
