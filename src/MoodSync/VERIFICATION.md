# Doğrulama · 5 Eylül 2026

- .NET 8.0.424 SDK ile Windows Release derleme ve yayınlama başarılı. Derleme: 0 hata, 0 uyarı.
- Windows Forms ana ekranı gerçek uygulamadan render edildi ve görüntüsü incelendi. Sol marka başlığındaki taşma giderildi.
- Python unittest: 3 test geçti (tek geçerli yüz, yüz bulunmaması/düşük güven/bilinmeyen etiket, çoklu yüz).
- C# kontrolleri geçti: rastgele tuz, doğru/yanlış parola, bozuk parola özeti, Python JSON hata aktarımı, iptal.
- Gerçek C# → Python → YOLO → JSON entegrasyonu: kaynak happy-photo.jpg dosyasıyla `positive`, güven 0.869930.
- Kaynak sad-woman.jpg dosyasıyla doğrudan model: `negative`, güven 0.883277.
- Kaynak neutral.jpg dosyası güven eşiğini geçmedi; uygulama bunu nötr kabul etmek yerine analiz hatası olarak ele aldı.

## Doğrulanamayanlar / kapsam

- SQL Server örneği ve bağlantısı verilmediğinden hesap oluşturma, giriş ve kalıcı geçmiş canlı veritabanında test edilmedi.
- Her ekranın manuel etkileşim ve farklı DPI/ekran çözünürlüğü testi tamamlanmadı; ana ekran 1320×850 render ile incelendi.
- Model yeniden eğitilmedi; bu örnekler doğruluk veya tarafsızlık ölçümü değildir.
- Eski veritabanının aktarımı, eski tercih puanlama sistemi ve öğrenen müzik önerileri bu sürümde uygulanmadı. Müzik koleksiyonu açıkça belirtilmiş sabit kategori eşleştirmesidir.
- Kamera ve metin analizi eklenmedi; mevcut kaynakta kullanılan fotoğraf yükleme akışı yenilendi.

Bu depodaki kaynak kod başka bilgisayarda README adımlarıyla kurulabilir. Yerel çalıştırılabilir teslim ve bilgisayara özel Python ayarları depoya dahil edilmemiştir. `python/requirements-lock.txt` test sırasında kullanılan tam paket sürümlerini içerir.
