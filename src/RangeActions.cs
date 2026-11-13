using System;
using UnityEngine;

namespace BasicModPack
{
    // One-time actions on the buildings within the mecha's construction range. Each returns the line the panel shows.
    public static class RangeActions
    {
        public const string NotLanded = "Land on a planet first.";

        // The player and the factory of the planet they stand on, with the range check; false when not on a planet.
        static bool Here(out Player player, out PlanetFactory factory, out Func<Vector3, bool> inRange)
        {
            player = GameMain.mainPlayer;
            factory = player?.factory;
            inRange = null;
            if (factory == null || GameMain.localPlanet?.factory != factory || player.package == null) return false;
            float range2 = player.mecha.buildArea * player.mecha.buildArea;
            Vector3 at = player.position;
            inRange = pos => (pos - at).sqrMagnitude <= range2;
            return true;
        }

        // Takes up to want of item from the inventory; returns how many came, with their proliferator points.
        static int Take(Player player, int item, int want, out int inc)
        {
            inc = 0;
            if (want <= 0) return 0;
            int id = item, count = want;
            player.package.TakeTailItems(ref id, ref count, out inc);
            return id == item ? count : 0;
        }

        // Drones, ships and warpers into the stations in range, up to each one's capacity (the game does this only when building).
        public static string FillStations()
        {
            if (!Here(out var player, out var factory, out var inRange)) return Lang.T(NotLanded);
            bool warp = GameMain.history.logisticShipWarpDrive;
            int drones = 0, ships = 0, warpers = 0, stations = 0;
            var pool = factory.transport.stationPool;
            for (int i = 1; i < factory.transport.stationCursor; i++)
            {
                StationComponent s = pool[i];
                if (s == null || s.id != i || s.isCollector || s.isVeinCollector || !inRange(factory.entityPool[s.entityId].pos)) continue;
                int before = drones + ships + warpers;
                int n = Take(player, 5001, s.workDroneDatas.Length - s.workDroneCount - s.idleDroneCount, out _);
                s.idleDroneCount += n;
                drones += n;
                if (s.isStellar)
                {
                    n = Take(player, 5002, s.workShipDatas.Length - s.workShipCount - s.idleShipCount, out _);
                    s.idleShipCount += n;
                    ships += n;
                    if (warp)
                    {
                        n = Take(player, 1210, s.warperMaxCount - s.warperCount, out _);
                        s.warperCount += n;
                        warpers += n;
                    }
                }
                if (drones + ships + warpers > before) stations++;
            }
            return stations == 0 ? Lang.T("Stations: nothing to fill (full, or none in your inventory).") : Lang.F("Stations: {0} filled with {1} drones, {2} vessels, {3} warpers.", stations, drones, ships, warpers);
        }

        // Fuel for the burning generators in range: tops up the fuel they hold, or gives an empty one the fuel you carry most of.
        public static string FuelGenerators()
        {
            if (!Here(out var player, out var factory, out var inRange)) return Lang.T(NotLanded);
            var pool = factory.powerSystem.genPool;
            int filled = 0, items = 0;
            for (int i = 1; i < factory.powerSystem.genCursor; i++)
            {
                ref PowerGeneratorComponent g = ref pool[i];
                if (g.id != i || g.fuelMask == 0 || g.gamma || g.wind || g.photovoltaic || !inRange(factory.entityPool[g.entityId].pos)) continue;
                int fuel = g.fuelId;
                if (fuel == 0)
                {
                    var fuels = ItemProto.fuelNeeds[g.fuelMask];
                    if (fuels == null) continue;
                    fuel = Most(fuels, player.package.GetItemCount);
                    if (fuel == 0) continue;
                }
                ItemProto proto = LDB.items.Select(fuel);
                int n = Take(player, fuel, (proto?.StackSize ?? 0) - g.fuelCount, out int inc);
                if (n == 0) continue;
                if (g.fuelId == 0) g.SetNewFuel(fuel, (short)n, (short)inc);
                else { g.fuelCount += (short)n; g.fuelInc += (short)inc; }
                filled++;
                items += n;
            }
            return filled == 0 ? Lang.T("Generators: nothing to fuel (full, or no fitting fuel carried).") : Lang.F("Generators: {0} fuelled with {1} items.", filled, items);
        }

        // The fuel among ids that count gives most of; 0 when none.
        public static int Most(int[] ids, Func<int, int> count)
        {
            int best = 0, most = 0;
            foreach (int id in ids)
            {
                int c = count(id);
                if (c > most) { most = c; best = id; }
            }
            return best;
        }

        // Rubble of destroyed buildings, and Dark Fog base remains the game allows to clear (as when a blueprint is placed on them).
        public static string ClearRuins()
        {
            if (!Here(out _, out var factory, out var inRange)) return Lang.T(NotLanded);
            var removableBases = factory.GetRemovableDFGBaseRuinIds();
            int cleared = 0;
            for (int i = 1; i < factory.ruinCursor; i++)
            {
                RuinData r = factory.ruinPool[i];
                if (r.id != i || !inRange(r.pos) || (r.modelIndex == 406 && !removableBases.Contains(i))) continue;
                factory.RemoveRuinWithComponet(i);
                cleared++;
            }
            return cleared == 0 ? Lang.T("Ruins: none in range.") : Lang.F("Ruins: {0} cleared.", cleared);
        }

        // Every storage in range opens slots slots to sorters (the red bar); 0 lifts the limit.
        public static string StorageLimit(string value)
        {
            if (!Here(out _, out var factory, out var inRange)) return Lang.T(NotLanded);
            if (!int.TryParse(value?.Trim(), out int slots) || slots < 0) return Lang.T("Storage limit: type the number of open slots first (0 removes the limit).");
            var pool = factory.factoryStorage.storagePool;
            int set = 0;
            for (int i = 1; i < factory.factoryStorage.storageCursor; i++)
            {
                StorageComponent s = pool[i];
                if (s == null || s.id != i || factory.entityPool[s.entityId].battleBaseId != 0 || !inRange(factory.entityPool[s.entityId].pos)) continue;
                s.SetBans(slots == 0 ? 0 : s.size - slots);
                set++;
            }
            return set == 0 ? Lang.T("Storage limit: no storage in range.") : slots == 0 ? Lang.F("Storage limit: removed on {0} storages.", set) : Lang.F("Storage limit: {0} storages open {1} slots each.", set, slots);
        }
    }
}
