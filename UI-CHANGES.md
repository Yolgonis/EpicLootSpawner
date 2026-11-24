# 1.4.0 · tek sayfalık arayüz

| Alan | 1.3.2 | 1.4.0 |
| --- | --- | --- |
| Gezinme | Kütüphane sekmeleri (Eşyalar/Şablonlar/Envanter) ve dört editör sekmesi | Üst çubukta Yeni eşya / Envanterim modu ve Şablonlar düğmesi; editörde sekme yok |
| Efekt/taş/kimlik seçimi | Ayrı sekmeye geçip geri dönmek gerekiyordu | Tasarım sayfasının üstünde açılan seçici; kartlarda tam açıklama, not ve normal aralık |
| Sayılar | Yalnız metin kutusu | − / + düğmeleri; efektlerde ayrıca kaydırıcı, Min, Maks |
| Ana eylem | Editör sütununun altında, içerikle birlikte | Her zaman görünen alt çubuk: teslim yeri, durum, geri alma, Eşyayı üret / Asıl eşyaya uygula |
| Sınırlar | Her zaman EpicLoot sınırları | İsteğe bağlı Limitsiz mod; aralık dışı değerler sarı uyarıyla |
| Nadirlik değişimi | Efekt ve taşları siliyordu | Seçimler korunur; uymayanlar alan hatası |
| Açıklama metinleri | Her alanın altında gri yardım satırı | Yalnız gerektiğinde; ayrıntı ipucu ve Ayarlar → Kullanım içinde |
| Sütunlar | %25 / %42 / %33, panel = yükseklik − 190 | %24 / %45 / %31, panel = yükseklik − 150 |

Seçici GUI.enabled ile ana pencereyi kilitler, Esc ile ilk kapanan penceredir ve panel kapanınca temizlenir. Taş etkileri yalnız taş seçicisi açıkken, özel kimlik kataloğu yalnız Legendary / Set seçicisi açıkken hesaplanır. Kaydırma alanlarına MinHeight eklenmedi. Native IMGUI görünümü sonraki normal oyun oturumunda kontrol edilecek.

# 1.3.1 · hata halinde kullanılabilir arayüz

Hatalı aralık ve özel güç tanımı açık alan mesajlarıyla gösterilir. Geçersiz aralıkta kaydırıcı/minimum/maksimum çizilmez; mevcut efekt kaldırılarak veya yapılandırma düzeltilerek tasarım onarılabilir. Bozuk efekt başlıkları kimliğiyle gösterilir; bozuk açıklama okunur hata mesajı verir. Başarısız şablon seçimi kayıt/etiket/envanter kaynağını değiştirmez. Önizleme doğrulamasındaki eski alan hataları temizlenir. Genel görünüm ve pencere boyutlandırma davranışı korunur.

# 1.3.0 · özel Legendary / Set seçimi

Tasarım sekmeleri iki satıra yerleşir: Düzenle, Efekt ekle, Taş ekle, Legendary / Set. Yeni sekme seçili temel eşyanın uygun özel kimliklerini oyun dilinde isim/açıklama/nadirlik/set bonuslarıyla gösterir; filtre, arama ve yenileme bulunur. Uzun kimlik adları tasarım sütununun genişliğine göre sarılır. Üretimde kimlik seçilir; envanter düzenlemesinde kimlik seçimi kilitlidir ve mevcut kimlik/set bilgisi korunur.

Düzenle özel GetRollRange değerlerini ve sabit/augment edilebilir başlangıç gücü ayrımını gösterir. Önizleme native tooltip ile set bilgilerini sunar; büyü kaldırma farkında kimlik/set kaybı açık yazılır. Yazımlar ve katalog hesaplama Update içinde yapılır. Native IMGUI görünümü sonraki normal oyun oturumunda kontrol edilir.

## 1.2.0 yeniden boyutlandırma

Sağ alt ◢ tutamacı ana pencereyi yatay/dikey boyutlandırır. Fiziksel işaretçi farkı sürükleme başındaki ölçekle arayüz birimine çevrilir; boyut değiştirmek yazı ölçeğini değiştirmez. Üç kolon oranı korunur, kaydırma alanı kalan yeni yüksekliği kullanır. Alt boyut, mevcut kompakt düzenin 1180×740 arayüz birimidir; ekran sınırı ve mevcut konum büyümeyi sınırlar.

MouseDrag/MouseUp boyutlarını Update kuyruğu uygular; çizim sırasında GUILayout düzeni değiştirilmez. Genişlik değişince metin ölçüsü önbelleği temizlenir. Modal açıkken tutamaç devre dışıdır; kapatma ve odak/ekran/ölçek değişimi yakalamayı bırakır. Görünüm piksel boyutunu ve yalnız boyutu sıfırlama düğmesini gösterir; config son tercih edilen boyutu saklar. Native fare/ölçek/çizim kontrolü sonraki normal oturumda yapılır.

## 1.1.1 işlem ekranlarında okunabilirlik

Tek taş çıkarma kartları ikon, soket numarası, oyun adı, katkı ve açıklamayı birlikte gösterir. Taş adı düğmesinin genişliği sabittir; yüksekliği native GUIStyle.CalcHeight ile hesaplanır. ScrollView minimum yüksekliği sabitlenmez. Taş seçimi, değişiklik özeti ve son eşya tooltip'i ayrı sekmelerdedir; büyü kaldırma özetle açılır.

Geri alma düğmesi engel varken de ayrıntıları açar; yalnız onay engellenir. Değişiklikler/eşya/gerekli taşlar sekmelerinde tüm eksik türler miktarlarıyla görünür. Ana editörde kısa engel bildirimi kullanılır; uzun adet listesi kaydırılabilen modaldadır. Onay önizlemeleri mevcut dayanıklılık oranı ve oyun dili değişince Update içinde yenilenir. Yeni native font/ölçek görünümü sonraki normal oyun oturumunda kontrol edilir.

## 1.1.0 envanter eylemleri

Büyüyü kaldır… ve Tek taş çıkar… aynı satırdadır. Tek taş çıkarma modalı asıl eşyanın takılı taşlarını sıralar; seçim önizlemeyi ve iade edilecek taşı yeniler. Hazırlanan taslak uygulanmaz. Eksik envanter yeri sadece onayı engeller; başka taş seçimi yapılabilir.

Son envanter işlemini geri al… tasarım geçmişinden ayrı düğmedir. Onay modalı işlem türünü, geri yüklenecek eşyayı ve envanterden alınacak taşları gösterir. Engeller Update içinde yenilenir. Esc/iptal/kapatma onayı temizler; tamamlanmış son işlem kaydı panel kapanışında korunur.

# 0.9.4 kullanım ve okunabilirlik

Menü Türkçe kalır; oyun metinleri seçili dili izler. Lumberjack'in EpicLoot açıklaması ve spawner'a ait Türkçe not artık ayrı alanlardır.

Katalog/envanter/şablon adları kalan kolon genişliği içinde sarılır. Satır yüksekliği GUIStyle.CalcHeight ile hesaplanıp ad/genişlik bazında önbelleğe alınır. GUILayout.MinHeight ile satırın maksimumunun aynı değere sabitlenmesi yerine hesaplanan yükseklik kullanılır. Editör/önizleme başlıkları en fazla 64 sanal piksel kullanır. İmleç üzerindeki tam ad/prefab, ana pencerenin alt bölümünde bilgi kutusu olarak çizilir; yalnızca Repaint olayında çizildiği için fare girişini almaz.

Alan yardım/hata metinleri mevcut font ve genişliğe göre büyür; kaydırma alanlarına MinHeight eklenmez. Ölçek ayarında uygulanacak gerçek yüzdesi ve ekrana sığdırma açıklaması görünür. Görünüm penceresi Görünüm/Tanılama/Kullanım sekmelerine ayrıldı. Ayrıntı doğrudan Tanılama'yı açar; rapor sürüm/ortam/tasarım ve tam hata bilgilerini panoya kopyalar.

0.9.3 ve 0.9.4 için kullanıcı genel görünümün düzgün olduğunu bildirdi. 1.0.0 aynı arayüz kodunu kullanır; sürüm etiketi güncellenmiştir. Farklı dil/çözünürlük ve tek tek kontrol maddeleri ayrıca kaydedilmedi. Otomatik kontroller native font veya Repaint sonucunu ölçmez.

# 0.9.3 eşya listesi

Eşya kütüphanesinde Ad seçeneği **Tier + Ad** oldu. Varsayılan sıra düşükten yüksek ilerleme tier'ı ve aynı tier içinde yerelleştirilmiş eşya adıdır. Tier 0 • Başlangıç, Tier 1, Tier 2… ve Tier bilinmiyor grup başlıkları liste içinde gösterilir. Eksik tier verisi olan eşyalar son gruptadır. Tür/Favori seçeneklerinde grup önceliği korunur; grupların içinde tier ve ad sırası kullanılır.

Tier verisi katalog yenilenirken EpicLoot ayarlarından okunur; çizimde yalnızca önbellekten alınır. Mevcut kaydırma alanına minimum yükseklik eklenmedi. Canlı başlık/uzun ad/kaydırma/seçim kontrolü sonraki uygun açılışta TESTPLAN.md ile yapılır.

# 0.9.2 arayüz düzenlemesi

Mevcut IMGUI arayüzü korunarak görsel düzen yenilendi. Normal limitler, eşya/efekt uyumluluğu, üretim ve asıl eşyaya Uygula davranışı değişmedi.

| Alan | Önce | 0.9.2 |
| --- | --- | --- |
| Yüzeyler | Düz ve benzer koyu bloklar | Ana panel/kart ayrımı, yumuşak köşeler ve ince çerçeveler. |
| Seçim | Diğer düğmelere yakın renk | Altın çerçeve ve sıcak metin; sekmelerde ayrı seçili görünüm. |
| Ana eylem | Altın üzerinde beyaz metin | Altın üzerinde koyu metin; geçersiz tasarımda nötr düğme. |
| İkonlar | Küçük ve serbest yerleşim | Çerçeveli 40–52 piksel alan; katalog/favori satırında en az 44 piksel yükseklik. |
| Arama | Tek metin alanı | Daha kısa etiket, eşya/efekt/taş/envanter/şablon aramalarında temizleme düğmesi. |
| Teslim | Aç/kapat kutusu ve açıklama | Envantere / Yere bırak seçenekleri. |
| Başlıklar | Numaralı büyük bölüm başlıkları | Kütüphane, Eşya tasarımı, Canlı önizleme; seçilen eşyanın ikonu/adı ve üretim/düzenleme modu. |
| Önizleme | Nadirlik ve iki satır sayaç | Eşya adı/ikonu, nadirlik ve tek özet şeridi. |
| Alan dağılımı | %23 / %41 / %36 | Kütüphane %25, editör %42, önizleme %33; panel aralarında 12 piksel boşluk. |

Normal düzen 1320×900, küçük ekran düzeni 1180×740 sanal piksel kullanır. Kullanıcının ölçeği ve ekran sınırı birlikte uygulanır; pencere görünür alana sığar. Kalan yüksekliği kullanan kaydırma alanları korunur; geçmişte 48 piksel sıkışmasına neden olan MinHeight seçeneği geri eklenmedi. Kontrol yüksekliği yalnızca düğme/alanlarda kullanılır.

Yuvarlatılmış yüzey dokuları tema hazırlanırken bir kez oluşturulur; her çizimde yeniden üretilmez. Eklenti kapatıldığında veya menü hatası temizliğinde serbest bırakılır. Dokular native Unity 9-slice border ile gerilir. Panel kendi çizim rengini kullanır; diğer arayüzün rengi işlem sonunda geri yüklenir.

Kaynaklar: src/Menu.cs tema ve ana düzen; src/UiControls.cs ortak arama; src/UiLayout.cs ekran/kolon hesabı; LibraryMenu/Editor/LivePreview/InventoryEditor/PresetMenu ilgili panel düzenleri.

Derleme ve mevcut otomatik kontroller yapıldı. Bunlar gerçek Unity font, çizim veya fare olaylarını doğrulamaz. Kullanıcı önceki görünümün düzgün göründüğünü bildirdi; 0.9.2 yeni görünümü sonraki uygun oyun açılışında kontrol edilecek. Ayrı küçük patch için yeniden açılış istenmez.

Oyun kontrolü: ikinci/sonraki eşya seçimi, uzun adlar, liste kaydırma, arama temizleme, teslim yeri, ölçek değişimi, şablon/efekt/taş pencereleri ve Uygula/Üret düğmesine erişim.
