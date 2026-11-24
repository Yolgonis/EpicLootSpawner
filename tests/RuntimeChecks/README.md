# Bağlı üretim kontrolleri

```powershell
dotnet run --project tests/RuntimeChecks/RuntimeChecks.csproj -c Release
```

Kaynak proje kökünden çalıştır; .NET 8 SDK gerekir. 72 kontrol gerçek Spawner.cs, Production.cs, ItemCopies.cs, normal sınırlar ve şablon/aktarım yardımcılarını bağlı kaynak olarak çalıştırır. Oyun açılmaz; kurulu oyun dosyaları değiştirilmez.

- Doğrudan ve soketten Kırılmazlık, klon sırasında Sync olması/olmaması, envantere ve yere teslim: sekiz kombinasyonda doğru başlangıç bayrağı ve kaldırma davranışı.
- Başlangıçta dayanıklılık kullanmayan eşya ve kaynak prefabın değişmemesi.
- 1/100 adet, kare başına sekiz üretim, dolu envanter, durdurma, kısmi teslim, yere teslim hatası, paralel başlatma ve dünyadan çıkış.
- Normal sınırlar, uyumlu efekt denetimi, geçersiz içe almanın tasarımı koruması ve API hatası.

Unity, prefab/loot kataloğu, oyuncu/Inventory, DropItem ve bazı EpicLoot API bağlantıları Harness.cs içinde taklit edilir. Inventory.AddItem(ItemData) doğrudan nesneyi saklar; DropItem ikinci klonu yapar. Bu sözleşmeler kurulu oyun DLL incelemesinden alınmıştır. EpicLoot Indestructible örneği BehaviorChecks/Fixtures içindedir; kaynak ve kurulu 0.14.13 DLL davranışı karşılaştırılmıştır. Gerçek ShardSocketManager değer hesabı, Unity olayları veya ağ/yer nesnesi yaşam döngüsü test edilmez.

Yeni üretim testi özgün 0.7.1 Spawner/Production kaynaklarıyla da çalıştırıldı; başlangıç dayanıklılığı kaybı nedeniyle beklenen yerde başarısız oldu. Bu tarihsel karşılaştırma geliştirme çalışma dizinindedir; dağıtım paketine dahil edilmez. Yeni üretim kontrolü eski davranışı yakalayan bir regresyondur.
1.0.1: gerçek Spawner kaynağı kalite 30 ile Build/FieldErrors/Import/Export ve envanter/yere teslim için çalıştırılır. Normal prefab maksimumu korunur; geçersiz kalite ve diğer limitler reddedilir. Oyun/EpicLoot tesisatı taklittir; canlı tooltip/hasar ölçeklenmesini simüle etmez.


1.3.0: Toplam 140 bağlı kaynak üretim kontrolü. SpecialChecks gerçek Spawner ile uygun/uygunsuz kimlik, native tanımlı değer aralıkları, sabit/augment edilmiş güçler, özel toplam sınır, set eşleşmesi, import atomikliği, geçmiş verisi ve envanter/yere teslimi sınar. LegendaryFixtures native kayıt/lookup şeklini taklit eder; NoRoll/çatışma ve temel eşya gereksinimleri açık fixture koşullarıdır. Kurulu DLL GetRollRange ve UniqueLegendaryHelper imzaları/lookup mantığı incelendi. Oyun içi equip FX veya giyilen setin gerçek bonusu bu başsız düzenekte ölçülmez.
