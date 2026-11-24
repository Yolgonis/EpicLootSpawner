# Panel yaşam döngüsü kontrolleri

```powershell
dotnet run --project tests/PanelChecks/PanelChecks.csproj -c Release
```

25 kontrol gerçek PanelLifecycle.cs kaynağını çalıştırır. Oyuncu/dünya/katalog/dil verisi henüz yokken panelin kapalı kalması, katalog hatasında girişin engellenmemesi, bozuk taslakta panelin kullanılabilmesi, kayıt hatalarına rağmen kapanma ve imlecin geri yüklenmesi, modal/iş kuyruğu temizliği, çizim hatasının ertelenmesi, tekrar hatada tek kurtarma, tema temizliği, üretimi durdurma isteği ve yeniden açılış sınanır.

Unity nesneleri, Cursor, katalog yükleme, kayıt ve günlük bağlantıları Harness.cs içinde taklit edilir. Test gerçek Unity OnGUI çizimini veya Input/Harmony patch olaylarını çalıştırmaz; Menu.cs ExitGUIException yolunu ve Plugin.Update olayını canlı oyunda doğrulamak gerekir. Başarılı derleme gerçek oyun/Unity API imzalarını ayrıca kontrol eder.

0.9.4: ayarlanan açma tuşunun hazır-olmama/kurtarma mesajında kullanılması, hata özetinden ayrı nested exception ayrıntısının korunması ve yeni hatada birlikte yenilenmesi için 4 bağlı kaynak regresyonu eklendi. Raporun gerçek pano/GUI çıktısı canlı oyun kontrolüdür.
