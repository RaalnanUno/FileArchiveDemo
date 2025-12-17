using Microsoft.Win32;

namespace FileArchiveDemo.Pdf;

public static class LibreOfficeLocator
{
    public static string? FindSofficeExe()
    {
        // 1) If user set SOFFICE_PATH env var, honor it
        var env = Environment.GetEnvironmentVariable("SOFFICE_PATH");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return env;

        // 2) Common install locations
        var candidates = new[]
        {
            @"C:\Program Files\LibreOffice\program\soffice.exe",
            @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
        };

        foreach (var c in candidates)
            if (File.Exists(c)) return c;

        // 3) Try registry (Windows)
        try
        {
            // LibreOffice writes InstallPath here on many installs
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\LibreOffice\LibreOffice");
            var installPath = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(installPath))
            {
                var exe = Path.Combine(installPath, "program", "soffice.exe");
                if (File.Exists(exe)) return exe;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
