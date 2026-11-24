# EpicLoot Spawner yol haritası

1.3.0 eklemesi: Özel Legendary/set kimliği seçerek üretim; mevcut özel eşyayı kimliği korunarak düzenleme; native özel değer aralıkları, set bonusları ve kalıcı tasarım kimlikleri.

Güncel paket: **1.4.1**. Limitsiz mod açıkken EpicLoot'un efekt içi tavanları da kalkar; aralık dışı değerler oyunda etki eder. 1.4.0: Arayüz tek sayfalık tasarım ekranına dönüştü; isteğe bağlı Limitsiz mod eklendi. Önceki: Yeni üretilen eşyalarda Crafted by satırı gösterilmez. Genel kod incelemesindeki kayıt/şablon/aralık/özel tanım sorunları düzeltildi. Kalite üst sınırı bilgi olarak görünür; efekt/soket/adet sınırları korunur. GameplayValidation=Pending. Runtime README yalnızca İngilizce MOD-PAGE.md özellik listesidir.

## Mevcut durum: 1.4.1

1.4.1: Kullanıcı Limitsiz modun "yalnız göstermelik" olduğunu bildirdi. Değerler eşyaya yazılıyordu, ancak EpicLoot bazı efekt toplamlarını kendi kodunda kırpıyordu. Kullanıcı tüm efekt içi tavanların kaldırılmasını seçti. 14 tavan Limitsiz moda bağlı Harmony yamalarıyla kaldırıldı; negatif hasar ve sıfır altı maliyet gibi güvenlik sınırları korundu. 1056 kaynak kontrolü ve 71 canlı EpicLoot kontrolü. Oyun içi etki Pending.

## Önceki: 1.4.0

1.4.0: Tek sayfalık arayüz, açıklamalı efekt/taş/Legendary seçicileri ve isteğe bağlı Limitsiz mod. 1014 kaynak kontrolü (441 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel). Oyun içi görünüm Pending.

## Önceki: 1.3.2

1.3.2: Yeni üretilen eşyalarda üretici kimliği ve adı temizlenir; Crafted by satırı görünmez. 8 yeni üretim regresyonuyla 987 kaynak kontrolü. Önceki eşyalar topluca değiştirilmez; sonraki normal oyun oturumunda tooltip ve kayıt/yeniden giriş sonucu gözlenir.

1.3.1: Mevcut kodun genel incelemesi ve 88 yeni regresyonla 979 kaynak kontrolü. Ayrıntılar CODE-REVIEW.md içindedir. Sonraki adım yeni kapsam açmadan normal oyun oturumunda mevcut Legendary/set ve bakım düzeltmelerinin kabulünü kaydetmek.

1.0.0, kullanıcının 0.9.4 için verdiği “her şey düzgün” geri bildirimi üzerine hazırlanan ilk tamamlanmış yerel sürümdür. Oyun kodunda yalnızca sürüm etiketi değişir. Önceki kaynak ve derleme projesiyle sürüm/ satır sonları dışında eşleşme ve aynı bağımlılık DLL hash'leri paketleme sırasında denetlenir. Genel oyun geri bildirimi ACCEPTANCE.json içinde kayıtlıdır; ayrıntılı kontrol listesinin her maddesi ayrı test edilmiş sayılmaz.

Kapsam: client tarafı, EpicLoot 0.14.13, normal oyun limitleri, yalnızca eşyanın alabileceği efektler ve Uygula ile asıl envanter eşyasının değiştirilmesi. Menü Türkçe kalır; eşya ve efekt metinleri oyun dilini izler. Eşyalar varsayılan olarak düşükten yükseğe tier, tier içinde ad sırasıyla listelenir.

| Aşama | Tamamlanan işler |
| --- | --- |
| 0.7.2 | Üretim/teslim dayanıklılığı ve geçmiş kaynak temizliği. |
| 0.8.0 | Koleksiyon etiketleri, kopyalama, özet kart, JSON aktarımı, çakışma önizlemesi ve geçiş yedeği. |
| 0.9.0–0.9.1 | Filtre/geçmiş önbelleği, taş önizlemesi, kurtarma, tanılama ve panel yaşam döngüsü. |
| 0.9.2–0.9.4 | Kart/sekme düzeni, EpicLoot ayarlarından tier sırası, uzun adlar, alan hata yüksekliği, ölçek önizlemesi, Türkçe menü ve tam hata raporu. |
| 1.0.0 | Kullanıcının olumlu genel geri bildirimiyle yerel sürümün tamamlanması, kabul kaydı ve kaynak/bağımlılık eşleşme denetimi. |

1.2.0 doğrulama: 891 kaynak kontrolü, 9 paketleme ve 16 izole kurulum kontrolü. Önceki 1.0.0 kabul kaydı tarihsel kanıt olarak korunur. İki bağımsız üretimde DLL, manifest ve iki ZIP aynı hash'leri verir. BUILDING.md yeniden üretimi, INSTALLING.md kurulum/güncellemeyi, COMPATIBILITY.md kanıt sınırlarını açıklar. PERFORMANCE.md başsız ölçümler içerir; Unity FPS veya oyun açılış süresini ölçmez.

1.3.0: Kullanıcı özel Legendary/set için hem düzenleme hem sıfırdan üretimi seçti. Uygun kimlik kataloğu, özel aralıklar, set bilgileri, kalıcı kimlik ve kimliği koruyan envanter işlemleri eklendi. Yeni oyun kabulü bekliyor.

## Onaylanan sıradaki özellikler

1. Tek taş çıkarıp iade etme — **1.1.0'da tamamlandı**; büyü ve boş soket korunur, kalan taş katkısı EpicLoot tarafından hesaplanır.
2. Asıl eşyaya uygulanmış son işlemi geri alma — **1.1.0'da tamamlandı**; oturum içi tek kayıt, iade taşlarının geri alınması ve dış değişiklik kontrolleri.
3. Mevcut özellikler ve arayüz — **1.1.1'de tamamlanan iyileştirmeler:** kartlı taş seçimi, sekmeli onay, eksik taş adetleri ve canlı dayanıklılık/dil yenilemesi.
4. Yeniden boyutlandırılabilir arayüz — **1.2.0'da tamamlandı**; köşeden sürükleme, boyut kalıcılığı ve uyarlanan kolonlar.
5. Sonraki normal oyun oturumunda 1.1.x/1.2.0 akışlarını birlikte kontrol et; sonuçlara göre bakım düzeltmeleri.
6. Özel Legendary/set üretimi ve düzenleme — **1.3.0'da tamamlandı**; uygun kimlik kataloğu, özel aralıklar, set bilgisi ve kimlik kalıcılığı. Yeni oyun kontrolleri TESTPLAN.md içinde.

2026-10-09 kapsam tercihi: **“Önce mevcut özellikleri ve arayüzü geliştirelim”**. Bu tercih önce 1.1.1/1.2.0 ile uygulandı. Sonraki 2026-10-09 tercihi **“Düzenleme ve sıfırdan üretim birlikte”** ile özel Legendary/set kapsamı 1.3.0'da genişletildi; runestone ayrı karar bekler.

## Sonraki bakım adımları

1. Bildirilen oyun hatalarını tanılama raporuyla incele ve ilgili otomatik regresyon kontrolünü ekle.
2. Sonraki normal oyun oturumlarında TESTPLAN.md üzerinden farklı dil/çözünürlük, kalıcılık, soket ve kurtarma sonuçlarını kaydet. Her küçük değişiklikte yeniden oyun açılışı isteme.
3. Valheim/EpicLoot güncellemesi gerektiğinde API ve davranış farklarını incele; yalnızca doğrulanan sürümleri uyumluluk tablosuna ekle.
4. Harici dağıtım istenirse lisans, yazar/paket metadata ve yayın hedefini kullanıcıyla kararlaştır. Yerel paketler tamamlanmıştır; harici yayın yapılmamıştır.

## Önce kullanıcı kararı gereken kapsamlar

- Runestone üretimi ve düzenleme kuralları.
- Birden fazla işlemi veya oyun oturumları arasında uygulanmış işlemi geri alma.
- Mevcut IMGUI yerine başka bir Unity arayüzüne geçiş.
- Sunucu yetkilendirmesi, toplu envanter düzenleme veya limitsiz üretim.

## Test ve sürüm düzeni

Küçük değişikliklerde derleme ve ilgili otomatik kontroller çalıştırılır. Büyük sürümde oyun kontrolü birleştirilir. Otomatik test, kurulu DLL incelemesi ve kullanıcı oyun geri bildirimi ayrı kaydedilir. Oyun kodu veya bağımlılık değişirse 0.9.4 kabul kaydı yeni sürüme otomatik taşınmaz; paketleme eşleşme denetimi bunu reddeder.
