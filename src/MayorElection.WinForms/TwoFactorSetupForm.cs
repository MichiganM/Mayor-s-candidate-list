using MayorElection.Services;
using MayorElection.WinForms.Ui;
using Microsoft.Extensions.Logging;
using QRCoder;
using U = MayorElection.WinForms.Ui.Ui;

namespace MayorElection.WinForms;

public sealed class TwoFactorSetupForm : ServiceForm
{
    private readonly PictureBox qr = new() { Size = new Size(230, 230), SizeMode = PictureBoxSizeMode.Zoom };

    public TwoFactorSetupForm(AuthService auth, ILogger<TwoFactorSetupForm> logger) : base(logger)
    {
        Text = "Настройка двухфакторной аутентификации";
        ClientSize = new Size(680, 620); MinimumSize = new Size(680, 660);
        var password = U.Input(true);
        var key = U.Input(); key.ReadOnly = true;
        var code = U.Input(); code.MaxLength = 8; code.Enabled = false;
        var prepare = U.Button("Получить QR-код"); var enable = U.Button("Включить 2FA"); enable.Enabled = false;
        var close = U.Button("Закрыть"); close.DialogResult = DialogResult.Cancel;
        var fields = U.Fields();
        U.Field(fields, "Текущий пароль", password); U.Field(fields, "", prepare);
        U.Field(fields, "Аутентификатор", new Label { Text = "Отсканируйте QR-код или введите ключ вручную.\nЗатем подтвердите кодом из приложения.", AutoSize = true });
        U.Field(fields, "QR-код", qr); U.Field(fields, "Секретный ключ", key);
        U.Field(fields, "Код из приложения", code);
        prepare.Click += async (_, _) => await RunAsync(async () =>
        {
            var setup = await auth.Begin2FASetupAsync(password.Text);
            password.Clear(); key.Text = setup.SharedKey;
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(setup.AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var image = new QRCode(data);
            var old = qr.Image; qr.Image = image.GetGraphic(8); old?.Dispose();
            code.Enabled = true; enable.Enabled = true;
        });
        enable.Click += async (_, _) => await RunAsync(async () =>
        {
            await auth.Enable2FAAsync(code.Text);
            code.Clear(); key.Clear();
            MessageBox.Show(this, "2FA включена. При следующем входе потребуется код аутентификатора.", "Готово");
            DialogResult = DialogResult.OK;
        });
        Controls.Add(fields); Controls.Add(U.Actions(enable, close)); CancelButton = close;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { qr.Image?.Dispose(); qr.Image = null; }
        base.Dispose(disposing);
    }
}
