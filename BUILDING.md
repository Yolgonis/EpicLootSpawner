# Derleme ve doğrulanmış paketleme

1.3.0 eklemesi: Özel Legendary/set kimliği seçerek üretim; mevcut özel eşyayı kimliği korunarak düzenleme; native özel değer aralıkları, set bonusları ve kalıcı tasarım kimlikleri.

Güncel paket: **1.4.1**. Limitsiz mod açıkken EpicLoot'un efekt içi tavanları da kalkar; aralık dışı değerler oyunda etki eder. 1.4.0: Arayüz tek sayfalık tasarım ekranına dönüştü; isteğe bağlı Limitsiz mod eklendi. Önceki: Yeni üretilen eşyalarda Crafted by satırı gösterilmez. Genel kod incelemesi ve kayıt/aralık/şablon/özel güç düzeltmeleri CODE-REVIEW.md içindedir. Kalite normal üst sınırı bilgi olarak görünür; efekt ve soket sınırları korunur. GameplayValidation=Pending. Runtime README yalnızca İngilizce MOD-PAGE.md özellik listesidir.


Bu akış proje sürümünü değiştirmeden mevcut 1.4.1 kaynağından kurulum ve kaynak arşivleri üretir. Bu kaynak için ACCEPTANCE.json kullanılmaz; önceki sürümlerin kullanıcı geri bildirimleri tests/evidence altında tarihsel kanıt olarak kalır. Ayrıntılı oyun kontrol listesi TESTPLAN.md içindedir.

## Gereksinimler

- Windows, PowerShell 7.2 veya sonrası ve .NET 8 SDK. Mevcut ortamda SDK 8.0.423 kullanıldı.
- Yerel Valheim kurulumu ve EpicLoot 0.14.13, BepInEx 5, Jotunn içeren profil. EpicLoot'un kendi bağımlılıkları da kurulu olmalı.
- İlk NuGet geri yüklemesinde ağ erişimi. Derleme için .NET Framework reference assemblies, BepInEx assembly publicizer ve Newtonsoft.Json referansı csproj tarafından alınır; bu paketler kullanıcıya ikinci oyun/mod DLL'i olarak dağıtılmaz.

Proje .NET Framework 4.8.1 hedefler. .NET 8 SDK testleri ve derleme araçlarını çalıştırır; modun hedefi net481 olarak kalır. Oyun/EpicLoot API referansları yerel kurulumdan okunur; kaynak arşivi oyun DLL'lerini içermez.

## Tek komutla derle, test et, paketle

Kaynak projenin kökünde çalıştır. Profil yolunu kendi kullandığın profile göre değiştir:

```powershell
pwsh -NoProfile -File .\scripts\Build-Release.ps1 `
  -GamePath 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' `
  -ProfilePath "$env:APPDATA\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Sini" `
  -OutputDirectory '.\artifacts\local-release'
```

GamePath ve ProfilePath verilmezse csproj içindeki yerel varsayılanlar kullanılır. Thunderstore profilinde beklenen yollar `BepInEx/core`, `BepInEx/plugins/RandyKnapp-EpicLoot/EpicLoot.dll` ve `BepInEx/plugins/ValheimModding-Jotunn/Jotunn.dll` şeklindedir. Farklı klasör düzeninde csproj HintPath değerlerini uyarlamak gerekir; paketleme komutunun sürüm okuduğu EpicLoot yolu da aynı düzeni kullanır.

Komut:

1. Kaynak ve proje sürümlerini, yerel DLL'leri ve EpicLoot'un gerçek mod sürümünü kontrol eder. Oyun DLL'lerini çalıştırmadan metadata okur. EpicLoot'un dosya sürümü 0.13.0 olsa da gerçek mod sürümü 0.14.13 olabilir; kontrol Version sabitini kullanır.
2. Deterministic derleme ve PathMap ile Release DLL'i üretir. Dört test projesini çalıştırır; başarısızlıkta paket oluşturmaz. ACCEPTANCE.json varsa hedef sürümü, sürüm alanları hariç mod/proje kaynak hash değerlerini ve temel bağımlılık hash değerlerini doğrular; değişiklik varsa genel kullanıcı kabulünü devralmayı reddeder.
3. Kaynakların işlem sırasında değişmediğini, DLL sürümünün kaynakla eşleştiğini ve kopyalanan dosyaların hash değerlerini denetler.
4. Kurulum ZIP'i, kaynak ZIP'i ve RELEASE.json manifestini oluşturur. Her ZIP girdisini SHA256 ile doğrular. Kabul kaydı varsa GameplayValidation=UserReportedBaseline ve GameplayEvidence kaydedilir. Kayıt yoksa GameplayValidation=Pending olur. Genel kullanıcı bildirimi ayrıntılı native test sonucu olarak işaretlenmez.

Var olan çıktı klasörüne yazılmaz; farklı bir OutputDirectory seç. Önceki sürüm arşivleri ve kurulu profil korunur. Çıktı klasörüne yalnızca yeni paketleme dosyaları yazılır; oyuna otomatik kurulum yapılmaz. Hata sonrası oluşmuş kısmi çıktı klasörünü yeniden kullanmak yerine yeni klasör seç.

## Çıktı ve tekrar kontrol

Çıktı klasöründe `EpicLootSpawner-<sürüm>.zip`, `EpicLootSpawner-<sürüm>-source.zip`, `RELEASE.json`, `BUILD-REPORT.json` ve inceleme için runtime/source klasörleri bulunur. Manifest kaynak ve DLL hash değerlerini, bağımlılık sürüm/hash değerlerini, SDK'yı ve başarılı test sayılarını içerir. Her iki ZIP'in içinde aynı manifest bulunur. Rapor tam derleme/test çıktısını ve zamanı içerir; ZIP'e alınmaz.

```powershell
pwsh -NoProfile -File .\scripts\Verify-Release.ps1 -ReleaseDirectory '.\artifacts\local-release'
```

Doğrulama dosyaların manifestle eşleştiğini gösterir; dijital imza veya oyun uyumluluğu kanıtı değildir. Kurulum ZIP'i spawner DLL'i, belgeleri ve kurulum scriptlerini içerir. Config, karakter/dünya verileri, oyun DLL'leri ve test fixture'ları kurulum arşivine konmaz. Kaynak ZIP'i test fixture'larını içerir; BUILDING/COMPATIBILITY dosyaları ve scriptler de dahildir.

ZIP girdileri sıralıdır ve sabit tarihle yazılır. Aynı kaynak, SDK, işletim sistemi/PowerShell/.NET araçları ve bağımlılık DLL'leriyle iki ayrı çıktı klasöründe aynı arşiv hash'leri beklenir. Kaynak, bağımlılık veya araç sürümü değişince aynı hash beklenmez; farklı makinelerde sonuç ayrıca ölçülmeden garanti edilmez.

## Yalnızca derleme veya test

```powershell
dotnet build EpicLootSpawner.csproj -c Release `
  '-p:GamePath=C:\Program Files (x86)\Steam\steamapps\common\Valheim' `
  "-p:ProfilePath=$env:APPDATA\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Sini"
dotnet run --project tests/SpawnerChecks.csproj -c Release
dotnet run --project tests/BehaviorChecks/BehaviorChecks.csproj -c Release
dotnet run --project tests/RuntimeChecks/RuntimeChecks.csproj -c Release
dotnet run --project tests/PanelChecks/PanelChecks.csproj -c Release
```

Test projeleri kurulu Valheim'i başlatmaz. Test yöntemleri ve taklit edilen API sınırları ilgili test README dosyalarında kayıtlıdır.

## Paketleme regresyonu

```powershell
pwsh -NoProfile -File .\tests\scripts\ReleaseWorkflowChecks.ps1 -OutputDirectory '.\artifacts\workflow-checks'
```

Bu kontrol iki bağımsız Release derlemesi yapar; her derlemede 1056 kaynak kontrolü ve kurulu EpicLoot DLL'ine bağlanan 71 canlı tavan kontrolü çalışır. Aynı DLL, manifest ve iki ZIP'in hash'leri karşılaştırılır. Mevcut çıktı klasörüne yazma, eksik bağımlılık, kaynak/proje sürüm farkı ve bozulmuş kurulum/kaynak ZIP'leri ayrıca sınanır. Kabul kaydı bulunmayan 1.4.1 için dokuz paketleme kontrolü uygulanır. Kabul kaydı olan önceki 1.0.0 akışında dört ek kabul kontrolüyle 13 kontrol çalışır. Bunlar kaynak kontrollerinden ayrıdır. Çıktı klasörleri inceleme için tutulur; kurulu profile yazılmaz.

İzole kurulum regresyonu için tests/scripts/InstallerChecks.ps1 kullanılır. PackagePath yeni runtime ZIP'i, EpicLootAssembly kurulu 0.14.13 DLL'i, PreviousSpawnerAssembly önceki spawner DLL'i ve OutputDirectory yeni bir test klasörü olmalıdır. On altı kontrol gerçek kurulum yardımcılarını örnek profilde çalıştırır; ilk kurulum, güncelleme/yedek, config koruma, ikinci DLL, değişmiş hedef, yanlış bağımlılık, bozuk paket ve CLI önizlemesi kapsanır. Gerçek oyuncu profiline yazılmaz. Kurulum CLI komutu ve -Apply kullanımı INSTALLING.md içindedir.
## Kabul kaydı ve kaynak eşleşmesi

ACCEPTANCE.json yalnızca yeni sürüm için geçerli bir kayıt varsa iki ZIP içinde bulunur ve manifestte GameplayEvidence olarak yer alır. Get-GameplaySourceFingerprint yalnızca Plugin.Version ve csproj Version değerlerini, ayrıca satır sonlarını normalleştirir. Tüm src/*.cs, derleme projesi ve her bağımlılığın SHA256 değeri temel 0.9.4 ile eşleşmelidir. Kaynak/proje/bağımlılık değişirse önceki kabul otomatik devralınmaz; yeni çalışma için kabul kaydını güncelle veya kaldırıp Pending paket üret. 13 akış kontrolü, önceki dokuz kontrolün yanında kaynak eşleşmesi ve kaynak/kabul sürümü/bağımlılık farklarının reddini kapsar.
