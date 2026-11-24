# 1.4.1 ayrıntılı oyun kontrolü

## Limitsiz efekt tavanları · sonraki normal oturum

1. Ayarlar → Tanılama: "Efekt içi tavanlar" satırı 14/14 bağlı gösteriyor mu? Limitsiz açıkken "kaldırıldı" mı yazıyor?
2. Limitsiz: Açık. Bir zırha ateş direnci 95 yaz, kuşan ve ateşte dur. Hasar EpicLoot'un normal direnç sınırındakinden belirgin şekilde az mı? 100 ve üstünde ateş hasarı tamamen kesiliyor ama can dolmuyor mu?
3. Hareket hızı veya hasar gibi normal efektlerde büyük değer (ör. 300) etkili mi? Bu efektlerin EpicLoot'ta tavanı yoktu; 1.4.0'da da çalışıyor olmalıydı.
4. Bulk Up 200: yemek canı normal %100 sınırının üstüne çıkıyor mu?
5. Limitsiz: Kapalı yap, eşyayı çıkarıp tekrar kuşan. Direnç ve Bulk Up EpicLoot'un normal tavanına dönüyor mu?
6. Oyunu kapatıp aç; günlükte "Limitsiz efekt tavanı bağlanamadı" uyarısı yok mu?

1056 başsız kaynak kontrolü ve 71 canlı EpicLoot kontrolü tavan hesabını ve yamaların bağlanmasını denetler; oyundaki gerçek hasar/can sonucu Pending.

# 1.4.0 ayrıntılı oyun kontrolü

## Yeni arayüz · sonraki normal oturum

1. F8 ile aç. Üst çubuk tek satıra sığıyor mu; sol/orta/sağ sütunlar ve alt çubuk görünüyor mu?
2. Soldan eşya seç. Orta sayfada nadirlik, Seviye/Adet/Soket − / + düğmeleri ve Özel ad doğru çalışıyor mu?
3. + Efekt ekle: kartlarda açıklama ve normal aralık görünüyor mu; art arda iki efekt eklenebiliyor mu; Bitti ve Esc pencereyi kapatıyor mu?
4. Efekt kartında − / +, kaydırıcı, Min, Maks değeri değiştiriyor mu; ← Geri bunu geri alıyor mu?
5. Soketi 2 yap, boş yuvadaki + Taş tak ile taş tak. Taş kartında etki açıklaması görünüyor mu?
6. Legendary / Set… penceresinde açıklama ve set bonusları görünüyor mu; kimlik seçimi başlangıç güçlerini ekliyor mu?
7. Limitsiz: Açık. Efekt değerini normal maksimumun üstüne yaz; kart sarı uyarı gösteriyor, önizleme geçerli kalıyor ve üretilen eşyanın açıklamasında değer görünüyor mu? Normal sınırdan fazla efekt ve soket ekle, üret ve oyundan çıkıp girince eşya korunuyor mu?
8. Limitsiz: Kapalı. Aynı tasarım alan hatası ve "Limitsiz modu aç" önerisi gösteriyor mu?
9. Envanterim'e geç, eşya seç; alt çubukta Büyüyü kaldır…, Taş çıkar…, Asıl eşyaya uygula çalışıyor mu? Yeni eşya'ya dönünce tasarım kopya olarak kalıyor mu?
10. Şablonlar listesinden kaydet ve yükle. Pencereyi sağ alt köşeden boyutlandır; ölçek değişiminde düzen bozuluyor mu?

1014 başsız kaynak kontrolü doğrulama ve üretim yollarını denetler; Unity çizimi ve gerçek EpicLoot enchanting/augment ekranlarındaki davranış Pending olarak kalır.

# 1.3.2 ayrıntılı oyun kontrolü

## 1.3.2 üretici bilgisi · sonraki normal oturum

Ayrı bir oyun açılışı gerektirmez. Sonraki normal kullanımda:

- Spawner'dan envantere eşya üret; açıklamasında Crafted by / Üreten satırı görünmemeli.
- Yere üretip eşyayı al; üretici satırı yine görünmemeli.
- Legendary/set ve çoklu üretimde kontrol et; eşya adı/kimliği, kalite, efektler, taşlar ve set bilgileri görünmeye devam etmeli.
- Üretilen eşyayı kayıt/yeniden giriş sonrasında incele; üretici satırı geri gelmemeli.

987 başsız kaynak kontrolü üretim yolları ve veriyi denetler; gerçek tooltip ve yeniden giriş sonucu Pending olarak kalır. Eski envanter eşyaları bu sürüm yüklenince otomatik değiştirilmez.

## 1.3.1 bakım düzeltmeleri · sonraki normal oturum

Ayrı oyun açılışı gerektirmez; aşağıdaki akışlar bir sonraki normal kullanımda mevcut Legendary/set kontrolleriyle birlikte gözlenir. Başsız kaynak testleri 979, paketleme 9 ve izole kurulum 16 kontroldür; native oyun kabulü Pending kalır.

- Geçerli eski şablon ve favoriler yüklenir; başarısız/eksik eşyaya ait şablon seçimi mevcut tasarımı, kayıt adını ve envanter seçimini değiştirmez.
- Normal efekt aralıkları, kaydırıcı ve Minimum/Maksimum davranışı sürer. Özel kimliğin kendi aralığı kullanılmaya devam eder.
- Yapılandırma kaynaklı geçersiz özel güç/aralık hatası alanlarda görünür; menü kullanılabilir kalır. Yapılandırma düzeltildikten sonra tanımları yenilemek üretimi yeniden açar.
- Efekt isim/açıklamaları oyun dilini izler. Panel açma/kapatma, pencere boyutlandırma, şablon yükleme, envanter Uygula ve taş iadesi/geri alma birlikte kontrol edilir.

Bozuk dosya/yapılandırma negatif senaryoları otomatik fixture'larda denenmiştir; oyun kontrolü için gerçek kullanıcı dosyasını bozmak gerekmez.

## 1.3.0 özel Legendary/set · sonraki normal oyun oturumu

- [ ] Temel eşyayı seç; Legendary / Set sekmesinde yalnız uygun kimlikler listelensin. Farklı eşya türünde liste değişsin; bağımsız/set filtreleri ve arama çalışsın.
- [ ] Bir özel kimlik seç: eski efekt/taş/ad temizlensin, kalite/adet korunsun, doğru nadirlik/set ve başlangıç güçleri gelsin. Minimum/Maksimum ve kaydırıcı özel aralığı kullansın; aralık dışı değer üretimi engellesin.
- [ ] Üretilen eşyayı envanterde ve yerde kontrol et: oyun içi adı/açıklaması, özel güçleri, görünümü ve soketleri doğru olsun. Set parçalarını giyerek eşiklerde native bonusları ve equip görsellerini kontrol et.
- [ ] Ekteki Message of Lindisfarne benzeri mevcut özel eşyayı Envanter'den seç; değeri/kaliteyi/taşı düzenle ve Uygula. Kimlik/set üyeliği ve augment edilmiş güçler korunsun; mevcut eşya referansı değişmesin.
- [ ] Sabit güç silinemesin; augment edilebilir başlangıç güçleri değiştirilebilsin. Ek efektler yalnız uygun ve çatışmasız seçenekler olsun.
- [ ] Tek taş çıkar ve geri al: kimlik/set korunsun. Büyüyü kaldır: kimlik/set de kaldırılsın, taşlar iade edilsin. Son işlemi geri al: kimlik/set ve taşlar geri gelsin.
- [ ] Şablon, pano, koleksiyon, tasarım geçmişi ve son tasarımda kimlik geri gelsin; eski sıradan şablonlar aynı şekilde yüklensin. Sonraki normal oyun açılışında kalıcılık kontrol edilsin.
- [ ] Eksik tanım/tutarsız set/uyumsuz eşya reddedilsin; sıradan eşyaya sessiz dönüş yapılmasın. Özel tanımları yenile ve oyun dili değişimi listeyi yenilesin.
- [ ] Yeniden boyutlandırma, uzun isimlerin sarılması ve mevcut üretim/taş/geri alma akışları çalışmaya devam etsin.


## 1.2.0 — sonraki normal oyun oturumunda

- [ ] Sağ alt ◢ köşesinden genişlet/daralt; yatay ve dikey ölçü bağımsız değişir. Başlıktan taşıma, eşya seçimi, metin alanı ve kaydırma çalışır; GUILayout hatası yoktur.
- [ ] %75/%100/%150 ölçeklerde sürükleme işaretçiyi izler; yazı ölçeği sürükleme boyunca aynı kalır. Ekran kenarında sınırlandırılır; alt boyutta üç sütun ve ana eylemler erişilebilir.
- [ ] Paneli kapat/aç ve sonraki normal oyun açılışında kontrol et: boyut geri gelir. Görünüm → Pencere boyutunu sıfırla yalnız boyutu; Varsayılan konum/ölçek/boyutu sıfırlar.
- [ ] Modal açıkken köşeden boyutlandırma başlamaz. Sürüklerken F8/Esc ile kapat veya odağı başka pencereye geçir; yeniden açıldığında fare yakalaması takılı kalmaz.
- [ ] Çözünürlüğü küçült/büyüt: pencere ekrana sığar; saklanan büyük ekran boyutu geri gelir. Uzun adlar yeni sütun genişliğinde sarılır; son envanter işlemi/geçmiş/şablon değişmez.

## 1.1.1 — aynı normal oyun oturumunda

- [ ] Tek taş çıkar… kartlarında ikon, soket numarası, oyun dilindeki efekt adı/açıklaması ve katkı görünür. Etkisiz taş ve uzun taş adı okunabilir; ikinci/sonraki taş seçilir.
- [ ] Taş seçimi/Değişiklikler/Son eşya sekmeleri doğru içeriği gösterir. Büyüyü kaldır… değişiklik özetiyle açılır, aynı taşlar adet halinde gruplanır. Vazgeç/Esc hiçbir eşya değiştirmez.
- [ ] İade taşlarından farklı iki türü sandığa koy: geri alma ayrıntıları yine açılır, bütün eksik türlerde gereken/mevcut/eksik adetleri görünür. Onay engellenir; taşları geri getirince açık pencerede onay kullanılabilir.
- [ ] Geri alma ekranını açmadan önce eşyayı tamir et: gösterilen dayanıklılık sonucu ve gerçek geri alma mevcut dayanıklılık oranında eşleşir.
- [ ] Açık işlem penceresinde oyun dili veya tamir/aşınma durumu değişirse önizleme yenilenir. Küçük ekran/ölçekte sekme, kart, onay/iptal ve kaydırma erişilebilir; GUILayout hatası yoktur.

## 1.1.0 — sonraki normal oyun oturumunda birlikte

- [ ] Envanterde iki farklı taşlı eşyayı seç. Tek taş çıkar… penceresinde ikinci taşı seç; önizleme büyüyü/diğer taşı korur. Vazgeç/Esc eşya ve envanteri değiştirmez.
- [ ] Onay yalnız seçili taşı doğru renk/nadirlikte iade eder. Normal efektler, kalite, özel ad, soket kapasitesi ve diğer taş kalır; kalan taşların gerçek tooltip değeri yeniden hesaplanır.
- [ ] Son envanter işlemini geri al… önceki eşya önizlemesini gösterir; onay iade taşını envanterden alıp doğru soketi geri yükler. Taş mevcut yığına birleşmişse yalnız iade adedi alınır.
- [ ] Uygula ile kalite/efekt değiştir ve geri al. Büyüyü kaldır ve geri al. Sonradan envantere aldığın diğer eşyalar ve mevcut dayanıklılık oranı korunur.
- [ ] Geri almadan önce bir iade taşını sandığa koy: işlem engellenir. Taşı geri getir: tekrar denenebilir. Eksik taş, dolu envanter, kullanılan/dışarıdan değişmiş eşya ve çift onay kayıp/çoğaltma yapmaz.
- [ ] Paneli kapatıp aç: son işlem kaydı korunur. Yeni başarılı envanter işlemi eski kaydın yerini alır; dünyadan/karakterden çıkış kaydı temizler. Tasarım Geri al/İleri al bu işlemden ayrıdır.
- [ ] Küçük ekran/ölçekte envanter eylemleri, taş seçimi, onay ve iptal erişilebilir; GUILayout hatası oluşmaz.

2026-10-09 kullanıcı bildirimi: **“Büyü kaldırma düzgün çalışıyor”** (önceki 1.0.2). Bu genel özellik bildirimi aşağıdaki ayrıntılı maddelerin her biri için ayrı sonuç değildir. Yeni 1.1.0 akışları henüz oyun sonucu olarak işaretlenmez.

## 1.0.2 büyü kaldırma — sonraki normal oyun oturumunda

- [ ] Envanterden büyülü eşyayı seç; Büyüyü kaldır… normal eşya ve iade edilecek taşları gösterir. Vazgeç/Esc hiçbir eşya veya taşı değiştirmez.
- [ ] Onaydan sonra büyü/nadirlik/özel ad/soketler kalkar; kalite ve dayanıklılık oranı korunur. Takılı shardstone'ların renk/nadirlik ve sayıları doğru olarak iade edilir.
- [ ] Taşlara yer yoksa büyü ve taşlar aynı kalır. Mevcut yığında yer varsa iade o yığını kullanır.
- [ ] Kullanılan/değişmiş/kayıp eşya engellenir; işlemi iki kez onaylamak ikinci taş iadesi yapmaz. Kırılmazlık kaldırıldıktan sonra dayanıklılık normal tüketilir.

## 1.0.1 kalite kontrolü — sonraki normal oyun oturumunda

- [ ] Normal maksimumu 4 olan bir eşya seç; kalite alanında normal 1–4 aralığı ve üst sınırın uygulanmadığı bilgisi görünür.
- [ ] Kalite 30 ile önizle ve envantere/yere üret; eşyanın kalitesi 30 kalır. Şablon kaydet/yükle aynı değeri korur.
- [ ] Envanter eşyasını kalite 30 yapıp Uygula; kalite korunur, dayanıklılık oranı ve yabancı mod verileri korunur.
- [ ] Sıfır, negatif, kesirli ve geçersiz kalite reddedilir; normal efekt değer/sayı, soket ve adet sınırları devam eder.

Bu liste tek oyun açılışında uygulanır; her küçük patch için yeniden başlatma gerekmez. Çalışan profilde EpicLoot 0.14.13 ve tek EpicLootSpawner DLL olmalı. Sonraki normal açılıştaki kalıcılık kontrolü ayrıca belirtilmiştir. Otomatik kontroller gerçek Unity/oyun sonucunun yerine geçmez.

Kullanıcı 0.9.3 ve 0.9.4 için genel olumlu geri bildirim verdi; 0.9.4 sonucu “her şey düzgün” olarak kaydedildi. 1.0.0 kaynak/proje ve bağımlılıkları temel sürümle sürüm alanları hariç eşleşir. Sürüm değişimi için ayrı oyun açılışı istenmez. Aşağıdaki maddeler kapsamlı kontrol rehberidir; her biri için ayrı sonuç bildirilmediğinden işaretlenmedi.

## Menü ve seçim

- [ ] Katalog henüz hazır değilken F8 giriş/imleç kilidi bırakmaz. Dünya hazır olduktan sonra panel açılır; açma/kapama sonrasında oyuncu normal giriş alır. Beklenmedik hata bildiriliyorsa F8 ile yeniden aç ve ilgili günlüğü kaydet.

- [ ] F8 aç/kapat, Esc ve modal pencereler çalışır; ana panelde ikinci ve sonraki eşya seçilir. Şablon, envanter, efekt, taş ve önizleme listeleri kalan yüksekliği kullanır ve kaydırılır.
- [ ] Arama + kategori + favori birlikte çalışır; Tier + Ad/Tür/Favori sıralaması ve filtre temizleme beklenen eşyaları gösterir.
- [ ] Ölçek %75/%100/%150, panel sürükleme ve küçük ekran düzeninde ana eylemler erişilebilir; günlükte GUILayout hatası yoktur.
- [ ] Uyumsuz efektler listelenmez; oyun içi ad/açıklama/aralık görünür. Geçersiz sayı hata verir; efekt değerlerinde sessiz kırpma veya efekt limiti üstü üretim olmaz.
- [ ] Efekt sınırı/dolu soket/boş sonuç eylemleri doğru sekmeye yönlendirir. Taş sekmesinde önizlemeler gruplar halinde tamamlanır; tasarım değişince eski sonuç gösterilmez.

## 0.9.4 kullanım ve tanılama

- [ ] Uzun katalog/envanter/şablon adları kolon dışına taşmadan sarılır; tüm satırlar kaydırılır ve seçilebilir. İmleç üzerindeyken tam ad/prefab bilgi kutusu görünür; bilgi kutusu düğme girişini engellemez.
- [ ] Uzun alan hatası/yardım metni kesilmeden okunur. Envanter Uygula engeli ve ana eylemler küçük ekranda erişilebilir.
- [ ] Oyun Türkçe ve İngilizce olduğunda menü Türkçe kalır; eşya/nadirlik/efekt adları ve Lumberjack açıklaması oyun dilini izler. Spawner'ın özel odun notu ayrı Türkçe metindir.
- [ ] Görünüm/Tanılama/Kullanım sekmeleri, Panele dön ve Esc çalışır. Görünümde seçilen ölçeğin uygulanacak gerçek yüzdesi ekrana sığdırmayla eşleşir.
- [ ] Ayrıntı Tanılama'yı açar; raporu kopyalayıp sürüm/dil/ekran/ölçek/tasarım/alan hatası satırlarını kontrol et. Rapor panoya gider; dışarıya otomatik gönderilmez.
- [ ] Açma tuşunu değiştirdiysen Kullanım ve hata kurtarma mesajı yeni tuşu gösterir. Tanılama son hata özetini ve ayrıntısını aynı hataya ait olarak gösterir.

## Tier sıralaması

- [ ] Tier + Ad seçildiğinde başlangıç → düşük → yüksek tier sırası ve grup başlıkları görünür. Aynı tier içinde eşya adı A–Z sıralanır; nadirlik veya yükseltme seviyesi liste sırasını değiştirmez.
- [ ] Tier bilgisi olmayan modlu eşyalar Tier bilinmiyor grubunda en sonda ve ada göre listelenir. Arama/kategori/favori filtreleriyle sıra korunur.
- [ ] Tür ve Favori seçeneklerinde ilgili gruplar korunur; grup içinde tier ve ad sırası kullanılır. Tier grubunun ikinci ve sonraki eşya satırları seçilir; kaydırma alanı kalan yüksekliği kullanır.
- [ ] EpicLoot ilerleme ayarını değiştirdiysen Yenile sonrasında yeni sıra kullanılır. Özel boss/biyom sırası sabit vanilla malzeme adına göre ezilmez.

## Üretim ve asıl eşya

- [ ] 1 ve 100 adet üretim; dolu envanter; üretimi durdurma; envanter ve yere teslim doğru adet verir. Durum alanı tamamlanan/kalan adedi doğru gösterir.
- [ ] Normal dayanıklı eşyaya doğrudan Kırılmazlık ekleyip üret. Aynı oturumda envanterden seç, efekti kaldır, Uygula: kullanımda dayanıklılık yeniden düşer. Envantere ve yere teslimi ayrı dene.
- [ ] Aynı akışı Kırılmazlık veren shardstone ile dene; taşı kaldırınca dayanıklılık tüketimi geri gelir. Başlangıçta dayanıklılık kullanmayan eşya bu özelliğini korur.
- [ ] Envanter düzenlemesinde önizleme asıl eşyayı değiştirmez. Uygula seviye/efekt/soketi değiştirir; konum, varyant, diğer mod verileri ve dayanıklılık oranını korur. Kullanılan veya dışarıdan değişmiş eşya engellenir; yeniden seçim eylemi çalışır.
- [ ] Farklı envanter eşyaları arasında seçim ve geri al/ileri al doğru kaynağa döner; değişiklik özeti ve tam tooltip taş katkısını gösterir.
- [ ] Lumberjack için tam sayı ve kesirli ekipman/taş toplamını dene; normal dönüşümle örneğin 31,5 toplamı 31 ekstra odun verir.

## Şablon koleksiyonu ve paylaşım

- [ ] Yeni şablon kaydet; etiket ekle; ad/etiket filtresi ve kart özetini kontrol et. Farklı adla kopyala; kopyayı düzenle; özgün kayıt korunur. Yeniden adlandırma/silme etiketleri kapsar.
- [ ] Birkaç kaydı ✓ ile seç ve yeni JSON dosyasına aktar. Var olan dosya yolu üzerine yazımı engeller. Dosyayı oku; kayıtlar önizlenir, tasarım değişmez.
- [ ] Mevcut adla çakışmayı farklı ad vererek çöz ve yeniden seç. Yalnızca seçilen kayıtları al; eski kayıt üzerine yazılmaz. Geçersiz/uyumsuz kayıt seçilemez. Bozuk başka dosya okumak eski içe alma önizlemesini temizler.
- [ ] Pano ile tek tasarım gidiş/dönüşü çalışır; bozuk metin mevcut tasarımı korur.
- [ ] Güncelleme öncesi v1 depo varsa ilk kayıt işleminde .v1.bak oluşur; sonraki yazımlarda bu özgün yedek korunur.

## Kurtarma ve tanılama

- [ ] Görünüm tanılamasında mod/EpicLoot sürümü, süreler ve son işlem/hata görünür; panoya kopyalanır.
- [ ] Mevcut .bak yedeğini incele, sonra vazgeç: aktif dosya değişmez. Alınacak/atlanacak sayılar ve kaynak gösterilir.
- [ ] Geri yüklemeyi kontrollü bir test verisinde dene: onaydan önce yazım olmaz; onay mevcut dosyanın .corrupt-… kopyasını saklar. Önizleme sonrası dosya dışarıdan değişirse yazım reddedilir.
- [ ] Taslak kurtarma paneli kapatır; sonraki F8 açılışında kurtarılan tasarım yüklenir. Mevcut envanter eşyasına yazım yapılmaz.

## Sonraki normal oyun açılışı

- [ ] Şablonlar/etiketler/favoriler, son taslak, ölçek/konum ve sıralama tercihi korunur. Taslak asıl envanter nesnesine otomatik bağlanmaz.

Hata bildiriminde sürüm, eylem sırası, beklenen/gerçek sonuç ve BepInEx günlüğündeki ilgili hatayı kaydet. Görünüm penceresinin tanılama metni eklenebilir.

0.9.2 görünüm ek kontrolü: seçili satır/sekme, uzun eşya isimleri, arama temizleme düğmeleri, Envantere/Yere bırak seçimi ve ölçek değişiminde eylem düğmelerinin erişimini kontrol et.
