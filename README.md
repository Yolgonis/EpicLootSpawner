# EpicLoot Spawner 1.4.1

EpicLoot 0.14.13 için ayrı, client side eşya üretim ve düzenleme modu. Kalite seviyesinde eşyanın normal üst sınırını aşmaya izin verir. Efekt değeri, efekt sayısı ve soket sınırları normalde EpicLoot'u izler; isteğe bağlı Limitsiz mod bunları kaldırır. Eşya/efekt uyumluluğu her zaman korunur.

## 1.4.1: Limitsiz mod oyunda da etki eder

1.4.0'da aralık dışı değerler eşyaya yazılıyordu, ama EpicLoot bazı efekt toplamlarını kendi kodunda kırptığı için oyunda fark yaratmıyordu. Limitsiz mod açıkken bu tavanlar da kalkar: direnç tavanı (MaxResistance yerine en fazla %100), Bulk Up can bonusu, Mercenary yumuşak tavanı, Lucky Loot çarpanı ve adet sınırları, parry penceresi, yavaşlatma %10 tabanı, iksir bekleme süresi, gece ve Off-Set hasar azaltma, kan bedeli azaltma, Shocking Charge en az yük ve adrenalin verimi. Alınan hasarın negatife düşmesi, sıfırın altına inen maliyet/süre ve 1'in altına çekilen can gibi oyunu bozan güvenlik sınırları korunur. Tavanlar kuşanılan tüm eşyalar için birlikte kalkar; mod kapatılınca EpicLoot varsayılanı geri gelir. Ayarlar → Tanılama, bağlanan tavan sayısını gösterir.

1056 kaynak kontrolü ve kurulu EpicLoot DLL'i üzerinde 71 canlı Harmony kontrolü. Oyun içi etki **Pending**.

## 1.4.0: yeni arayüz ve Limitsiz mod

Panel tek sayfalık bir tasarım ekranına dönüştürüldü:

- **Üst çubuk:** Yeni eşya / Envanterim modu, ← Geri / İleri →, Şablonlar, Limitsiz, Ayarlar, Yenile, Kapat.
- **Sol sütun:** eşya listesi (arama, kategori, favori, sıralama) ya da Envanterim veya Şablonlar.
- **Orta sütun:** nadirlik, seviye/adet/soket (− / + ile), özel ad, efektler ve soket yuvaları aynı sayfada. Sekme yok.
- **Seçiciler:** + Efekt ekle, + Taş tak ve Legendary / Set… üstte bir pencere açar. Her kart tam oyun içi açıklamayı, notları ve normal aralığı gösterir. Pencere açıkken birden fazla seçim yapılabilir; Bitti ile kapanır.
- **Sağ sütun:** canlı önizleme; envanter düzenlemede Değişiklikler / Yeni eşya / Mevcut.
- **Alt çubuk:** teslim yeri, durum, son işlemi geri alma ve her zaman görünen ana düğme (Eşyayı üret / Asıl eşyaya uygula). Envanter modunda Büyüyü kaldır… ve Taş çıkar… da buradadır.

**Limitsiz mod** üst çubuktan veya config'teki `Tasarım → Limitsiz mod` ayarından açılır. Açıkken efekt değerleri normal aralığın dışına çıkabilir (negatif dahil), en fazla 30 efekt ve 12 soket kullanılabilir. Eşya türüne uymayan, çakışan veya yinelenen efektler yine eklenemez; değerler sonlu sayı olmalıdır. Aralık dışı değerler efekt kartında sarı uyarıyla işaretlenir. Kurulu EpicLoot kaynağında taş takma yalnızca boş soketi denetler; fazladan soketlere taş takılabilir. Çok sayıda efekt veya aşırı değerlerin EpicLoot'un enchanting/augment ekranlarında nasıl davrandığı oyun içinde henüz denenmedi.

Nadirlik değiştirmek artık efekt ve taşları silmez; uymayan değerler alan hatası olarak gösterilir. 1014 kaynak kontrolü (441 temel/dosya/geometri, 338 davranış, 206 üretim, 29 panel). Yeni arayüzün oyun içi görünümü **Pending**.

## 1.3.2: ganimet gibi üretici bilgisi

Spawner ile yeni üretilen eşyalarda üretici kimliği `0`, üretici adı boş olur. Oyun bu nedenle **Crafted by / Üreten** satırını göstermez. Önizleme, envantere/yere teslim, çoklu üretim ve özel Legendary/set üretimi aynı temel oluşturma yolunu kullanır. Bu değişiklik native eşya verisindedir; genel tooltip satırı gizlenmez. Mevcut envanter eşyalarını düzenleme önceki üretici bilgisini korur; eski eşyalar topluca değiştirilmez.

Kurulu Valheim ItemData clone/tooltip/kayıt akışı incelendi: üretici bilgisi yalnız kimlik sıfır değilse gösterilir ve kaydedilir. Başsız testler üretici verisi olan prefab fixture'ından üretilen eşyada her iki alanın temizlendiğini, prefabın değişmediğini ve teslimde temiz kaldığını doğrular. **987 kaynak kontrolü** (441 temel/dosya/geometri, 338 davranış, 180 üretim, 28 panel); 8 yeni üretim regresyonu. 9 paketleme ve 16 izole kurulum kontrolü. Native oyun kabulü **Pending**; sonraki normal oturumda eşya açıklaması gözlenebilir.

## 1.3.1: genel kod incelemesi ve düzeltmeler

Mevcut üretim, envanter, taş iadesi/geri alma, şablon, kayıt/kurtarma, arayüz/geometri ve paketleme kodları gözden geçirildi. Eksik sürüm/liste alanlı şablon ve favori dosyaları artık boş kayıt sayılmaz; dosya ve önceki bellek durumu korunur, yazma engellenir. Geçerli eski şablon biçimi ve açıkça boş listeler desteklenmeye devam eder.

NaN/sonsuz/ters efekt aralıkları alan hatası olarak gösterilir ve üretimi engeller; böyle bir aralık için kaydırıcı ve minimum/maksimum işlemleri çizilmez. Geçersiz kaydırıcı adımı sonlu girdiyi NaN'a dönüştürmez. Hatalı efekt isim/açıklama biçimi katalogdaki diğer güçleri gizlemez. Önizleme yeniden kontrol edilirken eski alan hataları temizlenir.

Yüklenemeyen şablon tasarım, kayıt adı, etiket ve envanter seçimini değiştirmez. Bozuk taslak geri yükleme alanları değiştirmeden reddedilir; tamamlanmamış sayı girdileri saklanmaya devam eder. Eksik, yinelenmiş veya null Legendary başlangıç güçleri native değer hesabından önce yakalanır; kimlik hatası gösterilir, üretim engellenir ve mevcut tasarım korunur.

**979 kaynak kontrolü** (441 temel/dosya/geometri, 338 davranış, 172 üretim, 28 panel); önceki sürüme göre 88 regresyon eklendi. Release akışı iki bağımsız derlemenin eşitliğini 9 kontrolle, kurulum/güncelleme ise izole profilde 16 kontrolle sınar. Gerçek bağımlılıklara karşı derleme yapılır; oyun içi son kabul **Pending** kalır. Ayrıntılı inceleme `CODE-REVIEW.md`, sonraki normal oyun oturumunun kontrolleri `TESTPLAN.md` içindedir.

## 1.3.0: özel Legendary/set üretimi ve düzenleme

Temel eşyayı seçip tasarım alanındaki **Legendary / Set** sekmesini aç. Yüklenen EpicLoot tanımlarından yalnız bu eşya türüne uygun özel kimlikler listelenir; bağımsız/set filtreleri ve ad/kimlik/set araması bulunur. Özel eşya adı, açıklaması, nadirliği, set adı ve kaç parçada hangi bonusun etkinleştiği oyun dilinde gösterilir. Mythic kayıtları aynı native katalog üzerinden desteklenir; runestone kapsamı değişmez.

Kimlik seçimi mevcut efekt/taş/özel ad seçimlerini temizler; kalite ve adet korunur. EpicLoot başlangıç güçleri kendi minimum değerleriyle eklenir; nadirlik ve set üyeliği kimlikten alınır. Değer alanı, kaydırıcı, Minimum/Maksimum ve doğrulama `GetRollRange` ile özel eşyanın değer tanımını kullanır. Bu aralık sıradan nadirlik aralığından farklı olabilir. Yapılandırılmış GuaranteedEffectCount varsa kullanılır; başlangıç güçlerinin sayısından küçük bir toplam sınır uygulanmaz. Ek güçler normal eşya/çatışma kurallarına tabidir; soket/adet sınırları korunur.

**Envanter** sekmesinde bilinen, tanımlanmış ve desteklenen soketlere sahip özel eşya artık seçilebilir. Önizleme ve **Asıl eşyaya uygula** kimliği/set üyeliğini korur; envanter düzenlemesinde kimlik değiştirme ve nadirlik seçimi kapalıdır. EpicLoot'un augment edilebilir başlangıç gücü değiştirilmiş mevcut eşyaları desteklenir. CanBeAugmented=false olan kimliğe ait sabit gücün silinmesi engellenir; değeri tanımlı aralıkta düzenlenebilir. Tür adı, augment/temper indeksleri, yabancı mod verileri ve dayanıklılık oranı önceki kurallarla korunur.

Tek taş çıkarma kimlik/set üyeliğini korur. Tam büyü kaldırma bunları da kaldırır ve fark özetinde gösterir; son envanter işlemini geri alma kimlik, büyü ve soketleri geri yükler. Şablon, pano/koleksiyon aktarımı, son tasarım ve tasarım geçmişi LegendaryID/SetID alanlarını taşır. Eski şablonlar alanları olmadan okunur. Yeni özel kimlikli kayıtlar eski mod sürümlerine aktarılmaz; eski sürüme dönüşte önceki yedekleri kullan.

Eksik özel tanım, yanlış eşya türü, yanlış nadirlik veya tutarsız set eşleşmesi üretim/yazımdan önce reddedilir; sessizce sıradan eşyaya dönüş yapılmaz. Yeni kimlik seçimindeki hata mevcut tasarımı değiştirmez. Yapılandırma değiştiğinde **Özel tanımları yenile** ile liste yenilenir. Katalog yalnız bu sekme açıldığında seçili temel eşya için hesaplanır; OnGUI içinde kimlik yazımı yapılmaz.

**891 kaynak kontrolü** (395 temel/dosya/geometri, 328 davranış, 140 üretim, 28 panel), 9 paketleme ve 16 izole kurulum kontrolü. Kurulu EpicLoot 0.14.13 DLL'inde kimlik/set lookup ve GetRollRange API'leri incelendi; gerçek bağımlılıklara karşı derleme yapılır. Başsız testler native Unity çizimi, equip görseli veya giyilen setin gerçek oyun bonusunu ölçmez. Yeni akışların oyun kabulü sonraki normal oyun oturumunda topluca kaydedilir; GameplayValidation=Pending.

## 1.2.0: yeniden boyutlandırılabilir pencere

Ana pencerenin **sağ alt köşesindeki ◢ tutamacını** sol fare tuşuyla sürükle. Genişlik ve yükseklik bağımsız değişir; yazı ölçeği aynı kalır. Kütüphane/tasarım/önizleme sütunları ile kaydırma alanları yeni boyuta uyarlanır. Başlıktan taşıma aynı şekilde çalışır.

Pencere ekrandan taşmadan büyür; üç sütunun ve ana eylemlerin okunabilir kalması için alt boyut 1180×740 sanal arayüz birimidir. Küçük ekranda mevcut otomatik ölçek uygulandığından bu değer fiziksel piksel ölçüsü değildir. Görünüm alanı gerçek piksel boyutunu gösterir. Seçim sürükleme bırakılınca ve panel kapanırken config'e kaydedilir; sonraki açılışta geri yüklenir. Daha küçük çözünürlüğe geçince görüntü sınırlanır, büyük ekran için kaydedilen tercih korunur.

**Görünüm → Pencere boyutunu sıfırla**, yalnız boyutu ekranın varsayılanına döndürür; **Varsayılan** konum/ölçek/boyutu birlikte sıfırlar. Modal onay açıkken ana pencere boyutlandırılmaz. Panel kapatma, odak kaybı ve ekran/ölçek değişimi sürükleme yakalamasını bırakır. Boyutlar GUI olayının ortasında değiştirilmez; Update içinde uygulanır. Genişlik değişince eski metin yüksekliği önbelleği temizlenir.

**811 kaynak kontrolü** (384 temel/geometri/dosya, 297 davranış, 102 üretim, 28 panel), 9 paketleme ve 16 izole kurulum kontrolü. Boyut/ölçek dönüşümü, sınırlar, çözünürlük değişimi, geçersiz config ve kapatma akışı başsız testlerle kontrol edilir; native fare sürükleme ve çizim sonucu sonraki normal oyun oturumunda birlikte kontrol edilebilir.

## 1.1.1: mevcut envanter işlemlerinde okunabilirlik

Kullanıcı tercihi doğrultusunda mevcut özellikler ve arayüz iyileştirildi; özel Legendary/set ve runestone kapsamı genişletilmedi.

**Tek taş çıkar…** ekranı taş ikonu, soket numarası, oyun dilindeki taş/efekt adı, mevcut soket katkısı ve açıklamayı kartlarda gösterir. Etkisiz taşlar ayrı açıklanır. Uzun taş adları sabit genişlikte, metne göre hesaplanan yükseklikte sarılır. Taş seçimi, Değişiklikler ve Son eşya sekmeleri bulunur. **Büyüyü kaldır…** ekranı önce değişiklik özetini açar; taş iadeleri ad ve adet halinde gruplanır.

**Son envanter işlemini geri al…** engel olsa da ayrıntılar için açılabilir. Değişiklikler, Geri yüklenecek eşya ve Gerekli taşlar sekmeleri bulunur. Eksik taş mesajı bütün eksik türleri, gereken/mevcut/eksik adetlerini gösterir. Gerekli taşlar geri getirildiğinde açık penceredeki onay tekrar kullanılabilir; onay anındaki kontroller sürer.

Geri alma önizlemesi eski kayıt dayanıklılığını göstermek yerine mevcut eşyanın dayanıklılık oranına göre hazırlanır; gerçek geri alma aynı hazırlama yöntemini kullanır. Açık pencereler tamir/aşınma veya oyun dili değişince Update içinde yenilenir; değişmeyen soket hesabı tekrar yapılmaz. Önizleme ve seçim asıl eşyayı değiştirmez.

**737 kaynak kontrolü** (311 temel/dosya, 297 davranış, 102 üretim, 27 panel), 9 paketleme ve 16 izole kurulum kontrolü. Yeni davranış, menü seçimleri ve önizleme eşleşmesi başsız Unity/API düzenekleriyle kontrol edilir; native font/çizim ve yeni akışlar için oyun sonucu sonraki normal oturumda birlikte kaydedilebilir.

## 1.1.0: tek taş çıkarma ve uygulanmış işlemi geri alma

Envanterden büyülü eşyayı seç; **Tek taş çıkar…** penceresinde asıl eşyanın takılı taşlarından birini seç ve **Seçili taşı çıkar ve iade et** ile onayla. Taslak seçimleri uygulanmaz. Büyü, nadirlik, özel ad, normal efektler, toplam soket sayısı ve diğer taşlar korunur. EpicLoot 0.14.13 `RemoveShard` API'si kalan taş katkısını yeniden hesaplar. Seçili taş envantere iade edilir; yer yoksa hiçbir değişiklik yapılmaz.

**Son envanter işlemini geri al…**, bu oyun oturumundaki son başarılı **Uygula**, **Büyüyü kaldır** veya **Tek taş çıkar** işlemini ayrı onay/önizleme ile geri alır. En fazla bir işlem tutulur; sonraki başarılı envanter işlemi önceki kaydın yerini alır. Menü kapanıp açılınca kayıt korunur; dünyadan/karakterden çıkınca silinir, dosyaya yazılmaz. Üretim ve tasarım geçmişi bu kayda dahil değildir.

Geri alma eşyanın önceki kalite/büyü/soketlerini aynı ItemData üzerinde geri yükler. Konum ve mevcut dayanıklılık oranı korunur. Taş iadesi yapılmışsa aynı tür ve metadata taşıyan gerekli sayıda taş envanterden alınır; eksik/harcanmış/değişmiş taş, kayıp/kullanılan/dışarıdan değişmiş eşya veya farklı oyuncu envanteri geri almayı engeller. Eşdeğer taşlar envantere geri getirildiğinde yeniden denenebilir. İşlem öncesindeki tüm envanter daha sonra geri yüklenmez; sonradan edinilen diğer eşyalar korunur. İşlem ortası hatasında aynı işlemdeki yazımlar ve yığın miktarları geri yüklenir; çift onay ek taş üretmez.

**713 kaynak kontrolü** (311 temel/dosya, 273 davranış, 102 üretim, 27 panel), 9 paketleme ve 16 izole kurulum kontrolü uygulanır. Release derlemesi kurulu EpicLoot/Valheim API'lerine karşı yapılır. Başsız testlerde Unity/API bağlantıları kısmen taklit edilir; yeni 1.1.0 oyun kontrolü sonraki normal oturumda topluca yapılabilir.

2026-10-09 kullanıcı bildirimi: **“Büyü kaldırma düzgün çalışıyor”**. Bu, önceki 1.0.2 özelliğine ait olumlu oyun sonucudur; yeni tek taş çıkarma ve geri alma kabulü değildir. Ayrı kanıt `tests/evidence/UserFeedback-1.0.2.json` içinde saklanır; önceki ZIP'ler değiştirilmez.

## 1.0.2: büyü kaldırma ve taş iadesi

Envanter sekmesinden eşyayı seç; Düzenle alanındaki **Büyüyü kaldır…** düğmesini kullan. Onay penceresi normal eşya önizlemesini ve iade edilecek taşları gösterir. **Büyüyü kaldır ve taşları iade et**, asıl eşyanın efektlerini, büyülü nadirlik/özel adını ve tüm soketlerini kaldırır. Mevcut tasarımın değişiklikleri uygulanmaz. Kaynak eşyadaki shardstone'lar EpicLoot'un ReconstructShardItem API'siyle geri oluşturulur ve envantere iade edilir.

Kalite seviyesi (normal maksimumun üzerindeki değerler dahil), varyant, konum, yabancı mod verileri ve dayanıklılık oranı korunur. Kullanılan eşyayı önce çıkar. Önizleme/Vazgeç/Esc asıl eşyayı değiştirmez. Eşya dışarıdan değişirse veya taşlar için topluca yeterli yer yoksa işlem reddedilir. Başarısız iadede eklenen taşlar ve birleştirilmiş yığınlar geri alınır; başarısız büyü kaldırmada önceki büyü verisi geri yüklenir. Geri al/ileri al tasarım geçmişidir; bu yazımı otomatik geri almaz.

Mevcut envanter editörünün desteklediği eşyalar kapsanır; özel Legendary/set kimliği, tanımlanmamış eşya ve desteklenmeyen soket engelleri korunur. Normal tasarımda taş çıkarma malzeme iadesi yapmaz; bu yeni tam büyü kaldırma işlemi asıl eşyadaki taşları iade eder.

622 kaynak kontrolü (311 temel/dosya, 183 davranış, 102 üretim, 26 panel), 9 paketleme ve 16 izole kurulum kontrolü uygulanır. Native API imzaları kurulu EpicLoot 0.14.13 ve Valheim DLL'lerinden incelendi; başsız envanter/API testleri canlı oyunun yerine geçmez. Sonraki normal oyun oturumunda yeni işlemi kontrol edebilirsin.

## 1.0.1: kalite üst sınırı bilgi olarak gösterilir

Normal kalite aralığı 1–4 olan bir eşyaya 30 yazıldığında önizleme, üretim, şablon aktarımı ve envanter Uygula kaliteyi 30 olarak korur. Menü normal aralığı ve üst sınırın uygulanmadığını gösterir; prefabın normal maksimumu değiştirilmez. Giriş en az 1 olan, 32 bit tam sayıya sığan bir sayı olmalıdır. Efekt değer/sayı sınırları, soket kuralları, uyumsuz efekt kontrolü ve adet sınırı aynı kalır.

575 kaynak kontrolü (311 temel/dosya, 137 davranış, 102 üretim, 25 panel), 9 paketleme ve 16 izole kurulum kontrolü uygulanır. Oyun kodu değiştiği için önceki 1.0.0 genel kabulü yeni sürüme taşınmaz; paket gameplay durumu Pending olur. Kalite 30 üretimini sonraki normal oyun oturumunda kontrol edebilirsin; bu küçük güncelleme için ayrı oyun açılışı gerekmez.

İkon ve İngilizce mod yöneticisi özellik sayfası pakete dahildir. Kaynak README geliştirme belgelerini korur; runtime README yalnızca MOD-PAGE.md özellik listesidir.

## 1.0.0: ilk tamamlanmış yerel paket

1.0.0, kullanıcının **“her şey düzgün”** olarak doğruladığı 0.9.4 üzerine hazırlanmıştır. Modun oyun ve arayüz kodu aynı kalır; kaynak/proje sürümü ve paket belgeleri güncellenmiştir. Paketleme, tüm `src/*.cs` dosyalarını ve derleme projesini sürüm alanları hariç temel sürümle aynı SHA256 girdilerinde doğrular. Derleme bağımlılıkları da temel sürümün hash değerleriyle eşleşir.

- Eşya, seviye, nadirlik, uyumlu efektler ve shardstone soketleri tasarlanabilir.
- Tier + Ad sırası düşükten yükseğe ilerler; aynı tier içinde oyun içi ad A–Z kullanılır. Kategori, arama ve favoriler birlikte çalışır.
- Envanter seçimi önizleme oluşturur; **Asıl eşyaya uygula** seçili eşyayı değiştirir. Geri al/ileri al tasarım geçmişini yönetir.
- Etiketli şablonlar, koleksiyon/pano aktarımı, son tasarım ve yedek kurtarma bulunur.
- Türkçe menü, oyun dilindeki eşya/efekt metinleri, sarılan uzun adlar, Görünüm/Tanılama/Kullanım sekmeleri ve rapor kopyalama bulunur.

**534 kaynak kontrolü**, **13 paketleme kontrolü** ve **16 izole kurulum kontrolü** başarılı. İki bağımsız derlemede aynı DLL, manifest ve ZIP hash değerleri alındı. Kurulum/güncelleme için `INSTALLING.md`, kaynak derlemek için `BUILDING.md` kullan. Önceki şablon/favori/taslak dosyalarını koruyarak mevcut DLL güncellenebilir.

`ACCEPTANCE.json`, 0.9.4 kullanıcı geri bildirimini ve kod/bağımlılık eşleşmesini kaydeder. Bu genel geri bildirim ayrıntılı kontrol listesinin her maddesi için ayrı sonuç yerine geçmez; farklı dil, çözünürlük ve sürümler için özel ölçüm kaydedilmedi. 1.0.0'ın sürüm değişimi ayrı oyun açılışı gerektirmez; sonraki normal kullanımda sürüm etiketi kontrol edilebilir.

Bu paket yerel kullanıma hazırdır. Harici platforma dağıtım için lisans, yazar ve yayın hedefi ayrıca kararlaştırılır.

## 0.9.4: 1.0 hazırlığı — kullanım ve tanılama

**Menü Türkçe kalır; eşya/nadirlik/efekt metinleri oyun dilini izler.** Lumberjack'in EpicLoot açıklaması artık Türkçe özel metinle değiştirilmez; spawner'ın odun davranışı ayrı **Spawner notu** olarak gösterilir. Efekt listesindeki arama hem oyun açıklamasını hem bu notu kapsar; dil değişince önbellek yenilenir.

Eşya/envanter/şablon adlarına kolon içinde ayrılan genişlik sabittir; satır yüksekliği sarılmış metne göre hesaplanır. Uzun adlar listeyi yatayda genişletmez. Editör/önizleme başlığı sınırlı alan kullanır; tam ad ve prefab imleç üzerindeyken alt taraftaki bilgi kutusunda gösterilir. Satır yüksekliği ad/genişlik bazında önbelleğe alınır; katalog/dil/tema yenilenince temizlenir. Alan hata/açıklama satırları metne göre büyür; kaydırma alanları kalan yüksekliği kullanmaya devam eder.

**Görünüm → Görünüm / Tanılama / Kullanım** sekmeleri eklendi. Görünüm, seçilen ölçeğin mevcut ekrana uygulanacak gerçek yüzdesini gösterir. Kullanım, üretim/Uygula, tier, tuşlar, sınırlar ve kayıtların kısa rehberidir. Durum çubuğundaki **Ayrıntı** doğrudan Tanılama'yı açar. **Tanılama raporunu kopyala** sürümleri, dil/ekran/ölçek, katalog ve tasarım durumunu, alan hatalarını, süreleri ve son hatanın tam exception ayrıntısını panoya yazar. Rapor otomatik gönderilmez. Hata kurtarma mesajı ayarlanan açma tuşunu kullanır.

Release derlemesi sıfır hata/uyarıyla tamamlandı; **534 kaynak kontrolü** (304 temel, 133 davranış, 72 üretim, 25 panel), ayrıca 9 paketleme ve 16 izole kurulum kontrolü geçti. Kullanıcı 0.9.3 görünümü/liste düzeninin düzgün olduğunu bildirdi; bu geri bildirim ayrıntılı üretim, soket, kurtarma ve farklı çözünürlük kabulünün tamamını doğrulamaz. Yeni 0.9.4 görünüm/rapor/uzun ad kontrolleri sonraki uygun oyun açılışında topluca yapılabilir.

## 0.9.3: tier ve ada göre eşya sıralaması

Varsayılan **Tier + Ad** sıralaması eşyaları düşükten yüksek tier'a, aynı tier içinde oyunda görünen adlarına göre A–Z sıralar. Listede **Tier 0 • Başlangıç**, **Tier 1**, **Tier 2** gibi grup başlıkları görünür. Tier burada EpicLoot'un yüklü `iteminfo` ve biyom/boss ilerleme verisidir; nadirlik ve eşya yükseltme seviyesi değildir. Paylaşılan boss anahtarları tek ilerleme basamağı sayılır; birden fazla boss gereksinimi olan eşya en ileri gereksiniminde listelenir.

Tier bilgisi olmayan veya bilinmeyen boss gerektiren modlu eşyalar **Tier bilinmiyor** grubunda en sonda, ada göre sıralanır. İsimden malzeme/tier tahmini yapılmaz. **Tür** ve **Favori** seçenekleri kendi gruplarını korur; her grup içinde tier ve ad sırası kullanılır. Arama, kategori ve yalnızca favoriler filtreleri birlikte çalışır. Önceki Ad tercihi (config değeri 0) artık Tier + Ad anlamına gelir; diğer tercihlerin numaraları değişmez. EpicLoot ilerleme ayarları değişince menüde **Yenile** kullan veya paneli yeniden aç.

Release derlemesi ve **523 kaynak kontrolü** (304 temel, 126 davranış, 72 üretim, 21 panel), ayrıca 9 paketleme ve 16 izole kurulum kontrolü geçti. Tier verisi katalog yenilenirken hazırlanır; çizimde tekrar taranmaz. Canlı grup başlığı/kaydırma kontrolü sonraki uygun oyun açılışında TESTPLAN.md ile yapılabilir.

## 0.9.2: arayüz düzenlemesi

Koyu tema; yuvarlatılmış kartlar, ince çerçeveler, belirgin seçili satırlar ve daha okunaklı ana eylemlerle yenilendi. Kütüphane ve editör genişletildi; panel araları düzenlendi. İkonlar tutarlı çerçevelere alındı. Aramalarda temizleme düğmesi; üretimde Envantere / Yere bırak seçimi bulunur. Seçili eşya adı/ikonu ve önizleme özeti daha görünürdür.

Normal limitler, yalnızca uyumlu efektler, şablonlar ve asıl eşyaya Uygula davranışı korunur. Ayrıntılı önce/sonra tablosu `UI-CHANGES.md` içindedir. Release derlemesi ve 499 kaynak kontrolü, ayrıca 9 paketleme ve 16 izole kurulum kontrolü geçti. Yeni görünümün canlı Unity doğrulaması sonraki uygun oyun açılışında yapılabilir.

## 0.9.1: panel kurtarma ve doğrulamalı güncelleme

Panel, oyuncu/dünya/katalog/dil verisi hazır olduktan ve katalog başarıyla yüklendikten sonra açılır. Açılış hatası oyuncu girişini engellemez. Beklenmedik çizim/güncelleme hatasında panel kapanır, imleç geri bırakılır, devam eden üretime durma isteği gönderilir ve günlükte hata saklanır. F8 ile tekrar açılabilir; hatadan kalan tema yeniden oluşturulur. Kapatmada kayıt hatası olsa da imleç ve modal/iş kuyruğu temizlenir. Normal limitler ve envanter Uygula davranışı korunur.

Yeni kurulum/güncelleme aracı paketi ve gerçek EpicLoot sürümünü doğrular; varsayılan olarak yalnızca önizleme yapar. `-Apply` ile, oyun kapalıyken, önceki DLL'i plugins dışında yedekleyip yeni DLL'i atomik olarak kurar. Yeniden adlandırılmış ikinci spawner DLL'lerini de bulur; paket/kurulu DLL değişikliği varsa işlemi durdurur. Kullanım `INSTALLING.md` içindedir.

Release derlemesi sıfır hata/uyarıyla geçti. **499 kaynak kontrolü** (304 temel, 102 davranış, 72 üretim, 21 panel), ayrıca **9 paketleme ve 16 izole kurulum kontrolü** geçti. Gerçek oyun doğrulaması bekliyor; 1.0.0 kabulü henüz tamamlanmadı. Bu küçük sürüm için ayrı oyun açılışı gerekmez; TESTPLAN.md toplu kontrolünü sonraki uygun açılışta kullan.

## 0.9.0: birleşik geliştirme sürümü

0.7.2 üretim düzeltmeleri, 0.8.0 şablon koleksiyonu ve 0.9.0 kullanım iyileştirmeleri tek pakette tamamlandı. Kırılmazlık etkili üretimde başlangıç dayanıklılık bilgisi hem envantere hem yere teslimin kopyalama adımlarında korunur; doğrudan efekt veya soket katkısı kaldırıldığında normal dayanıklılık kullanımı geri gelir. Geçmişten çıkan envanter kaynakları temizlenir.

### Etiketli şablon koleksiyonu

**Şablonlar** listesi ikon, eşya adı, nadirlik, seviye, efekt/soket sayısı ve etiket özetini gösterir. **Yeni kaydet / Düzenle** ayrı kayıt formunu açar. Şablon farklı adla kopyalanabilir; ad değiştirme ve silme işlemleri etiketleri de kapsar. Etiketleri virgülle ayır: en fazla 8 etiket, her biri en fazla 24 karakter. Arama ad/prefab/etiketleri kapsar; etiket filtresindeki birden fazla etiket birlikte aranır.

Listede **✓** ile dışa alınacak kayıtları seç; **Koleksiyon** penceresinde görünen kayıtları birlikte seçebilir ve yeni bir `.json` dosyasına yazabilirsin. Önerilen yer `BepInEx/config/EpicLootSpawnerExports` klasörüdür. Var olan dosyanın üzerine yazılmaz. İçe alma için tam dosya yolunu yazıp **Dosyayı oku / önizle** kullan. Hatalı kayıtlar ve ad çakışmaları gösterilir; çakışan adı değiştirip kaydı yeniden seç. **Seçilen kayıtları al**, yalnızca seçili geçerli kayıtları tek dosya yazımıyla ekler; mevcut tasarım ve asıl envanter eşyası değişmez. Tasarımlar alınmadan hemen önce kurulu EpicLoot kurallarıyla tekrar doğrulanır.

Koleksiyon biçimi `EpicLootSpawner.Collection`, sürüm 1; tek dosyada en fazla 500 kayıt ve 1.048.576 karakter kabul edilir. Dosya okuma ayrıca 4 MiB ile sınırlıdır. Tek tasarımın **Pano** paylaşımı da kullanılabilir.

Etiketler `EpicLootSpawner.presets.json` içindeki sürüm 2 depoda saklanır. Eski sürüm 1 kayıtları okunur; ilk açık kayıt işleminde özgün dosya kalıcı `.v1.bak` yedeğine alınır. Normal `.bak` önceki yazımı tutmaya devam eder. Güncelleme için JSON dosyalarını silmek gerekmez.

### Menü ve tanılama

Eşya listesi **Ad / Tür / Favoriler** seçenekleriyle sıralanır; arama ve kategori filtreleri birlikte çalışır. Sıralama tercihi panel kapanırken saklanır. Boş sonuç, efekt sınırı, dolu soket ve dışarıdan değişmiş asıl eşya durumlarında ilgili düzeltme eylemleri gösterilir.

Değişmeyen şablon filtreleri ve geçmiş düğmelerinin durumu tekrar hesaplanmaz. Taş önizlemeleri taş sekmesi açıkken küçük gruplar halinde hazırlanır; tasarım değişince yenilenir. Envanter listesi gerektiğinde taranır; seçili asıl eşyanın değiştiği ayrıca kontrol edilir. **Görünüm** sürümleri, katalog/önizleme sürelerini ve son işlem/hata bilgisini gösterir; tanılama metni panoya kopyalanabilir. Ölçüm yöntemi ve sınırları `PERFORMANCE.md` içindedir; oyun açılışı veya gerçek Unity FPS ölçülmedi.

### Yedek ve kayıt kurtarma

**Görünüm → Yedek kurtarma** veya bozuk şablon deposundaki kurtarma düğmesiyle Şablonlar/Favoriler/Taslak seç. `.bak` yedeğini incelemek dosyayı değiştirmez. Şablonlarda ilk `.v1.bak` da incelenebilir. Okunabilir şablon/favori dosyalarından yapısal olarak geçerli kayıtlar ayrı kurtarılabilir; bozuk JSON sözdizimi için okunabilir yedek gerekir.

Önizleme alınacak/atlanacak kayıt sayısını gösterir. Açık onaydan önce yazım yapılmaz; dosya arada dışarıdan değişirse işlem reddedilir. Onay sırasında mevcut dosya ayrıca `.corrupt-…` adıyla saklanır. **Taslak kurtarma paneli kapatır; sonraki F8 açılışında kurtarılan tasarım yüklenir.** Asıl envanter eşyasına yazım yapılmaz.

### Kurulum ve doğrulama

Oyunu kapat; ZIP içindeki `BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll` dosyasıyla önceki DLL'i değiştir. Aynı eklentinin tek kopyasını tut. EpicLoot ve diğer oyun bağımlılıkları kurulum paketine eklenmez.

Release derlemesi sıfır hata/uyarıyla tamamlandı. **478 otomatik kontrol** geçti: 304 temel/dosya kontrolü, 102 bağlı davranış kontrolü ve 72 gerçek üretim kaynağı kontrolü. Testler oyun başlatmaz; Unity ve bazı oyun/EpicLoot API bağlantıları taklit edilir. Gerçek menü ve oyun akışlarının doğrulaması bekliyor. Küçük patch için oyunu yeniden açmak gerekmez; birleşik sürüm için `TESTPLAN.md` listesini tek açılışta kullan.

## 0.7.1: listelerin sıkışması ve eşya seçimi

0.7.0'da kaydırma alanlarına eklenen minimum yükseklik ayarı, kurulu Unity sürümünde yüksekliği 48 piksele sabitleyip alanın büyümesini durduruyordu. Eşya listesi, düzenleme alanı ve önizleme tek satıra sıkışıyordu. Bu ayar kaldırıldı; eşya, efekt, taş, envanter, şablon, paylaşım ve önizleme alanları kalan panel yüksekliğini kullanır.

Eşya seçimi GUI çizimi bittikten sonra tasarıma aktarılır; seçilen eşyanın adı durum alanında gösterilir. Seçim tasarım geçmişine eklenir ve önceki eşyanın efekt/taş seçimlerini temizler. Normal üretim sınırları korunur.

Yeni davranış kontrolü, eski 0.7.0 kaynağında listenin 48 piksele sıkışmasını yakalar; düzeltilmiş kaynakta ikinci ve sonraki satırların seçilmesini, işlemin ertelenmesini ve tasarım geçmişini kontrol eder. Gerçek Unity çizimi için oyun içi doğrulama ayrıca gerekir.

## 0.7.0: değişiklik özeti ve menü kullanımı

### Asıl eşyada ne değişecek?

Envanter düzenlemesindeki sağ panelde **Değişiklikler / Yeni eşya / Mevcut** sekmeleri bulunur. Değişiklikler görünümü, gerçek asıl eşya ile hazırlanmış aday eşyanın seviye, nadirlik, özel ad ve soket sayısını karşılaştırır. Eşya efektlerini kimliklerine göre eşleştirir; eklenen/kaldırılan/değeri değişen efektleri oyun içi metinleriyle gösterir. Yeşil eklemeyi, turuncu kaldırmayı, altın renk değişimi belirtir; bir sayının artması otomatik olarak daha iyi kabul edilmez.

Taşlar soket sırasıyla karşılaştırılır. Taş bulunduğunda, eşya + taş toplamındaki değişiklikler ayrıca gösterilir. Toplamlar EpicLoot'un `GetEffects/GetTotalEffectValue` yöntemlerinden okunur; aday taş değerleri gerçek Build/soketleme akışından gelir. Etkisiz taşlar taş listesinde kalır ve yapay efekt oluşturmaz. Sadece taşın katkısı değişse de toplam etkilerde görünür. Aynı tasarımda değişiklik olmadığı yazılır. **Değişiklikleri büyüt**, özeti ayrı pencerede açar; diğer sekmeler gerçek tam tooltip'i korur.

### Görünüm ayarları

Üstteki **Görünüm** düğmesi menü ölçeğini %75–150 arasında seçip uygulamayı sağlar. Panel küçük ekranlarda daha kısa düzene geçer; gerektiğinde istenen ölçek ekrana sığacak kadar azaltılır. Pencerede seçilen ve kullanılan ölçek ayrı gösterilir. Listeler kalan alana uyum sağlar; uzun şablon formu da kaydırılabilir. Önizleme/paylaşım pencereleri ekran düzenine sığdırılır.

Paneli üst başlığından sürükleyebilirsin. Ölçek ve panel kapanırken son konum, `BepInEx/config/local.epicloot.spawner.cfg` dosyasında saklanır. Çözünürlük değişince pencere görünür alana taşınır. **Varsayılan**, ölçeği %100'e ve konumu başlangıç noktasına getirir. **Vazgeç/Esc**, uygulanmamış ölçek seçimini kapatır.

### Hataları yerinde düzelt

Seviye, adet, soket, özel ad ve sayısal efekt değerlerinde hata ilgili alanın altında gösterilir; hatalı kutu turuncu vurgulanır. Seviye/adet/soket sınırları aynı yerde görünür. Birden fazla temel giriş hatası birlikte gösterilebilir. Uyumsuz efekt ve takılamayan taşta ilgili kartın nedeni yazılır. Değerler sessizce kırpılmaz; yarım girişler taslak ve geçmişte korunur. Aç/kapat efektlerinde **Değeri 1 yap** ile açıkça düzeltilebilir. Üretim/Uygula öncesinde gerçek oyun doğrulaması tekrar çalışır.

### Listeler ve ölçüm

Efekt ve taş listeleri oyun içi görünen adlarına göre sıralanır. Eşya adları, efekt kartlarının metinleri ve eşya/efekt/taş filtre sonuçları ilgili seçim/arama değişene kadar saklanır. Favori, nadirlik, dil veya **Yenile** değişikliği ilgili sonucu yeniler. GUI çizimi önizleme geçerliliğini kontrol ederken tekrar tekrar tam tasarım JSON'u oluşturmaz.

Görünüm penceresinde son katalog/önizleme süreleri görülebilir. **Menü sürelerini günlüğe yaz**, bu süreleri BepInEx günlüğüne kaydeder. Bu ölçümler menü işlemlerini kapsar; oyunun açılış süresine ilişkin bir hızlanma ölçülmedi. Oyun ayarları dışarıdan değiştiğinde **Yenile** kullan.

## 0.6.1: incelemede bulunan hataların düzeltmeleri

- Envanter eşyasına Kırılmazlık ekleyip kaldırırken EpicLoot'un başlangıç dayanıklılık bayrağı korunur. Efekt kaldırılınca dayanıklılık kullanımı geri gelir; başlangıçta dayanıklılık kullanmayan eşyalarda kapalı kalır. Önizleme asıl eşyayı değiştirmez. Uygulama başarısız olursa büyü verisiyle birlikte başlangıç bayrağı da geri yüklenir.
- Lumberjack için ekipman ve shardstone'lardan gelen kesirli toplam, EpicLoot'un normal dönüşümüyle uyumlu olarak aşağı yuvarlanır: **31,5 → 31 ekstra odun**. Tam sayı toplamları aynen kullanılır. Efekt girişindeki normal sınırlar ve tam sayı şartı korunur. Bonus üretim hataları yakalanır; ağaç/kütük işlemini kesmez. Teslim başladıktan sonra hata oluşursa çift bonusu önlemek için ikinci bonus yolu çalıştırılmaz.
- Prefabı bulunamayan son taslağın koruması, yalnızca teslim tercihini veya taş arama nadirliğini değiştirince kalkmaz. Yeni bir eşya tasarımı seçildiğinde otomatik kayıt tekrar kullanılabilir; önceki kayıt `.bak` içinde tutulur.

## 0.6.0: paylaşım ve son tasarıma devam

### Şablon paylaşımı

Şablonlar sekmesindeki **Paylaş / İçe al** ayrı bir pencere açar. **Tasarımı kopyala** geçerli mevcut tasarımı JSON paylaşım metni olarak panoya yazar. Başka bir oyuncudan aldığın metni **Panodan yapıştır** ile veya metin alanına doğrudan yapıştırarak al; **Tasarıma yükle** doğrulayıp editöre aktarır. Hata pencerede gösterilir ve mevcut tasarım korunur. Esc/Tasarıma dön ana panele döner.

İçe alma eşya üretmez, envanterdeki asıl eşyayı değiştirmez ve şablon dosyasına otomatik yeni kayıt eklemez. Kalıcı şablon için ad verip Kaydet/Güncelle kullan. İçe alınan tasarım geri alınabilir. Paylaşım, spawner'ın düzenlediği prefab/nadirlik/seviye/adet/ad/efekt/shardstone seçimlerini taşır; envanter nesnesinin kimliği ve diğer mod verileri paylaşılmaz. Kaynak EpicLoot ayarları alıcıda farklıysa alıcının normal kuralları geçerlidir; uyumsuz tasarım reddedilir.

Paylaşım biçimi sürümlüdür. Desteklenmeyen sürüm, eksik belge alanları, bozuk JSON ve 65536 karakteri aşan metin reddedilir. Tüm şablon deposu JSON'u yerine bu pencereden üretilen tek tasarım metnini kullan.

### Son taslağı geri yükle

Son tasarım, değişikliklerde iki saniyelik duraklamadan sonra ve panel kapanırken `BepInEx/config/EpicLootSpawner.draft.json` dosyasına kaydedilir. Oyunu yeniden açıp F8 ile paneli ilk açtığında geri yüklenir. Yarım yazılmış veya geçersiz sayılar taslakta korunur; önizleme tekrar hata gösterir ve üretim engellenir. Envantere/yere verme tercihi ve taş seçimi nadirliği de saklanır. Bu taslak tek kayıttır; adlandırılmış şablonların yerini almaz.

Envanterdeki asıl eşyayla bağlantı saklanmaz. Yeniden açılışta tasarım üretim modunda gelir; asıl eşyayı düzenlemek için Envanter sekmesinden tekrar seç. Geri al/ileri al geçmişi oturumluk kalır. Eşya prefabı bulunamadığında kayıt dosyası korunur; yeni bir eşya tasarımı seçince otomatik kayıt yeniden kullanılabilir. Teslim tercihi, taş arama nadirliği veya eşya seçmeden sayı alanlarını değiştirmek bu korumayı kaldırmaz. Bozuk taslak dosyasının üzerine yazılmaz. Dosyayı düzeltip oyunu yeniden başlatabilirsin. Alt bilgi otomatik taslak kaydı durumunu gösterir.

Taslak dosyası atomik yazılır ve önceki hal `.bak` yedeğinde tutulur. Aynı taslak tekrar yazılmaz. Bu özellik dünya veya karakter verilerini kendiliğinden değiştirmez.

## 0.5.0: kütüphane ve envanter editörü

### Eşyayı daha hızlı bul

- Eşyalar sekmesinde Tümü/Silah/Zırh/Kalkan/Alet/Diğer kategori filtreleri bulunur. Eşya türü oyunun SharedData bilgisinden alınır.
- Liste oyun içi ada göre sıralanır; aramada isim ve prefab kimliği birlikte kullanılır.
- Satırdaki ☆/★ düğmesi favoriyi değiştirir. **Yalnızca favoriler** filtresi kategori ve aramayla birlikte çalışır.
- Favoriler `BepInEx/config/EpicLootSpawner.favorites.json` dosyasında saklanır; oyun kapanınca kaybolmaz. Atomik yazma, `.bak` yedeği ve bozuk dosyanın üzerine yazmama uygulanır.

### Tasarım geçmişi

- **Geri al / İleri al**, eşya seçimi, nadirlik, seviye, ad, efektler ve taşlar dahil tasarımın son 60 durumunu tutar.
- Yazma ve kaydırıcı hareketleri kısa duraklamadan sonra gruplanır. Geçersiz veya yarım kalmış sayı girişi de geri alınabilir.
- Geri aldıktan sonra yeni bir değişiklik yaparsan ileri alma dalı temizlenir.
- Geçmiş yalnızca bu oyun oturumundaki tasarımı kapsar. Şablon/favori dosyası işlemlerini veya üretilmiş eşyaları geri almaz.
- Envanter eşyasına uygulanmış bir değişikliği geri çevirmek için tasarımı **Geri al** ile geri getirip **Asıl eşyaya uygula** düğmesine tekrar bas.

### Envanterdeki asıl eşyayı düzenle

1. **Envanter** sekmesinden eşyayı seç. Aynı adlı eşyalar envanter koordinatlarıyla ayrılır.
2. Nadirlik, seviye, efektler ve shardstone'ları normal EpicLoot kuralları içinde düzenle.
3. Sağdaki **Değişiklikler / Yeni eşya / Mevcut** sekmeleriyle sonucu karşılaştır.
4. Kullanılan eşyayı önce çıkar. Sonra **ASIL EŞYAYA UYGULA** ile değişikliği seçili tek eşyaya yaz.

Seçmek, önizlemek ve tasarımı değiştirmek asıl eşyanın verilerini değiştirmez. Uygula sırasında envanterdeki aynı ItemData nesnesi güncellenir; eşya yeniden üretilmez. Envanter konumu, görünüm varyantı, yığın/adet ve diğer modların özel verileri korunur. Dayanıklılık mevcut maksimuma oranıyla yeni seviyeye taşınır; işlem otomatik tamir yapmaz. Korunan efektlerin augment/temper işaretleri efekt kimliğine göre yeniden eşlenir; kaldırılan efektin işareti de kalkar.

Eşya artık envanterde değilse, büyü verisi/seviyesi seçimden sonra dışarıdan değişmişse veya kullanılıyorsa uygulama engellenir. Dışarıdan değişen eşyayı tekrar seç. Önceden gelen sınır dışı efektler tasarıma alınır ama düzeltilmeden Uygula etkinleşmez; sessizce kırpılmaz.

Bu sürüm büyülenebilir normal eşyaları, sıradan büyülü eşyaları ve shardstone soketlerini düzenler. Özel LegendaryID/SetID taşıyan eşyalar, tanımlanmamış eşyalar, yığınlar ve runestone/kaynak prefabı eksik soketler açıklamayla engellenir. Normal Legendary/Mythic nadirliği tek başına engel değildir. Malzeme tüketilmez; tasarımdan çıkarılan taşlar envantere geri verilmez. **Yeni eşya üretimine dön** tasarımı koruyup üretim moduna döner.

## 0.4.1: şablon yönetimi

- Şablonlar sekmesinde ada göre arama ve eşleşen/toplam kayıt sayısı gösterilir.
- Seçili şablon vurgulanır. Üstteki ad alanına yeni adı yazıp **Adını değiştir** ile kayıt adını değiştirebilirsin; eşya tasarımı değişmez.
- Başka bir şablonun adı kullanılamaz; büyük/küçük harf değişikliği yapılabilir.
- **Sil** sonrasında kayıt adıyla onay gösterilir. **Vazgeç** veya Esc silmeyi iptal eder; paneli kapatmak da bekleyen onayı temizler.
- Silme mevcut tasarımı veya önceden üretilmiş eşyaları değiştirmez. Son değişiklikten önceki dosya `.bak` yedeğinde tutulur; bu yedek her başarılı kayıt değişikliğinde yenilenir.
- Yeniden adlandırma ve silme, kayıt işlemleri gibi atomik yazılır. Dosyaya yazılamazsa bellekteki kayıtlar korunur.
- EpicLoot kurallarına göre yüklenemeyen eski bir şablon da listeden seçilip silinebilir veya yeniden adlandırılabilir; yükleme hatasında mevcut eşya tasarımı korunur.
- Geçersiz tasarımda şablon kaydetme düğmesi devre dışıdır. Mevcut **Kaydet / Güncelle** aynı adı kullanırsan kaydı günceller; farklı ad yazarsan ayrı kayıt oluşturur.

## 0.4.0: canlı tasarım paneli

Üç sütun: solda **Eşyalar/Şablonlar/Envanter** kütüphanesi, ortada **Düzenle/Efekt ekle/Taş ekle** sekmeleri, sağda sürekli görünen **Canlı önizleme**.

- Eşya, nadirlik, seviye, efekt değeri ve taş seçimi değişince gerçek eşya tooltip'i sağda yenilenir. Yazma sırasında kısa bir bekleme uygulanır; önizleme OnGUI çiziminin içinde hesaplanmaz.
- Hatalı adet, seviye, efekt değeri veya soket seçimi önizlemede hata gösterir. Tasarım düzeltilene kadar üretim düğmesi devre dışıdır.
- Sayısal efektlerde manuel değer alanına ek olarak kaydırıcı ve **Minimum/Maksimum** düğmeleri bulunur. Kaydırıcı EpicLoot'un tanımladığı değer adımına göre ayarlanır ve aralığı aşmaz.
- Aç/kapat efektleri sabit 1 olarak gösterilir.
- Taş kartı, seçilen eşyaya takılınca elde edilecek efekt adını, değerini ve açıklamasını gösterir. Takılabilir durumlarda EpicLoot'un mevcut taşlarla güç/azalma hesabı simüle edilir. Mevcut taşların değerleri de değişebiliyorsa tam sonuç sağdaki eşya önizlemesinde, taş eklendikten sonra görülür.
- Takılamayan taşlarda neden yazılır ve **Taşı tak** düğmesi devre dışıdır. Boş soket yoksa Düzenle sekmesinden izin verilen soket sayısını artır.
- Taş eşya türünde etkisiz kalıyorsa açıkça belirtilir; EpicLoot izin veriyorsa yine takılabilir.
- **Önizlemeyi büyüt** uzun tooltip'i ayrı pencerede açar. Esc tasarım paneline döner.
- Arama, sekme ve liste değişiklikleri Update'e ertelenir; önceki ikon ve GUI düzen hatası düzeltmeleri korunur.

Önizleme eşyayı envantere/yere üretmez ve taş tüketmez. Üretimde seçimler ayrıca yeniden doğrulanır. Oyun ayarları dışarıdan değiştiğinde **Yenile** ile önizleme ve listeleri yeniden hesapla.

## Geçerli kurallar

- Eşya listesinde yalnızca EpicLoot'un büyülenebilir kabul ettiği prefablar bulunur.
- Efekt listesi `MagicItemEffectDefinitions.GetAvailableEffects` ile seçilen eşya, nadirlik ve mevcut efektlere göre filtrelenir. Uyumsuz ve çakışan efektler eklenemez.
- Efekt sayısı, EpicLoot'un mevcut nadirlik/enchanting ayarlarındaki maksimuma göre sınırlandırılır. Limitsiz modda en fazla 30.
- Efekt değerleri gösterilen min/max aralığında olmalıdır; Limitsiz modda herhangi bir sonlu sayı olabilir. Aç/kapat efektleri 1 kullanır.
- Eşya seviyesi en az 1 tam sayı olmalıdır; prefabın normal maksimumu yalnızca bilgi olarak gösterilir.
- Üretim adedi 1–100 arasındadır.
- Soket sayısı EpicLoot'un ilgili nadirlik için izin verdiği maksimumla sınırlıdır; Limitsiz modda en fazla 12. Taş sayısı boş soketleri aşamaz.
- Shardstone'lar `CanSocket/AddShard` ile takılır: çakışma, kategori ve aynı renk taşların güç hesabı EpicLoot'a bırakılır.
- Özel eşya adı en fazla 80 karakterdir.
- Envanter dolunca işlem durur; kalan eşyalar otomatik yere bırakılmaz.

Nadirlik değişince efekt/taş seçimleri korunur; yeni nadirliğe uymayanlar alan hatası olarak gösterilir. Önceki sınırsız sürümlerin şablonları yüklemede yeniden doğrulanır; sınırları aşan şablon sessizce değiştirilmez, hata gösterilir ve mevcut seçim korunur. Önceden üretilmiş eşyalar geriye dönük değiştirilmez.

Görsel tema, oyun içi efekt adları/açıklamaları, aralık gösterimi, şablon sistemi, ikon hatası düzeltmesi ve GUI işlemlerini Update'e erteleme korunur. Lumberjack'in 0.3.2'de eklenen tam ekstra odun hesabı spawner ile üretilen veya düzenlenen silahlarda korunur; değer artık normal aralıkta olmalıdır. Ekipmandan gelen kesirli toplamlar aşağı yuvarlanır. Bu client düzeltmesi ganimet kodu istemcide çalışırsa uygulanır; uzak sunucu/oyuncu sahipliğinde garanti yoktur.

## Kurulum ve kullanım

Oyunu kapat, ZIP içindeki `BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll` ile eski DLL'i değiştir. Spawner sunucuya kurulmaz; EpicLoot'un kendi ağ gereksinimleri devam eder.

Dünyada **F8** ile aç. Soldan eşyayı seç; ortada nadirliği ve seviyeyi ayarla. **+ Efekt ekle** ve **+ Taş tak** açıklamalı seçiciyi açar. Sağdaki önizlemeyi kontrol et ve alttaki **Eşyayı üret** düğmesini kullan. Esc önce açık pencereyi, sonra paneli kapatır.

Üst çubuktaki **Şablonlar** listesinden **Yeni kaydet / Düzenle** seçimleri `BepInEx/config/EpicLootSpawner.presets.json` dosyasında tutar. Eski dosya `.bak` yedeğine alınır; bozuk dosyanın üzerine yazılmaz.

## Doğrulama

1.0.1 için **575 otomatik kontrol** uygulanır: 311 temel/dosya, 137 davranış, 102 üretim ve 25 panel kontrolü. Temel kontroller sınırları, kaydırıcıları, ikon indeksini, tasarım geçmişini, dayanıklılık oranını, efekt işaretlerini, paylaşımı ve şablon/favori/taslak dosyalarını kapsar. Kontroller aynı/farklı tasarımı, sıfır değerli efektin varlığını, taş sırasını, birden fazla alan hatasını ve 640×480–3840×2160 arasında ölçek/konum hesabını kapsar. Geometri kontrolleri gerçek Unity çizimini doğrulamaz.

Davranış kontrolleri gerçek InventoryEditor, SessionDraft, ExactWoodDrops, ComparisonMenu, CatalogMenu, LibraryMenu ve HistoryMenu kaynak yöntemlerini çalıştırır. Unity, envanter, biçimlendirme ve API tesisatı taklit edilir; EpicLoot Indestructible ve MagicItem toplam yöntemleri, kurulu 0.14.13 DLL'iyle davranışı karşılaştırılmış kaynak örneklerinden alınır. Önceki düzeltmelerin yanında kesirli taş katkısının özete aktarılması, etkisiz taş, aynı tasarım, karşılaştırmada asıl verilerin değişmemesi, filtre birleşimleri ve değişmeyen kartların tekrar biçimlendirilmemesi kontrol edilir. Kaydırma alanı kontrolü, kurulu Unity DLL'inde doğrulanan minimum/maksimum yükseklik davranışını taklit ederek eşya düğmelerinin erişilebilirliğini ve ertelenmiş seçimi sınar. Testler gerçek shardstone değer hesaplamasını, Unity çizimini/gerçek GUI veya pano olaylarını, ağaç kırılmasını veya ağ sahipliğini simüle etmez. Ayrıntılar `tests/BehaviorChecks/README.md` dosyasındadır.

0.4.0/0.4.1 için çalışma ve 0.9.3/0.9.4 için olumlu genel oyun geri bildirimi alındı. 1.0.0 oyun kodu ve derleme girdileri, kullanıcı tarafından kontrol edilen 0.9.4 ile sürüm alanları hariç eşleşir. Ayrıntılı kontrol listesi `TESTPLAN.md`, kullanıcı geri bildirimi `ACCEPTANCE.json` içinde tutulur. Otomatik testler, DLL incelemesi ve kullanıcı geri bildirimi ayrı kaydedilir.
```powershell
dotnet build EpicLootSpawner.csproj -c Release
dotnet run --project tests/SpawnerChecks.csproj -c Release
dotnet run --project tests/BehaviorChecks/BehaviorChecks.csproj -c Release
dotnet run --project tests/RuntimeChecks/RuntimeChecks.csproj -c Release
dotnet run --project tests/PanelChecks/PanelChecks.csproj -c Release
```

Kaynak proje EpicLoot 0.14.13 kurulu profiline ve yerel Valheim DLL'lerine göre derlenir. Gerekirse `GamePath` ve `ProfilePath` MSBuild özelliklerini değiştir.

`BUILDING.md` derleme/test/paketleme rehberini, `COMPATIBILITY.md` sürümleri ve kanıtları içerir. `scripts/Build-Release.ps1` kabul kaydı varsa kaynak/proje ve bağımlılık eşleşmesini doğrulayıp önceki arşivleri koruyarak paket üretir. `scripts/Verify-Release.ps1` dosya hash değerlerini kontrol eder. Mod sürümü 1.0.2; yeni büyü kaldırma davranışı için ayrıntılı oyun sonucu henüz kaydedilmedi.

