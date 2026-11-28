using System.Collections.Generic;

namespace BasicModPack
{
    public enum Language { English, Chinese, Turkish }

    // The pack's own texts; the English text is the key. Chosen on the panel (EN / TR / 中文), kept in the config file.
    public static class Lang
    {
        public static Language Current = Language.English;

        // English -> { Chinese, Turkish }
        static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            // Panel
            ["One-time actions"] = new[] { "一次性操作", "Tek seferlik işlemler" },
            ["Always on"] = new[] { "持续生效", "Sürekli aktif işlemler" },
            ["Apply"] = new[] { "应用", "Uygula" },
            ["Apply all"] = new[] { "全部应用", "Tümünü uygula" },
            ["ON"] = new[] { "开", "AÇIK" },
            ["OFF"] = new[] { "关", "KAPALI" },
            ["{0} opens this panel. Actions work within your construction range; switches and the language are kept in the config file."] = new[]
            {
                "{0} 打开此面板。操作在建造范围内生效；开关和语言保存在配置文件中。",
                "{0} bu paneli açar. İşlemler inşa menzilin içinde çalışır; anahtarlar ve dil yapılandırma dosyasında saklanır.",
            },

            // Features
            ["Upgrade in range"] = new[] { "范围内升级", "Menzilde yükselt" },
            ["Belts and sorters in construction range become the best tier you carry; the old ones go back to your inventory."] = new[]
            {
                "建造范围内的传送带和分拣器升级为你携带的最高等级；旧的返回背包。",
                "İnşa menzilindeki bant ve sorterlar taşıdığın en iyi seviyeye yükselir; eskileri envantere döner.",
            },
            ["Fill stations in range"] = new[] { "填充范围内物流站", "Menzildeki istasyonları doldur" },
            ["Logistics drones, vessels and space warpers from your inventory into the stations in range, up to their capacity."] = new[]
            {
                "把背包中的物流无人机、物流运输船和空间翘曲器放入范围内的物流站，直到上限。",
                "Envanterdeki lojistik dron, gemi ve warper'lar menzildeki istasyonlara kapasiteleri kadar konur.",
            },
            ["Fuel generators in range"] = new[] { "为范围内发电机加燃料", "Menzildeki jeneratörlere yakıt koy" },
            ["Tops up the fuel of burning generators in range; an empty one gets the fitting fuel you carry most of."] = new[]
            {
                "补满范围内燃料发电机的燃料；空的发电机放入你携带最多的适用燃料。",
                "Menzildeki yakıtlı jeneratörlerin yakıtını tamamlar; boş olana en çok taşıdığın uygun yakıt konur.",
            },
            ["Clear ruins in range"] = new[] { "清除范围内废墟", "Menzildeki kalıntıları temizle" },
            ["Removes the rubble of destroyed buildings, and Dark Fog base remains the game lets you clear. Ruins hold no items."] = new[]
            {
                "清除被摧毁建筑的废墟，以及游戏允许清除的黑雾基地残骸。废墟不含物品。",
                "Yıkılan binaların enkazını ve oyunun izin verdiği Kara Sis üs kalıntılarını kaldırır. Kalıntılardan eşya çıkmaz.",
            },
            ["Storage limit in range"] = new[] { "范围内储物仓限制", "Menzildeki depo sınırı" },
            ["Sets the red limit bar of every storage in range to the number of open slots typed here; 0 removes it."] = new[]
            {
                "把范围内每个储物仓的红色限制条设为此处输入的开放格数；0 为取消限制。",
                "Menzildeki her deponun kırmızı sınır çubuğunu buraya yazılan açık slot sayısına çeker; 0 sınırı kaldırır.",
            },
            ["Pick up drops in range"] = new[] { "拾取范围内掉落物", "Menzildeki ganimeti topla" },
            ["Items on the ground in construction range fly to you. ON: by itself, as the item filter allows; OFF: with Apply."] = new[]
            {
                "建造范围内地上的物品飞向你。开：按拾取筛选自动拾取；关：点击应用拾取。",
                "İnşa menzilindeki yerdeki eşyalar sana uçar. AÇIK: toplama filtresine göre kendiliğinden; KAPALI: Uygula ile.",
            },
            ["Item filter:"] = new[] { "拾取筛选：", "Toplama filtresi:" },
            ["Pick all"] = new[] { "全部拾取", "Hepsini topla" },
            ["Pick none"] = new[] { "全不拾取", "Hiçbirini toplama" },
            ["Invert"] = new[] { "反选", "Tersine çevir" },
            ["Random"] = new[] { "随机", "Rastgele" },
            ["Item filter: every item is picked up automatically."] = new[] { "拾取筛选：所有物品自动拾取。", "Toplama filtresi: tüm eşyalar otomatik toplanır." },
            ["Item filter: no item is picked up."] = new[] { "拾取筛选：不拾取任何物品。", "Toplama filtresi: hiçbir eşya toplanmaz." },
            ["Item filter: inverted; the rest is picked up automatically."] = new[] { "拾取筛选：已反选；其余物品自动拾取。", "Toplama filtresi: tersine çevrildi; diğerleri otomatik toplanır." },
            ["Item filter: each item set at random."] = new[] { "拾取筛选：每种物品随机设置。", "Toplama filtresi: her eşya rastgele ayarlandı." },
            ["Station templates"] = new[] { "物流站模板", "İstasyon şablonları" },
            ["Save a station's slots, limits and range settings under a name, then apply them to any station of the same type from its window."] = new[]
            {
                "以名称保存物流站的栏位、上限和范围设置，然后在同类物流站窗口中一键应用。",
                "Bir istasyonun slot, sınır ve menzil ayarlarını bir adla kaydet, sonra aynı türdeki istasyonlara penceresinden uygula.",
            },
            ["Power warning"] = new[] { "电力警告", "Güç uyarısı" },
            ["A message and a sound when the planet you are on gets less than 90% of the power it needs."] = new[]
            {
                "所在星球的电力满足率低于 90% 时，显示提示并播放声音。",
                "Bulunduğun gezegen ihtiyacı olan gücün %90'ından azını aldığında mesaj ve ses verir.",
            },
            ["Station stock warning"] = new[] { "物流站库存警告", "İstasyon eksik uyarısı" },
            ["Names the interstellar station that has run out of vessels, or of warpers while it has vessels."] = new[]
            {
                "提示运输船用完、或有运输船但翘曲器用完的星际物流站名称。",
                "Gemisi biten ya da gemisi varken warper'ı biten yıldızlararası istasyonun adını söyler.",
            },
            ["Build counter"] = new[] { "建造计数", "İnşa sayacı" },
            ["Beside the cursor while building: how many you carry, and how many a dragged belt line takes."] = new[]
            {
                "建造时在光标旁显示：携带数量，以及拖动的传送带需要多少。",
                "İnşa ederken imlecin yanında: kaç tane taşıdığın ve sürüklenen bant hattının kaç parça tuttuğu.",
            },
            ["Best chain recipe"] = new[] { "最优连锁配方", "En verimli zincir tarifi" },
            ["When the replicator makes a missing input along the way, it uses the recipe that can make the most of it from your inventory, among every recipe that makes it, other mods' included."] = new[]
            {
                "复制器在连锁制造缺少的原料时，从所有能制造该原料的配方（包括其他模组的配方）中，使用用背包物品能制造最多的配方。",
                "Çoğaltıcı eksik bir malzemeyi zincirle üretirken, onu yapan tüm tarifler arasından (diğer modlarınkiler dahil) envanterinle en çok üretebilen tarifi kullanır.",
            },
            ["Auto replenish"] = new[] { "自动补充", "Otomatik ikmal" },
            ["The arrow button on drone, vessel and bot slots is on for every station and distributor. Only the planet you are on is filled from your inventory, one building after another."] = new[]
            {
                "所有物流站和配送器的无人机、运输船和配送机槽位上的箭头按钮保持开启。只从背包补充你所在星球上的建筑，逐个进行。",
                "Tüm istasyon ve dağıtıcılarda dron, gemi ve kurye yuvalarındaki ok düğmesi açık olur. Yalnız bulunduğun gezegendeki binalar, sırayla, envanterinden doldurulur.",
            },
            ["Distributor item from depot"] = new[] { "配送器物品取自储物仓", "Dağıtıcı eşyası depodan" },
            ["A copied distributor placed on a depot that holds items picks its item from the depot instead of keeping the copied one."] = new[]
            {
                "复制的配送器放在装有物品的储物仓上时，从储物仓中选取物品，而不是保留复制的物品。",
                "Eşya bulunan bir deponun üstüne konan kopyalanmış dağıtıcı, kopyalanan eşyayı tutmak yerine eşyasını depodan seçer.",
            },
            ["Control panel copy/paste"] = new[] { "控制面板复制/粘贴", "Kontrol panelinde kopyala/yapıştır" },
            ["A copy/paste button on control panel rows of stations, distributors and advanced mining machines. Right click cancels the copy."] = new[]
            {
                "在控制面板中物流站、配送器和大型采矿机的行上添加复制/粘贴按钮。右键取消复制。",
                "Kontrol panelindeki istasyon, dağıtıcı ve gelişmiş madenci satırlarına kopyala/yapıştır düğmesi. Sağ tık kopyayı iptal eder.",
            },

            // Results and messages
            ["Land on a planet first."] = new[] { "请先登陆星球。", "Önce bir gezegene in." },
            ["Upgrade: nothing to upgrade in range."] = new[] { "升级：范围内没有可升级的。", "Yükseltme: menzilde yükseltilecek bir şey yok." },
            ["Upgrade: {0} upgraded."] = new[] { "升级：已升级 {0} 个。", "Yükseltme: {0} yükseltildi." },
            ["Upgrade: {0} upgraded; stopped, inventory full."] = new[] { "升级：已升级 {0} 个；背包已满，已停止。", "Yükseltme: {0} yükseltildi; envanter dolu, durdu." },
            ["Stations: nothing to fill (full, or none in your inventory)."] = new[] { "物流站：没有可填充的（已满，或背包中没有）。", "İstasyonlar: doldurulacak bir şey yok (dolu ya da envanterde yok)." },
            ["Stations: {0} filled with {1} drones, {2} vessels, {3} warpers."] = new[] { "物流站：填充了 {0} 座，无人机 {1}、运输船 {2}、翘曲器 {3}。", "İstasyonlar: {0} istasyona {1} dron, {2} gemi, {3} warper kondu." },
            ["Generators: nothing to fuel (full, or no fitting fuel carried)."] = new[] { "发电机：没有可添加的（已满，或没有适用燃料）。", "Jeneratörler: yakıt konacak yer yok (dolu ya da uygun yakıt yok)." },
            ["Generators: {0} fuelled with {1} items."] = new[] { "发电机：为 {0} 台添加了 {1} 个燃料。", "Jeneratörler: {0} jeneratöre {1} yakıt kondu." },
            ["Ruins: none in range."] = new[] { "废墟：范围内没有。", "Kalıntılar: menzilde yok." },
            ["Ruins: {0} cleared."] = new[] { "废墟：已清除 {0} 处。", "Kalıntılar: {0} temizlendi." },
            ["Storage limit: type the number of open slots first (0 removes the limit)."] = new[] { "储物仓限制：请先输入开放格数（0 为取消限制）。", "Depo sınırı: önce açık slot sayısını yaz (0 sınırı kaldırır)." },
            ["Storage limit: no storage in range."] = new[] { "储物仓限制：范围内没有储物仓。", "Depo sınırı: menzilde depo yok." },
            ["Storage limit: removed on {0} storages."] = new[] { "储物仓限制：已取消 {0} 个储物仓的限制。", "Depo sınırı: {0} depoda kaldırıldı." },
            ["Storage limit: {0} storages open {1} slots each."] = new[] { "储物仓限制：{0} 个储物仓各开放 {1} 格。", "Depo sınırı: {0} depoda {1} slot açık." },
            ["Pickup: nothing to pick up in range."] = new[] { "拾取：范围内没有可拾取的物品。", "Toplama: menzilde toplanacak eşya yok." },
            ["Pickup: {0} items on their way."] = new[] { "拾取：{0} 组物品正在飞来。", "Toplama: {0} eşya yolda." },
            ["Left: {0} out of range, {1} kept by the item filter, {2} still falling."] = new[] { "剩余：超出范围 {0}，被拾取筛选保留 {1}，仍在下落 {2}。", "Kalan: {0} menzil dışında, {1} toplama filtresinde, {2} hâlâ düşüyor." },
            ["Pickup: inventory full."] = new[] { "拾取：背包已满。", "Toplama: envanter dolu." },
            ["Low power on {0}: {1}% of demand met."] = new[] { "{0} 电力不足：仅满足需求的 {1}%。", "{0} gezegeninde güç düşük: ihtiyacın %{1}'i karşılanıyor." },
            ["Out of stock: {0}."] = new[] { "库存耗尽：{0}。", "Tükendi: {0}." },
            ["{0} and {1} more"] = new[] { "{0} 等另外 {1} 座", "{0} ve {1} istasyon daha" },
            ["vessels"] = new[] { "运输船", "gemi" },
            ["warpers"] = new[] { "翘曲器", "warper" },
            ["{0}: {1} to place, {2} carried"] = new[] { "{0}：需放置 {1}，携带 {2}", "{0}: {1} yerleştirilecek, {2} taşınıyor" },
            ["{0}: {1} carried"] = new[] { "{0}：携带 {1}", "{0}: {1} taşınıyor" },
            [" ({0} short)"] = new[] { "（缺 {0}）", " ({0} eksik)" },

            // Station templates window
            ["Template {0}"] = new[] { "模板 {0}", "Şablon {0}" },
            ["No template for this station type yet."] = new[] { "此类物流站还没有模板。", "Bu istasyon türü için henüz şablon yok." },
            ["1 template for this station type"] = new[] { "此类物流站有 1 个模板", "Bu istasyon türü için 1 şablon" },
            ["{0} templates for this station type"] = new[] { "此类物流站有 {0} 个模板", "Bu istasyon türü için {0} şablon" },
            ["Template saved: {0}"] = new[] { "模板已保存：{0}", "Şablon kaydedildi: {0}" },
            ["Template applied: {0}"] = new[] { "模板已应用：{0}", "Şablon uygulandı: {0}" },
            ["Save"] = new[] { "保存", "Kaydet" },
            ["Fold or unfold"] = new[] { "折叠或展开", "Daralt veya genişlet" },
            ["Shows one template, or the whole list."] = new[] { "只显示一个模板，或显示全部。", "Tek şablonu ya da tüm listeyi gösterir." },
            ["Apply template"] = new[] { "应用模板", "Şablonu uygula" },
            ["Sets this station's items, limits, slot directions and range settings."] = new[] { "设置此物流站的物品、上限、栏位方向和范围。", "Bu istasyonun eşyalarını, sınırlarını, slot yönlerini ve menzil ayarlarını uygular." },
            ["Delete template"] = new[] { "删除模板", "Şablonu sil" },
        };

        public static string T(string english) =>
            Current != Language.English && texts.TryGetValue(english, out var t) ? t[(int)Current - 1] : english;

        public static string F(string english, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, T(english), args);

        // For the check: every text has both translations, with the same placeholders as the English.
        public static IEnumerable<KeyValuePair<string, string[]>> All => texts;
    }
}
