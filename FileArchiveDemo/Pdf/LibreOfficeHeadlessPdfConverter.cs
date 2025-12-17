using System.Diagnostics;
using System.Text;

namespace FileArchiveDemo.Pdf;

public sealed class LibreOfficeHeadlessPdfConverter : IPdfConverter
{
    private readonly PdfOptions _options;

    public LibreOfficeHeadlessPdfConverter(PdfOptions options)
    {
        _options = options;
    }

    public async Task<byte[]> ConvertToPdfAsync(string originalFileName, string? extension, byte[] originalBytes)
    {
        Directory.CreateDirectory(_options.TempPath);

        // Unique work folder per conversion prevents collisions.
        var workDir = Path.Combine(_options.TempPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        // Ensure we have an extension so LibreOffice knows what it is.
        var ext = NormalizeExtension(extension, originalFileName) ?? ".bin";
        var inputPath = Path.Combine(workDir, "input" + ext);

        try
        {
            await File.WriteAllBytesAsync(inputPath, originalBytes);

            // LibreOffice writes output using the input base name, so we know expected pdf path.
            var expectedPdfPath = Path.Combine(workDir, "input.pdf");

            var args =
                $"--headless --nologo --nofirststartwizard --norestore " +
                $"--convert-to pdf --outdir \"{workDir}\" \"{inputPath}\"";

            var (exitCode, stdout, stderr) = await RunProcessAsync(
                fileName: _options.SofficePath,
                arguments: args,
                workingDirectory: workDir,
                timeout: TimeSpan.FromSeconds(_options.TimeoutSeconds)
            );

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"LibreOffice conversion failed (exit {exitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            }

            if (!File.Exists(expectedPdfPath))
            {
                // Sometimes LO names output differently; fallback: find the newest pdf in the folder.
                var pdf = Directory.EnumerateFiles(workDir, "*.pdf", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (pdf is null)
                {
                    throw new FileNotFoundException(
                        $"LibreOffice reported success but no PDF was produced.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                }

                expectedPdfPath = pdf;
            }

            return await File.ReadAllBytesAsync(expectedPdfPath);
        }
        finally
        {
            // Best-effort cleanup. Don’t fail conversion result because cleanup failed.
            try { Directory.Delete(workDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string? NormalizeExtension(string? extension, string originalFileName)
    {
        var ext = extension;

        if (string.IsNullOrWhiteSpace(ext))
            ext = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(ext))
            return null;

        if (!ext.StartsWith('.'))
            ext = "." + ext;

        return ext.ToLowerInvariant();
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        if (!proc.Start())
            throw new InvalidOperationException("Failed to start LibreOffice process.");

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        var completed = await Task.Run(() => proc.WaitForExit((int)timeout.TotalMilliseconds));
        if (!completed)
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"LibreOffice conversion timed out after {timeout.TotalSeconds} seconds.");
        }

        return (proc.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
