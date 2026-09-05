using MoodSync;

var one = Passwords.Hash("Doğru parola 123");
var two = Passwords.Hash("Doğru parola 123");
if (one == two || !Passwords.Verify("Doğru parola 123", one) || Passwords.Verify("yanlış", one) || Passwords.Verify("x", "bad"))
    throw new Exception("Password check failed");
Console.WriteLine("PASS: unique salt, correct password, wrong password, invalid hash");
var detector = new Detector(new Settings { PythonExecutable = args.Length > 0 ? args[0] : "python" });
try { await detector.AnalyzeAsync("C:/missing-image.png", CancellationToken.None); throw new Exception("Missing input accepted"); }
catch (InvalidOperationException ex) when (ex.Message.Contains("Geçerli bir")) { Console.WriteLine("PASS: Python JSON error reaches C# without stale output"); }
using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
try { await detector.AnalyzeAsync("C:/missing-image.png", cancellation.Token); throw new Exception("Cancellation ignored"); }
catch (OperationCanceledException) { Console.WriteLine("PASS: analysis cancellation"); }
if (args.Length > 1)
{
    var result = await detector.AnalyzeAsync(args[1], CancellationToken.None);
    if (result.Mood != "positive") throw new Exception("Expected positive sample");
    Console.WriteLine($"PASS: real C# → Python → YOLO → JSON result: {result.Mood}, {result.Confidence:0.000}");
}
