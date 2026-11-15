using MayorElection.Services;
using MayorElection.Services.DTOs;
using MayorElection.WinForms.Ui;
using Microsoft.Extensions.Logging;
using U = MayorElection.WinForms.Ui.Ui;

namespace MayorElection.WinForms;

public sealed class LoginForm : ServiceForm
{
    private readonly AuthService auth;
    private readonly TextBox login = U.Input();
    private readonly TextBox password = U.Input(true);
    private readonly TextBox code = U.Input();
    private readonly Label status = new() { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(8) };
    private readonly Button submit = U.Button("Войти");
    private readonly Button register = U.Button("Регистрация");
    private readonly Button restart = U.Button("Назад к паролю");
    private bool secondFactor;
    public event EventHandler? Authenticated;

    public LoginForm(AuthService auth, Func<RegisterForm> registration, ILogger<LoginForm> logger) : base(logger)
    {
        this.auth = auth;
        Text = "Выборы мэра — вход";
        ClientSize = new Size(550, 340);
        MinimumSize = new Size(550, 380);
        login.MaxLength = 64;
        code.MaxLength = 8;
        code.Enabled = false;
        restart.Visible = false;
        var fields = U.Fields();
        U.Field(fields, "Логин", login); U.Field(fields, "Пароль", password);
        U.Field(fields, "Код аутентификатора", code);
        Controls.Add(fields);
        Controls.Add(status);
        Controls.Add(U.Actions(submit, register, restart));
        status.Text = "Войдите для работы со списком кандидатов.";
        AcceptButton = submit;
        submit.Click += async (_, _) => await RunAsync(SignInAsync);
        register.Click += (_, _) => { using var form = registration(); form.ShowDialog(this); };
        restart.Click += (_, _) => ResetLogin();
    }

    private async Task SignInAsync()
    {
        var result = secondFactor ? await auth.Verify2FAAsync(code.Text) : await auth.LoginAsync(login.Text, password.Text);
        password.Clear(); code.Clear();
        switch (result.Status)
        {
            case LoginStatus.Succeeded:
                BeginInvoke(() => Authenticated?.Invoke(this, EventArgs.Empty));
                break;
            case LoginStatus.RequiresTwoFactor:
                secondFactor = true; login.Enabled = false; password.Enabled = false;
                code.Enabled = true; register.Visible = false; restart.Visible = true;
                submit.Text = "Подтвердить"; status.Text = "Введите 6 цифр из аутентификатора (в течение 5 минут).";
                BeginInvoke(() => code.Focus());
                break;
            case LoginStatus.LockedOut:
                ResetLogin(); status.Text = "Вход заблокирован на 15 минут после пяти ошибок.";
                break;
            default:
                status.Text = secondFactor ? "Неверный код или срок входа истёк. Повторите код либо вернитесь к паролю."
                    : "Неверный логин или пароль.";
                break;
        }
    }

    private void ResetLogin()
    {
        auth.Logout(); secondFactor = false;
        login.Enabled = true; password.Enabled = true; code.Enabled = false;
        password.Clear(); code.Clear(); register.Visible = true; restart.Visible = false;
        submit.Text = "Войти"; status.Text = "Введите логин и пароль.";
    }
}
