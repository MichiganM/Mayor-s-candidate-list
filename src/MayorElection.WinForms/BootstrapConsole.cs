using System.Runtime.InteropServices;

namespace MayorElection.WinForms;

internal static class BootstrapConsole
{
    public static void Display(string credentials)
    {
        // WinExe has no console when launched from Explorer. Allocate one only for first-run credentials.
        if (GetConsoleWindow() == IntPtr.Zero && !AllocConsole())
            throw new InvalidOperationException("Cannot open console for initial administrator credentials.");
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        writer.WriteLine(credentials);
        writer.WriteLine("Сохраните пароль администратора в безопасном месте. Повторно он не выводится.");
    }

    [DllImport("kernel32.dll")] private static extern bool AllocConsole();
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
}
