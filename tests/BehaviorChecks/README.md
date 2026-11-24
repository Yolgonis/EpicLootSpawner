# Davranış kontrolleri

```powershell
dotnet run --project tests/BehaviorChecks/BehaviorChecks.csproj -c Release
```

Komutu kaynak projenin kökünden çalıştır. .NET 8 SDK gerekir; bu test projesi Valheim'i başlatmaz veya kurulu oyun dosyalarını değiştirmez.

133 kontrol, gerçek `InventoryEditor.cs`, `SessionDraft.cs`, `ExactWoodDrops.cs`, `ComparisonMenu.cs`, `CatalogMenu.cs`, `LibraryMenu.cs` ve `HistoryMenu.cs` yöntemlerini bağlı kaynak olarak çalıştırır. ItemTiers, ItemDifference, UiLayout, DesignHistory, EffectDisplay, Rules, Presets, EffectChoice, DraftStore ve Favorites da gerçek kaynak dosyalarıdır. Unity/GUI, envanter, prefab çözümleme, metin biçimlendirme ve EpicLoot API tesisatı `Harness.cs` içinde taklit edilir.

- Kırılmazlık ekleme/kaldırma/yeniden ekleme; önizlemenin asıl eşyadan ayrı olması; seviye, dayanıklılık oranı, konum, varyant ve diğer mod verilerinin korunması.
- Kopyalama sırasında Indestructible.Sync çağrısının olması ve olmaması; API yazımı başarısız olunca eski büyü verisi/bayrakların geri gelmesi ve sonraki kaldırmanın çalışması; başlangıçta dayanıklılık kullanmayan eşyanın korunması.
- Eksik prefablı taslağın teslim tercihi, taş arama nadirliği ve boş tasarım girişiyle korunması; yeni eşya seçimiyle kaydın açılması ve eski dosyanın yedeklenmesi. Gerçek DraftStore/AtomicFile, geçici dosyalarla çalışır.
- Tam sayı ve kesirli ekipman toplamında odun adedi; yığınlara bölme; kısmi/ilk teslim hatası; bonusu okuma hatası; normal silah ve diğer saldırganların orijinal yolda kalması; geçersiz toplamın hata fırlatmaması.
- Gerçek karşılaştırma kaynağının eşya ve soket değerlerini ayırması; kesirli toplama, etkisiz taş, sadece soket katkısının değişmesi, aynı tasarım, büyüsüz kaynak ve asıl eşyanın değişmemesi. Soket değerleri testte verilen nihai değerlerdir; ShardSocketManager.RecomputeSocketValues çalıştırılmaz.
- Gerçek katalog kaynağında birleşik arama/kategori/favori, taş nadirliği, efekt açıklaması araması, dil değişimi ve boş sonuçlar. Değişmeyen filtre dizileri tekrar üretilmez; kart biçimlendirme sayacı artmaz. UI düğmeleri gerçek Unity olaylarıyla sürülmez; test değişen filtre durumunu ve geçersizleştirme işaretini verir.
- Gerçek eşya kütüphanesinde kalan panel yüksekliği; ikinci ve sonraki satırın seçimi, GUI çizimi sırasında değişiklik yapılmaması, üretim moduna geçiş, önceki efekt/taş seçimlerinin temizlenmesi, gerçek tasarım geçmişi ve devre dışı panelde tıklama engeli. Aynı kontrol 0.7.0 LibraryMenu kaynağıyla çalıştırıldığında 48 piksele sıkışma nedeniyle başarısız olur.

Kaydırma alanı taklidi, kurulu `UnityEngine.IMGUIModule.dll` içindeki `GUILayoutEntry.ApplyOptions` ve `GUIScrollGroup.CalcHeight` davranışından doğrulanan minimum/maksimum yükseklik ve genişleme ilişkisini kullanır. `MinHeight(48)`, başlangıçtaki sıfır maksimumu 48'e yükseltir; kaydırma grubu bu maksimumu geri yükleyip büyümeyi kapatır. Yalnızca `ExpandHeight(true)` kalan alanı kullanabilir. Düğmelerin görünürlük kontrolü sabit satır aralıklarıyla taklit edilir; gerçek font, ekran koordinat dönüşümü, Unity çizimi veya olay sistemi test edilmez.

`Fixtures/EpicLootIndestructible.cs`, RandyKnapp/ValheimMods içindeki `EpicLoot/src/Magic/MagicItemEffects/Indestructible.cs` kaynağından alınmıştır (commit `69704d5fbe86910e6b0de7f68c054028f20e3826`). Sync/Revert/OriginallyUsesDurability davranışı kurulu EpicLoot 0.14.13 DLL'inin decompile çıktısıyla karşılaştırılmıştır. Örnek yalnızca bu test projesine derlenir; spawner DLL'ine veya kurulum paketine dahil edilmez.

`Fixtures/EpicLootMagicTotals.cs`, aynı commit'teki MagicItem.GetSocketedEffects/GetEffects/GetTotalEffectValue/SumEffectValue yöntemlerini içerir. Davranış kurulu DLL'in `work/Comparison-api-check` decompile çıktısıyla karşılaştırılmıştır. Bu örnek de yalnızca test projesine derlenir; tam EpicLoot runtime'ını temsil etmez.

Bu düzenek çalışan Unity sahnesini, gerçek GUI/pano olaylarını, ekipman hesaplarının tamamını, ağaç kırılmasını veya ağ sahipliğini simüle etmez. Bu akışlar sonraki toplu oyun testinde doğrulanmalıdır.

0.9.0 ek kontrolleri gerçek ItemCopies kaynağını, 200 envanter seçimi sonrasında kaynak temizliğini, geri al/ileri al ve yeni dalı, sıralama seçeneklerini ve DrawHistory çiziminde tasarım JSON üretiminin kaldırılmasını kapsar. 10.000 başsız çizim çağrısı ölçümü PERFORMANCE.md içinde açıklanır; Unity çizim süresi değildir.
24 tier/sıralama kontrolü: canlı API biçimine uygun boss/eşya fixture ile düşükten yükseğe ilerleme, yerelleştirilmiş ad/prefab eşitlik çözümü, Ocean ortak boss tekrarı, arama/kategori/favori filtreleri, çoklu boss gereksinimi, eksik/null metadata, özel ilerleme sırası ve yenileme sınanır. API imzaları kurulu EpicLoot 0.14.13 DLL içinde incelendi; TierChecks API bağlantısını taklit eder ve native Unity başlık/fare çizimini doğrulamaz.

0.9.4: 7 oyun dili/efekt kontrolü, Lumberjack açıklamasının API metni olarak korunması ve Türkçe spawner notunun ayrı tutulmasını, açıklama aramasını, dil değişimi önbelleğini ve eksik açıklama desteğini kapsar. GetEffectText/GetEffectDescription bağlantıları doğrulanmış localization sözleşmesini taklit eder. NameButton font ölçümü ve bilgi kutusu çizimi bu başsız testte taklit edilmez; yalnızca tıklama bağlantısı sağlanır.

1.0.2: Disenchant.cs gerçek kaynağı normal önizleme, shardstone iadesi, toplu kapasite, yığın birleştirme, kısmi iade/son bildirim hatasında geri yükleme ve kaynak değişimi için çalıştırılır. Inventory ve ReconstructShardItem test tesisatı taklittir; kurulu DLL imza/uygulaması yerel olarak incelendi. Canlı oyun pano/GUI ve soket renk/nadirlik kontrolü ayrıca gerekir.


1.3.0: Toplam 328 bağlı kaynak davranış kontrolü. SpecialInventoryChecks gerçek envanter seçimi/yükleme/Uygula, kimlik koruması, büyü kaldırma farkı ve geri alma akışlarını çalıştırır. SpecialItemMenu gerçek kaynağı, oyun dilindeki kartlar ve GUI olayında ertelenen seçim için bağlıdır. Yeni üretim SelectSpecial uygulaması burada fake Spawner ile temsil edilir; gerçek Spawner RuntimeChecks içinde ayrıca test edilir. LegendaryFixtures lookup/set/requirements nesneleri kurulu EpicLoot 0.14.13 şekillerini taklit eder. Native item-type predicate, equip FX ve set bonusu hesaplaması oyun içinde ayrıca kontrol edilir.
