using MayorElection.Services;
using MayorElection.Services.DTOs;
using MayorElection.WinForms.Ui;
using Microsoft.Extensions.Logging;
using U = MayorElection.WinForms.Ui.Ui;

namespace MayorElection.WinForms;

public sealed class RegisterForm : ServiceForm
{
    public RegisterForm(AuthService auth, ILogger<RegisterForm> logger) : base(logger)
    {
        Text = "Регистрация пользователя"; ClientSize = new Size(590, 350);
        MinimumSize = new Size(590, 390);
        var login = U.Input(); login.MaxLength = 64;
        var password = U.Input(true); var confirmation = U.Input(true);
        var fields = U.Fields();
        U.Field(fields, "Логин", login); U.Field(fields, "Пароль", password);
        U.Field(fields, "Повтор пароля", confirmation);
        U.Field(fields, "Требования", new Label { Text = "Логин: 3–64 символа (латиница, цифры, _, ., -).\nПароль: 12–128 символов, заглавная и строчная\nбуквы, цифра и специальный символ.", AutoSize = true });
        ValidationField("UserName", login); ValidationField("Password", password);
        ValidationField("ConfirmPassword", confirmation);
        var save = U.Button("Создать пользователя");
        var cancel = U.Button("Отмена"); cancel.DialogResult = DialogResult.Cancel;
        save.Click += async (_, _) => await RunAsync(async () =>
        {
            await auth.RegisterAsync(new RegisterRequest(login.Text, password.Text, confirmation.Text));
            password.Clear(); confirmation.Clear();
            MessageBox.Show(this, "Пользователь создан. Теперь войдите с новым логином.", "Регистрация");
            DialogResult = DialogResult.OK;
        });
        Controls.Add(fields); Controls.Add(U.Actions(save, cancel));
        AcceptButton = save; CancelButton = cancel;
    }
}
