using BepInEx.Configuration;
using UnityEngine;

namespace BasicModPack
{
    // Items lying on the planet within construction range fly to the mecha, by the game's own pickup: an item marked with
    // expire = 35 is added to the inventory by TrashSystem.GameTick. The game marks only what lies within 50 m.
    // Switched on it runs twice a second and follows the item filter like the game's auto pickup (only items set to pick up);
    // Apply picks up everything the filter does not exclude, like clicking an item does.
    public static class LootPickup
    {
        public static ConfigEntry<bool> Enabled;

        public static string Run()
        {
            int n = Pick(true, out string why, out int far, out int kept, out int falling);
            if (why != null) return why;
            string s = n == 0 ? Lang.T("Pickup: nothing to pick up in range.") : Lang.F("Pickup: {0} items on their way.", n);
            // What stays on this planet, and why: the answer to "why is that one still lying there".
            if (far + kept + falling > 0) s += " " + Lang.F("Left: {0} out of range, {1} kept by the item filter, {2} still falling.", far, kept, falling);
            return s;
        }

        public static void Tick()
        {
            if (Enabled.Value && Time.frameCount % 30 == 0) Pick(false, out _, out _, out _, out _);
        }

        // ---- The game's item filter in one click. Its states: 1 picked up automatically, 0 never, no entry by hand only. ----

        public enum FilterMode { All, None, Invert, Random }

        public static int Next(FilterMode mode, int? current, System.Random random) =>
            mode == FilterMode.All ? 1 : mode == FilterMode.None ? 0 : mode == FilterMode.Invert ? (current == 1 ? 0 : 1) : random.Next(2);

        public static string SetFilter(FilterMode mode)
        {
            var filters = GameMain.mainPlayer?.controller?.actionPick?.pickFilters;
            if (filters == null) return Lang.T("Land on a planet first.");
            var random = new System.Random();
            foreach (ItemProto item in LDB.items.dataArray)
            {
                if (item == null) continue;  // every item, also those the filter window does not list
                int? now = filters.TryGetValue(item.ID, out int v) ? v : (int?)null;
                filters[item.ID] = Next(mode, now, random);
            }
            // An open filter window works on its own copy and writes it back on close: reload it from the new settings.
            var window = UIRoot.instance.uiGame.lootFilter;
            if (window != null && window.active)
            {
                var t = HarmonyLib.Traverse.Create(window);
                foreach (string m in new[] { "GetFilterSettings", "RefreshIcons", "SetBufferData", "SetMaterialProps" }) t.Method(m).GetValue();
            }
            return Lang.T(mode == FilterMode.All ? "Item filter: every item is picked up automatically."
                : mode == FilterMode.None ? "Item filter: no item is picked up."
                : mode == FilterMode.Invert ? "Item filter: inverted; the rest is picked up automatically."
                : "Item filter: each item set at random.");
        }

        // Same rules as PlayerAction_Pick: auto takes items set to 1 in the filter (and 1099), manual all but those set to 0.
        public static bool Wanted(int item, int? filter, bool manual) => manual ? filter != 0 : item == 1099 || filter == 1;

        static int Pick(bool manual, out string why, out int far, out int kept, out int falling)
        {
            why = null;
            far = kept = falling = 0;
            Player player = GameMain.mainPlayer;
            PlanetData planet = GameMain.localPlanet;
            var trash = GameMain.data?.trashSystem?.container;
            if (player == null || planet == null || trash == null || player.package == null || !player.isAlive) { why = Lang.T("Land on a planet first."); return 0; }
            if (player.package.isFull) { why = Lang.T("Pickup: inventory full."); return 0; }  // a full inventory would throw the item back out
            var filters = player.controller?.actionPick?.pickFilters;
            float range2 = player.mecha.buildArea * player.mecha.buildArea;
            Vector3 at = player.position;
            var objs = trash.trashObjPool;
            var datas = trash.trashDataPool;
            int marked = 0;
            for (int i = 0; i < trash.trashCursor; i++)
            {
                int item = objs[i].item;
                if (item <= 0 || objs[i].expire >= 0) continue;
                if (datas[i].landPlanetId != planet.astroId)
                {
                    if (datas[i].landPlanetId == 0 && datas[i].nearPlanetId == planet.id) falling++;  // not landed yet: the game picks up landed items only
                    continue;
                }
                int? filter = filters != null && filters.TryGetValue(item, out int f) ? f : (int?)null;
                if (!Wanted(item, filter, manual)) { kept++; continue; }
                if ((objs[i].rPos - at).sqrMagnitude > range2) { far++; continue; }
                objs[i].expire = 35;
                marked++;
            }
            if (marked > 0) VFAudio.Create("knock-1", player.transform, Vector3.zero, play: true, 3, -1, -1L);
            return marked;
        }
    }
}
