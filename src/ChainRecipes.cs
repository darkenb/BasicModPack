using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BasicModPack
{
    // The replicator makes a missing input along the way (chain production) with one fixed recipe per item, the item's
    // `handcraft`. With this switch on, an item's handcraft becomes, among every unlocked handcraftable recipe that makes it
    // (the game's and any mod's, read live from the recipe table), the one that can make the most from what you carry,
    // counting inputs the chain makes on the way (Obtainable).
    // A score depends on what you carry only, never on other items' choices, so recipes that feed each other cannot flip
    // back and forth. The choice is made before the replicator plans a recipe and stays fixed while it plans, so the check
    // and the queued tasks always agree. Switched off, every item gets back the recipe it had.
    public static class ChainRecipes
    {
        public static ConfigEntry<bool> Enabled;

        // The item's handcraft before we set it, and what we set.
        class Swap { public RecipeProto Original, Set; public int OriginalCount; }
        static readonly Dictionary<int, Swap> swaps = new Dictionary<int, Swap>();
        static readonly Dictionary<int, int> chosenIn = new Dictionary<int, int>();  // item -> pass of its last choice
        static readonly Dictionary<int, long> have = new Dictionary<int, long>();   // what the replicator can draw on
        static readonly Dictionary<int, long> value = new Dictionary<int, long>();  // item -> Obtainable, this pass
        static readonly HashSet<int> path = new HashSet<int>();
        static Dictionary<int, List<RecipeProto>> makers;
        static RecipeProto[] indexed;
        static int depth, pass, passFrame = int.MinValue;
        static bool warned;

        public static void Apply(Harmony harmony) => harmony.PatchAll(typeof(ChainRecipes));

        // Every way the replicator plans a recipe (the queue, its counts, the tree's red marks, other mods' buttons) starts here.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.AddTask))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryAddTask))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.PredictTaskCount))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryTaskWithTestPackage))]
        static void Enter(MechaForge __instance, int recipeId, MethodBase __originalMethod)
        {
            if (depth++ > 0 || Enabled == null || !Enabled.Value) return;  // nested calls plan with the choices already made
            try { Choose(__instance, recipeId, __originalMethod.Name == nameof(MechaForge.AddTask)); }
            catch (Exception e)
            {
                if (!warned) Plugin.Log.LogError("Best chain recipe: " + e);
                warned = true;
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.AddTask))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryAddTask))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.PredictTaskCount))]
        [HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryTaskWithTestPackage))]
        static void Leave() => depth--;

        // Switched off: every item gets its own recipe back.
        public static void Tick()
        {
            if (Enabled != null && !Enabled.Value && swaps.Count > 0) RestoreAll();
        }

        // Once a frame, and on every order placed, what is carried is read again and each item is chosen again on first use.
        static void Choose(MechaForge forge, int recipeId, bool now)
        {
            var recipe = LDB.recipes.Select(recipeId);
            if (forge?.player?.package == null || recipe?.Items == null || GameMain.history == null) return;
            if (now || Time.frameCount != passFrame)
            {
                pass++;
                passFrame = Time.frameCount;
                Carried(forge);
                value.Clear();
            }
            var seen = new HashSet<int>();
            foreach (int item in recipe.Items) Visit(item, seen, 0);
        }

        // The item's recipe first, then the inputs of that recipe: only the recipes the chain will use are walked.
        static void Visit(int item, HashSet<int> seen, int level)
        {
            if (level > 20 || !seen.Add(item)) return;
            var proto = LDB.items.Select(item);
            if (proto == null) return;
            if (!chosenIn.TryGetValue(item, out int p) || p != pass)
            {
                chosenIn[item] = pass;
                var candidates = Candidates(item, proto);
                if (candidates.Count == 0) Restore(item, proto);
                else
                {
                    var original = swaps.TryGetValue(item, out var s) && proto.handcraft == s.Set ? s.Original : proto.handcraft;
                    var scores = new long[candidates.Count];
                    int current = -1, own = -1;
                    long most = 0;
                    path.Clear();
                    path.Add(item);
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        scores[i] = Score(candidates[i], item, id => Obtainable(id, Have, MakersOf, value, path, 1));
                        most = Math.Max(most, scores[i]);
                        if (candidates[i] == proto.handcraft) current = i;
                        if (candidates[i] == original) own = i;
                    }
                    int best = Best(scores, most == 0 && own >= 0 ? own : current);  // nothing carried for any: the game's own recipe
                    Set(item, proto, candidates[best]);
                }
            }
            var chosen = proto.handcraft;
            if (chosen?.Items != null) foreach (int input in chosen.Items) Visit(input, seen, level + 1);
        }

        static long Have(int id) => have.TryGetValue(id, out long n) ? n : 0;
        static List<RecipeProto> MakersOf(int id)
        {
            var proto = LDB.items.Select(id);
            return proto == null ? new List<RecipeProto>() : Candidates(id, proto);
        }

        // Items one recipe makes from what its inputs can be had in (whole batches x made per batch).
        public static long Score(RecipeProto r, int item, Func<int, long> obtainable) => Math.Min(Batches(r, obtainable) * Made(r, item), Cap);
        const long Cap = 1L << 40;

        // How many of an item the replicator can get: what is carried plus what its best recipe makes from its own inputs,
        // made along the way too (enriched bricks from stone dust that is crushed from carried stone). A pure function of
        // what is carried, so choices cannot feed back into it. Inputs two recipes share count for each, so it overstates;
        // it only ranks recipes. A recipe cycle or a chain deeper than 8 counts what is carried there; results are kept
        // per pass (memo), the first walk of a pass decides one that a cycle cut short.
        public static long Obtainable(int item, Func<int, long> carried, Func<int, List<RecipeProto>> recipes, Dictionary<int, long> memo, HashSet<int> path, int level)
        {
            if (memo.TryGetValue(item, out long known)) return known;
            long own = carried(item);
            if (level > 8 || !path.Add(item)) return own;
            long most = 0;
            foreach (var r in recipes(item)) most = Math.Max(most, Score(r, item, id => Obtainable(id, carried, recipes, memo, path, level + 1)));
            path.Remove(item);
            return memo[item] = Math.Min(own + most, Cap);
        }

        // Items the recipe makes from what is carried: whole batches (0 when an input is missing).
        public static long Batches(RecipeProto r, Func<int, long> carried)
        {
            long batches = long.MaxValue;
            for (int i = 0; i < r.Items.Length && i < r.ItemCounts.Length; i++)
                if (r.ItemCounts[i] > 0) batches = Math.Min(batches, carried(r.Items[i]) / r.ItemCounts[i]);
            return batches == long.MaxValue ? 0 : batches;
        }

        // What the replicator can draw on, as the game plans: the inventory, the queue's leftovers and (with
        // DeliverySlotsTweaks) the delivery slots. Items a queued order needs were taken out of the inventory when it was placed.
        static void Carried(MechaForge forge)
        {
            have.Clear();
            void Add(int id, long n) { if (id > 0 && n > 0) have[id] = (have.TryGetValue(id, out long c) ? c : 0) + n; }
            var package = forge.player.package;
            for (int i = 0; i < package.size; i++) Add(package.grids[i].itemId, package.grids[i].count);
            var delivery = forge.player.deliveryPackage;
            if (delivery?.grids != null) foreach (var g in delivery.grids) Add(g.itemId, g.count);
            if (forge.extraItems?.items != null) foreach (var kv in forge.extraItems.items) Add(kv.Key, kv.Value);
        }

        // The highest score; a tie keeps the given one (keep), else the first in the game's recipe order.
        public static int Best(long[] scores, int keep)
        {
            int best = 0;
            for (int i = 1; i < scores.Length; i++) if (scores[i] > scores[best]) best = i;
            return keep >= 0 && keep < scores.Length && scores[keep] == scores[best] ? keep : best;
        }

        // Unlocked handcraftable recipes that make the item and do not use it themselves (one that does cannot help when
        // the item is missing). Dark Fog drops go into basic items only: a recipe that turns them into a finished product
        // (Dyson parts, fuel rods, ammunition, units, buildings) is left out. Handcraft and unlocks are read live: mods
        // switch them while the game runs.
        static List<RecipeProto> Candidates(int item, ItemProto proto)
        {
            var all = LDB.recipes.dataArray;
            if (makers == null || indexed != all) Index(all);
            var list = new List<RecipeProto>();
            if (!makers.TryGetValue(item, out var mine)) return list;
            bool basic = Basic(item, proto.Type);
            foreach (var r in mine)
                if (r.Handcraft && GameMain.history.RecipeUnlocked(r.ID) && Array.IndexOf(r.Items ?? new int[0], item) < 0
                    && (basic || !UsesDarkFog(r))) list.Add(r);
            return list;
        }

        // Raw resources and materials (ingots, plates, liquids, graphite ...), as the game types its items. Dyson sphere
        // parts are finished products whatever their type (GenesisBook types frame material as a material).
        public static bool Basic(int id, EItemType type) => (type == EItemType.Resource || type == EItemType.Material) && Array.IndexOf(DysonParts, id) < 0;
        public static readonly int[] DysonParts = { 1501, 1125, 1502, 1503 };  // solar sail, frame material, sphere component, small carrier rocket

        // Dark Fog drops: the game's six (Dark Fog Matrix 5201 ... Energy Shard 5206) and any item typed Dark Fog.
        public static bool DarkFogDrop(int id, EItemType type) => id >= 5201 && id <= 5206 || type == EItemType.DarkFog;

        static bool UsesDarkFog(RecipeProto r)
        {
            foreach (int input in r.Items ?? new int[0])
            {
                var p = LDB.items.Select(input);
                if (DarkFogDrop(input, p != null ? p.Type : EItemType.Unknown)) return true;
            }
            return false;
        }

        static void Index(RecipeProto[] all)
        {
            makers = new Dictionary<int, List<RecipeProto>>();
            foreach (var r in all)
            {
                if (r?.Results == null) continue;
                foreach (int item in r.Results)
                {
                    if (!makers.TryGetValue(item, out var list)) makers[item] = list = new List<RecipeProto>();
                    if (!list.Contains(r)) list.Add(r);
                }
            }
            indexed = all;
        }

        // How many of the item one batch of the recipe makes.
        public static int Made(RecipeProto r, int item)
        {
            int made = 0;
            for (int i = 0; i < r.Results.Length && i < r.ResultCounts.Length; i++) if (r.Results[i] == item) made += r.ResultCounts[i];
            return made;
        }

        static void Set(int item, ItemProto proto, RecipeProto recipe)
        {
            if (!swaps.TryGetValue(item, out var s)) swaps[item] = s = new Swap { Original = proto.handcraft, OriginalCount = proto.handcraftProductCount };
            else if (proto.handcraft != s.Set) { s.Original = proto.handcraft; s.OriginalCount = proto.handcraftProductCount; }  // another mod set it since
            proto.handcraft = recipe;
            proto.handcraftProductCount = Made(recipe, item);
            s.Set = recipe;
        }

        static void Restore(int item, ItemProto proto)
        {
            if (!swaps.TryGetValue(item, out var s)) return;
            if (proto.handcraft == s.Set) { proto.handcraft = s.Original; proto.handcraftProductCount = s.OriginalCount; }
            swaps.Remove(item);
        }

        static void RestoreAll()
        {
            foreach (int item in new List<int>(swaps.Keys))
            {
                var proto = LDB.items.Select(item);
                if (proto != null) Restore(item, proto);
                else swaps.Remove(item);
            }
            chosenIn.Clear();
        }
    }
}
