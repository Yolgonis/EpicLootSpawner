using System;
using System.Globalization;
using System.Text;
using EpicLoot;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private string DiagnosticText()
        {
            var report = new StringBuilder("EpicLoot Spawner tanılama raporu\n");
            void Line(string label, string value) => report.Append(label).Append(": ").AppendLine((value ?? "").Replace('\r', ' ').Replace('\n', ' '));
            Line("Zaman (UTC)", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Line("Spawner", Version);
            Line("EpicLoot", API.GetPluginVersion().ToString());
            Line("Valheim", global::Version.GetVersionString());
            Line("Unity", Application.unityVersion);
            Line("Menü dili", "Türkçe");
            Line("Oyun dili", Localization.instance?.GetSelectedLanguage());
            Line("Açma tuşu", OpenKeyName);
            Line("Ekran", Screen.width + "×" + Screen.height);
            Line("Pencere / tercih edilen boyut", layout.Width + "×" + layout.Height + " / " + preferredWidth + "×" + preferredHeight);
            Line("İstenen / kullanılan ölçek", uiScale.Value.ToString("0.###", CultureInfo.InvariantCulture) + " / " + layout.Scale.ToString("0.###", CultureInfo.InvariantCulture));
            Line("Katalog", filteredItems.Length + "/" + items.Count + " eşya, " + stones.Count + " taş");
            Line("Sıralama", itemSort == 0 ? "Tier + Ad" : itemSort == 1 ? "Tür" : "Favori");
            Line("Mod", inventoryKey == null ? "Yeni eşya üretimi" : "Asıl envanter eşyasını düzenleme");
            Line("Limitsiz mod", builder.Unlimited ? "Açık" : "Kapalı");
            Line("Efekt içi tavanlar", EffectCaps.Active.Count + "/" + EffectCaps.Total + " bağlı, " + (EffectCaps.Lifted ? "kaldırıldı" : "EpicLoot varsayılanı")
                + (EffectCaps.Missing.Count > 0 ? "; bağlanamayan: " + string.Join(", ", EffectCaps.Missing) : ""));
            Line("Seçili eşya", builder.Prefab ? Label(builder.Prefab) : "Yok");
            Line("Özel kimlik / set", (builder.LegendaryID ?? "Yok") + " / " + (builder.SetID ?? "Yok"));
            Line("Prefab", builder.Prefab ? builder.Prefab.name : "Yok");
            Line("Tasarım", "Seviye " + builder.Quality + ", " + builder.Rarity + ", " + builder.Effects.Count + " efekt, " + builder.Stones.Count + "/" + builder.SocketCount + " taş");
            Line("Üretim", production == null ? "Beklemede" : produced + "/" + requested);
            Line("Katalog / önizleme / taş (ms)", lastCatalogMs.ToString("0.0", CultureInfo.InvariantCulture) + " / " + lastPreviewMs.ToString("0.0", CultureInfo.InvariantCulture) + " / " + lastStoneMs.ToString("0.0", CultureInfo.InvariantCulture));
            Line("Durum", status);
            Line("Son işlem", lastOperation);
            Line("Son hata", string.IsNullOrEmpty(lastError) ? "Yok" : lastError);
            Line("Önizleme", previewError ?? (PreviewValid ? "Geçerli" : "Güncelleniyor"));
            foreach (var entry in fieldErrors) Line("Alan " + entry.Key, entry.Value);
            if (!string.IsNullOrEmpty(lastErrorDetail)) report.AppendLine("\nHata ayrıntısı:").AppendLine(lastErrorDetail);
            return report.ToString();
        }

        private void DrawDiagnostics()
        {
            GUILayout.Label("Sürümler ve ortam", section);
            GUILayout.Label("Spawner " + Version + " • EpicLoot " + API.GetPluginVersion() + "\nValheim " + global::Version.GetVersionString() + " • Unity " + Application.unityVersion, muted);
            GUILayout.Label("Ekran " + Screen.width + "×" + Screen.height + " • Ölçek %" + Mathf.RoundToInt(layout.Scale * 100) + "\nOyun dili: " + Localization.instance.GetSelectedLanguage() + " • Açma tuşu: " + OpenKeyName, muted);
            GUILayout.Label(items.Count + " eşya • " + stones.Count + " taş • " + stonesEvaluated + " taş hesabı", muted);
            GUILayout.Label("Son katalog: " + lastCatalogMs.ToString("0.0") + " ms  •  Önizleme: " + lastPreviewMs.ToString("0.0") + " ms  •  Taş: " + lastStoneMs.ToString("0.0") + " ms", muted);
            GUILayout.Label("Mevcut durum", section);
            GUILayout.Label(SafeLabel(string.IsNullOrEmpty(status) ? "Hazır" : status));
            GUILayout.Label("Önizleme: " + SafeLabel(previewError ?? (PreviewValid ? "Geçerli" : "Güncelleniyor")), muted);
            foreach (var entry in fieldErrors) GUILayout.Label(SafeLabel(entry.Key + ": " + entry.Value), errorHint, GUILayout.Width(590));
            GUILayout.Label("Son işlem: " + SafeLabel(lastOperation), muted);
            GUILayout.Label("Son hata: " + SafeLabel(string.IsNullOrEmpty(lastError) ? "Yok" : lastError), errorHint);
            if (!string.IsNullOrEmpty(lastErrorDetail)) GUILayout.Label(SafeLabel(lastErrorDetail), muted, GUILayout.Width(590));
            if (GUILayout.Button("Tanılama raporunu kopyala")) QueueAction(() =>
            {
                GUIUtility.systemCopyBuffer = DiagnosticText();
                status = "Tanılama raporu panoya kopyalandı.";
            });
            if (GUILayout.Button("Yedekleri ve kurtarmayı aç")) QueueAction(() => { showAppearance = false; showRecovery = true; recoveryCandidate = null; });
        }

        private void DrawQuickGuide()
        {
            void Topic(string title, string text) { GUILayout.Label(title, section); GUILayout.Label(text, body); GUILayout.Space(4); }
            Topic("Yeni eşya", "Üstte Yeni eşya seçiliyken soldaki listeden eşyayı seç. Ortadaki sayfada nadirliği, seviyeyi, adedi, soket sayısını ve adı ayarla. "
                + "+ Efekt ekle ve + Taş tak açılan listede her seçeneği oyun içi açıklamasıyla gösterir. Sağdaki önizleme geçerli olunca alttan teslim yerini seçip Eşyayı üret'e bas.");
            Topic("Legendary / Set", "Eşya başlığının yanındaki Legendary / Set… düğmesi bu eşya için uygun özel kimlikleri açıklama ve set bonuslarıyla listeler. "
                + "Kimlik seçmek mevcut efekt, taş ve adı temizler; başlangıç güçleri otomatik eklenir.");
            Topic("Limitsiz mod", "Üst çubuktaki Limitsiz düğmesi açıkken efekt değerleri normal aralığın dışına çıkabilir (negatif dahil), en fazla "
                + Spawner.UnlimitedEffects + " efekt ve " + Spawner.UnlimitedSockets + " soket kullanılabilir. Eşya türüne uymayan veya yinelenen efektler yine eklenemez. "
                + "Aralık dışı değerler efekt kartında sarı uyarıyla gösterilir. Aynı ayar config dosyasında da saklanır. "
                + "Açıkken EpicLoot'un efekt içi tavanları da kalkar: direnç en fazla %100 (MaxResistance yerine), Bulk Up, Mercenary, Lucky Loot, parry, yavaşlatma, iksir süresi, "
                + "gece/Off-Set hasar azaltma, kan bedeli, Shocking Charge ve adrenalin verimi. Negatif hasar, sıfır altı maliyet/süre ve 1'in altına düşen can gibi güvenlik sınırları korunur. "
                + "Tavanlar oyuncunun tüm kuşanılmış eşyaları için geçerlidir; mod kapatılınca EpicLoot varsayılanına döner.");
            Topic("Envanterim", "Üstte Envanterim'e geçip çantandaki bir eşyayı seç. Seçmek eşyayı değiştirmez. Tasarımı düzenle, sağdaki Değişiklikler sekmesini incele ve "
                + "alttaki Asıl eşyaya uygula ile kaydet. Kullanılan eşyayı önce çıkar. Büyüyü kaldır… ve Taş çıkar… işlemleri asıl taşları envantere iade eder; önce onay ekranı açılır.");
            Topic("Geri alma", "Üstteki ← Geri / İleri → tasarım değişikliklerini geri alır. Alttaki Son işlemi geri al…, bu oturumdaki son envanter işlemini (uygulama, büyü kaldırma, taş çıkarma) geri alır.");
            Topic("Şablonlar", "Üstteki Şablonlar düğmesi sol sütunu kayıtlı tasarımlara çevirir. Buradan kaydedebilir, düzenleyebilir, panoya kopyalayabilir ve koleksiyon dosyası olarak dışa/içe aktarabilirsin.");
            Topic("Tuşlar ve pencere", OpenKeyName + " paneli açar/kapatır. Esc önce açık pencereyi, sonra paneli kapatır. Pencereyi üst kenarından sürükle; sağ alt ◢ köşesinden boyutlandır. "
                + "Ölçek bu ekrandaki Görünüm sekmesinden değişir.");
            Topic("Sorun bildirimi", "Alt çubuktaki ? düğmesi Tanılama'yı açar. Raporu kopyala; yaptığın işlemi, beklediğin ve gördüğün sonucu raporla birlikte paylaş.");
        }
    }
}
