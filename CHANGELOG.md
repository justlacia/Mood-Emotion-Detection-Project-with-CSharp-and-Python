# Değişiklik Geçmişi

## 2026-09-05 — Bitirme projesinin modernizasyon güncellemesi

Önceki C# / Python yüz ifadesi ve müzik öneri uygulamasını temel alan yeni sürüm `src/MoodSync` altında eklendi. Eski kaynaklar ve commit geçmişi korundu.

### Eklenenler

- .NET 8 Windows Forms proje yapısı.
- Türkçe lacivert/mor arayüz, fotoğraf önizlemesi, koleksiyon, hesap ve geçmiş ekranları.
- JSON üzerinden C#–Python iletişimi, süre sınırı ve iptal işleme.
- Misafir modu, yeni SQL hesap/geçmiş tabloları ve parametrik veri erişimi.
- Tuzlanmış PBKDF2 parola saklama.
- Python ve C# kontrolleri, test edilen bağımlılık sürümleri ve ekran görüntüsü.
- Bitirme projesi bağlamını ve önceki sürümle ilişkisini açıklayan README.

### Düzeltilenler

- Bilgisayara sabitlenmiş model/betik yolları yerine uygulama konumuna bağlı yollar.
- Ortak `output.txt` dosyasından eski/boş sonuç okunması riski.
- Analiz sırasında arayüzü bekleten senkron süreç akışı.
- Yüz bulunamamasının otomatik olarak nötr kabul edilmesi.
- Düşük güven ve çoklu yüz için belirsiz davranış.

### Henüz tamamlanmayanlar

Canlı SQL doğrulaması, eski veri ve tercih puanlarının aktarımı, öğrenen müzik kişiselleştirmesi ve kapsamlı model değerlendirmesi. Müzik önerileri bu sürümde sabit kategori eşleştirmesi kullanır. Model önceki sürümden korunmuştur.
