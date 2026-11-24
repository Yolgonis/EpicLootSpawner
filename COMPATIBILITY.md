# Uyumluluk ve oyun kanıtı

1.3.0 eklemesi: Özel Legendary/set kimliği seçerek üretim; mevcut özel eşyayı kimliği korunarak düzenleme; native özel değer aralıkları, set bonusları ve kalıcı tasarım kimlikleri.

Güncel paket: **1.4.1**. Limitsiz mod açıkken EpicLoot'un efekt içi tavanları da kalkar; aralık dışı değerler oyunda etki eder. 1.4.0: Arayüz tek sayfalık tasarım ekranına dönüştü; isteğe bağlı Limitsiz mod eklendi. Önceki: Yeni üretilen eşyalarda Crafted by satırı gösterilmez. Genel inceleme CODE-REVIEW.md içindedir. Kalite normal üst sınırı bilgi olarak görünür; efekt/soket/adet sınırları korunur. Önceki 1.0.2 büyü kaldırma için kullanıcı olumlu oyun sonucu bildirdi (2026-10-09); bu kabul yeni kaynak için devralınmaz. GameplayValidation=Pending. Runtime README yalnızca İngilizce MOD-PAGE.md özellik listesidir.

1.4.1 otomatik doğrulaması: 483 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel; toplam 1056 kaynak kontrolü. Ayrıca tests/LiveCapChecks, derlenen DLL'i kurulu EpicLoot 0.14.13, Harmony ve oyun DLL'leriyle yükler. 14 tavan yamasını gerçekten uygular ve 10 hedefi Limitsiz kapalı/açık çağırır. Valheim tiplerine dokunan 4 hedef .NET Framework'te JIT edilemez; bunların varlığı, imzası ve Mathf.Clamp01 çağrısı Mono.Cecil ile denetlenir. Tavanların oyundaki etkisi Pending.

1.4.0 otomatik doğrulaması: 441 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel; toplam 1014 kaynak kontrolü. 9 paketleme ve 16 izole kurulum kontrolü ayrıdır. Native oyun çizimi ve gerçek set bonusları sonraki normal oturumda kontrol edilir.


Son yerel inceleme: 2026-10-09. Kullanıcının “Büyü kaldırma düzgün çalışıyor” geri bildirimi önceki 1.0.2 özelliğine aittir; ayrıntılı test maddelerinin tümünü veya yeni 1.2.0 arayüzünü doğrulamaz. `tests/evidence/UserFeedback-1.0.2.json` tarih, tam bildirim ve doğrulanan eski ZIP hash'ini saklar. Eski release arşivleri değişmez; yeni paketin gameplay durumu Pending olarak kalır.

| Bileşen | İncelenen yerel sürüm | Kanıt / durum |
| --- | --- | --- |
| Valheim | 1.0.16 | Kurulu assembly_valheim.dll içindeki Version.CurrentVersion incelendi; bu DLL'lerle derlendi. |
| EpicLoot | 0.15.6 (1.5.0 ile) | Kurulu BepInPlugin değeri 0.15.6. Spawner'ın kullandığı 114 tip/metot/alan ve 14 tavan hedefi Cecil ile 0.14.13 ve 0.15.6 DLL'lerine karşı karşılaştırıldı; 0.15.6'daki üç fark (ItemInfo/Data() kaldırıldı, TryCreateDecreasedCooldownMead → ModifyRestoringMead) 1.5.0'da giderildi. 1.4.1 ve öncesi 0.14.13 içindir. |
| BepInEx | 5.4.23.5 | Kurulu core DLL dosya sürümü; derleme referansı. |
| Jotunn | 2.30.2 | Kurulu DLL dosya sürümü; derleme referansı. |
| Unity | 6000.0.75f1 | Kurulu UnityPlayer ürün sürümü; IMGUI minimum/maksimum yükseklik davranışı incelendi. |
| Derleme | net481, .NET SDK 8.0.423 | Release derlemesi, bağlı kaynak testleri ve paket hash denetimi. |

Bu tablo yerel derleme ortamını kaydeder. Kullanıcının genel olumlu geri bildirimi, tüm dil/çözünürlük ve oyun akışlarının ayrı ayrı test edildiği anlamına gelmez. Başka Valheim/EpicLoot sürümleri doğrulanmadı. EpicLoot DLL FileVersion/ProductVersion değeri 0.13.0 kalmış olabilir; modun gerçek bildirdiği sürüm 0.14.13'tür.

Spawner client tarafındadır; kendi ağ gereksinimi NotEnforced/None olarak tanımlıdır. EpicLoot'un kendi oyuncu/sunucu bağımlılıkları devam eder. Uzak ağ sahipliğinde Lumberjack bonusu garanti edilmez.

## Kanıt ayrımı

- 1.2.0 otomatik testleri: 384 temel/geometri/dosya, 297 davranış, 102 üretim, 28 panel kontrolü; toplam 811. Unity ve bazı API bağlantıları taklit edilir.
- 1.2.0 paketleme: 9 akış kontrolü; iki bağımsız üretimde DLL, manifest ve iki ZIP hash'i eşleşir. Eksik bağımlılık, sürüm farkı, mevcut çıktı, bozuk ZIP, kabul kaydına uymayan kod/sürüm/bağımlılık, kabul kaydı bulunan önceki 1.0.0 akışındaki dört ek kontrolde reddedildi.
- Kurulum: 16 kontrol geçici örnek profilde çalıştırılır; ilk kurulum/güncelleme, önceki DLL yedeği, config koruma, ikinci DLL ve değişen hedef/bozuk paket denetlenir. Gerçek kullanıcı profili değiştirilmez; CLI oyun açıkken -Apply işlemini reddeder.
- Kurulu DLL incelemesi: API imzaları (RemoveShard/ReconstructShardItem, Inventory.RemoveItem), dayanıklılık Sync/Revert, Inventory/DropItem kopyalama sözleşmesi ve Unity kaydırma davranışı.
- Oyun geri bildirimi: 0.4.0/0.4.1 için çalışma; 0.9.1 ve 0.9.3 için olumlu görünüm/liste geri bildirimi; 0.9.4 sonrasında “her şey düzgün”. 2026-10-09 tarihinde önceki 1.0.2 büyü kaldırma için “Büyü kaldırma düzgün çalışıyor” bildirildi. Ayrıntılı checklist maddeleri tek tek onaylanmış değildir.
- Başsız ekran geometrisi kontrolleri 640×480–3840×2160 ve %75–150 ölçek hesabını kapsar; tüm bu kombinasyonlarda canlı menü sonucu kaydedilmemiştir.
- Türkçe/İngilizce oyun metinleri Localization üzerinden alınır; her iki dilde ayrı canlı sonuç kaydedilmemiştir.

## Sonraki doğrulama ve dağıtım

TESTPLAN.md sonraki normal oyun oturumlarında ayrıntılı sonuç kaydı için kullanılabilir; yalnızca sürüm etiketinin değiştiği 1.0.0 için yeniden oyun açılması istenmez. Yeni oyun/mod sürümlerinde inceleme ve test yapılmadan destek kapsamı genişletilmez. Harici dağıtım için lisans, paket adı/yazar/bağımlılık metadata ve yayın hedefi kullanıcıyla kararlaştırılır; yerel paket hazırlığı harici yayın anlamına gelmez.

Efekt/soket/adet limitleri ve yalnızca uyumlu efektler korunur; kalite üst sınırı bilgi olarak gösterilir. Tek işlem geri alma oturum içindedir; oturumlar arası kalıcı işlem geçmişi, runestone ve sunucu yetkilendirmesi mevcut kapsamda değildir. Özel Legendary/set kimliği 1.3.0 ile üretim ve envanter düzenlemesinde desteklenir; yalnız yüklü, uygun native tanımlar kullanılabilir.
