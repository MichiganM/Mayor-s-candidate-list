using MayorElection.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MayorElection.WinForms;

public sealed class ElectionApplicationContext : ApplicationContext
{
    private readonly IServiceScopeFactory scopeFactory;
    private IServiceScope? scope;
    private Form? active;
    private bool switching;

    public ElectionApplicationContext(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
        ShowLogin();
    }

    private void ShowLogin()
    {
        scope = scopeFactory.CreateScope();
        var login = scope.ServiceProvider.GetRequiredService<LoginForm>();
        login.Authenticated += (_, _) => ShowMain();
        Show(login);
    }

    private void ShowMain()
    {
        switching = true;
        var previous = active;
        Show(scope!.ServiceProvider.GetRequiredService<MainForm>());
        ((MainForm)active!).LogoutRequested += (_, _) => Logout();
        previous?.Dispose();
        switching = false;
    }

    private void Logout()
    {
        switching = true;
        scope!.ServiceProvider.GetRequiredService<AuthService>().Logout();
        active?.Dispose(); scope.Dispose(); scope = null;
        ShowLogin(); switching = false;
    }

    private void Show(Form form)
    {
        active = form;
        form.FormClosed += (_, _) => { if (!switching) ExitThread(); };
        form.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { active?.Dispose(); scope?.Dispose(); }
        base.Dispose(disposing);
    }
}
