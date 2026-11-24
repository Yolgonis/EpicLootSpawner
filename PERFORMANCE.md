# 0.9.0 menü maliyeti ölçümü

.NET 8 başsız test düzeneğinde aynı iş yükünün önceki tekrar hesaplama yolu ile yeni önbellek yolu karşılaştırıldı. Süreler tek çalıştırma örneğidir; makine yüküne göre değişebilir.

| İş yükü | Önceki yol | 0.9.0 yolu | Davranış kontrolü |
| --- | ---: | ---: | --- |
| 300 şablonda 2.000 aynı filtre sorgusu; 20 ısınma turu | 62,1829 ms | 0,0146 ms | İki yolda da 222.000 toplam sonuç; değişmeyen dizinin yeniden kullanımı. |
| 10.000 geçmiş çizim çağrısı | 29,8028 ms | 0,4638 ms | Önceki yol 20.000 tasarım anlık görüntüsü üretir; gerçek yeni DrawHistory 0 üretir. |

İlk ölçüm tests/FeatureChecks.cs içinde önceki sıralama/filtre iş yüküyle gerçek PresetQuery önbelleğini karşılaştırır. Sonuç dizisi her sorguda yeniden oluşturulmaz; depo revizyonu, arama/etiket, depo kimliği veya hazır durumu değişirse yenilenir. İkinci ölçüm tests/BehaviorChecks/AdditionalChecks.cs içinde iki eski JSON kontrolüyle gerçek HistoryMenu.DrawHistory kaynağını karşılaştırır. GUI testte taklit edilir.

Ayrıca taş önizleme çalışması taş sekmesine bağlıdır: her karede en fazla üç yeni taş veya yaklaşık 4 ms bütçe dolana kadar işlenir. Bir tek taşın hesaplaması bu bütçeyi aşabilir; işlem ortasında kesilmez. Sonuç tasarım değişince geçersizleşir. Envanter listesi yalnızca envanter sekmesinde yenilenir; seçili asıl eşya denetimi devam eder.

Bu ölçümler Unity çizimi, gerçek soket hesaplama yükü, FPS, bellek profili veya oyunun açılış süresini ölçmez. Görünüm penceresinde kurulu oyundaki son katalog/önizleme/taş grubu süreleri ayrıca görülebilir. Beş dakikalık açılış süresi için hızlanma iddiası yoktur.

```powershell
dotnet run --project tests/SpawnerChecks.csproj -c Release
dotnet run --project tests/BehaviorChecks/BehaviorChecks.csproj -c Release
```

Ölçüm çıktıları BENCHMARK ve HISTORY_BENCHMARK olarak yazılır. Doğruluk kontrolleri zaman eşiğine bağlı değildir.
## 0.9.4 metin yerleşimi

Uzun eşya/envanter/şablon adlarının GUIStyle.CalcHeight sonucu ad/genişlik bazında önbelleğe alınır. Katalog/dil veya tema yenilenince ölçüm önbelleği temizlenir. Başlık ve alan yardım metni mevcut fontla ölçülür; gerçek Unity font/GUI maliyeti bu ortamda ölçülmedi. Önceki başsız ölçümler 0.9.0 verileridir; 0.9.4 FPS ölçümü olarak sunulmaz.

## 1.1.0 işlem denetimi

Taş çıkarma tooltip'i ve soket hesabı yalnız seçim/onay eyleminde hazırlanır. İade kapasitesi açık modalda, geri alma uygunluğu son işlem varken en fazla 0,5 saniyede bir Update içinde denetlenir. OnGUI gerçek envanteri kopyalamaz veya soket hesabı yapmaz. Geri alma taş eşleşmesi envanter metadata'sını tarar; bu yeni akış için native FPS/bellek ölçümü yapılmadı.

## 1.1.1 onay önizlemeleri

Açık modalda kaynak dayanıklılığı, maksimum dayanıklılığı veya oyun dili değişince tooltip ve fark özeti yeniden hazırlanır. Değişmeyen soket adayının yeniden hesaplanmadığı bağlı kaynak testinde denetlenir. Kart adları OnGUI'de native fontla hesaplanır (en fazla mevcut soket sayısı); taş etkileri ve envanter kapasitesi burada hesaplanmaz. Bu ek akış için native FPS veya font maliyeti ölçümü yapılmadı.

## 1.2.0 boyutlandırma

İşaretçi farkı ile boyut sınırlandırması sabit miktarda hesap içerir. Genişlik değişince eski metin yüksekliği girdileri temizlenir; her sürükleme boyutu için ayrı önbellek tutulmaz. Config boyutu bırakma/kapatma sırasında kaydeder, her MouseDrag olayında yazmaz. Native sürükleme FPS veya font maliyeti bu ortamda ölçülmedi.


## 1.3.0 özel kimlik kataloğu

Tüm eşya × kimlik kombinasyonları açılışta hesaplanmaz. Legendary / Set sekmesi görünürken yalnız seçili prefab/kalite/dil için kimlik listesi hesaplanır ve önbelleğe alınır; elle yenileme mevcut config tanımlarını tekrar okur. Her GUI olayında yalnız kartlar çizilir. Native FPS veya bu sürümün oyun açılış süresi ölçülmedi.
