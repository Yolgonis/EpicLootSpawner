# 1.3.2 üretici verisi kontrolü · 2026-10-10

Spawner.BaseItem yeni eşya kopyasında m_crafterID=0L ve m_crafterName="" atar. Native Valheim ItemDrop.ItemData Clone, tooltip ve Serialize/Deserialize metotları kurulu assembly_valheim.dll üzerinden incelendi. Tooltip üretici satırını yalnız m_crafterID!=0 koşuluyla yazar; aynı koşul kayıt bayrağına girer. Sıfır üretici kimliği kaydedildiğinde yükleme üretici adı boş, kimlik sıfır olarak döner. Ortak oluşturma yolu mevcut önizleme, Legendary/set, envanter/yere teslim ve çoklu üretimde kullanılır.

Üretici bilgisi taşıyan prefab fixture'ı yeni üretimde temizlenir; prefabın kendisi değiştirilmez. 8 yeni üretim kontrolü eklendi: 987 kaynak = 441 temel/dosya/geometri + 338 davranış + 180 üretim + 28 panel. 9 paketleme ve 16 izole kurulum kontrolü. Global tooltip yaması eklenmedi; mevcut envanter eşyalarında geçmiş üretici bilgisi topluca silinmez. Native oyun kabulü Pending kalır.

# 1.3.1 kod incelemesi · 2026-10-09

1.3.0'ın bütün üretim C# dosyaları ile derleme, arşiv doğrulama, kurulum ve izole test akışları incelendi. Son kaynakta 48 üretim C# dosyası bulunur. Bu inceleme yeni özellik kapsamı açmadan somut hataları düzeltir; oyun içinde ölçülmemiş davranışları doğrulanmış saymaz.

| Bulgu | Düzeltme | Regresyon kanıtı |
| --- | --- | --- |
| Eksik Version veya Presets/Prefabs alanı varsayılanlarla boş geçerli dosya sayılıyordu; sonraki kayıt eski dosyayı değiştirebiliyordu. | Zorunlu alanlar açıkça tanımlandı; başarısız okuma Ready=false bırakır, eski bellek ve dosya korunur. Favori JSON okuma ayarları sınırlandırıldı. | Eksik/null alan, yazma engeli, dosya baytlarının ve eski belleğin korunması, eski şablon ve boş favori listesi. |
| NaN/sonsuz/ters tanımlı aralıklarda sayı karşılaştırmaları sınırı uygulamayabiliyordu; sonsuz Increment kaydırıcıdan NaN üretebiliyordu. | Yapılandırma aralığı sonlu ve sıralı olmalı; alan hatası ve üretim reddi. Hatalı aralıkla slider/min/max çizilmez; sonsuz adım uygulanmaz. | Saf sınır kontrolleri, gerçek Spawner özel aralık/Build/FieldErrors ve bozuk değerlerle kimlik seçiminin tasarımı koruması. |
| Şablon yükleme kimliği tasarım doğrulanmadan değişiyordu. | Şablon önce detached Import ile doğrulanır; kayıt/etiket/envanter seçimi başarıdan sonra değişir. | Gerçek LoadPreset metodu ve başarısız import fixture'ı, başarılı seçim ve üretim moduna geçiş. |
| RestoreDraft null efekt girdisinde mevcut alanları değiştirip yarım kalabiliyordu. | Yapısal doğrulama ve listelerin kopyası canlı alanlara yazmadan önce tamamlanır. | Gerçek Spawner null taslak/efekt/taş, önceki tasarımın tamamının korunması; eksik sayı girdisinin saklanması. |
| Eksik garantili efekt tanımı sabit güç korumasını atlatabiliyordu; null/tekrarlı tanımlar native aralık hesabını bozabiliyordu. | Özel kimlik ve garantili güç tanımı önce doğrulanır; geçersiz kayıtlar katalogdan çıkarılır. Aralık kartları ve düzenleme alanı anlaşılır kimlik hatası gösterir. | Gerçek Spawner boş güçle import bypass reddi, Build, katalog, FieldErrors, null güç/listeler, yinelenmiş güç ve yapılandırma düzeltildikten sonra toparlanma. |
| Hatalı format yer tutucusu isim/açıklama kartlarını bozabiliyordu; yeni önizleme hata verirse eski alan hataları kalabiliyordu. | FormatException için okunabilir geri dönüş; yeni kontrol öncesi alan hataları temizlenir. | Gerçek EffectDisplay ve katalog kodu; bozuk başlık/açıklama, sağlam kartlar ve özel aralık yapılandırma hatası. |

## Kontrol kapsamı

- Üretim/adet, oyun kuralları, kalite, efekt uyumluluğu, özel kimlik, soketler ve envantere/yere teslim.
- Mevcut eşyanın kimliğini, yabancı mod verilerini, augment/temper indekslerini ve dayanıklılık oranını koruyan düzenleme.
- Büyü kaldırma/tek taş çıkarma, toplu envanter alanı hesabı, malzeme iadeleri, işlem geri alma, dış değişiklik kontrolü ve başarısız yazımda geri dönüş.
- Şablonlar, favoriler, son taslak, pano/JSON aktarımı, koleksiyon çakışmaları, yedek kurtarma ve 60 durumluk geçmiş.
- IMGUI olayları, eylemlerin Update'e ertelenmesi, pencere/ölçek geometrisi, sürükleme, modallar, dil/metin önbellekleri, önizleme ve tanılama.
- Sürüm/bağımlılık eşleşmesi, kaynak snapshot'ı, deterministik paketler, SHA256 denetimi, mevcut paketleri koruma, kurulum yedekleri ve stale-plan/çift DLL engelleri.

## Doğrulama ve sınırlar

Kaynak testleri: **979 PASS** = 441 temel/dosya/geometri + 338 davranış + 172 üretim + 28 panel. **88 yeni regresyon**. İki bağımsız Release derlemesinde gerçek Valheim/EpicLoot bağımlılıkları kullanılır; 9 paketleme ve 16 izole kurulum kontrolü uygulanır. Derleme uyarı/hata sonucu RELEASE/BUILD-REPORT kanıtında saklanır.

Başsız testler Unity/EpicLoot API bağlantılarının bir kısmını fixture ile temsil eder; gerçek IMGUI çizimi, ekipman görseli ve giyilen setin oyun içi bonusunu ölçmez. GameplayValidation=Pending. Oyun yeniden başlatılmadı ve kurulu profile yazılmadı. Sonraki normal oturum kontrolleri TESTPLAN.md içindedir.

Runestone, oturumlar arası kalıcı envanter geri alma, başka EpicLoot sürümleri ve sunucu yetkilendirmesi mevcut kapsam dışında kalır. Geçerli normal efekt/soket/adet sınırları korunur; kalite üst sınırı yalnız bilgi olarak gösterilir.
