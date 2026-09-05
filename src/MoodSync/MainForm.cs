using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace MoodSync;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(15, 18, 29), Card = Color.FromArgb(25, 29, 44),
        Muted = Color.FromArgb(157, 165, 185), Accent = Color.FromArgb(170, 143, 255);
    private readonly Repository repository;
    private readonly Detector detector;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Panel content = new() { Dock = DockStyle.Fill, Padding = new(32), AutoScroll = true };
    private readonly Label status = TextLabel("Bir fotoğraf seçerek başlayın.", 11, Muted);
    private readonly Label resultTitle = TextLabel("Henüz analiz yok", 22);
    private readonly Label resultDetail = TextLabel("Sonucunuz burada görünecek.", 11, Muted);
    private readonly PictureBox preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(20, 23, 36) };
    private readonly Button analyze = Button("İfadeyi analiz et  →", true), choose = Button("＋  Fotoğraf seç");
    private readonly FlowLayoutPanel tracks = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
    private readonly Label accountLabel = TextLabel("Misafir oturumu", 10, Muted);
    private readonly List<HistoryEntry> sessionHistory = [];
    private readonly List<Track> catalog = [new("Gülümse", "Sezen Aksu", "Pop", "positive"), new("Can't Stop the Feeling!", "Justin Timberlake", "Pop", "positive"), new("Walking on Sunshine", "Katrina & The Waves", "Pop", "positive"), new("Weightless", "Marconi Union", "Ambient", "negative"), new("River Flows in You", "Yiruma", "Piyano", "negative"), new("Saturn", "Sleeping At Last", "Alternatif", "negative"), new("Sunset Lover", "Petit Biscuit", "Elektronik", "neutral"), new("A Walk", "Tycho", "Elektronik", "neutral"), new("Nuvole Bianche", "Ludovico Einaudi", "Piyano", "neutral")];
    private string? selectedImage;
    private string? currentMood;
    private Account? account;
    private bool busy;

    public MainForm(Settings settings)
    {
        repository = new(settings.ConnectionString); detector = new(settings);
        Text = "MoodSync · Kendine kulak ver"; BackColor = Background; ForeColor = Color.White;
        Font = new("Segoe UI", 10); MinimumSize = new(1100, 760); Size = new(1320, 850);
        StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Dpi;
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, Padding = new(22, 30, 22, 24), BackColor = Color.FromArgb(19, 22, 35) };
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var brand = TextLabel("moodsync", 21, Accent); brand.Width = 176;
        nav.Controls.Add(brand);
        nav.Controls.Add(TextLabel("KENDİNE KULAK VER", 9, Muted));
        nav.Controls.Add(new Panel { Height = 42, Width = 170 });
        foreach (var item in new[] { "◈   Keşfet", "◷   Analiz geçmişi", "♫   Müzik koleksiyonu", "⚙   Hesabım" })
        {
            var button = Button(item); button.Width = 174; button.Height = 48;
            button.TextAlign = ContentAlignment.MiddleLeft; button.Margin = new(0, 0, 0, 12);
            button.Click += async (_, _) =>
            {
                if (busy) return;
                if (item.Contains("Keşfet")) ShowHome();
                else if (item.Contains("geçmişi")) await ShowHistory();
                else if (item.Contains("koleksiyonu")) ShowCollection();
                else ShowAccount();
            };
            nav.Controls.Add(button);
        }
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 90 };
        accountLabel.Dock = DockStyle.Top; footer.Controls.Add(accountLabel);
        footer.Controls.Add(new Label { Text = "Sana ait bir ritim.\nSana özel bir an.", Dock = DockStyle.Bottom, Height = 44, ForeColor = Muted });
        sidebar.Controls.Add(nav); sidebar.Controls.Add(footer);
        Controls.Add(content); Controls.Add(sidebar);
        choose.Click += (_, _) => ChooseImage(); analyze.Click += async (_, _) => await Analyze();
        analyze.Enabled = false; ShowHome();
        FormClosing += (_, _) => lifetime.Cancel();
        FormClosed += (_, _) => { preview.Image?.Dispose(); lifetime.Dispose(); };
    }

    private static Label TextLabel(string text, float size, Color? color = null) => new()
    {
        Text = text, AutoSize = false, Height = (int)(size * 2.4 + 8), Width = 500,
        Font = new("Segoe UI", size, size >= 18 ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? Color.White, BackColor = Color.Transparent, Margin = new(0, 0, 0, 8)
    };
    private static Button Button(string text, bool primary = false) => new()
    {
        Text = text, Height = 44, Width = 190, FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Accent : Card, ForeColor = primary ? Background : Color.White,
        FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand,
        Font = new("Segoe UI", 10, FontStyle.Bold), Margin = new(0, 0, 12, 10)
    };
    private void ClearPage()
    {
        // Persistent preview/result controls survive navigation; old page containers do not.
        foreach (Control control in new Control[] { preview, choose, analyze, resultTitle, resultDetail, status, tracks })
            control.Parent?.Controls.Remove(control);
        while (content.Controls.Count > 0) { var old = content.Controls[0]; content.Controls.Remove(old); old.Dispose(); }
    }
    private FlowLayoutPanel Page(string eyebrow, string title, string subtitle)
    {
        ClearPage();
        var page = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        page.Controls.Add(TextLabel(eyebrow.ToUpperInvariant(), 9, Accent));
        page.Controls.Add(TextLabel(title, 30));
        var description = TextLabel(subtitle, 11, Muted); description.Width = 820; page.Controls.Add(description);
        content.Controls.Add(page); return page;
    }
    private void ShowHome()
    {
        var page = Page("SENİN ALANIN / KEŞFET", "Bugün hangi ritimdesin?", "Bir fotoğraf seç. Yüz ifadeni keşfet. O ana eşlik edecek müziği bul.");
        var hero = new WavePanel { Width = 940, Height = 115, Margin = new(0, 8, 0, 24), Padding = new(24, 18, 20, 10) };
        hero.Controls.Add(new Label { Text = "Her anın bir melodisi var.", Location = new(24, 18), Size = new(540, 40), Font = new("Segoe UI", 21, FontStyle.Bold), BackColor = Color.Transparent });
        hero.Controls.Add(new Label { Text = "Biraz yavaşla. Kendine kulak ver.", Location = new(26, 68), AutoSize = true, ForeColor = Color.FromArgb(220, 211, 250), BackColor = Color.Transparent });
        page.Controls.Add(hero);
        var row = new FlowLayoutPanel { Width = 950, Height = 365, WrapContents = false, Margin = new(0) };
        var photo = new Panel { Width = 455, Height = 350, BackColor = Card, Padding = new(18), Margin = new(0, 0, 20, 0) };
        var top = TextLabel("01   FOTOĞRAFIN", 10, Muted); top.Dock = DockStyle.Top;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new(0, 12, 0, 0) };
        choose.Width = 165; analyze.Width = 225; actions.Controls.Add(choose); actions.Controls.Add(analyze);
        photo.Controls.Add(preview); photo.Controls.Add(top); photo.Controls.Add(actions);
        if (preview.Image is null) { preview.Paint -= EmptyPreview; preview.Paint += EmptyPreview; }
        var result = new Panel { Width = 465, Height = 350, BackColor = Card, Padding = new(22), Margin = new(0) };
        var resultStack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        resultStack.Controls.Add(TextLabel("02   İFADE ANALİZİ", 10, Muted));
        resultTitle.Width = 415; resultDetail.Width = 415; resultDetail.Height = 62;
        resultStack.Controls.Add(resultTitle); resultStack.Controls.Add(resultDetail);
        var note = TextLabel("Bu sonuç yüz ifadesinin model tahminidir; gerçek ruh halini kesin olarak göstermez.", 10, Muted); note.Width = 415; note.Height = 64;
        resultStack.Controls.Add(note); status.Width = 415; status.Height = 88; resultStack.Controls.Add(status);
        result.Controls.Add(resultStack); row.Controls.Add(photo); row.Controls.Add(result); page.Controls.Add(row);
        page.Controls.Add(TextLabel("Sana eşlik etsin", 20));
        tracks.Dock = DockStyle.None; tracks.Width = 940; tracks.Height = 215;
        page.Controls.Add(tracks);
        RenderTracks(catalog.Where(t => t.Mood == currentMood));
    }
    private void EmptyPreview(object? sender, PaintEventArgs e)
    {
        if (preview.Image is not null) return;
        TextRenderer.DrawText(e.Graphics, "＋\nFotoğrafını buraya ekle\nJPG veya PNG · en fazla 20 MB", Font, preview.ClientRectangle, Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }
    private void ChooseImage()
    {
        using var dialog = new OpenFileDialog { Filter = "Fotoğraf|*.jpg;*.jpeg;*.png", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 20 * 1024 * 1024) throw new InvalidOperationException("En fazla 20 MB boyutunda bir fotoğraf seçin.");
            using var source = Image.FromFile(dialog.FileName);
            if ((long)source.Width * source.Height > 40_000_000) throw new InvalidOperationException("Fotoğraf çok büyük. Çözünürlüğü küçültün.");
            var copy = new Bitmap(source); var old = preview.Image; preview.Image = copy; old?.Dispose();
            selectedImage = dialog.FileName; currentMood = null; analyze.Enabled = true;
            resultTitle.Text = "Analize hazır"; resultDetail.Text = "Fotoğrafın seçildi. Analizi başlatabilirsin.";
            status.Text = "Fotoğraf yalnızca bu bilgisayarda işlenir."; RenderTracks([]);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Fotoğraf açılamadı"); }
    }
    private async Task Analyze()
    {
        if (selectedImage is null || busy) return;
        busy = true; analyze.Enabled = choose.Enabled = false;
        resultTitle.Text = "İfaden inceleniyor…"; resultDetail.Text = "Model hazırlanıyor. Bu işlem biraz sürebilir."; status.Text = "Analiz sürüyor…";
        try
        {
            var result = await detector.AnalyzeAsync(selectedImage, lifetime.Token);
            if (IsDisposed) return;
            resultTitle.Text = MoodName(result.Mood);
            currentMood = result.Mood;
            resultDetail.Text = $"Model güven skoru: %{result.Confidence * 100:0}\nBu ana uygun öneriler aşağıda.";
            sessionHistory.Insert(0, new(DateTime.Now, result.Mood, result.Confidence));
            RenderTracks(catalog.Where(t => t.Mood == result.Mood));
            status.Text = "Analiz tamamlandı. Misafir geçmişi bu oturumda tutulur.";
            if (account is not null)
            {
                try { await repository.SaveAsync(account.Id, result); status.Text = "Analiz tamamlandı ve hesabına kaydedildi."; }
                catch (Exception) { status.Text = "Analiz tamamlandı; veritabanına kaydedilemedi. Bağlantını kontrol et."; }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) { resultTitle.Text = "Analiz tamamlanamadı"; resultDetail.Text = "Fotoğrafı ve Python kurulumunu kontrol et."; status.Text = ex.Message; RenderTracks([]); } }
        finally { busy = false; if (!IsDisposed) analyze.Enabled = choose.Enabled = true; }
    }
    private static string MoodName(string mood) => mood switch { "positive" => "Pozitif bir ifade", "negative" => "Hüzünlü bir ifade", _ => "Nötr bir ifade" };
    private void RenderTracks(IEnumerable<Track> selection)
    {
        while (tracks.Controls.Count > 0) { var old = tracks.Controls[0]; tracks.Controls.Remove(old); old.Dispose(); }
        var list = selection.ToList();
        if (list.Count == 0) { tracks.Controls.Add(TextLabel("Analizden sonra müzik önerilerin burada görünecek.", 11, Muted)); return; }
        foreach (var track in list)
        {
            var panel = new Panel { Width = 910, Height = 62, BackColor = Card, Margin = new(0, 0, 0, 8) };
            panel.Controls.Add(new Label { Text = "♫", Location = new(16, 12), Size = new(40, 38), Font = new("Segoe UI", 22), ForeColor = Accent });
            panel.Controls.Add(new Label { Text = track.Title, Location = new(64, 8), Size = new(410, 24), Font = new("Segoe UI", 11, FontStyle.Bold) });
            panel.Controls.Add(new Label { Text = track.Artist + "  ·  " + track.Genre, Location = new(64, 34), Size = new(410, 22), ForeColor = Muted });
            var listen = Button("YouTube'da ara  ↗"); listen.SetBounds(704, 9, 190, 42);
            listen.Click += (_, _) => { try { Process.Start(new ProcessStartInfo("https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(track.Artist + " " + track.Title)) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            panel.Controls.Add(listen); tracks.Controls.Add(panel);
        }
    }
    private void ShowCollection()
    {
        var page = Page("MÜZİK / KOLEKSİYON", "Biraz ilham, biraz müzik.", "Başlangıç koleksiyonu · İfade kategorisine göre seçilmiş öneriler. Dinlemek için tarayıcıda ara.");
        tracks.Dock = DockStyle.None; tracks.Width = 940; tracks.Height = 520; RenderTracks(catalog); page.Controls.Add(tracks);
    }
    private async Task ShowHistory()
    {
        var page = Page("SENİN ALANIN / GEÇMİŞ", "Anlarının izleri.", account is null ? "Bu misafir oturumunda tamamlanan analizler." : "Hesabına kaydedilen son 100 analiz.");
        var message = TextLabel("Geçmiş yükleniyor…", 11, Muted); page.Controls.Add(message);
        busy = true;
        try
        {
            var history = account is null ? sessionHistory : await repository.HistoryAsync(account.Id);
            if (IsDisposed || page.IsDisposed) return;
            message.Text = history.Count == 0 ? "Henüz analiz yok. Keşfet ekranından bir fotoğraf ekle." : $"{history.Count} tamamlanan analiz";
            foreach (var entry in history)
            {
                var label = TextLabel($"{entry.CreatedAt:dd MMM yyyy · HH:mm}     {MoodName(entry.Mood)}     %{entry.Confidence * 100:0}", 12);
                label.Width = 900; label.Height = 58; label.BackColor = Card; label.Padding = new(15); page.Controls.Add(label);
            }
        }
        catch (Exception) { if (!message.IsDisposed) message.Text = "Geçmiş yüklenemedi. SQL bağlantısını ve tablo kurulumunu kontrol edin."; }
        finally { busy = false; }
    }
    private void ShowAccount()
    {
        var page = Page("SENİN ALANIN / HESAP", account is null ? "Hoş geldin." : $"Merhaba, {account.Name}.", "Analiz geçmişini hesabında sakla ve kaldığın yerden devam et.");
        if (account is not null)
        {
            var logout = Button("Oturumu kapat"); logout.Click += (_, _) => { account = null; sessionHistory.Clear(); accountLabel.Text = "Misafir oturumu"; ShowAccount(); }; page.Controls.Add(logout); return;
        }
        if (!repository.Configured)
        {
            var info = TextLabel("Misafir olarak fotoğraf analizi yapabilirsin.\nHesap ve kalıcı geçmiş için SQL bağlantısı henüz yapılandırılmadı.\nKurulum adımları proje README dosyasında.", 12, Muted);
            info.Width = 850; info.Height = 120; page.Controls.Add(info); return;
        }
        var name = new TextBox { PlaceholderText = "Adın (yeni hesap için)", Width = 410, MaxLength = 100 };
        var email = new TextBox { PlaceholderText = "E-posta adresin", Width = 410, MaxLength = 254 };
        var password = new TextBox { PlaceholderText = "Parolan (en az 10 karakter)", Width = 410, UseSystemPasswordChar = true, MaxLength = 256 };
        foreach (var box in new[] { name, email, password }) { box.Font = new("Segoe UI", 13); box.Margin = new(0, 10, 0, 12); page.Controls.Add(box); }
        var login = Button("Giriş yap", true); var register = Button("Hesap oluştur");
        var error = TextLabel("", 11, Muted); error.Width = 820; error.Height = 100;
        page.Controls.Add(login); page.Controls.Add(register); page.Controls.Add(error);
        async Task Submit(bool create)
        {
            busy = true; login.Enabled = register.Enabled = false;
            try
            {
                account = create ? await repository.RegisterAsync(name.Text, email.Text, password.Text) : await repository.LoginAsync(email.Text, password.Text);
                if (IsDisposed) return;
                if (account is null) { error.Text = "E-posta veya parola hatalı."; return; }
                sessionHistory.Clear(); accountLabel.Text = account.Name; ShowHome();
            }
            catch (Exception ex) { if (!error.IsDisposed) error.Text = ex is ArgumentException ? ex.Message : "Bağlantı kurulamadı. SQL ayarlarını ve tablo kurulumunu kontrol edin."; }
            finally { busy = false; if (!login.IsDisposed) login.Enabled = register.Enabled = true; }
        }
        login.Click += async (_, _) => await Submit(false); register.Click += async (_, _) => await Submit(true);
    }
}

internal sealed class WavePanel : Panel
{
    public WavePanel() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var gradient = new LinearGradientBrush(ClientRectangle, Color.FromArgb(86, 62, 151), Color.FromArgb(39, 40, 75), 0f);
        e.Graphics.FillRectangle(gradient, ClientRectangle); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(100, 191, 163, 255), 2);
        for (var row = 0; row < 7; row++)
        {
            var points = Enumerable.Range(0, 130).Select(i => new PointF(570 + i * 3, 48 + row * 7 + (float)Math.Sin(i / 16.0 + row * .38) * (20 + row * 2))).ToArray();
            e.Graphics.DrawLines(pen, points);
        }
    }
}
