namespace FileArchiveDemo.Email;

public static class ReportBuilder
{
    public static string BuildPdfRunReport(
        DateTime startedUtc,
        DateTime finishedUtc,
        int ingestedCount,
        int pendingBefore,
        int convertedSuccess,
        int convertedFailed,
        IEnumerable<string>? convertedFiles = null,
        IEnumerable<string>? failedFiles = null)
    {
        var nl = Environment.NewLine;

        string JoinLines(string title, IEnumerable<string>? items)
        {
            var list = (items ?? Array.Empty<string>()).ToList();
            if (list.Count == 0) return $"{title}: (none){nl}";
            return title + ":" + nl + string.Join(nl, list.Select(x => "  - " + x)) + nl + nl;
        }

        return
            $"FileArchiveDemo - PDF Conversion Report{nl}" +
            $"Started (UTC):  {startedUtc:O}{nl}" +
            $"Finished (UTC): {finishedUtc:O}{nl}{nl}" +
            $"Ingested this run:  {ingestedCount}{nl}" +
            $"Pending before run: {pendingBefore}{nl}" +
            $"PDF converted OK:   {convertedSuccess}{nl}" +
            $"PDF failed:         {convertedFailed}{nl}{nl}" +
            JoinLines("Converted files", convertedFiles) +
            JoinLines("Failed files", failedFiles);
    }
}
