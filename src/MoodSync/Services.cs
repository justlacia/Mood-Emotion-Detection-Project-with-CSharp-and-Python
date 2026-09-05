using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace MoodSync;

public sealed class Detector(Settings settings)
{
    public async Task<Detection> AnalyzeAsync(string image, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.AnalysisTimeoutSeconds, 10, 600)));
        var start = new ProcessStartInfo(settings.PythonExecutable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true, StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        start.Environment["PYTHONUTF8"] = "1";
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "python", "detect.py"));
        start.ArgumentList.Add(image);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Python başlatılamadı.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            if (cancellationToken.IsCancellationRequested) throw;
            throw new TimeoutException("Analiz süre sınırını aştı. Python ve model kurulumunu kontrol edin.");
        }
        var output = await stdout;
        var errors = await stderr;
        if (process.ExitCode != 0)
        {
            try
            {
                using var error = JsonDocument.Parse(output);
                throw new InvalidOperationException(error.RootElement.GetProperty("error").GetString());
            }
            catch (JsonException) { throw new InvalidOperationException("Python çalıştırılamadı. Kurulumu kontrol edin.\n" + errors[..Math.Min(errors.Length, 600)]); }
        }
        var result = JsonSerializer.Deserialize<Detection>(output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (result is null || !new[] { "positive", "negative", "neutral" }.Contains(result.Mood)
            || !double.IsFinite(result.Confidence) || result.Confidence is < 0 or > 1)
            throw new InvalidOperationException("Model geçerli bir sonuç döndürmedi.");
        return result;
    }
}

public sealed class Repository(string connectionString)
{
    public bool Configured => !string.IsNullOrWhiteSpace(connectionString);
    private async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
    public async Task<Account?> LoginAsync(string email, string password)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("SELECT Id, DisplayName, PasswordHash FROM dbo.MoodSyncAccounts WHERE Email=@email", connection);
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || !Passwords.Verify(password, reader.GetString(2))) return null;
        return new(reader.GetInt32(0), reader.GetString(1));
    }
    public async Task<Account> RegisterAsync(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) throw new ArgumentException("Ad 1–100 karakter olmalı.");
        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address) || address.Address != email.Trim() || email.Length > 254)
            throw new ArgumentException("Geçerli bir e-posta adresi yazın.");
        if (password.Length < 10) throw new ArgumentException("Parola en az 10 karakter olmalı.");
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("INSERT INTO dbo.MoodSyncAccounts(DisplayName,Email,PasswordHash) OUTPUT INSERTED.Id VALUES(@name,@email,@hash)", connection);
        command.Parameters.AddWithValue("@name", name.Trim());
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@hash", Passwords.Hash(password));
        try { return new(Convert.ToInt32(await command.ExecuteScalarAsync()), name.Trim()); }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { throw new ArgumentException("Bu e-posta zaten kayıtlı."); }
    }
    public async Task SaveAsync(int userId, Detection result)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("INSERT INTO dbo.MoodSyncHistory(AccountId,Mood,Confidence) VALUES(@id,@mood,@confidence)", connection);
        command.Parameters.AddWithValue("@id", userId); command.Parameters.AddWithValue("@mood", result.Mood);
        command.Parameters.AddWithValue("@confidence", result.Confidence);
        await command.ExecuteNonQueryAsync();
    }
    public async Task<List<HistoryEntry>> HistoryAsync(int userId)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("SELECT TOP (100) CreatedAt,Mood,Confidence FROM dbo.MoodSyncHistory WHERE AccountId=@id ORDER BY CreatedAt DESC,Id DESC", connection);
        command.Parameters.AddWithValue("@id", userId);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<HistoryEntry>();
        while (await reader.ReadAsync()) rows.Add(new(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc).ToLocalTime(), reader.GetString(1), reader.GetDouble(2)));
        return rows;
    }
}

public static class Passwords
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600000, HashAlgorithmName.SHA256, 32);
        return $"600000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string encoded)
    {
        try
        {
            var parts = encoded.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations is < 100000 or > 1000000) return false;
            var salt = Convert.FromBase64String(parts[1]); var expected = Convert.FromBase64String(parts[2]);
            return expected.Length == 32 && salt.Length == 16 && CryptographicOperations.FixedTimeEquals(expected, Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32));
        }
        catch (FormatException) { return false; }
    }
}
