using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace BasicModPack
{
    // Auto replenish and distributor item from depot; the switches are the pack's (Plugin.Awake), PanelCopy's too.
    public static class GameTweaks
    {
        internal static ConfigEntry<bool> AutoReplenish, DepotItem, PanelCopyPaste;

        internal static void Apply()
        {
            var harmony = new Harmony(Plugin.Guid + ".Game");
            harmony.PatchAll(typeof(GameTweaks));
            harmony.PatchAll(typeof(PanelCopy));
            Plugin.Log.LogInfo("Game tweaks patched (auto replenish " + AutoReplenish.Value + ", distributor item from depot " + DepotItem.Value + ", control panel copy/paste " + PanelCopyPaste.Value + ")");
        }

        static void TurnOn(PlanetTransport t, int stationId, int dispenserId)
        {
            if (stationId > 0)
            {
                var s = t.stationPool[stationId];
                if (s != null && s.id == stationId && !s.isCollector && !s.isVeinCollector)
                {
                    s.droneAutoReplenish = true;
                    s.shipAutoReplenish = true;
                }
            }
            if (dispenserId > 0)
            {
                var d = t.dispenserPool[dispenserId];
                if (d != null && d.id == dispenserId) d.courierAutoReplenish = true;
            }
        }

        // New buildings: Init clears the switches.
        [HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
        static void NewStation(PlanetTransport __instance, StationComponent __result)
        {
            if (AutoReplenish.Value && __result != null) TurnOn(__instance, __result.id, 0);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewDispenserComponent))]
        static void NewDispenser(PlanetTransport __instance, int __result)
        {
            if (AutoReplenish.Value) TurnOn(__instance, 0, __result);
        }

        // Copied buildings and blueprints: the copied settings overwrite the switches; the game fills right after this.
        [HarmonyPostfix, HarmonyPatch(typeof(BuildingParameters), nameof(BuildingParameters.ApplyPrebuildParametersToEntity))]
        static void Copied(int entityId, PlanetFactory factory)
        {
            if (!AutoReplenish.Value || factory?.entityPool == null) return;
            TurnOn(factory.transport, factory.entityPool[entityId].stationId, factory.entityPool[entityId].dispenserId);
        }

        // Saved buildings.
        [HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.Import))]
        static void Loaded(PlanetTransport __instance)
        {
            if (!AutoReplenish.Value) return;
            for (int i = 1; i < __instance.stationCursor; i++) TurnOn(__instance, i, 0);
            for (int i = 1; i < __instance.dispenserCursor; i++) TurnOn(__instance, 0, i);
        }

        // The game fills from the inventory when a building is placed, pasted or its window opens. Windows of stations on
        // other planets (opened from a station list) would take from the inventory too: only the planet you are on fills.
        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityAutoReplenishIfNeeded))]
        static bool EntityFill(PlanetFactory __instance) => !AutoReplenish.Value || IsLocal(__instance);

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
        static bool StationFill(PlanetFactory __instance) => !AutoReplenish.Value || IsLocal(__instance);

        static bool IsLocal(PlanetFactory f) => f != null && f.planet != null && f.planet == GameMain.localPlanet;

        // Keeps topping up on the planet you are on, one building every few ticks (stations first, then distributors),
        // so drones lost later or bought later reach every building without opening it. Silent: no popup per building.
        public const int TicksPerBuilding = 6;
        static int tick, cursor;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "FixedUpdate")]
        static void Step()
        {
            if (!AutoReplenish.Value || !GameMain.isRunning || GameMain.isPaused || ++tick < TicksPerBuilding) return;
            tick = 0;
            var data = GameMain.data;
            var f = data?.localPlanet?.factory;
            var package = data?.mainPlayer?.package;
            if (f?.transport == null || package == null || data.guideRunning) return;
            var t = f.transport;
            cursor = NextSlot(cursor, t.stationCursor, t.dispenserCursor);
            if (cursor < t.stationCursor)
            {
                var s = t.stationPool[cursor];
                if (s == null || s.id != cursor || s.isCollector || s.isVeinCollector) return;
                if (s.droneAutoReplenish) s.idleDroneCount += Take(package, 5001, s.workDroneDatas.Length - s.idleDroneCount - s.workDroneCount);
                if (s.isStellar && s.shipAutoReplenish) s.idleShipCount += Take(package, 5002, s.workShipDatas.Length - s.idleShipCount - s.workShipCount);
            }
            else
            {
                int id = cursor - t.stationCursor;
                var d = t.dispenserPool[id];
                if (d == null || d.id != id || !d.courierAutoReplenish) return;
                d.idleCourierCount += Take(package, 5003, d.workCourierDatas.Length - d.idleCourierCount - d.workCourierCount);
            }
        }

        // Slots 1..stations-1 are stations, stations+1..stations+dispensers-1 distributors (each pool starts at id 1).
        public static int NextSlot(int cursor, int stations, int dispensers)
        {
            int total = stations + dispensers;
            cursor++;
            if (cursor >= total) cursor = 1;
            return cursor == stations ? (cursor + 1 >= total ? 1 : cursor + 1) : cursor;
        }

        static int Take(StorageComponent package, int item, int missing)
        {
            if (missing <= 0 || package.GetItemCount(item) <= 0) return 0;
            int id = item, count = missing;
            package.TakeTailItems(ref id, ref count, out _);
            return id == item && count > 0 ? count : 0;
        }

        // The game guesses a distributor's item from its depot only while the item is empty, but a copied distributor
        // already has the copied item by then. Clearing it lets the game's guess (and DeliveryItemChain's) run as for a new one.
        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.OnAddonBuilt))]
        static void AddonBuilt(PlanetFactory __instance, int entityId)
        {
            if (!DepotItem.Value) return;
            int id = __instance.entityPool[entityId].dispenserId;
            var d = id > 0 ? __instance.transport.dispenserPool[id] : null;
            if (d == null || d.id != id || d.storage == null) return;
            if (ShouldRepick(d.filter, DepotItems(d.storage))) d.filter = 0;
        }

        // Every item kind in the depot stack from the distributor's box down, as the game's GuessFilter walks it.
        static List<int> DepotItems(StorageComponent top)
        {
            var items = new List<int>();
            var bottom = top.bottomStorage;
            for (var s = top; s != null; s = s == bottom ? null : s.previousStorage)
                for (int i = 0; i < s.size; i++)
                {
                    int item = s.grids[i].itemId;
                    if (item > 0 && s.grids[i].count > 0 && !items.Contains(item)) items.Add(item);
                }
            return items;
        }

        // Keep a copied item the depot already holds; an empty depot keeps the copied item too.
        public static bool ShouldRepick(int filter, List<int> depot) => filter > 0 && depot.Count > 0 && !depot.Contains(filter);
    }
}
