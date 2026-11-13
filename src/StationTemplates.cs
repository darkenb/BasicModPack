using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BasicModPack
{
    // Named station settings, copied and applied with the game's own building parameters (the same data as its copy/paste),
    // in a floating window shown while a station window is open. It can be dragged by its title, remembers where it was left,
    // folds down to its title bar and scrolls when the list is long.
    public static class StationTemplates
    {
        public static ConfigEntry<bool> Enabled;
        static ConfigEntry<string> position;
        static ConfigEntry<bool> folded;

        public class Template { public string Name; public int ProtoId; public int[] Params; }

        // Storage 192 + slots 128 + settings 64; the game's array is 2048 long, the rest is unused for stations.
        public const int Stored = 384, GameLength = 2048, VisibleRows = 8;
        static string FilePath => Path.Combine(Paths.ConfigPath, "BUG_BasicModPack.templates.txt");
        static List<Template> templates;

        static GameObject win, body;
        static RectTransform root, viewport, content;
        static ScrollRect scroll;
        static InputField nameInput;
        static Text infoText;
        static RectTransform buttonBox;
        static GameObject plusBar;
        // Pad: the game's inset between frame and contents. Folding (from FoldFrom templates up) keeps one row of the list.
        const float Width = 300f, Pad = 24f, Row = 30f, ListTop = -118f, Bottom = 24f;
        public const int FoldFrom = 3;
        static readonly List<UIButton> applyButtons = new List<UIButton>(), deleteButtons = new List<UIButton>();
        static int shownStation = -1, shownVersion = -1, version;
        static bool dismissed, placed;

        public static void Bind(ConfigFile config)
        {
            position = config.Bind("Station templates", "WindowPosition", "", "Where the template window was last left (x,y); empty: next to the station window.");
            folded = config.Bind("Station templates", "Folded", false, "The template list shows one row (with 3 or more templates).");
        }

        public static string Format(Template t) =>
            Clean(t.Name) + "\t" + t.ProtoId + "\t" + string.Join(",", t.Params.Take(Stored).Select(v => v.ToString()).ToArray());

        public static Template Parse(string line)
        {
            var parts = line.Split('\t');
            if (parts.Length != 3 || parts[0].Length == 0 || !int.TryParse(parts[1], out int proto)) return null;
            var values = parts[2].Split(',');
            var p = new int[GameLength];
            for (int i = 0; i < values.Length && i < Stored; i++)
                if (!int.TryParse(values[i], out p[i])) return null;
            return new Template { Name = parts[0], ProtoId = proto, Params = p };
        }

        public static string Clean(string name) => new string((name ?? "").Where(c => c != '\t' && c != '\n' && c != '\r').ToArray()).Trim();

        public static string FormatPosition(Vector2 p) => p.x.ToString("0", CultureInfo.InvariantCulture) + "," + p.y.ToString("0", CultureInfo.InvariantCulture);

        public static bool ParsePosition(string s, out Vector2 p)
        {
            p = Vector2.zero;
            var parts = (s ?? "").Split(',');
            if (parts.Length != 2 || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) || float.IsNaN(x) || float.IsNaN(y)) return false;
            p = new Vector2(x, y);
            return true;
        }

        // Window height for a list of `count` templates: the list shows up to VisibleRows and scrolls past that.
        public static int Rows(int count, bool fold) => fold && count >= FoldFrom ? 1 : Math.Max(1, Math.Min(count, VisibleRows));

        public static float Height(int count, bool fold) => -ListTop + Rows(count, fold) * Row + Bottom;

        static List<Template> All()
        {
            if (templates != null) return templates;
            templates = new List<Template>();
            try
            {
                if (File.Exists(FilePath))
                    foreach (var line in File.ReadAllLines(FilePath))
                    {
                        var t = Parse(line);
                        if (t != null) templates.Add(t);
                    }
            }
            catch (Exception e) { Debug.LogWarning("BUG_BasicModPack: templates not read: " + e.Message); }
            return templates;
        }

        static void Write()
        {
            version++;
            try
            {
                string temp = FilePath + ".new";
                File.WriteAllLines(temp, templates.Select(Format).ToArray());
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
            }
            catch (Exception e) { Debug.LogWarning("BUG_BasicModPack: templates not saved: " + e.Message); }
        }

        static UIStationWindow Window => UIRoot.instance.uiGame.stationWindow;

        static StationComponent Station(out PlanetFactory factory)
        {
            var w = Window;
            factory = w.factory;
            if (factory == null || w.stationId <= 0 || w.stationId >= factory.transport.stationCursor) return null;
            var s = factory.transport.stationPool[w.stationId];
            return s != null && s.id == w.stationId && !s.isCollector && !s.isVeinCollector ? s : null;
        }

        // After a language change: the window is built again, in the new language, the next time it shows.
        public static void Rebuild()
        {
            if (win != null) UnityEngine.Object.Destroy(win);
            win = null;
            applyButtons.Clear();
            deleteButtons.Clear();
            shownStation = -1;
            placed = false;  // the new window goes to the saved position
        }

        public static void Tick()
        {
            var w = Window;
            PlanetFactory factory = null;
            var station = w != null && w.active && Enabled.Value ? Station(out factory) : null;
            if (w == null || !w.active) dismissed = false;  // the close button hides it until the station window closes
            if (station == null || dismissed)
            {
                if (win != null && win.activeSelf) win.SetActive(false);
                shownStation = -1;
                return;
            }
            if (win == null) Build(w);
            if (!win.activeSelf)
            {
                win.SetActive(true);
                win.transform.SetAsLastSibling();
                if (!placed) Place(w);
            }
            if (shownStation == station.id && shownVersion == version) return;
            if (shownStation != station.id)
            {
                string name = factory.ReadExtraInfoOnEntity(station.entityId);
                nameInput.text = string.IsNullOrEmpty(name) ? Lang.F("Template {0}", All().Count + 1) : name;
                scroll.verticalNormalizedPosition = 1f;
            }
            shownStation = station.id;
            shownVersion = version;
            int proto = factory.entityPool[station.entityId].protoId;
            var list = All().Where(t => t.ProtoId == proto).ToList();
            while (applyButtons.Count < list.Count) AddRow();
            for (int i = 0; i < applyButtons.Count; i++)
            {
                bool used = i < list.Count;
                applyButtons[i].gameObject.SetActive(used);
                deleteButtons[i].gameObject.SetActive(used);
                if (used) applyButtons[i].GetComponentInChildren<Text>().text = list[i].Name;
            }
            content.sizeDelta = new Vector2(0f, list.Count * Row);
            infoText.text = list.Count == 0 ? Lang.T("No template for this station type yet.") : list.Count == 1 ? Lang.T("1 template for this station type") : Lang.F("{0} templates for this station type", list.Count);
            Layout(list.Count);
        }

        static int ListCount() => applyButtons.Count(b => b.gameObject.activeSelf);

        static void Layout(int count)
        {
            bool fold = folded.Value;
            Ui.OneButton(buttonBox, count < FoldFrom);  // the fold button only where there is something to fold
            plusBar.SetActive(fold);
            Vector2 before = TopLeft();  // the title bar stays put when the height changes, whatever the window's pivot
            root.sizeDelta = new Vector2(Width, Height(count, fold));
            root.anchoredPosition += before - TopLeft();
            viewport.sizeDelta = new Vector2(Width - 2 * Pad, Rows(count, fold) * Row);
            barRect.sizeDelta = new Vector2(5f, viewport.sizeDelta.y);
        }

        static Vector2 TopLeft()
        {
            var corners = new Vector3[4];
            root.GetWorldCorners(corners);
            return ((RectTransform)root.parent).InverseTransformPoint(corners[1]);
        }

        // First opening: top-left corner next to the station window's top-right corner. Later: where it was left.
        static void Place(UIStationWindow w)
        {
            placed = true;
            if (ParsePosition(position.Value, out Vector2 saved))
            {
                root.anchoredPosition = saved;
                return;
            }
            var station = new Vector3[4];
            var mine = new Vector3[4];
            w.windowTrans.GetWorldCorners(station);
            root.GetWorldCorners(mine);
            root.position += station[2] - mine[1];
            root.localPosition = new Vector3(root.localPosition.x + 8f, root.localPosition.y, 0f);  // stay on the UI plane
        }

        static List<Template> ForOpenStation(out StationComponent station, out PlanetFactory factory)
        {
            station = Station(out factory);
            if (station == null) return new List<Template>();
            int proto = factory.entityPool[station.entityId].protoId;
            return All().Where(t => t.ProtoId == proto).ToList();
        }

        static void Save()
        {
            var station = Station(out PlanetFactory factory);
            string name = Clean(nameInput.text);
            if (station == null || name.Length == 0) return;
            var bp = default(BuildingParameters);
            if (!bp.CopyFromFactoryObject(station.entityId, factory, false) || bp.type != BuildingType.Station || bp.parameters == null) return;
            int proto = factory.entityPool[station.entityId].protoId;
            All().RemoveAll(t => t.ProtoId == proto && t.Name == name);  // same name: overwrite
            templates.Add(new Template { Name = name, ProtoId = proto, Params = bp.parameters });
            Write();
            UIRealtimeTip.Popup(Lang.F("Template saved: {0}", name));
        }

        static void Apply(int row)
        {
            var list = ForOpenStation(out StationComponent station, out PlanetFactory factory);
            if (row >= list.Count) return;
            var p = new int[GameLength];
            Array.Copy(list[row].Params, p, Math.Min(list[row].Params.Length, GameLength));
            // Same routine the game runs when a pasted building is finished: storage items, limits, slot directions and range settings.
            // Items no longer kept are handed back to the player by SetStationStorage, as when the player changes a slot.
            BuildingParameters.ApplyPrebuildParametersToEntity(station.entityId, 0, 0, p, null, factory);
            Window.stationId = Window.stationId;  // redraws the window
            UIRealtimeTip.Popup(Lang.F("Template applied: {0}", list[row].Name));
        }

        static void Delete(int row)
        {
            var list = ForOpenStation(out _, out _);
            if (row >= list.Count) return;
            templates.Remove(list[row]);
            Write();
        }

        static void Build(UIStationWindow w)
        {
            win = Ui.Window(Lang.T("Station templates"), Width, () => { dismissed = true; win.SetActive(false); }, keepMenu: true);
            root = (RectTransform)win.transform;
            root.sizeDelta = new Vector2(Width, Height(0, false));

            // Drag strip over the title bar, short of the fold and close buttons on its right.
            var handle = Ui.Card(root, Vector2.zero, new Vector2(Width - 96f, 40f), Color.clear);
            handle.raycastTarget = true;
            handle.gameObject.AddComponent<DragHandle>().Moved = () => position.Value = FormatPosition(root.anchoredPosition);

            // The game's menu button next to the X, as on the station window, drawn as a minus (a plus while folded) in the X's colour.
            buttonBox = (RectTransform)root.Find("panel-bg/btn-box");
            var menu = buttonBox.Find("menu-wnd-btn");
            var icon = menu.GetComponentsInChildren<Image>(true).First(i => i.name == "mbicon");
            var x = buttonBox.Find("close-wnd-btn/x-icon").GetComponent<Image>();
            icon.sprite = null;
            icon.material = x.material;
            icon.color = x.color;
            icon.rectTransform.sizeDelta = new Vector2(12f, 2f);
            icon.rectTransform.anchoredPosition = new Vector2(2f, 0f);
            var plus = UnityEngine.Object.Instantiate(icon, icon.transform.parent, false);
            plus.rectTransform.sizeDelta = new Vector2(2f, 12f);
            plusBar = plus.gameObject;
            // The game's popup-menu component wakes when the button first shows (3rd template) and takes the click; remove it.
            foreach (var g in menu.GetComponentsInChildren<UIGenericMenuButton>(true)) UnityEngine.Object.DestroyImmediate(g);
            foreach (var b in menu.GetComponentsInChildren<Button>(true))
            {
                b.onClick = new Button.ButtonClickedEvent();
                b.interactable = true;
            }
            // Clicks arrive through the game's UIButton (IPointerClickHandler), not Button.onClick.
            // One subscriber only: nested UIButtons would each toggle and cancel the fold out.
            menu.GetComponentInChildren<UIButton>(true).onClick += _ =>
            {
                folded.Value = !folded.Value;
                Layout(ListCount());
            };
            foreach (var ub in menu.GetComponentsInChildren<UIButton>(true))
            {
                ub.tips.tipTitle = Lang.T("Fold or unfold");
                ub.tips.tipText = Lang.T("Shows one template, or the whole list.");
            }

            body = new GameObject("body", typeof(RectTransform));
            var area = (RectTransform)body.transform;
            area.SetParent(root, false);
            Ui.Place(area, Vector2.zero, new Vector2(Width, 0f));

            // The station window's name field has no box of its own (it sits on the title), so it gets a well and an inset.
            var field = new Vector2(Width - 2 * Pad - 72f, 26f);
            nameInput = Ui.NewInput(area, new Vector2(Pad, -52f), field, 40);
            Ui.NewButton(area, new Vector2(Width - Pad - 66f, -52f), new Vector2(66f, 26f), Lang.T("Save"), Save);

            infoText = Ui.NewText(Ui.LabelTpl, area, new Vector2(Pad, -86f), new Vector2(Width - 2 * Pad, 22f), TextAnchor.MiddleLeft, "");
            infoText.fontSize = 12;

            // Scroll list: a masked viewport, the rows in a content box, mouse wheel or the thin bar on the right.
            var view = Ui.Card(area, new Vector2(Pad, ListTop), new Vector2(Width - 2 * Pad, Row), Color.clear);
            view.raycastTarget = true;  // the wheel scrolls anywhere over the list
            viewport = view.rectTransform;
            view.gameObject.AddComponent<RectMask2D>();
            var box = new GameObject("content", typeof(RectTransform));
            content = (RectTransform)box.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            scroll = view.gameObject.AddComponent<WheelScroll>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = Row;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = Scrollbar(viewport);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            applyButtons.Clear();
            deleteButtons.Clear();
            win.SetActive(false);
        }

        static void AddRow()
        {
            int row = applyButtons.Count;
            float y = -row * Row;
            const float Inner = Width - 2 * Pad;  // right edges line up with the Save button
            var apply = Ui.NewButton(content, new Vector2(0f, y), new Vector2(Inner - 32f, 26f), "", () => Apply(row));
            var delete = Ui.NewButton(content, new Vector2(Inner - 28f, y), new Vector2(28f, 26f), "×", () => Delete(row));
            delete.GetComponentInChildren<Text>().fontSize = 20;
            apply.tips.tipTitle = Lang.T("Apply template");
            apply.tips.tipText = Lang.T("Sets this station's items, limits, slot directions and range settings.");
            delete.tips.tipTitle = Lang.T("Delete template");
            applyButtons.Add(apply);
            deleteButtons.Add(delete);
        }

        static Scrollbar Scrollbar(RectTransform viewport)
        {
            var bar = Ui.Card(viewport.parent, Vector2.zero, Vector2.zero, Ui.Well);
            bar.raycastTarget = true;
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = viewport.anchoredPosition + new Vector2(viewport.sizeDelta.x + 4f, 0f);  // in the right margin, clear of the rows
            var handle = Ui.Card(rt, Vector2.zero, Vector2.zero, Ui.Soft);
            handle.raycastTarget = true;
            var h = handle.rectTransform;
            h.anchorMin = Vector2.zero;
            h.anchorMax = Vector2.one;
            h.offsetMin = h.offsetMax = Vector2.zero;
            var s = bar.gameObject.AddComponent<Scrollbar>();
            s.handleRect = h;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            barRect = rt;
            return s;
        }

        static RectTransform barRect;
    }

    // Drags its window in the parent's local space (the UI camera is perspective, so world moves would leave the UI plane).
    public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action Moved;
        RectTransform window, parent;
        Vector2 startPointer, startPosition;

        bool Local(PointerEventData e, out Vector2 p) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out p);

        public void OnBeginDrag(PointerEventData e)
        {
            window = (RectTransform)transform.parent;
            parent = (RectTransform)window.parent;
            startPosition = window.anchoredPosition;
            if (!Local(e, out startPointer)) window = null;
        }

        public void OnDrag(PointerEventData e)
        {
            if (window != null && Local(e, out Vector2 p)) window.anchoredPosition = startPosition + p - startPointer;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (window == null) return;
            // Keep the title bar reachable: clamp its top edge inside the parent.
            Rect area = parent.rect;
            var corners = new Vector3[4];
            window.GetWorldCorners(corners);
            Vector2 topLeft = parent.InverseTransformPoint(corners[1]);
            Vector2 shift = Vector2.zero;
            if (topLeft.x < area.xMin) shift.x = area.xMin - topLeft.x;
            if (topLeft.x + window.rect.width > area.xMax) shift.x = area.xMax - topLeft.x - window.rect.width;
            if (topLeft.y > area.yMax) shift.y = area.yMax - topLeft.y;
            if (topLeft.y - 40f < area.yMin) shift.y = area.yMin - topLeft.y + 40f;
            window.anchoredPosition += shift;
            Moved?.Invoke();
        }
    }
}
