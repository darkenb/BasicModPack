using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BasicModPack
{
    // Control panel (logistics list) rows of stations, distributors and advanced mining machines get a copy/paste button
    // left of their own two. First click copies that building; a click on another row of the same building type pastes
    // its settings, item slots and filters there; a click on the copied row drops the copy. Shift+click always copies.
    // Orbital collectors get no button. Settings go through the game's own paste (BuildingParameters), which already
    // refuses another building type; station item slots, which the game's paste leaves alone, are set here.
    public static class PanelCopy
    {
        const string ButtonName = "bug-copy-paste";

        static BuildingParameters clip;
        static int clipAstro, clipEntity, clipProto;
        static Slot[] clipSlots;
        static long clipGroups;
        static ERemoteRoutePriority clipPriority;
        static bool HasClip => clipProto != 0;

        struct Slot { public int item, max; public ELogisticStorage local, remote; }

        static Sprite copySprite, pasteSprite;

        static bool Enabled => GameTweaks.PanelCopyPaste == null || GameTweaks.PanelCopyPaste.Value;

        [HarmonyPostfix, HarmonyPatch(typeof(UIControlPanelObjectEntry), nameof(UIControlPanelObjectEntry.InitFromPool))]
        static void Init(UIControlPanelObjectEntry __instance)
        {
            var view = ViewButton(__instance);
            if (view == null) return;
            var mine = Mine(view);
            if (mine == null)
            {
                mine = Create(__instance, view);
                if (mine == null) return;
            }
            mine.gameObject.SetActive(Enabled && Target(__instance, out _, out _) && !IsCollector(__instance));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIControlPanelObjectEntry), "_OnUpdate")]
        static void Update(UIControlPanelObjectEntry __instance)
        {
            if (!__instance.inUse) return;
            var view = ViewButton(__instance);
            var mine = view == null ? null : Mine(view);
            if (mine == null || !mine.gameObject.activeSelf) return;
            if (!Target(__instance, out var f, out int entity)) return;
            bool source = HasClip && f.planetId == clipAstro && entity == clipEntity;
            bool fits = HasClip && !source && f.entityPool[entity].protoId == clipProto;
            var icon = Icon(mine);
            if (icon != null)
            {
                icon.sprite = HasClip && !source ? pasteSprite : copySprite;
                var c = icon.color;
                c.a = HasClip && !source && !fits ? 0.3f : 1f;  // another building type: paste is refused
                icon.color = c;
            }
            mine.highlighted = source;
        }

        static UIButton ViewButton(UIControlPanelObjectEntry e)
        {
            switch (e)
            {
                case UIControlPanelStationEntry s: return s.viewToTargetButton;
                case UIControlPanelDispenserEntry d: return d.viewToTargetButton;
                case UIControlPanelAdvancedMinerEntry m: return m.viewToTargetButton;
                default: return null;
            }
        }

        static UIButton FillButton(UIControlPanelObjectEntry e)
        {
            switch (e)
            {
                case UIControlPanelStationEntry s: return s.fillNecessaryButton;
                case UIControlPanelDispenserEntry d: return d.fillNecessaryButton;
                default: return null;
            }
        }

        static UIButton Mine(UIButton view)
        {
            var t = view.transform.parent?.Find(ButtonName);
            return t == null ? null : t.GetComponent<UIButton>();
        }

        static bool IsCollector(UIControlPanelObjectEntry e) => e is UIControlPanelStationEntry s && s.station != null && s.station.isCollector;

        static bool Target(UIControlPanelObjectEntry e, out PlanetFactory factory, out int entity)
        {
            factory = null;
            entity = e.target.objId;
            var planet = GameMain.data?.galaxy?.PlanetById(e.target.astroId);
            factory = planet?.factory;
            return factory != null && entity > 0 && entity < factory.entityCursor && factory.entityPool[entity].id == entity;
        }

        // Clone of the row's locate button, moved left of the row's leftmost own button by one button step.
        static UIButton Create(UIControlPanelObjectEntry entry, UIButton view)
        {
            try
            {
                if (copySprite == null) { copySprite = Draw(false); pasteSprite = Draw(true); }
                var go = UnityEngine.Object.Instantiate(view.gameObject, view.transform.parent, false);
                go.name = ButtonName;
                var btn = go.GetComponent<UIButton>();
                var vrt = (RectTransform)view.transform;
                var fill = FillButton(entry);
                float step = fill != null ? Mathf.Abs(vrt.anchoredPosition.x - ((RectTransform)fill.transform).anchoredPosition.x) : 0f;
                if (step < 1f) step = vrt.rect.width + 8f;
                float left = fill != null ? Mathf.Min(vrt.anchoredPosition.x, ((RectTransform)fill.transform).anchoredPosition.x) : vrt.anchoredPosition.x;
                ((RectTransform)go.transform).anchoredPosition = new Vector2(left - step, vrt.anchoredPosition.y);
                var icon = Icon(btn);
                if (icon != null) { icon.sprite = copySprite; icon.preserveAspect = true; }
                btn.tips.itemId = 0;
                btn.tips.tipTitle = "Copy / paste settings";
                btn.tips.tipText = "Click: copy this building's settings, item slots and filters.\nThen click the same button on another building of the same type to paste there.\nRight click (any row) or click the copied building again: cancel the copy. Shift+click: copy this one instead.";
                btn.button.interactable = true;
                btn.button.onClick.RemoveAllListeners();
                btn.button.onClick.AddListener(() => Click(entry));
                btn.onRightClick += _ => Drop();
                return btn;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Control panel copy button could not be made: " + e);
                return null;
            }
        }

        // The image that shows the locate sign: the one with a sprite that is not a frame, else the button's own.
        static Image Icon(UIButton b)
        {
            Image found = null;
            foreach (var img in b.GetComponentsInChildren<Image>(true))
                if (img.sprite != null && (found == null || img.transform != b.transform)) found = img;
            return found;
        }

        static void Click(UIControlPanelObjectEntry entry)
        {
            if (!entry.inUse || !Target(entry, out var f, out int entity)) return;
            bool source = HasClip && f.planetId == clipAstro && entity == clipEntity;
            if (!HasClip || VFInput.shift) { Copy(f, entity); return; }
            if (source) { Drop(); return; }
            if (f.entityPool[entity].protoId != clipProto || !clip.CanPasteToFactoryObject(entity, f))
            {
                UIRealtimeTip.Popup("Cannot paste here: copied from " + (LDB.items.Select(clipProto)?.name ?? "another building"), sound: false);
                return;
            }
            Paste(f, entity);
        }

        static void Drop()
        {
            if (!HasClip) return;
            clipProto = 0;
            UIRealtimeTip.Popup("Copy cancelled", sound: false);
        }

        static void Copy(PlanetFactory f, int entity)
        {
            var p = default(BuildingParameters);
            if (!p.CopyFromFactoryObject(entity, f, copyInserters: false, copyAllSettings: true))
            {
                UIRealtimeTip.Popup("Nothing to copy", sound: false);
                return;
            }
            clip = p;
            clipAstro = f.planetId;
            clipEntity = entity;
            clipProto = f.entityPool[entity].protoId;
            clipSlots = null;
            int sid = f.entityPool[entity].stationId;
            var s = sid > 0 ? f.transport.stationPool[sid] : null;
            if (s != null && s.id == sid && !s.isVeinCollector)
            {
                clipSlots = new Slot[s.storage.Length];
                for (int i = 0; i < s.storage.Length; i++)
                    clipSlots[i] = new Slot { item = s.storage[i].itemId, max = s.storage[i].max, local = s.storage[i].localLogic, remote = s.storage[i].remoteLogic };
                clipGroups = s.remoteGroupMask;
                clipPriority = s.routePriority;
            }
            VFAudio.Create("equip-0", GameMain.mainPlayer.transform, Vector3.zero, play: true, 0, -1, -1L);
            UIRealtimeTip.Popup("Copied: " + (LDB.items.Select(clipProto)?.name ?? "building"), sound: false);
        }

        static void Paste(PlanetFactory f, int entity)
        {
            int skipped = 0;
            int sid = f.entityPool[entity].stationId;
            var s = sid > 0 ? f.transport.stationPool[sid] : null;
            if (s != null && s.id == sid && clipSlots != null)
            {
                for (int i = 0; i < s.storage.Length && i < clipSlots.Length; i++)
                {
                    var want = clipSlots[i];
                    if (!SlotFits(s.storage[i].itemId, s.storage[i].count, want.item)) { skipped++; continue; }
                    f.transport.SetStationStorage(sid, i, want.item, want.max, want.local, want.remote, GameMain.mainPlayer);
                }
                if (s.isStellar)
                {
                    s.remoteGroupMask = clipGroups;
                    s.routePriority = clipPriority;
                }
            }
            // The game's own paste: settings, its sound, popup and the open windows' refresh. It reads the static clipboard.
            var keep = BuildingParameters.clipboard;
            BuildingParameters.clipboard = clip;
            try { f.PasteBuildingSetting(entity); }
            finally { BuildingParameters.clipboard = keep; }
            if (s != null && s.id == sid)
            {
                f.transport.RefreshStationTraffic(sid);
                if (s.isStellar) GameMain.data.galacticTransport.RefreshTraffic(s.gid);
            }
            if (skipped > 0) UIRealtimeTip.Popup(skipped + " slot(s) kept: they still hold another item", sound: false);
        }

        // A slot takes the copied item unless it still holds stock of a different one (changing it would move that
        // stock into the inventory, or onto the ground when the inventory is full).
        public static bool SlotFits(int item, int count, int want) => item == want || item == 0 || count <= 0;

        // 48x48 white line icons, tinted by the button like the game's own: two sheets (copy) or a clipboard (paste).
        static Sprite Draw(bool paste)
        {
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            void Box(int x0, int y0, int x1, int y1, bool fillIn)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        if (fillIn || x - x0 < 4 || x1 - x < 4 || y - y0 < 4 || y1 - y < 4) px[y * n + x] = new Color32(255, 255, 255, 255);
            }
            void Clear(int x0, int y0, int x1, int y1)
            {
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) px[y * n + x] = new Color32(0, 0, 0, 0);
            }
            if (!paste)
            {
                Box(16, 16, 43, 43, false);   // back sheet
                Clear(4, 4, 31, 31);
                Box(4, 4, 31, 31, false);     // front sheet
            }
            else
            {
                Box(8, 2, 39, 41, false);     // board
                Clear(16, 37, 31, 47);
                Box(16, 36, 31, 46, true);    // clip
                Box(15, 24, 32, 27, true);    // lines
                Box(15, 13, 28, 16, true);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
