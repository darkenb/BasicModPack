using UnityEngine;
using UnityEngine.UI;

namespace BasicModPack
{
    // Game window templates and colours, the same as DarkFogTuner and PowerPlantCalc.
    // A scroll list moved by the mouse wheel and its bar only; a click and drag on the list does nothing.
    public class WheelScroll : ScrollRect
    {
        public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e) { }
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData e) { }
        public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData e) { }
    }

    public static class Ui
    {
        // Panel teal, line cyan, selection orange. Glass order: rest, hover, pressed, text.
        public static readonly Color Ink = new Color32(232, 246, 252, 255), Soft = new Color32(176, 208, 220, 255), Accent = new Color32(253, 150, 94, 255), AccentInk = new Color32(28, 14, 6, 255), Scrim = new Color32(4, 22, 30, 168), Well = new Color32(0, 10, 16, 120);
        static readonly Color[] Glass = { new Color32(167, 233, 255, 56), new Color32(167, 233, 255, 92), new Color32(167, 233, 255, 32), Ink };

        static Transform Src => UIRoot.instance.uiGame.generatorWindow.transform;
        public static Text LabelTpl => Src.Find("power-network-desc/desc-group/label-1").GetComponent<Text>();
        public static Text ValueTpl => Src.Find("power-network-desc/desc-group/value-1").GetComponent<Text>();
        static GameObject ButtonTpl => Src.Find("ray-receiver/switch-button-1").gameObject;

        // The generator window with its contents and behaviour removed; one close button, or with keepMenu the game's
        // two-button box (menu-wnd-btn left of the close button) for the caller to use.
        public static GameObject Window(string title, float width, UnityEngine.Events.UnityAction onClose, bool keepMenu = false)
        {
            var win = Object.Instantiate(Src.gameObject, Src.parent, false);
            win.name = title;
            Object.DestroyImmediate(win.GetComponent<UIPowerGeneratorWindow>());
            foreach (Transform c in win.transform) if (c.name != "panel-bg") Object.Destroy(c.gameObject);
            if (!keepMenu) OneButton((RectTransform)win.transform.Find("panel-bg/btn-box"), true);
            StripLocalizers(win);
            Backdrop(win.transform.Find("panel-bg"));
            var close = win.transform.Find("panel-bg/btn-box/close-wnd-btn").GetComponent<Button>();
            close.onClick = new Button.ButtonClickedEvent();
            close.onClick.AddListener(onClose);
            var t = win.transform.Find("panel-bg/title-text").GetComponent<Text>();
            t.text = title;
            t.rectTransform.sizeDelta = new Vector2(width - 80f, t.rectTransform.sizeDelta.y);
            win.AddComponent<UIScrollZone>();  // the game gives the wheel to the camera unless the pointer is over a scroll zone
            var root = (RectTransform)win.transform;
            root.anchoredPosition = new Vector2(-300f, 300f);
            root.sizeDelta = new Vector2(width, 200f);
            return win;
        }

        // The game's box holds two 32-wide buttons in 64; with the close button alone it shrinks to 48, which keeps the
        // frame's slanted left end clear of the hover area. The right edge and the X stay where the game has them.
        public static void OneButton(RectTransform box, bool one)
        {
            var menu = box.Find("menu-wnd-btn");
            if (menu != null) menu.gameObject.SetActive(!one);
            float right = box.anchoredPosition.x + box.sizeDelta.x / 2f;  // pivot is the centre
            float width = one ? 48f : 64f;
            box.sizeDelta = new Vector2(width, box.sizeDelta.y);
            box.anchoredPosition = new Vector2(right - width / 2f, box.anchoredPosition.y);
            ((RectTransform)box.Find("close-wnd-btn")).anchoredPosition = new Vector2(one ? 8f : 16f, 0f);
        }

        // A text field in a well: the station window's name field, which has no box of its own, restyled.
        public static InputField NewInput(Transform parent, Vector2 pos, Vector2 size, int limit)
        {
            var well = Card(parent, pos, size, Well);
            well.gameObject.AddComponent<Outline>().effectColor = new Color32(167, 233, 255, 60);
            var input = Object.Instantiate(UIRoot.instance.uiGame.stationWindow.nameInput, parent, false);
            StripLocalizers(input.gameObject);
            // The name field brings the game's signal-icon picker (a button inside the field) and its rich-text overlay; plain text here.
            foreach (var ui in input.GetComponentsInChildren<UIInputField>(true))
            {
                if (ui.iconSelectBtn != null) Object.DestroyImmediate(ui.iconSelectBtn.gameObject);
                if (ui.overText != null && ui.overText != input.textComponent) Object.DestroyImmediate(ui.overText.gameObject);
                Object.DestroyImmediate(ui);
            }
            // The name field's own plate and its hover highlight (UIButton colour transitions); the well is the box.
            foreach (var b in input.GetComponentsInChildren<UIButton>(true)) Object.DestroyImmediate(b);
            input.transition = Selectable.Transition.None;
            foreach (var img in input.GetComponentsInChildren<Image>(true)) img.color = Color.clear;
            // The station window writes the open station's name into its field's placeholder; the copy kept it and showed it.
            if (input.placeholder is Text hint) hint.text = "";
            input.onEndEdit = new InputField.EndEditEvent();
            input.onValueChanged = new InputField.OnChangeEvent();
            input.characterLimit = limit;
            Place((RectTransform)input.transform, pos + new Vector2(8f, 0f), new Vector2(size.x - 16f, size.y));
            var typed = input.textComponent;
            typed.material = null;  // the title's glow material
            typed.color = Ink;
            typed.fontSize = 14;
            typed.alignment = TextAnchor.MiddleLeft;
            typed.rectTransform.anchorMin = Vector2.zero;
            typed.rectTransform.anchorMax = Vector2.one;
            typed.rectTransform.offsetMin = typed.rectTransform.offsetMax = Vector2.zero;
            input.selectionColor = new Color32(253, 150, 94, 110);
            return input;
        }

        // A masked list that scrolls with the wheel or a thin bar in the right margin. Fill scroll.content (top-left placed),
        // set its height, then call SizeScrollArea with the visible height.
        public static ScrollRect ScrollArea(Transform parent, Vector2 pos, float width, float step)
        {
            var view = Card(parent, pos, new Vector2(width, 0f), Color.clear);
            view.raycastTarget = true;  // the wheel scrolls anywhere over the list
            view.gameObject.AddComponent<RectMask2D>();
            var content = (RectTransform)new GameObject("content", typeof(RectTransform)).transform;
            content.SetParent(view.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = content.sizeDelta = Vector2.zero;
            var scroll = view.gameObject.AddComponent<WheelScroll>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = step;
            scroll.viewport = view.rectTransform;
            scroll.content = content;

            var bar = Card(parent, pos + new Vector2(width + 8f, 0f), new Vector2(5f, 0f), Well);
            bar.raycastTarget = true;
            var handle = Card(bar.transform, Vector2.zero, Vector2.zero, Soft);
            handle.raycastTarget = true;
            var h = handle.rectTransform;
            h.anchorMin = Vector2.zero;
            h.anchorMax = Vector2.one;
            h.offsetMin = h.offsetMax = Vector2.zero;
            var s = bar.gameObject.AddComponent<Scrollbar>();
            s.handleRect = h;
            s.targetGraphic = handle;
            s.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = s;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        public static void SizeScrollArea(ScrollRect scroll, float height)
        {
            var view = scroll.viewport;
            view.sizeDelta = new Vector2(view.sizeDelta.x, height);
            var bar = (RectTransform)scroll.verticalScrollbar.transform;
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, height);
            scroll.verticalNormalizedPosition = 1f;
        }

        // Height the text takes at its width, in layout units. The generator breaks the lines right, but the game's font
        // draws each line about 1.75 times its size apart (measured in game), more than the generator's own height.
        public const float LinePitch = 1.75f;

        public static float TextHeight(Text t, float width)
        {
            // Lines break at the game's real UI scale (glyph widths differ from scale 1, so a nearly full line may wrap only
            // on screen); the window is still inactive here, so the scale comes from the game's own canvas. 6 units of slack.
            var canvas = UIRoot.instance.uiGame.GetComponentInParent<Canvas>();
            var settings = t.GetGenerationSettings(new Vector2(width - 6f, 0f));
            settings.scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            settings.generateOutOfBounds = true;
            var gen = new TextGenerator();
            gen.Populate(t.text, settings);
            return LinesHeight(Mathf.Max(1, gen.lineCount), t.fontSize);
        }

        public static float LinesHeight(int lines, int fontSize) => Mathf.Ceil(lines * fontSize * LinePitch);

        public static Text NewText(Text tpl, Transform parent, Vector2 pos, Vector2 size, TextAnchor align, string text)
        {
            var t = Object.Instantiate(tpl, parent, false);
            StripLocalizers(t.gameObject);
            Place(t.rectTransform, pos, size);
            t.alignment = align;
            t.text = text;
            t.material = null;  // the value template's material overdrives bright colours into a glow
            t.color = Soft;
            t.raycastTarget = false;
            return t;
        }

        public static UIButton NewButton(Transform parent, Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = Object.Instantiate(ButtonTpl, parent, false);
            StripLocalizers(go);
            go.SetActive(true);
            Place((RectTransform)go.transform, pos, size);
            var text = go.GetComponentInChildren<Text>();
            text.text = label;
            var b = go.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(onClick);
            var ub = go.GetComponent<UIButton>();
            ub.tips.tipTitle = "";
            ub.tips.tipText = "";
            foreach (var tr in ub.transitions)
            {
                if (tr.target == null) continue;
                bool isText = tr.target is Text;
                tr.normalColor = tr.disabledColor = isText ? Glass[3] : Glass[0];
                tr.mouseoverColor = isText ? Glass[3] : Glass[1];
                tr.pressedColor = isText ? Glass[3] : Glass[2];
                tr.highlightColorOverride = isText ? AccentInk : Accent;
                tr.alphaOnly = false;
                tr.mouseoverSize = tr.pressedSize = tr.highlightSizeMultiplier = 1f;
            }
            ub.highlighted = false;  // the cloned template is the receiver's selected mode
            b.transition = Selectable.Transition.None;
            var fill = go.GetComponent<Image>();
            fill.material = null;  // the template's widget material boosts alpha, so colours would not read as written
            fill.color = Glass[0];
            text.color = Glass[3];
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = rt.sizeDelta = Vector2.zero;
            return ub;
        }

        // Deepens the game's glass so text holds on bright ground; first child, so border, title and close button stay on top.
        public static void Backdrop(Transform panel)
        {
            var bg = panel.GetComponent<Image>();
            var scrim = new GameObject("scrim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(panel, false);
            scrim.transform.SetAsFirstSibling();
            scrim.rectTransform.anchorMin = Vector2.zero;
            scrim.rectTransform.anchorMax = Vector2.one;
            scrim.rectTransform.anchoredPosition = scrim.rectTransform.sizeDelta = Vector2.zero;
            scrim.sprite = bg.sprite;
            scrim.type = bg.type;
            scrim.color = Scrim;
            scrim.raycastTarget = false;
        }

        public static Image Card(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var image = new GameObject("card", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            Place(image.rectTransform, pos, size);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        // Cloned game texts would otherwise be reset to their original translation keys.
        public static void StripLocalizers(GameObject go)
        {
            foreach (var l in go.GetComponentsInChildren<Localizer>(true)) Object.DestroyImmediate(l);
        }
    }
}
