using System.Text.Json;

namespace MoodSync;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))) ?? new();
            var local = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
            if (File.Exists(local)) settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(local)) ?? settings;
            if (args.Length == 2 && args[0] == "--render-preview")
            {
                using var form = new MainForm(settings);
                form.Show(); form.PerformLayout(); Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(Path.GetFullPath(args[1]), System.Drawing.Imaging.ImageFormat.Png);
                return;
            }
            Application.Run(new MainForm(settings));
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "MoodSync başlatılamadı"); }
    }
}

public sealed record Settings
{
    public string PythonExecutable { get; init; } = "python";
    public string ConnectionString { get; init; } = "";
    public int AnalysisTimeoutSeconds { get; init; } = 120;
}
public sealed record Detection(string Mood, double Confidence, string ImagePath);
public sealed record HistoryEntry(DateTime CreatedAt, string Mood, double Confidence);
public sealed record Account(int Id, string Name);
public sealed record Track(string Title, string Artist, string Genre, string Mood);
