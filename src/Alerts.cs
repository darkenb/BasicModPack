using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace BasicModPack
{
    // Low power on the planet you are on: one warning with a sound when supply drops under 90% of demand, again only
    // after it has recovered to 95%, so a network hovering at the edge does not repeat it.
    public static class PowerAlert
    {
        public static ConfigEntry<bool> Enabled;
        public const double Low = 0.9, Recovered = 0.95;
        static bool warned;
        static int planet;

        // Next warned state for the lowest network ratio (null: no network draws power).
        public static bool Next(bool warned, double? ratio) => ratio == null ? false : warned ? ratio < Recovered : ratio < Low;

        public static void Tick()
        {
            if (!Enabled.Value || UnityEngine.Time.frameCount % 60 != 0) return;
            PlanetFactory factory = GameMain.localPlanet?.factory;
            if (factory == null) { warned = false; return; }
            if (factory.planetId != planet) { planet = factory.planetId; warned = false; }
            var ps = factory.powerSystem;
            double? lowest = null;
            for (int i = 1; i < ps.netCursor; i++)
            {
                PowerNetwork net = ps.netPool[i];
                if (net == null || net.id != i || net.energyRequired <= 0) continue;
                if (lowest == null || net.consumerRatio < lowest) lowest = net.consumerRatio;
            }
            bool now = Next(warned, lowest);
            if (now && !warned) UIRealtimeTip.Popup(Lang.F("Low power on {0}: {1}% of demand met.", factory.planet.displayName, (int)(lowest.Value * 100)), "ui-error");
            warned = now;
        }
    }

    // Interstellar stations that run out of warpers (while they have ships) or of ships: one message per station when it
    // happens, with the station's name. Checked every five seconds over all planets.
    public static class StationAlert
    {
        public static ConfigEntry<bool> Enabled;
        static HashSet<string> short_ = new HashSet<string>();
        static GameData data;

        public static void Tick()
        {
            if (!Enabled.Value || UnityEngine.Time.frameCount % 300 != 0) return;
            if (GameMain.data != data) { data = GameMain.data; short_.Clear(); }
            bool warp = GameMain.history.logisticShipWarpDrive;
            var now = new HashSet<string>();
            var news = new List<string>();
            for (int f = 0; f < data.factoryCount; f++)
            {
                PlanetFactory factory = data.factories[f];
                var pool = factory?.transport?.stationPool;
                if (pool == null) continue;
                for (int i = 1; i < factory.transport.stationCursor; i++)
                {
                    StationComponent s = pool[i];
                    if (s == null || s.id != i || !s.isStellar || s.isCollector || s.isVeinCollector) continue;
                    int ships = s.idleShipCount + s.workShipCount;
                    string what = ships == 0 ? "vessels" : warp && s.warperCount == 0 ? "warpers" : null;
                    if (what == null) continue;
                    string key = factory.planetId + "/" + i + "/" + what;
                    now.Add(key);
                    if (!short_.Contains(key)) news.Add(Name(factory, s) + " (" + Lang.T(what) + ")");
                }
            }
            short_ = now;
            string list = string.Join(", ", news.Take(3).ToArray());
            if (news.Count > 3) list = Lang.F("{0} and {1} more", list, news.Count - 3);
            if (news.Count > 0) UIRealtimeTip.Popup(Lang.F("Out of stock: {0}.", list), "ui-error");
        }

        static string Name(PlanetFactory factory, StationComponent s)
        {
            string name = factory.ReadExtraInfoOnEntity(s.entityId);
            return string.IsNullOrEmpty(name) ? factory.planet.displayName + " #" + s.id : name;
        }
    }
}
