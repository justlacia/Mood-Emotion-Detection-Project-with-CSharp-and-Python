# MoodSync — Yenilenen Bitirme Projesinin Teknik Kurulumu

Türkçe Windows Forms arayüzü, Python / YOLO yüz ifadesi analizi ve isteğe bağlı SQL Server hesap/geçmiş kaydı.

Bu klasör, depodaki eski bitirme projesi uygulamasının yenilenen sürümünü içerir. Projenin amacı, eski–yeni sürüm karşılaştırması ve geliştirme planı için [ana README](../../README.md) dosyasını okuyun. Aşağıdaki komutlar bu klasörden çalıştırılmalıdır.

## Çalıştırma

1. Windows üzerinde .NET 8 SDK ve Python 3.12 kurulu olmalı.
2. Bu klasörde `python -m venv python/.venv` çalıştırın.
3. `python/.venv/Scripts/python.exe -m pip install -r python/requirements-lock.txt` çalıştırın (test edilen tam sürümler).
4. `appsettings.json` içindeki `PythonExecutable` değerini oluşturulan sanal ortamın `python.exe` dosyasının **tam yoluna** ayarlayın. JSON içinde ters eğik çizgileri çift yazın veya `/` kullanın.
5. `dotnet run --project MoodSync.csproj` çalıştırın. Visual Studio ile de `MoodSync.csproj` dosyasını açabilirsiniz.

Model `python/models/model.pt` konumundadır ve kaynak projeden alınmıştır. Uygulama modeli yerelde çalıştırır; fotoğraf dışarıya yüklenmez. İlk çalıştırmada model yüklenmesi biraz sürebilir. Önce misafir modunda, tek yüz içeren JPG/PNG ile deneyin.

## SQL Server

Misafir modu veritabanı olmadan çalışır; geçmiş yalnızca oturum belleğindedir. Kalıcı kayıt ve giriş için:

1. SQL Server'da kullanılacak veritabanını seçin ve `database/schema.sql` dosyasını bu veritabanında çalıştırın.
2. `appsettings.json` içindeki `ConnectionString` değerini kendi sunucu ve veritabanınıza göre ayarlayın. Windows kimlik doğrulaması örneği: `Server=localhost;Database=MoodSync;Integrated Security=True;Encrypt=True;TrustServerCertificate=False`. Sunucu sertifikası doğrulanabilir olmalıdır.
3. Uygulamayı yeniden açın; **Hesabım** ekranından hesap oluşturun.

Yerel özel ayarlar için çalıştırılan uygulamanın yanına `appsettings.local.json` koyabilirsiniz; mevcut üç ayarı da içermelidir. Bu dosya Git dışında tutulur.

Yeni tablolar `MoodSyncAccounts` ve `MoodSyncHistory` adlarını kullanır. Eski Users/MoodHistory tabloları değiştirilmez; eski hesapların otomatik aktarımı yoktur. Kaynakta veritabanı yedeği olmadığı için gerçek kullanıcı verileri taşınmamıştır.

## Tasarım ve davranış

- Lacivert/mor arayüz, fotoğraf önizlemesi, analiz durumları, müzik koleksiyonu, hesap ve geçmiş ekranları.
- Python çıktısı JSON üzerinden okunur. Zaman aşımı ve uygulama kapanışında işlem iptali vardır.
- Yüz bulunamaması, düşük güven ve birden fazla yüz sonuç olarak kaydedilmez.
- SQL komutları parametreli; bağlantılar işlem bitiminde serbest bırakılır.
- Parolalar rastgele tuz ve 600.000 tur PBKDF2-SHA256 ile özetlenir.
- Fotoğraf SQL'e kaydedilmez; yalnızca ifade kategorisi, güven skoru ve zaman saklanır.
- Müzik önerileri bu sürümde sabit başlangıç koleksiyonundan kategoriye göre seçilir; öğrenen kişiselleştirme değildir. YouTube arama düğmesi tarayıcı açar; uygulama içi oynatıcı değildir.
- Yüz ifadesi tahmini kişinin gerçek ruh halinin veya sağlık durumunun ölçümü değildir. Modelin doğruluğu bu çalışma kapsamında yeniden eğitilip ölçülmemiştir.

## Kontroller

`python -m unittest discover -s python -v`

`dotnet build MoodSync.csproj`

`dotnet run --project tests/Checks.csproj`

Gerçek model entegrasyonunu kontrol etmek için test komutuna `-- <python.exe-tam-yolu> <pozitif-ornek.jpg-tam-yolu>` eklenebilir.

Görsel kontrol çıktısı: `MoodSync.exe --render-preview C:/tam/yol/preview.png`

## Kaynak

Bu yenileme https://github.com/justlacia/Mood-Emotion-Detection-Project-with-CSharp-and-Python deposunun `3d8a708d0ca123147e29e4c5b2b516d089d97b5f` sürümü incelenerek oluşturuldu. Eğitimli model bu depodan korunmuştur. Eski kaynaklar depo kökünde, önceki açıklama `docs/README-legacy.md` içinde ve commit geçmişinde korunur. Depo, elifftosunn tarafından yayımlanan önceki çalışmadan çatallanmıştır; model bu yenileme sırasında yeniden eğitilmemiştir.
