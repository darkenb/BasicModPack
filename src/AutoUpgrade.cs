using System;
using UnityEngine;

namespace BasicModPack
{
    // One click upgrades every belt and sorter in the mecha's construction range through the game's own upgrade action:
    // the new item comes from the inventory, the old one goes back to it.
    public static class AutoUpgrade
    {
        public static string Run()
        {
            Player player = GameMain.mainPlayer;
            PlanetFactory factory = player?.factory;
            var build = player?.controller?.actionBuild;
            if (factory == null || GameMain.localPlanet?.factory != factory || player.package == null || build == null)
                return Lang.T(RangeActions.NotLanded);
            GameHistoryData history = GameMain.history;
            float range = player.mecha.buildArea;
            float range2 = range * range;
            Vector3 at = player.position;
            EntityData[] pool = factory.entityPool;
            int upgraded = 0;
            bool full = false;
            for (int id = 1; id < factory.entityCursor; id++)
            {
                if (pool[id].id != id || (pool[id].beltId == 0 && pool[id].inserterId == 0)) continue;
                if ((pool[id].pos - at).sqrMagnitude > range2) continue;
                ItemProto item = LDB.items.Select(pool[id].protoId);
                if (item == null || !item.canUpgrade) continue;
                int grade = BestGrade(item.Grade, item.Upgrades, i => history.ItemUnlocked(i) ? player.package.GetItemCount(i) : 0);
                if (grade == item.Grade) continue;
                if (player.package.isFull) { full = true; break; }  // the old item needs a free slot, or the game throws it out as trash
                if (build.DoUpgradeObject(id, grade, 0, out _)) upgraded++;
            }
            return upgraded == 0 && !full ? Lang.T("Upgrade: nothing to upgrade in range.") : Lang.F(full ? "Upgrade: {0} upgraded; stopped, inventory full." : "Upgrade: {0} upgraded.", upgraded);
        }

        // Highest grade above the current one that the inventory holds; the current grade when none.
        public static int BestGrade(int grade, int[] upgrades, Func<int, int> count)
        {
            for (int g = upgrades.Length; g > grade; g--)
                if (upgrades[g - 1] != 0 && count(upgrades[g - 1]) > 0) return g;
            return grade;
        }
    }
}
