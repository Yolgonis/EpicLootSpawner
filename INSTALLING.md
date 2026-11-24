# Kurulum, güncelleme ve geri dönüş

1.3.0 eklemesi: Özel Legendary/set kimliği seçerek üretim; mevcut özel eşyayı kimliği korunarak düzenleme; native özel değer aralıkları, set bonusları ve kalıcı tasarım kimlikleri.

Güncel paket: **1.4.1**. Limitsiz mod açıkken EpicLoot'un efekt içi tavanları da kalkar; aralık dışı değerler oyunda etki eder. 1.4.0: Arayüz tek sayfalık tasarım ekranına dönüştü; isteğe bağlı Limitsiz mod eklendi. Önceki: Yeni üretilen eşyalarda Crafted by satırı gösterilmez. Genel kod incelemesi ve bakım düzeltmeleri CODE-REVIEW.md içindedir. Kalite üst sınırı bilgi olarak görünür; efekt/soket/adet sınırları korunur. GameplayValidation=Pending. Runtime README yalnızca İngilizce MOD-PAGE.md özellik listesidir.


Spawner client tarafındadır. Mevcut hedef EpicLoot 0.14.13'tür; EpicLoot'un BepInEx/Jotunn ve diğer bağımlılıklarını kendi kurulumuna göre koru. 1.0.0, olumlu kullanıcı kontrolü alınan 0.9.4 ile aynı oyun davranışını kullanır; COMPATIBILITY.md kanıtları ve kapsamı kaydeder.

## Elle kurulum

Oyunu kapat. Kurulum ZIP'indeki `BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll` dosyasını kullandığın profilin aynı konumuna kopyala. Mod yöneticisi kullanıyorsan Steam klasörü yerine aktif profil kökünü kullan. Önceki DLL'i plugins klasörü dışında saklayabilirsin. Aynı modun eski/yeniden adlandırılmış DLL'ini plugins altında ikinci kopya olarak bırakma.

Menü dünyaya girdikten sonra F8 ile açılır. İlk açılışta katalog henüz hazır değilse panel açılmaz ve giriş engellenmez; hazır olunca F8 ile tekrar dene. Beklenmedik menü hatasında panel kapanır; hata BepInEx günlüğüne kaydedilir ve sonraki F8 açılışında yeniden denenebilir. Geçersiz taslak için panel açık kalır; yeni tasarım seçebilirsin.

## Doğrulamalı kurulum aracı

PowerShell 7.2 veya sonrası gerekir. Kurulum ZIP'ini herhangi bir çalışma klasörüne aç; araç bu klasörün `scripts` altındadır. Komutta **PackagePath özgün ZIP dosyası**, ProfilePath aktif profil kökü olmalıdır. Örnek:

```powershell
pwsh -NoProfile -File .\scripts\Install-Release.ps1 `
  -PackagePath 'C:\Mods\EpicLootSpawner-1.4.1.zip' `
  -ProfilePath "$env:APPDATA\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Sini"
```

Bu komut önizleme yapar; profile yazmaz. Paket/manifest/DLL hash'lerini, gerçek EpicLoot sürümünü, BepInEx/Jotunn dosyalarını ve ikinci spawner DLL'lerini kontrol eder. Hedef dosya ile yeni kurulum/güncelleme bilgisini gösterir. Oyunu kapattıktan sonra aynı komuta `-Apply` eklemek DLL'i kurar.

Güncellemede önceki DLL `BepInEx/config/EpicLootSpawnerBackups/<tarih-kimlik>/EpicLootSpawner.dll` altında saklanır. Yeni DLL önce geçici dosyaya çıkarılır; sürüm/hash kontrolünden sonra atomik değiştirme yapılır. Paket veya mevcut DLL önizleme sonrası değişirse kurulum durur. İkinci kopya bulunduğunda konumlar gösterilir; aracın eski DLL'leri otomatik silmesi yoktur. Bağlantılı kurulum yolları bu araçta desteklenmez; böyle bir düzende elle kurulum kullanılabilir.

Araç yalnızca spawner DLL'ini ve varsa önceki DLL yedeğini yazar. Şablon, favori, taslak, config, karakter ve dünya verilerini değiştirmez. Eski manifest içermeyen ZIP'ler elle kurulabilir; araç için yeni manifestli paket gerekir.

## Veri dosyaları

| Dosya | İçerik | Koruma / kurtarma |
| --- | --- | --- |
| BepInEx/config/local.epicloot.spawner.cfg | Tuş, ölçek, konum, sıralama ve tanılama tercihi | DLL güncellemesinde korunur. |
| BepInEx/config/EpicLootSpawner.presets.json | Şablon ve etiketler | Önceki yazım .bak; ilk v1 geçişinin kalıcı yedeği .v1.bak. |
| BepInEx/config/EpicLootSpawner.favorites.json | Favori prefablar | Önceki yazım .bak. |
| BepInEx/config/EpicLootSpawner.draft.json | Son tasarım ve teslim tercihi | Önceki yazım .bak; asıl envanter nesnesi bağlantısı saklanmaz. |
| BepInEx/config/EpicLootSpawnerExports | Kullanıcının aktardığı koleksiyon dosyaları | Var olan dosyaya sessizce yazılmaz. |

Dosya hatasında Görünüm → Tanılama → Yedekleri ve kurtarmayı aç ile önce yedeği incele. Önizleme yazım yapmaz; onay aktif dosyayı ayrıca .corrupt-… kopyasıyla saklar. Taslak kurtarma paneli kapatır ve sonraki F8'de kurtarılan taslağı yükler. DLL yedeği ile JSON .bak farklı şeylerdir.

## Önceki DLL'e dön

Oyunu kapat. Güncelleme aracının gösterdiği önceki DLL yedeğini mevcut `plugins/EpicLootSpawner/EpicLootSpawner.dll` üzerine kopyala. Config ve JSON dosyalarını otomatik geri çevirmek gerekmez. Özellikle etiketli sürüm 2 şablon deposu, 0.8.0 öncesi sürümlerde okunamayabilir; eski sürüme veri geri dönüşü gerekiyorsa mevcut dosyayı ayrıca saklayıp uygun yedeği bilinçli olarak geri yükle. Envantere uygulanmış değişiklikler DLL geri dönüşüyle geri alınmaz.

## Sorun bildirimi

F8 açılmıyorsa BepInEx günlüğünde `EpicLoot Spawner` başlangıç satırını, aktif profili ve ikinci DLL olasılığını kontrol et. Durum çubuğundaki Ayrıntı veya Görünüm → Tanılama üzerinden Tanılama raporunu kopyala kullan. Rapor oyun/mod sürümü, dil, ekran/ölçek, tasarım/alan durumu ve tam hata ayrıntısını panoya alır. Bildirimde adım sırası ve ilgili günlük hatasını ekle. TESTPLAN.md tek oyun açılışındaki birleşik kontrol listesidir.
