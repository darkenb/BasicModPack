using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]

namespace BasicModPack
{
    // One feature of the pack: a one-time action (Run, returns the line to show), a switch (Enabled), or both: then Apply
    // shows only while the switch is off. Value is an optional number field next to the action's button. Name and
    // Description are the English texts (also the config keys); the panel shows them through Lang.
    public class Feature
    {
        public string Name, Description;
        public ConfigEntry<bool> Enabled;
        public System.Func<string> Run;
        public ConfigEntry<string> Value;
        public string RowLabel;  // with Row: a second line of small buttons in the card
        public KeyValuePair<string, System.Func<string>>[] Row;
        public bool OneTime => Enabled == null;
    }

    [BepInPlugin(Guid, "BUG_BasicModPack", "1.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bug.dsp.basicmodpack";
        public static readonly List<Feature> Features = new List<Feature>();
        static ConfigEntry<KeyboardShortcut> hotkey;
        static ConfigEntry<Language> language;
        static ConfigEntry<string> panelPosition;
        static GameObject win;
        static readonly Dictionary<Feature, UIButton> switches = new Dictionary<Feature, UIButton>(), applies = new Dictionary<Feature, UIButton>();
        static readonly Dictionary<Language, UIButton> languageButtons = new Dictionary<Language, UIButton>();

        public static BepInEx.Logging.ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            hotkey = Config.Bind("General", "Hotkey", new KeyboardShortcut(KeyCode.B, KeyCode.LeftControl, KeyCode.LeftShift), "Opens and closes the mod pack panel.");
            language = Config.Bind("General", "Language", Language.English, "Language of the pack's texts; also chosen on the panel.");
            Lang.Current = language.Value;
            panelPosition = Config.Bind("General", "PanelPosition", "", "Where the panel was last left (x,y); empty: the default place.");

            Add("Upgrade in range", "Belts and sorters in construction range become the best tier you carry; the old ones go back to your inventory.", AutoUpgrade.Run);
            Add("Fill stations in range", "Logistics drones, vessels and space warpers from your inventory into the stations in range, up to their capacity.", RangeActions.FillStations);
            Add("Fuel generators in range", "Tops up the fuel of burning generators in range; an empty one gets the fitting fuel you carry most of.", RangeActions.FuelGenerators);
            Add("Clear ruins in range", "Removes the rubble of destroyed buildings, and Dark Fog base remains the game lets you clear. Ruins hold no items.", RangeActions.ClearRuins);
            var limit = Add("Storage limit in range", "Sets the red limit bar of every storage in range to the number of open slots typed here; 0 removes it.", null);
            limit.Value = Config.Bind("Features", "Storage limit slots", "", "Open slots that \"Storage limit in range\" sets; 0 removes the limit.");
            limit.Run = () => RangeActions.StorageLimit(limit.Value.Value);

            var pickup = Switch("Pick up drops in range", "Items on the ground in construction range fly to you. ON: by itself, as the item filter allows; OFF: with Apply.", LootPickup.Run, false);
            LootPickup.Enabled = pickup.Enabled;
            pickup.RowLabel = "Item filter:";
            pickup.Row = new[]
            {
                new KeyValuePair<string, System.Func<string>>("Pick all", () => LootPickup.SetFilter(LootPickup.FilterMode.All)),
                new KeyValuePair<string, System.Func<string>>("Pick none", () => LootPickup.SetFilter(LootPickup.FilterMode.None)),
                new KeyValuePair<string, System.Func<string>>("Invert", () => LootPickup.SetFilter(LootPickup.FilterMode.Invert)),
                new KeyValuePair<string, System.Func<string>>("Random", () => LootPickup.SetFilter(LootPickup.FilterMode.Random)),
            };
            StationTemplates.Bind(Config);
            StationTemplates.Enabled = Switch("Station templates", "Save a station's slots, limits and range settings under a name, then apply them to any station of the same type from its window.").Enabled;
            PowerAlert.Enabled = Switch("Power warning", "A message and a sound when the planet you are on gets less than 90% of the power it needs.").Enabled;
            StationAlert.Enabled = Switch("Station stock warning", "Names the interstellar station that has run out of vessels, or of warpers while it has vessels.").Enabled;
            BeltCounter.Enabled = Switch("Build counter", "Beside the cursor while building: how many you carry, and how many a dragged belt line takes.").Enabled;
            ChainRecipes.Enabled = Switch("Best chain recipe", "When the replicator makes a missing input along the way, it uses the recipe that can make the most of it from your inventory, among every recipe that makes it, other mods' included.").Enabled;
            ChainRecipes.Apply(new HarmonyLib.Harmony(Guid));
        }

        // A one-time action: Run only.
        Feature Add(string name, string description, System.Func<string> run)
        {
            var f = new Feature { Name = name, Description = description, Run = run };
            Features.Add(f);
            return f;
        }

        // A switch; with run also an Apply button for while it is off.
        Feature Switch(string name, string description, System.Func<string> run = null, bool on = true)
        {
            var f = Add(name, description, run);
            f.Enabled = Config.Bind("Features", name, on, description);
            return f;
        }

        static bool InGame => GameMain.isRunning && !DSPGame.IsMenuDemo && GameMain.mainPlayer != null;

        void Update()
        {
            ChainRecipes.Tick();  // also in the menu: switched off, the recipes go back at once
            if (!InGame || GameMain.isPaused || !UIRoot.instance.uiGame.active)
            {
                if (win != null) win.SetActive(false);
                BeltCounter.Hide();
                return;
            }
            if (hotkey.Value.IsDown() && !VFInput.inputing)
            {
                if (win == null) Build();
                if (win.activeSelf) KeepPosition();
                win.SetActive(!win.activeSelf);
                if (win.activeSelf) { win.transform.SetAsLastSibling(); Show(); }
            }
            StationTemplates.Tick();
            PowerAlert.Tick();
            StationAlert.Tick();
            BeltCounter.Tick();
            LootPickup.Tick();
        }

        static void Show()
        {
            foreach (var kv in switches)
            {
                bool on = kv.Key.Enabled.Value;
                kv.Value.highlighted = on;
                kv.Value.GetComponentInChildren<Text>().text = Lang.T(on ? "ON" : "OFF");
                if (applies.TryGetValue(kv.Key, out var apply)) apply.gameObject.SetActive(!on);  // switched on, it runs by itself
            }
            foreach (var kv in languageButtons) kv.Value.highlighted = kv.Key == Lang.Current;
        }

        static bool Applicable(Feature f) => f.Run != null && (f.Enabled == null || !f.Enabled.Value) && (f.Value == null || f.Value.Value.Trim().Length > 0);

        // Every action that has its Apply button showing (and its field, if any, filled in); the results in one message.
        static void ApplyAll() => UIRealtimeTip.Popup(string.Join("\n", Features.Where(Applicable).Select(f => f.Run()).ToArray()));

        static void SetLanguage(Language l)
        {
            if (l == Lang.Current) return;
            language.Value = Lang.Current = l;
            KeepPosition();
            Object.Destroy(win);
            Build();
            win.SetActive(true);
            win.transform.SetAsLastSibling();
            Show();
            StationTemplates.Rebuild();
        }

        // The game's own window drag can move the panel too: remember wherever it is when it closes or is rebuilt.
        static void KeepPosition()
        {
            if (win != null) panelPosition.Value = StationTemplates.FormatPosition(((RectTransform)win.transform).anchoredPosition);
        }

        static void Build()
        {
            // Pad: the game's inset between frame and contents, as in the station templates window. The list scrolls past MaxView.
            const float Width = 580f, Pad = 24f, Inner = Width - 2 * Pad, CardH = 60f, Gap = 6f, Button = 88f, Field = 64f, Inset = 10f, MaxView = 560f;
            win = Ui.Window("BUG Basic Mod Pack", Width, () => { KeepPosition(); win.SetActive(false); });
            var root = (RectTransform)win.transform;
            switches.Clear();
            applies.Clear();
            languageButtons.Clear();

            // Drag by the title bar; the panel opens where it was left, also after a language change.
            var handle = Ui.Card(root, Vector2.zero, new Vector2(Width - 80f, 36f), Color.clear);
            handle.raycastTarget = true;
            handle.gameObject.AddComponent<DragHandle>().Moved = () => panelPosition.Value = StationTemplates.FormatPosition(root.anchoredPosition);
            if (StationTemplates.ParsePosition(panelPosition.Value, out var saved)) root.anchoredPosition = saved;

            // Language: three small buttons on their own row under the title, inside the frame.
            var names = new[] { "EN", "中文", "TR" };
            foreach (Language l in new[] { Language.English, Language.Turkish, Language.Chinese })
            {
                int slot = l == Language.English ? 0 : l == Language.Turkish ? 1 : 2;
                var lang = l;
                languageButtons[l] = Ui.NewButton(root, new Vector2(Width / 2f - 75f + slot * 50f, -40f), new Vector2(46f, 22f), names[(int)l], () => SetLanguage(lang));
            }

            const float ListTop = -70f;
            var scroll = Ui.ScrollArea(root, new Vector2(Pad, ListTop), Inner, 40f);
            var content = scroll.content;
            float y = 0f;
            // Heading buttons line up with the card buttons below them.
            void Heading(string title, string buttonLabel, UnityEngine.Events.UnityAction onClick)
            {
                var t = Ui.NewText(Ui.ValueTpl, content, new Vector2(2f, y), new Vector2(Inner - Button, 26f), TextAnchor.MiddleLeft, Lang.T(title));
                t.color = Ui.Accent;
                t.fontSize = 15;
                if (onClick != null) Ui.NewButton(content, new Vector2(Inner - Inset - Button, y - 1f), new Vector2(Button, 24f), Lang.T(buttonLabel), onClick);
                y -= 30f;
            }

            void Card(Feature f)
            {
                bool both = f.Run != null && f.Enabled != null;
                float right = Button + 10f + (f.Value != null ? Field + 8f : 0f) + (both ? Button + 8f : 0f);
                float textW = Inner - Inset - right - 10f;
                var card = Ui.Card(content, new Vector2(0f, y), new Vector2(Inner, CardH), Ui.Well);
                var name = Ui.NewText(Ui.ValueTpl, content, new Vector2(Inset, y - 6f), new Vector2(textW, 22f), TextAnchor.MiddleLeft, Lang.T(f.Name));
                name.color = Ui.Ink;
                name.fontSize = 16;
                var desc = Ui.NewText(Ui.LabelTpl, content, new Vector2(Inset, y - 27f), new Vector2(textW, 0f), TextAnchor.UpperLeft, Lang.T(f.Description));
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.fontSize = 12;
                float descH = Ui.TextHeight(desc, textW);  // longer translations take more lines; the card grows with them
                desc.rectTransform.sizeDelta = new Vector2(textW, descH);
                float top = Mathf.Max(CardH, 27f + descH + 4f);
                var at = new Vector2(Inner - Inset - Button, y - (top - 28f) / 2f);
                if (f.Enabled != null)
                {
                    switches[f] = Ui.NewButton(content, at, new Vector2(Button, 28f), "", () =>
                    {
                        f.Enabled.Value = !f.Enabled.Value;
                        Show();
                    });
                    at.x -= Button + 8f;
                }
                if (f.Value != null)
                {
                    var input = Ui.NewInput(content, at - new Vector2(Field + 8f, 0f), new Vector2(Field, 28f), 4);
                    input.contentType = InputField.ContentType.IntegerNumber;
                    input.text = f.Value.Value;
                    input.onEndEdit.AddListener(v => f.Value.Value = v.Trim());
                }
                if (f.Run != null) applies[f] = Ui.NewButton(content, at, new Vector2(Button, 28f), Lang.T("Apply"), () => UIRealtimeTip.Popup(f.Run()));
                float h = top;
                if (f.Row != null)
                {
                    float rowY = y - top;
                    var label = Ui.NewText(Ui.LabelTpl, content, new Vector2(Inset, rowY), new Vector2(120f, 24f), TextAnchor.MiddleLeft, Lang.T(f.RowLabel));
                    label.fontSize = 12;
                    float w = (Inner - 2 * Inset - 120f - (f.Row.Length - 1) * 6f) / f.Row.Length;
                    for (int i = 0; i < f.Row.Length; i++)
                    {
                        var run = f.Row[i].Value;
                        Ui.NewButton(content, new Vector2(Inset + 120f + i * (w + 6f), rowY), new Vector2(w, 24f), Lang.T(f.Row[i].Key), () => UIRealtimeTip.Popup(run()));
                    }
                    h += 32f;
                }
                card.rectTransform.sizeDelta = new Vector2(Inner, h);
                y -= h + Gap;
            }

            Heading("One-time actions", "Apply all", ApplyAll);
            foreach (var f in Features.Where(f => f.OneTime)) Card(f);
            y -= 8f;
            Heading("Always on", null, null);
            foreach (var f in Features.Where(f => !f.OneTime)) Card(f);

            float listH = -y - Gap;
            content.sizeDelta = new Vector2(0f, listH);
            float viewH = Mathf.Min(listH, MaxView);
            Ui.SizeScrollArea(scroll, viewH);

            float noteY = ListTop - viewH - 10f;
            var note = Ui.NewText(Ui.LabelTpl, root, new Vector2(Pad, noteY), new Vector2(Inner, 0f), TextAnchor.UpperLeft,
                Lang.F("{0} opens this panel. Actions work within your construction range; switches and the language are kept in the config file.", hotkey.Value));
            note.fontSize = 12;
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            float noteH = Ui.TextHeight(note, Inner);
            note.rectTransform.sizeDelta = new Vector2(Inner, noteH);
            root.sizeDelta = new Vector2(Width, -noteY + noteH + Pad);
            win.SetActive(false);
        }
    }
}
