# 1.5.0

- Hedef EpicLoot sürümü 0.15.6 oldu; 0.14.x artık desteklenmez (BepInDependency en az 0.15.6 ister).
- EpicLoot 0.15 eşya verisi katmanını (`EpicLoot.Data.ItemInfo` / `item.Data()`) kaldırdı. Büyü kaldırma ve envanterde eşya yazma artık EpicLoot'un `ItemDataExtensions.RemoveMagicItem` metodunu kullanır; bu metot önbellekteki bileşeni, Indestructible durumunu ve kayıtlı büyü anahtarını birlikte temizler. 1.4.1 ile 0.15.6'da bu iki işlem tip yüklenemediği için hata veriyordu.
- Limitsiz moddaki iksir bekleme süresi tavanı yeni hedefe bağlandı: EpicLoot 0.15 `TryCreateDecreasedCooldownMead` metodunu kaldırıp aynı `Clamp01(1 - değer)` hesabını `ModifyRestoringMead` içine taşıdı. Diğer 13 tavan hedefinin imzası, parametre adları ve gövdesi 0.14.13 ile aynı; direnç tavanının kırpması ayrı bir yardımcıya taşındı ama spawner'ın prefix'i sonucu tamamen kendisi hesapladığı için etkilenmez.
- Kayıtlı eşyaların EpicLoot verisi aynı `m_customData` anahtarında kalır; mevcut eşyalar için geçiş gerekmez.
- 0.15.0 ile gelen set (farklı nadirlikte parçalar) ve yeni efektlerin oyun içi davranışı ayrıca test edilmedi; GameplayValidation=Pending.

# 1.4.1

- Limitsiz mod artık oyunda da etki eder. Önceden aralık dışı değerler eşyaya yazılıyor ve açıklamada görünüyordu; ancak EpicLoot bazı efektlerin toplamını kendi kodunda kırptığı için fazlası oyunda işlemiyordu.
- Limitsiz mod açıkken EpicLoot'un 14 efekt içi tavanı kalkar: direnç tavanı (MaxResistance yerine en fazla %100), Bulk Up can bonusu, Mercenary yumuşak tavanı, Lucky Loot çarpanı ve adet sınırları, parry penceresi, yavaşlatma %10 tabanı, iksir bekleme süresi, gece ve Off-Set hasar azaltma, kan bedeli azaltma, Shocking Charge en az yük ve adrenalin verimi.
- Güvenlik sınırları korunur: alınan hasar negatife düşüp can doldurmaz; maliyet ve süreler sıfırın altına inmez; can 1'in altına çekilmez; olasılıklar %100'ü geçmez. Lucky Loot çok aşırı değerlerde yine de 1000x çarpan ve 10000 adette durur.
- Tavanlar oyuncunun kuşandığı tüm eşyalar için birlikte kalkar. Limitsiz mod kapatılınca EpicLoot'un varsayılan davranışı hemen geri gelir. Bağlanamayan bir EpicLoot hedefi yalnızca o tavanı devre dışı bırakır, günlüğe uyarı yazılır ve Ayarlar → Tanılama'da görünür.
- Yeni kontroller: 42 temel tavan hesabı kontrolü ve kurulu EpicLoot 0.14.13 DLL'ine gerçek Harmony yamasıyla çalışan 71 canlı kontrol (tests/LiveCapChecks). Toplam 1056 kaynak kontrolü (483 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel) ve 71 canlı EpicLoot kontrolü. Valheim tiplerine dokunan 4 hedef (direnç, Bulk Up, Off-Set, gece) .NET Framework'te çalıştırılamadığı için IL ve imza düzeyinde doğrulandı. Oyun içi etki henüz gözlenmedi; GameplayValidation=Pending.

# 1.4.0

- Arayüz baştan düzenlendi. Üstte Yeni eşya / Envanterim modu, ← Geri / İleri →, Şablonlar, Limitsiz, Ayarlar, Yenile ve Kapat bulunur. Sekmeli editör kaldırıldı; nadirlik, seviye, adet, soket, ad, efektler ve soketler tek sayfada düzenlenir.
- Efekt ekleme, taş takma ve Legendary / Set seçimi tasarım sayfasının üstünde açılan seçici pencerede yapılır. Her kart oyun içi adı, tam açıklamayı, notları ve normal aralığı gösterir; seçici açıkken birden fazla seçim yapılabilir.
- Seviye, adet, soket ve efekt değerleri için − / + düğmeleri; efektlerde Min / Maks ve kaydırıcı. Ana eylem düğmesi ve teslim yeri her zaman görünen alt çubukta. Boş arama kutuları ipucunu kutunun içinde gösterir.
- Limitsiz mod (config: Tasarım → Limitsiz mod): efekt değerleri normal aralığın dışına çıkabilir (negatif dahil), en fazla 30 efekt ve 12 soket. Eşya türü uyumluluğu, yinelenen efekt ve özel güç kuralları ile sonlu sayı denetimi korunur. Aralık dışı değerler kartta sarı uyarıyla gösterilir; normal modda sınır hataları Limitsiz modu önerir.
- Nadirlik değiştirmek artık efekt ve taşları silmez; yeni nadirliğe uymayan değerler alan hatası olarak gösterilir.
- 27 yeni kontrol: limitsiz mod için 26 üretim, seçicinin kapanışta temizlenmesi için 1 panel kontrolü. Toplam 1014 kaynak kontrolü (441 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel). Yeni arayüzün oyun içi görünümü henüz doğrulanmadı; GameplayValidation=Pending.

# 1.3.2

- Yeni üretimde m_crafterID=0 ve m_crafterName boş atanır; eşya açıklamasında Crafted by / Üreten görünmez. Önizleme, envanter/yere teslim, çoklu üretim ve özel Legendary/set eşyaları kapsanır.
- Native tooltip ve üretici verisinin kayıt/clone davranışı kurulu Valheim DLL'inden kontrol edildi. Global tooltip gizleme veya eski envanterde toplu değişiklik yapılmaz.
- 8 yeni üretim regresyonu: toplam 987 kaynak, 9 paketleme ve 16 izole kurulum kontrolü. Oyun içi kontrol sonraki normal oturumda; GameplayValidation=Pending.

# 1.3.1

- Eksik şema alanlı şablon/favori dosyası yüklemesi ve üzerine yazılması engellendi; önceki bellek kayıtları korundu. Favori okuma JSON derinlik/metaveri ayarlarını diğer koleksiyon okuyucularıyla paylaşır.
- Sonlu olmayan/ters efekt aralıkları doğrulamada reddedilir ve arayüzde açıklanır; geçersiz aralıkla kaydırıcı kullanılmaz. Sonsuz kaydırıcı adımı sonlu değeri bozmaz.
- Hatalı efekt metinlerinde ad kimliğe düşer, açıklama okunur hata gösterir; diğer efekt kartları kullanılabilir. Yenilenen önizlemede eski alan hataları temizlenir.
- Başarısız şablon yüklemesi seçili kayıt adını/etiketini/envanter kaynağını değiştirmez. Bozuk taslak geri yükleme atomik doğrulanır.
- Legendary tanımında eksik/yinelenmiş/null başlangıç gücü, null kayıt ve kimlik anahtarı uyuşmazlığı yakalanır. Bozuk özel yapılandırma mevcut tasarım ve geçerli katalog kayıtlarını korur.
- Genel inceleme raporu eklendi; 88 regresyonla toplam 979 kaynak, 9 paketleme ve 16 izole kurulum kontrolü. Yeni native oyun kabulü bekliyor.

# 1.3.0

- Seçili temel eşya için ayrı Legendary / Set sekmesi: native özel Legendary/Mythic tanımları, bağımsız/set filtresi, oyun dilinde ad/açıklama/set bonusları ve arama.
- Kimlikten nadirlik/set ve başlangıç güçleri alınır; özel GetRollRange ve yapılandırılmış efekt sayısı uygulanır. Ek efektler normal eşya/çatışma kurallarıyla seçilir. Native sabit güçler korunur; augment edilmiş mevcut güçler desteklenir.
- Envanterdeki bilinen özel Legendary/set eşyası seçilebilir, önizlenebilir ve kimliği korunarak Uygula ile düzenlenebilir. Kimlik değiştirme için üretim modu kullanılır.
- Şablon/aktarım/son tasarım/geçmiş kimliği saklar. Büyü kaldırma özel kimlik ve set kaybını açıklar; geri alma bunları da geri yükler.
- 80 regresyon eklendi: 891 kaynak, 9 paketleme ve 16 izole kurulum kontrolü. Yeni native UI, equip görselleri ve gerçek set bonusları henüz oyun içinde doğrulanmadı.

# 1.2.0

- Ana pencereye sağ alt köşeden fareyle yeniden boyutlandırma eklendi. Yazı ölçeği değişmeden sütunlar ve kaydırma alanları yeni ölçüye uyarlanır.
- Pencere boyutu config'e kaydedilir; küçük ekranda sınırlandırılır, büyük ekran tercihi kaybolmaz. Görünüm gerçek piksel boyutunu ve yalnız boyutu sıfırlama düğmesini gösterir.
- Sürükleme ekran ve konuma göre sınırlandırılır; modal açıkken kapalıdır. Kapatma/odak kaybı/ekran veya ölçek değişimi fare yakalamasını bırakır. Boyut değişimi Update'e ertelenir; değişen genişlikte eski metin ölçümleri temizlenir.
- 73 boyut/ölçek/geometri kontrolü ve bir kapatma kontrolü eklendi: 811 kaynak, 9 paketleme ve 16 izole kurulum kontrolü. Yeni native sürükleme/çizim sonucu henüz kaydedilmedi.

# 1.1.1

- Tek taş çıkarma kartlarına ikon, soket numarası, oyun dilindeki efekt adı/açıklaması ve mevcut katkı eklendi; etkisiz taş açıklaması gösterilir. Uzun adlar sabit genişlikte metin yüksekliğine göre sarılır.
- Büyü kaldırma/taş çıkarma onayı Taşlar, Değişiklikler ve Son eşya sekmelerine ayrıldı. Aynı adlı iade taşları adet halinde gruplanır.
- Geri alma Değişiklikler, Geri yüklenecek eşya ve Gerekli taşlar sekmelerine ayrıldı. Eksik malzemede de ayrıntılar açılabilir; tüm eksik taş türleri gereken/mevcut/eksik miktarlarıyla belirtilir.
- Geri alma önizlemesindeki eski dayanıklılık gösterimi düzeltildi. Önizleme ve onay aynı aday hazırlamasını kullanır; açık modallar tamir/aşınma ve dil değişince yenilenir. Değişmeyen soket önizlemesi yeniden hesaplanmaz.
- 24 bağlı kaynak sunum/önizleme/seçim regresyonu eklendi: 737 kaynak kontrolü, 9 paketleme, 16 izole kurulum kontrolü. Canlı Unity görünümü/oyun sonucu henüz kaydedilmedi; ayrı küçük patch oyun açılışı istenmez.
- Envanter/dosya testlerinin geçici klasör temizliği, yalnız sahip olunan Spawner temp klasörlerinde Windows dosya kilidine karşı altı kısa ve sınırlı yeniden deneme yapar; kalıcı hata ve davranış testi hataları yine başarısız sonuç verir.
- Kullanıcının mevcut özellikler/arayüz tercihi kaydedildi; Legendary/set, runestone ve kalıcı/çoklu işlem geçmişi bu sürüme eklenmedi.

# 1.1.0

- Tek taş çıkar…: asıl eşyanın seçili shardstone'unu envantere iade eder; büyü, diğer taşlar ve soket kapasitesi korunur. Kalan soket değerleri EpicLoot RemoveShard ile yeniden hesaplanır; önizleme kaynak MagicItem verisinden ayrılır.
- Son envanter işlemini geri al…: oturumdaki son başarılı Uygula/büyü kaldırma/tek taş çıkarma işlemi için ayrı onay ve önceki eşya önizlemesi. İade taşları envanterden alınır; kayıt menü kapatmada korunur, oyuncu/oturum değişiminde temizlenir.
- Envanter yazımları ortak işlem hizmetine alındı. Valheim'in gerçek yığın birleştirme hedefleri geri alma fişine kaydedilir. Eksik taş, farklı metadata/world level, dışarıdan değişmiş/kullanılan/kayıp eşya, çift onay ve başarısız yeni işlem önceki geri alma kaydını bozmaz.
- İşlem hatası sırasında özgün eşya nesneleri, yığın sayıları, büyü verisi ve dayanıklılık davranışı geri yüklenir. Geri alma sırasında diğer envanter eşyaları ve mevcut konum/dayanıklılık oranı korunur.
- Türkçe kullanım rehberi ve İngilizce mod yöneticisi özellik listesi güncellendi; büyü kaldırma/tek taş çıkarma düğmeleri aynı satırda yer alır.
- 713 kaynak kontrolü, 9 paketleme ve 16 izole kurulum kontrolü. Yeni sürümün oyun sonucu Pending; önceki 1.0.2 büyü kaldırma için 2026-10-09 olumlu kullanıcı geri bildirimi ayrı kaydedildi.

# 1.0.2

- Envanter editörüne Büyüyü kaldır… ve ayrı onay/önizleme penceresi eklendi. Onay, tasarımı uygulamak yerine asıl eşyanın büyüsünü ve soketlerini kaldırır.
- Kullanıcı tercihi doğrultusunda takılı taşlar EpicLoot API'siyle yeniden oluşturulup envantere iade edilir. Toplu yer kontrolü aynı boş slotu iki kere saymaz; mevcut taş yığınlarının boş kapasitesi kullanılabilir.
- Eşya kimliği/kalite/konum/varyant, yabancı mod verileri ve dayanıklılık oranı korunur. Kırılmazlık kaldırıldığında özgün dayanıklılık davranışı geri gelir.
- Kaynak değişimi, kullanılan eşya, eksik taş prefabı, yer yetersizliği ve tekrar onay reddedilir. İade/son bildirim hatalarında taş yığınları ve büyü verisi geri yüklenir.
- Önizleme ve kapasite hesapları Update içinde yapılır; onay paneli kapanınca veya hatadan kurtarılınca temizlenir.
- İkon ve İngilizce mod sayfası güncellendi. 622 kaynak, 9 paketleme ve 16 izole kurulum kontrolü; canlı yeni özellik sonucu henüz kaydedilmedi.

# 1.0.1

- Eşyanın kalite üst sınırı giriş/üretim kontrolünden kaldırıldı. Normal kalite aralığı menüde görünür; üst sınırın uygulanmadığı belirtilir. Kalite kırpılmaz; prefabın normal maksimumu değiştirilmez.
- En az 1 olan tam sayı zorunluluğu, efekt değer/sayı sınırları, soket kuralları ve adet sınırı korunur.
- Kalite 30 için önizleme, şablon aktarımı, envanter/yere teslim ve asıl eşyaya Uygula regresyonları eklendi. Geçersiz kalite girişinde tasarım korunması kontrol edilir.
- İkon, sürümle eşleşen manifest ve yalnızca İngilizce özelliklerden oluşan mod yöneticisi README paketleme akışına eklendi.
- Önceki genel kullanıcı kabulü değişmiş oyun koduna taşınmaz. 1.0.1 gameplay durumu Pending; kaynak ve paket doğrulaması ayrı kaydedilir.

# 1.0.0

- Kullanıcının olumlu 0.9.4 genel oyun geri bildirimi temel alınarak ilk tamamlanmış yerel paket hazırlandı. Oyun/arayüz davranışı aynı; eklenti ve proje sürümü 1.0.0.
- README, kurulum, derleme, uyumluluk ve yol haritası tamamlanan yerel paket etrafında güncellendi. Normal limitler, client tarafı çalışma ve mevcut veri biçimleri korunur.
- ACCEPTANCE.json, kullanıcı bildirimi ve temel arşiv hash değerlerini taşır. Mod kaynağı/derleme projesi sürüm alanları hariç ve bağımlılık hash değerleri temel sürümle karşılaştırılır.
- Değişmiş kaynak, yanlış kabul sürümü veya farklı bağımlılıklar eski kullanıcı bildirimini otomatik devralamaz. Kayıt bulunmayan gelecekteki paketler Pending olarak işaretlenir.
- 534 kaynak, 13 paketleme ve 16 izole kurulum kontrolü geçti. İki bağımsız DLL/manifest/ZIP üretimi aynı hash değerlerini verdi. Önceki arşivler korundu.

# 0.9.4

- Menü Türkçe kalır; Lumberjack dahil EpicLoot efekt açıklamaları oyun dilinde korunur. Spawner'ın özel odun açıklaması ayrı not olarak gösterilir.
- Katalog/envanter/şablon adlarında sabit kolon genişliği ve metne göre hesaplanan satır yüksekliği kullanılır. Editör/önizleme başlığı sınırlıdır; tam ad/prefab bilgi kutusunda okunabilir. Ölçümler ad/genişlik bazında önbelleğe alınır.
- Alan yardım/hata metinleri metin yüksekliğine göre büyür. Kaydırma alanlarına minimum yükseklik sınırı eklenmez.
- Görünüm penceresine Tanılama ve Kullanım sekmeleri eklendi. Ölçek uygulanmadan önce ekranın izin verdiği gerçek ölçek gösterilir.
- Durum çubuğuna Ayrıntı eklendi. Panoya kopyalanan tanılama; oyun/mod/Unity sürümü, dil, tuş, ekran/ölçek, katalog/tasarım/alan durumu ve tam exception ayrıntısını içerir.
- Hata sonrası tekrar açma mesajı sabit F8 yerine ayarlanan tuşu kullanır; son hata özeti ve ayrıntısı birlikte güncellenir.
- 7 dil/efekt ve 4 panel/hata regresyonu eklendi: 534 kaynak kontrolü, 9 paketleme ve 16 izole kurulum kontrolü geçti. Kullanıcının 0.9.3 genel görünüm geri bildirimi kaydedildi; 0.9.4 canlı Unity kontrolü bekliyor.

# 0.9.3

- Varsayılan eşya sırası düşükten yükseğe tier, ardından yerelleştirilmiş ad ve kararlı prefab sırasıdır. Tier grup başlıkları eklendi.
- Tier verisi kurulu EpicLoot 0.14.13 AllItemsWithDetails ve BossKeysInOrder API'lerinden katalog yenilenirken alınır. Nadirlik/seviye tier sayılmaz; malzeme adına göre tahmin yapılmaz.
- Tekrarlanan boss anahtarları bir basamak sayılır; birden fazla gereksinimde en ileri boss kullanılır. Eksik/bilinmeyen metadata son alfabetik grupta gösterilir.
- Tür/Favori gruplarında tier ve ad sırası korunur. Arama, kategori, favori filtreleri ve seçim akışı korunur. Config 0 artık Tier + Ad anlamına gelir; 1/2 tercihleri değişmez.
- 24 bağlı davranış kontrolü eklendi; toplam 523 kaynak kontrolü, 9 paketleme ve 16 izole kurulum kontrolü geçti. Canlı Unity çizimi sonraki uygun açılışta doğrulanır.

# 0.9.2

- Yuvarlatılmış yüzeyler, ince panel/kart çerçeveleri, belirgin seçili sekme/satır ve altın üzerinde koyu ana eylem metni eklendi.
- Kolon boşluğu ve oranları düzenlendi; kütüphane ve editör genişletildi. Ekrana sığdırma ve kalan yüksekliği kullanan kaydırma alanları korunur.
- Eşya/efekt/taş/envanter/şablon aramalarına temizleme düğmesi eklendi.
- Çerçeveli ikon alanları, daha geniş favori/kaldırma düğmeleri, seçili eşya başlığı ve sade önizleme özeti eklendi.
- Teslim tercihi Envantere / Yere bırak seçimidir; üretim ve Uygula davranışı değişmez.
- Mevcut normal limitler, uyumluluk ve panel kurtarma korunur. Canlı Unity görünüm testi bekliyor.

# 0.9.1

- Panel açılışı oyuncu/dünya/katalog/dil hazırlığı ve başarılı katalog yüklemesi sonrasına taşındı; katalog hatasında giriş engellenmez.
- Beklenmedik Update/OnGUI hatası paneli kapatır, imleci geri bırakır, üretime durma isteği gönderir ve temayı yeniden hazırlamaya açar. Unity ExitGUIException normal kontrol akışı korunur.
- Kapatmada kayıt hatası modal/iş kuyruğu temizliğini ve imlecin geri yüklenmesini engellemez.
- Gerçek PanelLifecycle kaynağına bağlı 21 kontrol eklendi; kaynak test sayısı 499.
- Manifestli paket için önizlemeli kurulum/güncelleme scripti eklendi. Açık -Apply ile önceki DLL yedeklenir ve yeni dosya atomik olarak kurulur. Yeniden adlandırılmış ikinci DLL, bozuk paket ve değişmiş hedef engellenir.
- 16 izole kurulum kontrolü geçti; gerçek kullanıcı profili değiştirilmedi. INSTALLING.md kurulum/veri/geri dönüş rehberi eklendi.
- Paketleme artık dört kaynak testini çalıştırır ve kurulum scriptlerini runtime ZIP'e alır. Normal limitler ve client tarafı kapsam korunur; gerçek oyun kabulü bekliyor.

# 1.0.0 hazırlığı (0.9.0 dönemindeki çalışma)

- BUILDING.md ve COMPATIBILITY.md eklendi; gerçek oyun kabulü bekleyen durum olarak kaydedilir.
- Tek komutla Release derleme, üç test takımı, kaynak/DLL sürüm kontrolü ve SHA256 manifestli paketleme eklendi.
- ZIP girdileri sıralı ve sabit tarihli üretilir; aynı ortamda iki bağımsız DLL/manifest/arşiv eşleşmesi doğrulandı.
- Dokuz paketleme kontrolü eklendi; eski çıktıya yazma, eksik bağımlılık, sürüm farkı ve bozuk ZIP reddedilir.
- Oyun davranışı değişmedi; harici yayınlama veya lisans seçimi yapılmadı.

# 0.9.0

0.7.2, 0.8.0 ve 0.9.0 yol haritası tek sürümde birleştirildi.

## Üretim ve geçmiş (0.7.2)

- Kırılmazlık başlangıç dayanıklılığı üretim klonunda ve yere teslimin ikinci klonunda korunur; doğrudan efekt ve soket katkısı kapsanır.
- 1/100 adet, dolu envanter, durdurma, kısmi teslim ve hata için bağlı üretim regresyonları eklendi. Yeni test eski 0.7.1 kodundaki dayanıklılık kaybını yakalar.
- Geçmiş dışında kalan envanter kaynak kayıtları temizlenir; aktif kaynak ve 60 durumluk geçmiş korunur.

## Şablon koleksiyonu (0.8.0)

- Etiket, birleşik arama/etiket filtresi, özet kart ve farklı adla kopyalama eklendi.
- Kayıt formu ayrı pencereye taşındı; ana liste kalan yüksekliği kullanır.
- Seçili kayıtları JSON dosyasına aktarma ve dosyadan önizleyerek/seçerek alma eklendi. Ad çakışmaları mevcut kaydın üzerine yazmaz; yeniden adlandırılabilir.
- Eski v1 depo okunur; ilk açık yazımda özgün dosya kalıcı .v1.bak yedeğiyle korunur. Etiketli depo sürümü 2.

## Kullanım ve ölçüm (0.9.0)

- Ad/tür/favoriler sıralaması, filtre temizleme ve boş durumlarda düzeltme eylemleri eklendi.
- Geçmiş düğmelerindeki tekrar JSON üretimi kaldırıldı; şablon filtreleri önbelleğe alındı. Taş önizlemeleri ihtiyaç oldukça küçük gruplar halinde hesaplanır; envanter listesi aktif sekmede yenilenir.
- Yedek inceleme, açık onayla geri yükleme ve geçerli şablon/favori kayıtlarını kurtarma eklendi. Mevcut dosya ayrıca .corrupt-… adıyla saklanır; dışarıdan değişen dosyaya yazım engellenir.
- Görünüm penceresine sürüm, süre, son işlem/hata ve panoya tanılama kopyalama eklendi.
- net481 için Newtonsoft.Json derleme referansı düzeltildi; kurulum paketine ek JSON DLL konmaz.
- Release: 0 hata/uyarı; 304 temel, 102 davranış, 72 üretim kontrolü; toplam 478. PERFORMANCE.md ölçümleri ve TESTPLAN.md birleşik oyun listesi eklendi. Gerçek Unity ve oyun doğrulaması bekliyor.
# 0.7.1

- Kaydırma alanlarının 48 piksele sıkışmasına yol açan minimum yükseklik seçeneği kaldırıldı; listeler kalan panel yüksekliğini kullanır.
- Düzeltme eşya, efekt, taş, envanter, şablon, paylaşım ve her iki önizleme görünümünü kapsar.
- Eşya seçimi ertelenmiş tasarım işlemi olarak korunur; seçilen eşyanın adı durum alanına yazılır ve artık bulunmayan prefab için açıklama gösterilir.
- Gerçek LibraryMenu/HistoryMenu kaynağıyla liste yüksekliği, ikinci/sonraki satır seçimi, tasarım geçmişi ve devre dışı panel kontrolleri eklendi. Yeni regresyon kontrolü eski 0.7.0 kaynağında başarısız olur.
- Release derlemesi sıfır hata/uyarıyla tamamlandı; 245 temel ve 90 davranış kontrolü, toplam 335 kontrol geçti. Gerçek Unity çizimi ve oyun içi doğrulama bekliyor.

# 0.7.0

- Envanter düzenlemesinde değişiklik özeti eklendi: seviye, nadirlik, özel ad, efekt ve soket/taş değişiklikleri.
- Taş katkısı dahil toplam efektler gerçek EpicLoot yöntemlerinden okunur; etkisiz taşlar korunur ve taş sırası karşılaştırılır.
- Değişiklikler/Yeni eşya/Mevcut sekmeleri ve büyütülmüş değişiklik görünümü eklendi.
- %75–150 ölçek seçimi, son pencere konumunu saklama ve çözünürlük değişiminde görünür alana taşıma eklendi.
- Küçük ekran düzeni, kalan alana uyarlanan listeler ve kaydırılabilir şablon formu eklendi; önizleme/paylaşım pencereleri sığdırılır.
- Sayısal alanlarda hata vurgusu ve alan altında açıklama eklendi; birden fazla giriş hatası birlikte gösterilir.
- Aç/kapat efektleri için açık Değeri 1 yap işlemi eklendi; girişler sessizce kırpılmaz.
- Efekt ve taş listeleri oyun içi adla sıralanır; adlar/kart metinleri ve filtre sonuçları saklanır, ilgili değişikliklerde yenilenir.
- GUI çiziminde önizleme geçerliliği için tekrarlanan JSON üretimi kaldırıldı; menü sürelerini isteğe bağlı günlüğe yazma eklendi.
- Lumberjack açıklaması kesirli ekipman katkısını ve spawner ile düzenlenmiş silahları kapsayacak şekilde düzeltildi.
- Release derlemesi sıfır hata/uyarıyla tamamlandı; 245 temel ve 82 davranış kontrolü, toplam 327 kontrol geçti. Gerçek Unity menüsü ve oyun akışlarının toplu doğrulaması bekliyor.

# 0.6.1

- Envanter düzenlemesinde EpicLoot'un başlangıç dayanıklılık bayrağı korunur; Kırılmazlık kaldırılınca dayanıklılık kullanımı geri gelir.
- Önizlemede kopya eşyanın dayanıklılık takibi sıfırlanır; başarısız uygulamada büyü verisi ve dayanıklılık takibi birlikte geri yüklenir.
- Lumberjack'in ekipmandan gelen kesirli toplamları aşağı yuvarlanır (31,5 → 31); tam sayı giriş doğrulaması korunur.
- Bonus odun üretim hataları yakalanır; kısmi teslimden sonra ikinci bonus çalıştırılmaz.
- Teslim tercihi ve taş arama nadirliği, prefabı bulunamayan son taslağın kayıt korumasını kaldırmaz.
- Gerçek kaynak yöntemlerini bağlayan davranış kontrolleri eklendi; Unity/API taklitleri ve kurulu EpicLoot 0.14.13 ile davranışı karşılaştırılmış Indestructible kaynak örneği kullanılır.
- Release derlemesi sıfır hata/uyarıyla tamamlandı; 169 temel ve 58 davranış kontrolü, toplam 227 kontrol geçti. Oyun içi doğrulama bekliyor.

# 0.6.0

- Tek tasarım için sürümlü JSON paylaşımı, panoya kopyalama ve yapıştırarak içe alma eklendi.
- İçe alma detached adayda oyun kurallarıyla doğrulanır; hata mevcut tasarımı korur.
- İçe alma üretim moduna geçer, asıl eşyaya uygulamaz; geri al ile önceki tasarıma dönülebilir.
- Son tasarım ve üretim/taş nadirliği tercihleri otomatik taslak dosyasında saklanır.
- Yarım kalan girişler korunur; sonraki oyun açılışında ilk panel açılışında geri yüklenir.
- Envanter nesnesi bağlantısı ve geri al geçmişi dosyaya yazılmaz.
- Bozuk veya eksik prefablı taslakların üzerine otomatik yazma engellenir; dosyalar atomik/yedekli kaydedilir.
- Şablon içe alımında adet üst sınırını atlayan kontrol eksikliği düzeltildi.
- Release derlemesi sıfır hata/uyarı ile tamamlandı; 161 otomatik kontrol geçti. 0.5.0 ve 0.6.0'ın oyun içi doğrulaması bekliyor.

# 0.5.0

- Oyun içi ada göre sıralama ve eşya türüne göre kategori filtreleri eklendi.
- Kalıcı favoriler, satırda yıldız düğmesi ve yalnızca favoriler filtresi eklendi.
- 60 durumluk geri al/ileri al eklendi; yazma ve kaydırıcı değişiklikleri gruplanır, geçersiz taslaklar da saklanır.
- Envanter sekmesi, koordinatla eşya seçimi ve asıl eşya üzerinde Uygula eklendi.
- Mevcut/yeni eşya karşılaştırması ve büyütülmüş önizleme eklendi.
- Uygula mevcut ItemData nesnesini korur; konum, varyant ve diğer mod verileri korunur, dayanıklılık oranı taşınır.
- Augment/temper işaretleri korunan efekt kimlikleriyle yeniden eşlenir.
- Kullanılan, envanterden ayrılan veya büyü verisi/seviyesi dışarıdan değişen eşyalarda uygulama engellenir.
- Özel legendary/set, tanımlanmamış, yığın ve runestone soketli eşyalar açıklamayla düzenleme dışında tutulur.
- Normal üretim limitleri ve 0.4.1 şablon özellikleri korundu.
- Release derlemesi ve 132 otomatik kontrol geçti; 0.5.0'ın oyun içi doğrulaması bekliyor.

# 0.4.1

- Şablon arama, seçili kayıt vurgusu ve kayıt sayacı eklendi.
- Çakışan adları engelleyen yeniden adlandırma eklendi.
- Kayıt adıyla onaylı silme eklendi; Esc/Vazgeç ve paneli kapatma onayı iptal eder.
- Yeniden adlandırma/silme atomik yazma ve önceki dosyanın yedeğiyle yapılır; yazma hatasında bellekteki kayıt korunur.
- Geçersiz tasarımda şablon kaydetme düğmesi devre dışı bırakıldı.
- Alt bilgi normal değer aralıklarının zorunlu olduğunu belirtecek şekilde düzeltildi.
- Release derlemesi sıfır hata/uyarı ile tamamlandı; 94 otomatik kontrol geçti. Yeni şablon arayüzünün oyun içi doğrulaması bekliyor.

# 0.4.0

- Sürekli görünen, değişikliklerde yenilenen eşya önizlemesi eklendi.
- Düzenleme ve güç/taş seçimi orta sütunda sekmelere taşındı; sağ sütun önizlemeye ayrıldı.
- Geçersiz tasarımlarda hata ve üretim düğmesini devre dışı bırakma eklendi.
- Efekt değerleri için adımlı kaydırıcı ve minimum/maksimum düğmeleri eklendi.
- Shardstone'ın mevcut eşyaya katkısı ve takılamama nedeni kartta gösterilir.
- Taş katkısı gerçek EpicLoot soketleme hesabıyla geçici kopyada simüle edilir.
- Mevcut üretim sınırları, client tarafı çalışma, şablonlar ve GUI hata düzeltmeleri korundu.
- Release derlemesi ve 74 otomatik kontrol geçti; oyun içi doğrulama bekliyor.
# 0.3.4

- Eşya seviyesi, adet, efekt değeri ve özel eşya adı sınırları geri getirildi.
- Efekt/soket sayısı mevcut EpicLoot nadirlik ayarlarına göre sınırlandırıldı.
- Eşya listesi büyülenebilir prefablarla, efekt listesi seçili eşyanın alabileceği efektlerle sınırlandırıldı.
- Taşlar yeniden EpicLoot CanSocket/AddShard kurallarına göre takılır.
- Sınırsız şablonlar yüklemede oyun kurallarına göre doğrulanır; geçersiz yükleme mevcut seçimi değiştirmez.
- İkon/GUI hata düzeltmeleri ve Lumberjack çalışma hesabı korunur.
- 65 otomatik kontrol geçti; oyun içi doğrulama bekliyor.
# 0.3.3

- DrawIcon içindeki boş ikon dizisi ve geçersiz varyant indeksinden kaynaklanan menü hatası düzeltildi.
- Eksik ikonlarda yerleşim alanı korunur; ikon çizimi Repaint olayında yapılır.
- Menüde sekme/liste değişiklikleri Update'e ertelenerek Layout/Repaint düzeni korunur.
- Eksik ItemData/SharedData içeren prefablar menüye alınmaz.
- 53 otomatik kontrol geçti; oyun içi yeniden açma doğrulaması bekliyor.
# 0.3.2

- Spawner'dan yeni üretilen silahlarda Lumberjack için tam ekstra odun adedi yolu eklendi.
- Bonus odun sayısı dünya kaynak çarpanı, rastgele yığın miktarı ve düşme olasılığından bağımsızdır.
- Normal ganimet ve normal EpicLoot eşyaları korunur; orijinal bonus yolu atlanarak çift bonus önlenir.
- Kesirli Lumberjack adetleri reddedilir; odun yığınları toplam bonusu koruyarak oluşturulur.
- 47 otomatik kontrol geçti. Gerçek ağaç kırılması ve çok oyunculu sahiplik testleri bekliyor.
# 0.3.1

- Eşya seviyesi, üretim adedi, soket sayısı ve metin uzunluğu üst sınırları kaldırıldı.
- Bütün kayıtlı efektler ve ItemDrop prefabları listelenir; efekt uyumluluk ve tekrar filtreleri kaldırıldı.
- Taşlar çakışma veya kategori kontrolleri olmadan eklenir; soket sayısı gerektiğinde büyür.
- Şablon sayısı, boyutu ve efekt sayısı sınırları kaldırıldı.
- Büyük üretim karelere bölünür; üretim düğmesi işlemi durdurabilir.
- Envantere sığmayan eşyalar yere bırakılır.
- 36 otomatik kontrol geçti. Oyun içi doğrulama bekliyor.
# 0.3.0

- Sayısal efektlerde normal min/max aralığına göre engelleme kaldırıldı; sonlu özel değerler saklanır.
- Normal efekt değer aralıkları artık listede ve seçili efekt kartlarında görünür.
- Efekt isimleri ve açıklamaları EpicLoot'un kendi oyun içi formatter'ından alınır.
- Seçili efekt metinleri özel değer düzenlenirken güncellenir.
- Koyu tema, altın düğmeler, eşya/taş ikonları ve nadirlik renkleri eklendi.
- Kütüphane ve güç seçimi sekmelerle düzenlendi; efekt kartları açıklamalar içerir.
- Efekt isim ve açıklamalarında arama desteklenir.
- 50 otomatik kontrol geçti; oyun içi görsel ve davranış testleri henüz yapılmadı.

# 0.2.0

- Client tarafında kullanım için ağ uyumluluğu bildirimi eklendi; spawner sunucu kurulumu gerektirmez.
- İsimli eşya şablonlarını yerel dosyada kaydetme ve yükleme eklendi.
- Şablonlar yüklemeden önce EpicLoot'un geçerli kurallarına göre doğrulanır.
- Atomik şablon kaydı, önceki dosyanın yedeği ve bozuk dosyanın korunması eklendi.
- Önizleme ve Esc davranışı iyileştirildi.
- Seçili efekt değer aralıkları ve maksimum eşya seviyesi gösterilir.
- Sayısal adet gösterimi düzeltildi; boş efekt değeri için açık hata eklendi.
- 40 otomatik kontrol geçti. Oyun içi ve çok oyunculu doğrulama bekliyor.

# 0.1.0

- EpicLoot 0.14.13 için ilk eşya, efekt ve takılı shardstone üretim paneli.





